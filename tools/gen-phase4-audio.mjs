// Renders the Phase 4 sound of docs/plan.md: the three judgment cues (P4.1), written as mono
// 16-bit PCM WAVs under client/Assets/_Project/Audio/, named by the asset conventions so the
// importer gives them Decompress On Load and the rebuild catalogues them as `cue-perfect`,
// `cue-good` and `cue-miss`.
//
// Run: node tools/gen-phase4-audio.mjs
//
// A cue has to sit on the beat, so every one of them is struck in its first millisecond: the
// envelope opens over 0.3 ms and each partial starts at phase zero, which puts the loudest
// sample inside the first quarter cycle of its lowest tone. And a player must know which grade
// sounded without listening for it, so the three differ in register, in interval and in texture,
// not in volume: Perfect is a clean two-octave bell, Good a warm fifth an octave lower, and Miss
// a low pair a semitone apart whose beating and noise edge make it the only unpitched one.

import { join, resolve, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { RATE, attackMs, noise, normalise, writeWav } from './audio-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const AUDIO = join(ROOT, 'client', 'Assets', '_Project', 'Audio')

/** The plan's ceiling is 150 ms; every cue is rendered shorter than that. */
const CEILING_MS = 150

/** How long the envelope takes to open, in seconds: short enough that the strike is the first sample that counts. */
const ATTACK = 0.0003

/**
 * Renders one cue: a sum of partials under a shared exponential decay, faded to nothing by the
 * last sample so the file never clicks at its end.
 */
function cue({ ms, decay, peak, partials, grit = 0, gritDecay = 0.004, seed = 1 }) {
  const samples = Math.round((RATE * ms) / 1000)
  const data = new Float64Array(samples)
  const rand = noise(seed)

  for (let i = 0; i < samples; i++) {
    const t = i / RATE
    let value = 0
    for (const [hz, amplitude] of partials) {
      value += Math.sin(2 * Math.PI * hz * t) * amplitude
    }

    if (grit > 0) {
      value += rand() * grit * Math.exp(-t / gritDecay)
    }

    const open = Math.min(1, t / ATTACK)
    const tail = 1 - i / samples
    data[i] = value * open * Math.exp(-t / decay) * tail
  }

  return { ms, ...normalise(data, peak) }
}

// Perfect: A6 with its octave and a touch of the one above, decaying fast. Bright, short, clean.
const perfect = cue({
  ms: 110,
  decay: 0.035,
  peak: 0.9,
  partials: [
    [1760, 1],
    [3520, 0.38],
    [7040, 0.12],
  ],
})

// Good: A5 under a perfect fifth, decaying slower. An octave below Perfect and consonant, so it
// reads as the softer grade rather than a quieter one.
const good = cue({
  ms: 120,
  decay: 0.055,
  peak: 0.82,
  partials: [
    [880, 1],
    [1320, 0.34],
    [1760, 0.1],
  ],
})

// Miss: A3 against the semitone above it. The two beat against each other roughly thirteen times
// a second, and a noise edge blunts the pitch further, so nothing about it names a note.
const miss = cue({
  ms: 145,
  decay: 0.075,
  peak: 0.78,
  partials: [
    [220, 1],
    [233.08, 0.95],
    [110, 0.4],
  ],
  grit: 0.5,
  gritDecay: 0.008,
  seed: 0x515,
})

const cues = [
  ['sfx_cue_perfect.wav', perfect, 'bell, A6 + octaves'],
  ['sfx_cue_good.wav', good, 'warm fifth, A5'],
  ['sfx_cue_miss.wav', miss, 'low semitone pair with a noise edge'],
]

for (const [name, rendered, note] of cues) {
  if (rendered.ms >= CEILING_MS) {
    throw new Error(`${name} is ${rendered.ms} ms, at or over the ${CEILING_MS} ms ceiling`)
  }

  const attack = attackMs(rendered)
  if (attack >= 5) {
    throw new Error(`${name} reaches half its peak after ${attack.toFixed(2)} ms, past the 5 ms the plan allows`)
  }

  writeWav(join(AUDIO, name), rendered)
  console.log(`${name.padEnd(24)} ${String(rendered.ms).padStart(3)} ms  struck at ${attack.toFixed(2)} ms  ${note}`)
}

console.log(`\n${cues.length} WAVs written under Audio/, mono 16-bit PCM at ${RATE} Hz`)
