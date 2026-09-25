// The sound kit the generators under tools/ share: a deterministic noise source and a mono
// 16-bit PCM WAV writer. Every sound is rendered as float samples, normalised to a stated peak
// and written at 48 kHz, so the same script gives the same bytes on every machine.

import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname } from 'node:path'

export const RATE = 48000

/** A deterministic noise source, so the file is the same on every machine that renders it. */
export function noise(seed) {
  let state = seed >>> 0
  return () => {
    state = (state * 1664525 + 1013904223) >>> 0
    return state / 2147483648 - 1
  }
}

/** Normalises float samples to a peak amplitude and reports the gain that got them there. */
export function normalise(data, peak) {
  let loudest = 0
  for (const v of data) loudest = Math.max(loudest, Math.abs(v))
  return { data, gain: loudest > 0 ? peak / loudest : 0, samples: data.length }
}

export function writeWav(path, { data, gain, samples }) {
  const bytes = Buffer.alloc(44 + samples * 2)
  bytes.write('RIFF', 0, 'ascii')
  bytes.writeUInt32LE(36 + samples * 2, 4)
  bytes.write('WAVE', 8, 'ascii')
  bytes.write('fmt ', 12, 'ascii')
  bytes.writeUInt32LE(16, 16) // PCM header length
  bytes.writeUInt16LE(1, 20) // PCM
  bytes.writeUInt16LE(1, 22) // mono
  bytes.writeUInt32LE(RATE, 24)
  bytes.writeUInt32LE(RATE * 2, 28) // bytes per second
  bytes.writeUInt16LE(2, 32) // bytes per frame
  bytes.writeUInt16LE(16, 34) // bits per sample
  bytes.write('data', 36, 'ascii')
  bytes.writeUInt32LE(samples * 2, 40)
  for (let i = 0; i < samples; i++) {
    const v = Math.max(-1, Math.min(1, data[i] * gain))
    bytes.writeInt16LE(Math.round(v * 32767), 44 + i * 2)
  }

  mkdirSync(dirname(path), { recursive: true })
  writeFileSync(path, bytes)
}

/** Normalises a stereo pair to a peak amplitude over both channels, with one gain for both. */
export function normaliseStereo(left, right, peak) {
  let loudest = 0
  for (let i = 0; i < left.length; i++) loudest = Math.max(loudest, Math.abs(left[i]), Math.abs(right[i]))
  return { left, right, gain: loudest > 0 ? peak / loudest : 0, samples: left.length }
}

/** Writes a stereo 16-bit PCM WAV at RATE, the music format (docs/plan.md, Asset conventions). */
export function writeStereoWav(path, { left, right, gain, samples }) {
  const bytes = Buffer.alloc(44 + samples * 4)
  bytes.write('RIFF', 0, 'ascii')
  bytes.writeUInt32LE(36 + samples * 4, 4)
  bytes.write('WAVE', 8, 'ascii')
  bytes.write('fmt ', 12, 'ascii')
  bytes.writeUInt32LE(16, 16) // PCM header length
  bytes.writeUInt16LE(1, 20) // PCM
  bytes.writeUInt16LE(2, 22) // stereo
  bytes.writeUInt32LE(RATE, 24)
  bytes.writeUInt32LE(RATE * 4, 28) // bytes per second
  bytes.writeUInt16LE(4, 32) // bytes per frame
  bytes.writeUInt16LE(16, 34) // bits per sample
  bytes.write('data', 36, 'ascii')
  bytes.writeUInt32LE(samples * 4, 40)
  for (let i = 0; i < samples; i++) {
    const l = Math.max(-1, Math.min(1, left[i] * gain))
    const r = Math.max(-1, Math.min(1, right[i] * gain))
    bytes.writeInt16LE(Math.round(l * 32767), 44 + i * 4)
    bytes.writeInt16LE(Math.round(r * 32767), 46 + i * 4)
  }

  mkdirSync(dirname(path), { recursive: true })
  writeFileSync(path, bytes)
}

/**
 * How soon a rendered sound lands, in milliseconds: the first sample at or above half the peak.
 * The judgment cues are measured on this (P4.1), so the generator states what the suite checks.
 */
export function attackMs({ data }) {
  let peak = 0
  for (const v of data) peak = Math.max(peak, Math.abs(v))
  for (let i = 0; i < data.length; i++) {
    if (Math.abs(data[i]) >= peak / 2) return (i * 1000) / RATE
  }

  return Infinity
}
