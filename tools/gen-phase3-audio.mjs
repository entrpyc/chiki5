// Renders the Phase 3 sound of docs/plan.md: the disabled-press thud (P3.4), written as a mono
// 16-bit PCM WAV under client/Assets/_Project/Audio/, named by the asset conventions so the
// importer gives it Decompress On Load and the rebuild catalogues it as `press-disabled`.
//
// Run: node tools/gen-phase3-audio.mjs
//
// A refused press must never be mistaken for a judgment (PRD 3.3.5.3, 3.3.8.1). The three
// judgment cues are bright, sustained, pitched tones — 1760, 880 and 220 Hz. This is their
// opposite: a body under 100 Hz that drops a fifth in 40 ms, a noise transient the low-pass
// smothers, and nothing left after 120 ms. It carries no pitch to hear and no ring to place.

import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { RATE, noise, normalise, writeWav } from './audio-lib.mjs'

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const AUDIO = join(ROOT, 'client', 'Assets', '_Project', 'Audio')

const MS = 120 // the plan's ceiling is 150 ms
const PEAK = 0.55 // muted: it is a refusal, not an event

function render() {
  const samples = (RATE * MS) / 1000
  const data = new Float64Array(samples)
  const rand = noise(0x5c1c1)
  let phase = 0
  let low = 0

  for (let i = 0; i < samples; i++) {
    const t = i / RATE

    // The body: 96 Hz falling to 64 Hz over the first 40 ms, so it lands and sinks.
    const hz = 64 + 32 * Math.exp(-t / 0.04)
    phase += (2 * Math.PI * hz) / RATE
    const body = Math.sin(phase) * Math.exp(-t / 0.035)

    // The transient: 6 ms of noise, one-pole low-passed until it is a knock rather than a click.
    const hit = rand() * Math.exp(-t / 0.006)
    low += (hit - low) * 0.06

    const envelope = Math.min(1, t / 0.0015) * Math.pow(1 - i / samples, 1.5)
    data[i] = (body * 0.85 + low * 2.2) * envelope
  }

  return normalise(data, PEAK)
}

const name = 'sfx_press_disabled.wav'
writeWav(join(AUDIO, name), render())
console.log(`${name.padEnd(28)} ${MS} ms  mono 16-bit PCM at ${RATE} Hz  muted thud, unlike any judgment cue`)
