// Renders the Phase 11 music of docs/plan.md: one stand-in track per fixture enemy (P11.1),
// written as stereo 16-bit PCM WAVs at 48 kHz under client/Assets/_Project/Audio/ and named
// `mus_w<world>_<subject>.wav`, so the rebuild catalogues each under the id of the track sidecar
// that names the same World and subject (`mus_w1_ren.wav` -> `track-fixture-ren`).
//
// Run: node tools/gen-phase11-audio.mjs
//
// Every track is rendered *to* its enemy's shipped sidecar and chart under data/, which stay
// exactly as they are. The beat map is computed here the way `Chiki.Sim.BeatMap` computes it:
// integer microseconds per quarter beat, piecewise over the tempo map, the lap length rounded to
// whole milliseconds with halves up. The file is exactly the sidecar's offset plus one lap of
// that beat map long, in samples, so the chart and the music wrap together (P11.2).
//
// The loop has no seam because the whole track is rendered into a circular buffer one lap long:
// a note or a drum tail that runs past the loop point is written onto the start of the buffer,
// where it sounds over the next lap's first beats exactly as it would have in an endless piece.
//
// Each track is a bed of drum, bass and lead. Drum and bass follow the tempo map beat by beat.
// The lead plays on the chart's own action positions, so the music telegraphs the fight: left
// attacks sound left of centre and low in the chord, right attacks right and high, defends on
// the root, buffs an octave up. Every enemy has its own key, progression, groove and timbre.

import { readFileSync } from 'node:fs'
import { join, resolve, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { RATE, noise, normaliseStereo, writeStereoWav } from './audio-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const DATA = join(ROOT, 'data')
const AUDIO = join(ROOT, 'client', 'Assets', '_Project', 'Audio')

const readJson = (path) => JSON.parse(readFileSync(path, 'utf8'))

// ---------------------------------------------------------------------------------------------
// Beat map, mirrored from Chiki.Sim.BeatMap (PRD 4.14, 3.3.1.9)
// ---------------------------------------------------------------------------------------------

const QB_PER_BEAT = 4

/** Chiki.Sim.Beats.BeatMicroseconds: whole microseconds per beat, truncated. */
const beatMicros = (bpm) => Math.floor(60_000_000 / bpm)

/** Chiki.Sim.Fixed.Round with scale 1000: nearest, halves up. */
const roundThousandths = (value) => Math.floor((value + 500) / 1000)

function beatMap(sidecar) {
  const lengthQb = sidecar.lengthBeats * QB_PER_BEAT
  const changes = (sidecar.tempo.changes ?? []).map((c) => ({ qb: c.beat * QB_PER_BEAT, bpm: c.bpm }))

  const microsWithinLap = (positionQb) => {
    let micros = 0
    let segmentStart = 0
    let bpm = sidecar.tempo.bpm
    for (const change of changes) {
      if (change.qb >= positionQb) break
      micros += Math.floor(((change.qb - segmentStart) * beatMicros(bpm)) / QB_PER_BEAT)
      segmentStart = change.qb
      bpm = change.bpm
    }

    return micros + Math.floor(((positionQb - segmentStart) * beatMicros(bpm)) / QB_PER_BEAT)
  }

  const bpmAt = (positionQb) => {
    let bpm = sidecar.tempo.bpm
    for (const change of changes) {
      if (change.qb > positionQb % lengthQb) break
      bpm = change.bpm
    }

    return bpm
  }

  const lengthMs = roundThousandths(microsWithinLap(lengthQb))
  const samples = ((sidecar.offsetMs + lengthMs) * RATE) / 1000
  if (!Number.isInteger(samples)) throw new Error(`${sidecar.id}: a lap is not a whole number of samples`)

  return {
    lengthQb,
    lengthMs,
    samples,
    bpmAt,
    /** The sample a position within the lap starts on, beat 0 sitting offsetMs into the file. */
    sampleAt: (positionQb) => Math.round(((sidecar.offsetMs * 1000 + microsWithinLap(positionQb)) * RATE) / 1_000_000),
    /** Seconds per beat at a position. */
    beatSeconds: (positionQb) => beatMicros(bpmAt(positionQb)) / 1_000_000,
  }
}

// ---------------------------------------------------------------------------------------------
// The circular mix bus
// ---------------------------------------------------------------------------------------------

function bus(samples) {
  const left = new Float64Array(samples)
  const right = new Float64Array(samples)

  /**
   * Adds a voice starting at a sample, `seconds` long, panned from -1 (left) to 1 (right) with
   * constant power. Samples past the end wrap onto the start, which is what makes the lap loop.
   */
  const add = (start, seconds, pan, voice) => {
    const length = Math.round(seconds * RATE)
    const angle = ((pan + 1) * Math.PI) / 4
    const gl = Math.cos(angle)
    const gr = Math.sin(angle)
    for (let i = 0; i < length; i++) {
      const v = voice(i / RATE, i)
      const at = (((start + i) % samples) + samples) % samples
      left[at] += v * gl
      right[at] += v * gr
    }
  }

  return { left, right, add }
}

// ---------------------------------------------------------------------------------------------
// Instruments: each returns a voice function of time since onset, in seconds
// ---------------------------------------------------------------------------------------------

const TAU = 2 * Math.PI
const hz = (midi) => 440 * 2 ** ((midi - 69) / 12)

/** A pitch-swept sine thump: the sweep falls from `top` to `floor` Hz over a few tens of ms. */
function kick({ top = 130, floor = 46, sweep = 0.03, decay = 0.2, level = 1 }) {
  let phase = 0
  return (t) => {
    const f = floor + (top - floor) * Math.exp(-t / sweep)
    phase += (TAU * f) / RATE
    return Math.sin(phase) * Math.exp(-t / decay) * Math.min(1, t / 0.0005) * level
  }
}

/** A noise burst over a short tonal body. */
function snare(rand, { tone = 190, decay = 0.07, level = 0.6 }) {
  return (t) => {
    const body = Math.sin(TAU * tone * t) * Math.exp(-t / 0.045) * 0.55
    const hiss = rand() * Math.exp(-t / decay) * 0.8
    return (body + hiss) * Math.min(1, t / 0.0005) * level
  }
}

/** A differentiated noise tick: noise with its lows taken out, very short. */
function hat(rand, { decay = 0.018, level = 0.25 }) {
  let previous = 0
  return (t) => {
    const n = rand()
    const high = n - previous
    previous = n
    return high * 0.5 * Math.exp(-t / decay) * level
  }
}

/**
 * A pitched voice built from harmonics: `harmonics` is a list of [multiple, amplitude]. The
 * brightness closes with `darken` (higher harmonics fade faster), the note holds for `hold`
 * seconds and releases over `release`. `vibrato` is depth in semitones at 5.5 Hz.
 */
function tone(frequency, { harmonics, hold, release = 0.04, attack = 0.004, darken = 0, decay = Infinity, vibrato = 0, level = 1 }) {
  const phases = harmonics.map(() => 0)
  return (t) => {
    const bend = vibrato > 0 ? 2 ** ((vibrato * Math.sin(TAU * 5.5 * t) * Math.min(1, t / 0.15)) / 12) : 1
    let v = 0
    for (let k = 0; k < harmonics.length; k++) {
      const [multiple, amplitude] = harmonics[k]
      phases[k] += (TAU * frequency * multiple * bend) / RATE
      v += Math.sin(phases[k]) * amplitude * Math.exp(-t * darken * (multiple - 1))
    }

    const open = Math.min(1, t / attack)
    const shut = t < hold ? 1 : Math.max(0, 1 - (t - hold) / release)
    return v * open * shut * Math.exp(-t / decay) * level
  }
}

const saw = (n) => Array.from({ length: n }, (_, i) => [i + 1, 1 / (i + 1)])
const square = (n) => Array.from({ length: n }, (_, i) => [2 * i + 1, 1 / (2 * i + 1)])
const triangle = (n) => Array.from({ length: n }, (_, i) => [2 * i + 1, (i % 2 === 0 ? 1 : -1) / (2 * i + 1) ** 2])
const organ = [
  [1, 1],
  [2, 0.5],
  [3, 0.35],
  [4, 0.2],
  [6, 0.1],
]

// ---------------------------------------------------------------------------------------------
// The five tracks
// ---------------------------------------------------------------------------------------------

// A chord is its root as a MIDI note and its tones as semitones above the root. A drum pattern
// lists quarter-beat steps within one 4-beat bar (0..15); a bass pattern lists [step, length in
// steps, semitones above the chord root].

const MINOR = [0, 3, 7, 10]
const MAJOR = [0, 4, 7, 11]
const SUS = [0, 5, 7, 10]
const DIM = [0, 3, 6, 9]

const STYLES = {
  // Ren, Normal Tank, slow: a patient D minor walk, kick on one and three, a soft square lead.
  ren: {
    chords: [
      [50, MINOR],
      [46, MAJOR],
      [53, MAJOR],
      [48, SUS],
    ],
    kick: [0, 8],
    snare: [4, 12],
    hat: [0, 2, 4, 6, 8, 10, 12, 14],
    bass: [
      [0, 3, 0],
      [4, 2, 7],
      [8, 3, 0],
      [12, 2, 12],
    ],
    bassVoice: { harmonics: saw(6), darken: 2.5, level: 0.5 },
    lead: { harmonics: square(5), darken: 1.2, decay: 0.6, level: 0.3 },
    kickVoice: {},
    snareVoice: { tone: 180, decay: 0.08 },
    hatVoice: { level: 0.18 },
    seed: 0x4e11,
  },
  // Kess, Normal Aggressor, fast: E phrygian four on the floor, sixteenth hats, a bright saw lead.
  kess: {
    chords: [
      [52, MINOR],
      [53, MAJOR],
      [52, MINOR],
      [50, MINOR],
    ],
    kick: [0, 4, 8, 12],
    snare: [4, 12],
    hat: [...Array(16).keys()],
    bass: [
      [0, 1, 0],
      [2, 1, 0],
      [3, 1, 12],
      [6, 1, 0],
      [8, 1, 0],
      [10, 1, 7],
      [11, 1, 12],
      [14, 1, 0],
    ],
    bassVoice: { harmonics: saw(8), darken: 4, level: 0.5 },
    lead: { harmonics: saw(7), darken: 2, decay: 0.35, level: 0.26 },
    kickVoice: { top: 150, decay: 0.16 },
    snareVoice: { tone: 210, decay: 0.06 },
    hatVoice: { level: 0.14, decay: 0.012 },
    seed: 0x4e55,
  },
  // Vey, Normal Mentalist, fast: F sharp dorian, a syncopated kick, a wavering triangle lead.
  vey: {
    chords: [
      [54, MINOR],
      [59, MAJOR],
      [54, MINOR],
      [57, MAJOR],
    ],
    kick: [0, 6, 10],
    snare: [4, 12],
    hat: [2, 6, 10, 14],
    bass: [
      [0, 2, 0],
      [3, 1, 7],
      [6, 2, 10],
      [10, 2, 0],
      [13, 1, 3],
    ],
    bassVoice: { harmonics: triangle(5), darken: 1, level: 0.65 },
    lead: { harmonics: triangle(6), vibrato: 0.35, decay: 0.8, level: 0.34 },
    kickVoice: { top: 120, decay: 0.18 },
    snareVoice: { tone: 240, decay: 0.05, level: 0.45 },
    hatVoice: { level: 0.22, decay: 0.03 },
    seed: 0x4e77,
  },
  // Orm, Elite Tank, slow: C minor half time, one kick and one snare a bar, a low organ lead.
  orm: {
    chords: [
      [48, MINOR],
      [44, MAJOR],
      [43, MINOR],
      [43, MAJOR],
    ],
    kick: [0, 10],
    snare: [8],
    hat: [0, 4, 8, 12],
    bass: [
      [0, 6, 0],
      [8, 4, 0],
      [12, 4, -5],
    ],
    bassVoice: { harmonics: saw(6), darken: 1.5, level: 0.6 },
    lead: { harmonics: organ, decay: 1.2, level: 0.3 },
    kickVoice: { top: 110, floor: 40, decay: 0.3 },
    snareVoice: { tone: 150, decay: 0.12, level: 0.7 },
    hatVoice: { level: 0.2, decay: 0.04 },
    seed: 0x4e99,
  },
  // Malk, Boss Aggressor, fast: A harmonic minor over a driving sixteenth bass, a detuned square lead.
  malk: {
    chords: [
      [45, MINOR],
      [41, MAJOR],
      [44, DIM],
      [40, MAJOR],
    ],
    kick: [0, 4, 8, 11, 12],
    snare: [4, 12, 15],
    hat: [...Array(16).keys()],
    bass: [...Array(16).keys()].map((s) => [s, 1, s % 4 === 3 ? 12 : 0]),
    bassVoice: { harmonics: square(6), darken: 5, level: 0.42 },
    lead: { harmonics: square(6), darken: 1.5, decay: 0.4, level: 0.22, detune: 0.12 },
    kickVoice: { top: 160, decay: 0.15 },
    snareVoice: { tone: 200, decay: 0.07, level: 0.65 },
    hatVoice: { level: 0.16, decay: 0.01 },
    seed: 0x4ebb,
  },
}

const ACTION_POSITION = {
  'attack-left': { pan: -0.55, tone: 1 },
  'attack-right': { pan: 0.55, tone: 2 },
  defend: { pan: 0, tone: 0 },
  buff: { pan: 0, tone: 0, octave: 12 },
}

function render(sidecar, chart, style) {
  const map = beatMap(sidecar)
  const { left, right, add } = bus(map.samples)
  const rand = noise(style.seed)
  const bars = Math.ceil(sidecar.lengthBeats / 4)
  const chordAt = (qb) => style.chords[Math.floor(qb / 16) % style.chords.length]

  for (let bar = 0; bar < bars; bar++) {
    const barQb = bar * 16
    const [root] = chordAt(barQb)
    const within = (step) => barQb + step < map.lengthQb

    for (const step of style.kick.filter(within)) {
      add(map.sampleAt(barQb + step), 0.5, 0, kick(style.kickVoice))
    }

    for (const step of style.snare.filter(within)) {
      add(map.sampleAt(barQb + step), 0.35, 0.05, snare(rand, style.snareVoice))
    }

    for (const step of style.hat.filter(within)) {
      const accent = step % 4 === 0 ? 1 : 0.7
      add(map.sampleAt(barQb + step), 0.12, 0.3, hat(rand, { ...style.hatVoice, level: (style.hatVoice.level ?? 0.25) * accent }))
    }

    for (const [step, steps, semitones] of style.bass) {
      if (!within(step)) continue
      const qb = barQb + step
      const hold = (steps * map.beatSeconds(qb)) / QB_PER_BEAT - 0.02
      add(map.sampleAt(qb), hold + 0.05, 0, tone(hz(root - 12 + semitones), { ...style.bassVoice, hold }))
    }

  }

  // The lead: one note per charted action, on the action's position.
  const actions = chart.actions
    .map((a) => ({ kind: a.kind, qb: Math.round(a.position * QB_PER_BEAT) }))
    .sort((a, b) => a.qb - b.qb)
  actions.forEach((action, i) => {
    const next = i + 1 < actions.length ? actions[i + 1].qb : actions[0].qb + map.lengthQb
    const gapQb = Math.max(1, Math.min(QB_PER_BEAT, next - action.qb))
    const hold = (gapQb * map.beatSeconds(action.qb)) / QB_PER_BEAT * 0.85
    const [root, tones] = chordAt(action.qb)
    const shape = ACTION_POSITION[action.kind] ?? ACTION_POSITION.defend
    const midi = root + 12 + tones[shape.tone] + (shape.octave ?? 0)
    const start = map.sampleAt(action.qb)
    const length = hold + 0.25
    const lead = { ...style.lead, hold, release: 0.2 }
    if (lead.detune) {
      add(start, length, Math.max(-1, shape.pan - 0.3), tone(hz(midi - lead.detune), lead))
      add(start, length, Math.min(1, shape.pan + 0.3), tone(hz(midi + lead.detune), lead))
    } else {
      add(start, length, shape.pan, tone(hz(midi), lead))
    }
  })

  return { map, ...normaliseStereo(left, right, 0.8) }
}

// ---------------------------------------------------------------------------------------------

const fixtures = readJson(join(DATA, 'enemies', 'fixtures.json'))
const charts = new Map()
for (const enemy of fixtures.enemies) {
  charts.set(enemy.chart, readJson(join(DATA, 'charts', `${enemy.chart}.json`)))
}

const tracks = new Map()
for (const enemy of fixtures.enemies) {
  const sidecarName = enemy.track.replace(/^track-/, '') + '.json'
  tracks.set(enemy.track, readJson(join(DATA, 'tracks', sidecarName)))
}

let written = 0
for (const enemy of fixtures.enemies) {
  const subject = enemy.id.replace(/^enemy-/, '')
  const style = STYLES[subject]
  if (!style) throw new Error(`${enemy.id} has no style in this generator`)

  const sidecar = tracks.get(enemy.track)
  const chart = charts.get(enemy.chart)
  if (sidecar.id !== enemy.track) throw new Error(`${enemy.track}'s sidecar carries the id ${sidecar.id}`)
  if (chart.track !== enemy.track) throw new Error(`${enemy.chart} is charted on ${chart.track}, not ${enemy.track}`)

  const rendered = render(sidecar, chart, style)
  const name = `mus_w${sidecar.world}_${subject}.wav`
  writeStereoWav(join(AUDIO, name), rendered)
  written++

  const seconds = (rendered.samples / RATE).toFixed(3)
  console.log(
    `${name.padEnd(18)} ${String(rendered.samples).padStart(8)} samples  ${seconds} s  = ${sidecar.offsetMs} ms offset + ${rendered.map.lengthMs} ms (${sidecar.lengthBeats} beats)  ${chart.actions.length} lead notes`,
  )
}

console.log(`\n${written} WAVs written under Audio/, stereo 16-bit PCM at ${RATE} Hz`)
