#!/usr/bin/env python3
"""Generate the placeholder audio for Project PMD-Rogue.

Pure-python chiptune synthesis (no dependencies): short SFX blips and small
looping music tracks, written as mono 16-bit 22050 Hz WAVs into
PMDRoguelike/Content/Audio/. Deterministic — rerunning reproduces identical
files. Replace any file with real audio of the same name whenever you have it;
the content pipeline and AudioManager only care about the names.

Usage: python3 tools/generate_placeholder_audio.py
"""

import math
import os
import random
import struct
import wave

SR = 22050
OUT_ROOT = os.path.join(os.path.dirname(__file__), "..", "PMDRoguelike", "Content", "Audio")

rng = random.Random(20260710)


# ---------------------------------------------------------------- synthesis

def silence(dur):
    return [0.0] * int(SR * dur)


def osc(freq, dur, wave_kind="square", vol=0.5, sweep_to=None):
    """One enveloped oscillator note. sweep_to linearly glides the pitch."""
    n = int(SR * dur)
    out = []
    phase = 0.0
    for i in range(n):
        t = i / n
        f = freq + (sweep_to - freq) * t if sweep_to else freq
        phase += f / SR
        x = phase % 1.0
        if wave_kind == "square":
            s = 1.0 if x < 0.5 else -1.0
        elif wave_kind == "triangle":
            s = 4.0 * abs(x - 0.5) - 1.0
        elif wave_kind == "noise":
            s = rng.uniform(-1, 1)
        else:  # sine
            s = math.sin(2 * math.pi * x)
        # 5 ms attack, exponential-ish release
        env = min(1.0, i / (SR * 0.005)) * (1.0 - t) ** 1.5
        out.append(s * env * vol)
    return out


def mix(*layers):
    n = max(len(layer) for layer in layers)
    out = [0.0] * n
    for layer in layers:
        for i, s in enumerate(layer):
            out[i] += s
    return out


def concat(*parts):
    out = []
    for p in parts:
        out.extend(p)
    return out


def normalize(samples, peak=0.85):
    m = max(1e-9, max(abs(s) for s in samples))
    return [s / m * peak for s in samples]


def write_wav(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        frames = b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32767)) for s in samples)
        w.writeframes(frames)
    print(f"  {os.path.relpath(path, os.path.join(OUT_ROOT, '..'))}  ({len(samples) / SR:.2f}s)")


# ---------------------------------------------------------------- SFX

def gen_sfx():
    sfx = {
        "attack":     osc(500, 0.09, "noise", 0.5, sweep_to=200),
        "hit_normal": mix(osc(200, 0.12, "square", 0.5, sweep_to=120), osc(0, 0.05, "noise", 0.3)),
        "hit_super":  mix(osc(110, 0.25, "square", 0.6, sweep_to=60), osc(0, 0.12, "noise", 0.5)),
        "hit_weak":   osc(320, 0.06, "triangle", 0.35),
        "miss":       osc(600, 0.15, "sine", 0.3, sweep_to=200),
        "faint":      concat(osc(392, 0.09, "square", 0.4), osc(262, 0.09, "square", 0.4),
                             osc(196, 0.09, "square", 0.4), osc(131, 0.2, "square", 0.4)),
        "levelup":    concat(osc(523, 0.08, "square", 0.4), osc(659, 0.08, "square", 0.4),
                             osc(784, 0.08, "square", 0.4), osc(1047, 0.22, "square", 0.45)),
        "pickup":     concat(osc(660, 0.05, "square", 0.4), osc(990, 0.1, "square", 0.4)),
        "money":      concat(osc(988, 0.05, "sine", 0.45), osc(1319, 0.12, "sine", 0.45)),
        "stairs":     osc(200, 0.35, "sine", 0.4, sweep_to=820),
        "chest":      concat(osc(90, 0.15, "square", 0.35, sweep_to=140),
                             osc(660, 0.05, "square", 0.35), osc(990, 0.1, "square", 0.35)),
        "buy":        concat(osc(988, 0.05, "sine", 0.4), osc(988, 0.04, "sine", 0.0),
                             osc(1319, 0.05, "sine", 0.4), osc(1760, 0.1, "sine", 0.4)),
        "denied":     osc(110, 0.18, "square", 0.4),
        "status":     mix(osc(300, 0.25, "sine", 0.4, sweep_to=260), osc(330, 0.25, "sine", 0.3, sweep_to=290)),
        "menu":       osc(800, 0.045, "square", 0.3),
        "boss":       mix(osc(58, 0.55, "square", 0.55, sweep_to=45), osc(0, 0.4, "noise", 0.35)),
    }
    for name, samples in sfx.items():
        write_wav(os.path.join(OUT_ROOT, "Sfx", f"{name}.wav"), normalize(samples))


# ---------------------------------------------------------------- music

NOTE = {n: i for i, n in enumerate(["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"])}


def freq(name, octave):
    midi = 12 * (octave + 1) + NOTE[name]
    return 440.0 * 2 ** ((midi - 69) / 12)


def chord_tones(root, octave, minor):
    r = freq(root, octave)
    third = r * 2 ** ((3 if minor else 4) / 12)
    fifth = r * 2 ** (7 / 12)
    return [r, third, fifth, r * 2]


def track(chords, bpm, lead_wave="square", bass_wave="triangle", lead_vol=0.16, bass_vol=0.3,
          arp_pattern=(0, 1, 2, 3, 2, 1), bass_octave=2, lead_octave=4):
    """chords: list of (root, minor). One bar (4 beats) per chord, arp in 8ths."""
    beat = 60.0 / bpm
    eighth = beat / 2
    out = []
    for root, minor in chords:
        tones = chord_tones(root, lead_octave, minor)
        bass_f = freq(root, bass_octave)
        bar_lead = []
        bar_bass = []
        for step in range(8):  # eight 8th-notes per bar
            bar_lead.extend(osc(tones[arp_pattern[step % len(arp_pattern)]], eighth, lead_wave, lead_vol))
            bass_on = step % 2 == 0
            bar_bass.extend(osc(bass_f, eighth, bass_wave, bass_vol if bass_on else bass_vol * 0.4))
        out.extend(mix(bar_lead, bar_bass))
    return normalize(out, peak=0.7)


def gen_music():
    tracks = {
        # Bright and hopeful.
        "title": track([("C", False), ("A", True), ("F", False), ("G", False)] * 2, bpm=104),
        # Calm forest.
        "verdant": track([("C", False), ("G", False), ("A", True), ("F", False)] * 2, bpm=88,
                         lead_wave="sine", lead_vol=0.22, bass_vol=0.26),
        # Low and echoing.
        "caverns": track([("A", True), ("F", False), ("D", True), ("E", False)] * 2, bpm=80,
                         lead_wave="triangle", lead_vol=0.24, lead_octave=3, bass_octave=1),
        # Urgent climb.
        "spire": track([("E", True), ("C", False), ("D", False), ("B", True)] * 2, bpm=112,
                       arp_pattern=(0, 2, 1, 3, 0, 2)),
        # Aggressive.
        "boss": track([("D", True), ("A#", False), ("G", True), ("A", False)] * 2, bpm=140,
                      lead_vol=0.2, bass_vol=0.34, arp_pattern=(0, 3, 1, 3, 2, 3)),
    }
    for name, samples in tracks.items():
        write_wav(os.path.join(OUT_ROOT, "Music", f"{name}.wav"), samples)


if __name__ == "__main__":
    print("Generating placeholder audio...")
    gen_sfx()
    gen_music()
    print("Done.")
