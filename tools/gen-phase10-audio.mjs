// Renders the Phase 10 sound of docs/plan.md: the two calibration and metronome clicks (P10.4),
// written as mono 16-bit PCM WAVs under client/Assets/_Project/Audio/, named by the asset
// conventions so the rebuild catalogues them as `click-beat` and `click-accent`.
//
// Run: node tools/gen-phase10-audio.mjs
//
// A calibration tap is measured against the click's onset, so each click must land in its first
// millisecond and be over before the next beat can blur it: both are struck at full level within
// 0.2 ms and gone under 20 ms. The accent marks the bar (every fourth beat) by pitch and weight,
// not by length, so both clicks have the same onset: a woodblock-like knock, a bright partial
// over a short body, with a noise tick on the very first samples for the transient.

import { join, resolve, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { RATE, attackMs, noise, normalise, writeWav } from './audio-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const AUDIO = join(ROOT, 'client', 'Assets', '_Project', 'Audio')

/** The plan's ceiling is 20 ms for a click, and its onset must fall inside the first millisecond. */
const CEILING_MS = 20
const ONSET_MS = 1

function click({ ms, peak, partials, decay, tick, seed }) {
  const samples = Math.round((RATE * ms) / 1000)
  const data = new Float64Array(samples)
  const rand = noise(seed)
  for (let i = 0; i < samples; i++) {
    const t = i / RATE
    let value = 0
    for (const [hz, amplitude] of partials) value += Math.sin(2 * Math.PI * hz * t) * amplitude
    value += rand() * tick * Math.exp(-t / 0.0006)
    const open = Math.min(1, t / 0.0002)
    const tail = 1 - i / samples
    data[i] = value * open * Math.exp(-t / decay) * tail
  }

  return { ms, ...normalise(data, peak) }
}

// Beat: a 1.9 kHz knock with a body a fifth below.
const beat = click({ ms: 14, peak: 0.7, partials: [[1900, 1], [1267, 0.45]], decay: 0.0035, tick: 0.6, seed: 0x10b })

// Accent: a fourth higher and louder, with more body, so the bar is heard without counting.
const accent = click({ ms: 18, peak: 0.92, partials: [[2530, 1], [1265, 0.6], [5060, 0.2]], decay: 0.0045, tick: 0.8, seed: 0xacc })

const clicks = [
  ['sfx_click_beat.wav', beat, 'knock, 1.9 kHz'],
  ['sfx_click_accent.wav', accent, 'accent, 2.5 kHz, heavier'],
]

for (const [name, rendered, note] of clicks) {
  if (rendered.ms >= CEILING_MS) {
    throw new Error(`${name} is ${rendered.ms} ms, at or over the ${CEILING_MS} ms ceiling`)
  }

  const attack = attackMs(rendered)
  if (attack >= ONSET_MS) {
    throw new Error(`${name} reaches half its peak after ${attack.toFixed(2)} ms, past the ${ONSET_MS} ms the plan allows`)
  }

  writeWav(join(AUDIO, name), rendered)
  console.log(`${name.padEnd(22)} ${String(rendered.ms).padStart(3)} ms  struck at ${attack.toFixed(2)} ms  ${note}`)
}

console.log(`\n${clicks.length} WAVs written under Audio/, mono 16-bit PCM at ${RATE} Hz`)
