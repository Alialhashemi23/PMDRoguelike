#!/usr/bin/env python3
"""Fetch real PMD-style walk sprites from PMDCollab's SpriteCollab and convert
them into this game's sheet layout (2 frames x 4 rows [down,left,right,up],
32px frames), overwriting the generated placeholders in
PMDRoguelike/Content/Sprites/Species/.

Run this on a machine with normal internet access, then rebuild the game:

    python3 tools/fetch_pmd_sprites.py            # all species
    python3 tools/fetch_pmd_sprites.py pikachu    # just some
    python3 tools/fetch_pmd_sprites.py --source /path/to/SpriteCollab  # local clone

SpriteCollab (https://github.com/PMDCollab/SpriteCollab) is community-made art:
free for non-commercial fan projects WITH ARTIST CREDIT. This script writes
CREDITS-PMDCollab.txt next to the sprites; check each sprite's credits at
https://sprites.pmdcollab.org before distributing anything.

Pure python (no dependencies). Source sheets are 8-direction Walk-Anim.png
files whose frame size comes from AnimData.xml; we take the down/left/right/up
rows, sample two stepping frames, and center them into 32px cells
(nearest-neighbor downscale only when a frame is larger than 32px).
"""

import os
import struct
import sys
import urllib.request
import xml.etree.ElementTree as ET
import zlib

DEFAULT_SOURCE = "https://raw.githubusercontent.com/PMDCollab/SpriteCollab/master"
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "PMDRoguelike", "Content", "Sprites", "Species")
CELL = 32

# National dex numbers for this game's roster.
SPECIES_DEX = {
    "bulbasaur": 1, "ivysaur": 2, "charmander": 4, "squirtle": 7, "caterpie": 10,
    "pidgey": 16, "rattata": 19, "pikachu": 25, "zubat": 41, "oddish": 43,
    "growlithe": 58, "abra": 63, "machop": 66, "geodude": 74, "magnemite": 81,
    "gastly": 92, "gengar": 94, "onix": 95,
}

# SpriteCollab Walk-Anim rows are the 8 chunsoft directions, clockwise from down.
# Our sheet rows: 0=down, 1=left, 2=right, 3=up.
SOURCE_ROW_FOR_OURS = [0, 6, 2, 4]


# ---------------------------------------------------------------- PNG codec

def read_png(data):
    """Minimal PNG decoder: 8-bit RGB/RGBA/palette, filters 0-4, no interlace."""
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")
    pos, w, h, color_type, idat, palette, trans = 8, None, None, None, b"", None, None
    while pos < len(data):
        ln = struct.unpack(">I", data[pos:pos + 4])[0]
        tag = data[pos + 4:pos + 8]
        chunk = data[pos + 8:pos + 8 + ln]
        if tag == b"IHDR":
            w, h, depth, color_type, _, _, interlace = struct.unpack(">IIBBBBB", chunk)
            if depth != 8 or interlace != 0:
                raise ValueError(f"unsupported PNG (depth={depth}, interlace={interlace})")
        elif tag == b"PLTE":
            palette = [tuple(chunk[i:i + 3]) for i in range(0, len(chunk), 3)]
        elif tag == b"tRNS":
            trans = list(chunk)
        elif tag == b"IDAT":
            idat += chunk
        pos += 12 + ln

    channels = {2: 3, 3: 1, 6: 4}.get(color_type)
    if channels is None:
        raise ValueError(f"unsupported PNG color type {color_type}")

    raw = zlib.decompress(idat)
    stride = w * channels
    rows, prev = [], bytearray(stride)
    for y in range(h):
        line = raw[y * (stride + 1):(y + 1) * (stride + 1)]
        f, cur = line[0], bytearray(line[1:])
        for i in range(stride):
            a = cur[i - channels] if i >= channels else 0
            b = prev[i]
            c = prev[i - channels] if i >= channels else 0
            if f == 1: cur[i] = (cur[i] + a) & 255
            elif f == 2: cur[i] = (cur[i] + b) & 255
            elif f == 3: cur[i] = (cur[i] + (a + b) // 2) & 255
            elif f == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pred = a if (pa <= pb and pa <= pc) else b if pb <= pc else c
                cur[i] = (cur[i] + pred) & 255
        prev = cur
        row = []
        for x in range(w):
            if color_type == 6:
                row.append(tuple(cur[x * 4:x * 4 + 4]))
            elif color_type == 2:
                row.append((cur[x * 3], cur[x * 3 + 1], cur[x * 3 + 2], 255))
            else:  # palette
                idx = cur[x]
                r, g, b_ = palette[idx]
                a_ = trans[idx] if trans and idx < len(trans) else 255
                row.append((r, g, b_, a_))
        rows.append(row)
    return w, h, rows


def write_png(path, width, height, pixels):
    raw = b"".join(b"\x00" + b"".join(struct.pack("4B", *px) for px in row) for row in pixels)

    def chunk(tag, payload):
        c = tag + payload
        return struct.pack(">I", len(payload)) + c + struct.pack(">I", zlib.crc32(c))

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)))
        f.write(chunk(b"IDAT", zlib.compress(raw, 9)))
        f.write(chunk(b"IEND", b""))


# ---------------------------------------------------------------- conversion

def fetch(source, dex, filename):
    path = f"sprite/{dex:04d}/{filename}"
    if source.startswith("http"):
        with urllib.request.urlopen(f"{source}/{path}", timeout=30) as r:
            return r.read()
    with open(os.path.join(source, path), "rb") as f:
        return f.read()


def walk_frame_size(anim_xml):
    """Frame size of the Walk anim, following CopyOf references."""
    root = ET.fromstring(anim_xml)
    anims = {a.findtext("Name"): a for a in root.iter("Anim")}
    anim, seen = anims.get("Walk"), set()
    while anim is not None and anim.find("FrameWidth") is None:
        target = anim.findtext("CopyOf")
        if target in seen or target not in anims:
            raise ValueError("Walk anim has no frame size")
        seen.add(target)
        anim = anims[target]
    if anim is None:
        raise ValueError("no Walk anim in AnimData.xml")
    return int(anim.findtext("FrameWidth")), int(anim.findtext("FrameHeight"))


def extract_cell(rows, frame_x, frame_y, fw, fh):
    """One source frame, scaled (down only) and centered into a CELL x CELL cell."""
    scale = min(1.0, CELL / fw, CELL / fh)
    out_w, out_h = max(1, round(fw * scale)), max(1, round(fh * scale))
    cell = [[(0, 0, 0, 0)] * CELL for _ in range(CELL)]
    ox, oy = (CELL - out_w) // 2, (CELL - out_h) // 2
    for y in range(out_h):
        sy = frame_y + min(fh - 1, int(y / scale))
        for x in range(out_w):
            sx = frame_x + min(fw - 1, int(x / scale))
            cell[oy + y][ox + x] = rows[sy][sx]
    return cell


def convert(sheet_png, anim_xml):
    fw, fh = walk_frame_size(anim_xml)
    w, h, rows = read_png(sheet_png)
    frames = w // fw
    if frames < 1 or h < fh * 7:
        raise ValueError(f"unexpected Walk-Anim layout ({w}x{h}, frame {fw}x{fh})")

    # Two stepping frames spread across the cycle (fall back to 0 for 1-frame anims).
    picks = [frames // 4, (3 * frames) // 4] if frames > 1 else [0, 0]
    if picks[0] == picks[1] and frames > 1:
        picks[1] = (picks[0] + frames // 2) % frames

    sheet = [[(0, 0, 0, 0)] * (2 * CELL) for _ in range(4 * CELL)]
    for our_row, src_row in enumerate(SOURCE_ROW_FOR_OURS):
        for our_col, src_frame in enumerate(picks):
            cell = extract_cell(rows, src_frame * fw, src_row * fh, fw, fh)
            for y in range(CELL):
                for x in range(CELL):
                    sheet[our_row * CELL + y][our_col * CELL + x] = cell[y][x]
    return sheet


def main():
    args = [a for a in sys.argv[1:]]
    source = DEFAULT_SOURCE
    if "--source" in args:
        i = args.index("--source")
        source = args[i + 1]
        del args[i:i + 2]
    wanted = args or list(SPECIES_DEX)

    os.makedirs(OUT_DIR, exist_ok=True)
    done, failed = [], []
    for species in wanted:
        dex = SPECIES_DEX.get(species)
        if dex is None:
            print(f"  ?? unknown species '{species}' (known: {', '.join(SPECIES_DEX)})")
            continue
        try:
            anim_xml = fetch(source, dex, "AnimData.xml")
            sheet_png = fetch(source, dex, "Walk-Anim.png")
            sheet = convert(sheet_png, anim_xml)
            out = os.path.join(OUT_DIR, f"{species}.png")
            write_png(out, 2 * CELL, 4 * CELL, sheet)
            print(f"  ok  {species} (#{dex:04d}) -> Content/Sprites/Species/{species}.png")
            done.append((species, dex))
        except Exception as ex:
            print(f"  !!  {species} (#{dex:04d}) failed: {ex}")
            failed.append(species)

    if done:
        credits_path = os.path.join(OUT_DIR, "CREDITS-PMDCollab.txt")
        with open(credits_path, "w") as f:
            f.write("These walk sprites were converted from PMDCollab SpriteCollab\n"
                    "(https://github.com/PMDCollab/SpriteCollab), community-made art that is\n"
                    "free for non-commercial fan projects with artist credit.\n"
                    "Per-sprite artist credits: https://sprites.pmdcollab.org\n\nSprites used:\n")
            for species, dex in done:
                f.write(f"  {species}: sprite/{dex:04d}/Walk-Anim.png\n")
        print(f"\nWrote {credits_path} — keep it with the sprites.")

    print(f"\n{len(done)} converted, {len(failed)} failed."
          + (" Rebuild the game (dotnet build) to bake them in." if done else ""))
    return 0 if not failed else 1


if __name__ == "__main__":
    sys.exit(main())
