#!/usr/bin/env python3
"""Generate the placeholder pixel art for Project PMD-Rogue.

Pure-python PNG synthesis (no dependencies): per-species directional walk
sheets, per-dungeon tilesets, and prop/icon sprites, written under
PMDRoguelike/Content/Sprites/. Deterministic per species id. Replace any file
with real art of the same name and layout whenever you have it — the game only
cares about names and the 32px grid.

Sheet layout for species (64x128): 2 frames wide x 4 rows (down, left, right, up).

Usage: python3 tools/generate_placeholder_sprites.py
"""

import hashlib
import json
import os
import struct
import zlib

HERE = os.path.dirname(__file__)
OUT_ROOT = os.path.join(HERE, "..", "PMDRoguelike", "Content", "Sprites")
DATA_DIR = os.path.join(HERE, "..", "PMDRoguelike", "Content", "Data")

T = 32  # tile / frame size


# ---------------------------------------------------------------- PNG writer

def write_png(path, width, height, pixels):
    """pixels: list of rows, each row a list of (r,g,b,a)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    raw = b"".join(b"\x00" + b"".join(struct.pack("4B", *px) for px in row) for row in pixels)

    def chunk(tag, data):
        c = tag + data
        return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c))

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)))
        f.write(chunk(b"IDAT", zlib.compress(raw, 9)))
        f.write(chunk(b"IEND", b""))
    print(f"  Sprites/{os.path.relpath(path, OUT_ROOT)}")


def canvas(w, h, fill=(0, 0, 0, 0)):
    return [[fill for _ in range(w)] for _ in range(h)]


def put(img, x, y, color):
    if 0 <= y < len(img) and 0 <= x < len(img[0]):
        img[y][x] = color


def rect(img, x0, y0, w, h, color):
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            put(img, x, y, color)


def hex_rgb(s):
    s = s.lstrip("#")
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


def shade(c, f):
    return (max(0, min(255, int(c[0] * f))), max(0, min(255, int(c[1] * f))),
            max(0, min(255, int(c[2] * f))), 255)


def seeded(name):
    """Deterministic tiny PRNG from a string."""
    h = int(hashlib.md5(name.encode()).hexdigest(), 16)

    def nxt(lo, hi):
        nonlocal h
        h = (h * 6364136223846793005 + 1442695040888963407) % (1 << 64)
        return lo + (h >> 33) % (hi - lo + 1)

    return nxt


# ---------------------------------------------------------------- species

def draw_creature(img, ox, oy, body, rnd, row, frame):
    """One 32x32 frame of a blobby creature. row: 0=down,1=left,2=right,3=up."""
    dark = shade(body[:3], 0.55)
    light = shade(body[:3], 1.35)
    accent = shade(body[:3], 0.8)

    half_w = rnd["half_w"]
    height = rnd["height"]
    cx, cy = 16, 18
    bob = -1 if frame == 1 else 0

    # Shadow.
    for x in range(cx - half_w, cx + half_w):
        put(img, ox + x, oy + 28, (0, 0, 0, 70))

    # Body: symmetric ellipse-ish blob with outline.
    for y in range(-height, height + 1):
        span = int(half_w * (1 - (abs(y) / (height + 1)) ** 2) ** 0.5 + 0.5)
        for x in range(-span, span + 1):
            yy = oy + cy + y + bob
            xx = ox + cx + x
            edge = abs(x) >= span or abs(y) >= height
            put(img, xx, yy, dark if edge else (body[0], body[1], body[2], 255))

    # Belly patch (front rows only).
    if row in (0, 1, 2):
        for y in range(2, height):
            span = max(0, int(half_w * 0.45 - abs(y - height // 2)))
            for x in range(-span, span + 1):
                put(img, ox + cx + x, oy + cy + y + bob - 1, light)

    # Ears / crest nubs.
    ear = rnd["ear"]
    if ear:
        put(img, ox + cx - half_w + 2, oy + cy - height - 1 + bob, dark)
        put(img, ox + cx + half_w - 2, oy + cy - height - 1 + bob, dark)

    # Type accent stripe across the back.
    ac = rnd["accent"]
    if ac:
        for x in range(-half_w + 3, half_w - 2, 3):
            put(img, ox + cx + x, oy + cy - height + 2 + bob, ac)

    # Eyes by facing (none on the back row).
    def eye(x, y):
        put(img, ox + x, oy + y + bob, (255, 255, 255, 255))
        put(img, ox + x, oy + y + 1 + bob, (20, 20, 30, 255))

    ey = cy - height // 2
    if row == 0:
        eye(cx - half_w // 2, ey)
        eye(cx + half_w // 2, ey)
    elif row == 1:
        eye(cx - half_w + 3, ey)
    elif row == 2:
        eye(cx + half_w - 3, ey)

    # Feet: alternate per frame.
    fy = cy + height + bob
    if frame == 0:
        rect(img, ox + cx - half_w // 2 - 1, oy + fy, 2, 2, accent)
        rect(img, ox + cx + half_w // 2 - 1, oy + fy + 1, 2, 2, accent)
    else:
        rect(img, ox + cx - half_w // 2 - 1, oy + fy + 1, 2, 2, accent)
        rect(img, ox + cx + half_w // 2 - 1, oy + fy, 2, 2, accent)


TYPE_ACCENTS = {
    "fire": (255, 120, 40, 255), "water": (80, 140, 255, 255), "grass": (70, 200, 90, 255),
    "electric": (255, 220, 60, 255), "poison": (190, 90, 210, 255), "psychic": (255, 110, 170, 255),
    "ghost": (120, 90, 200, 255), "rock": (190, 170, 120, 255), "ground": (220, 180, 110, 255),
    "bug": (170, 200, 60, 255), "flying": (170, 190, 240, 255), "normal": (200, 200, 190, 255),
    "fighting": (200, 80, 60, 255), "steel": (180, 190, 200, 255), "dark": (90, 80, 90, 255),
    "ice": (140, 220, 240, 255), "dragon": (110, 90, 230, 255), "fairy": (250, 160, 200, 255),
}


def gen_species():
    with open(os.path.join(DATA_DIR, "Species.json")) as f:
        species = json.load(f)["species"]

    for s in species:
        body = hex_rgb(s["color"])
        nxt = seeded(s["id"])
        rnd = {
            "half_w": nxt(7, 10),
            "height": nxt(7, 10),
            "ear": nxt(0, 1) == 1,
            "accent": TYPE_ACCENTS.get(s["types"][0].lower()) if nxt(0, 2) > 0 else None,
        }
        img = canvas(2 * T, 4 * T)
        for row in range(4):
            for frame in range(2):
                draw_creature(img, frame * T, row * T, body, rnd, row, frame)
        write_png(os.path.join(OUT_ROOT, "Species", f"{s['id']}.png"), 2 * T, 4 * T, img)


# ---------------------------------------------------------------- tiles

def gen_tiles():
    with open(os.path.join(DATA_DIR, "Dungeons.json")) as f:
        dungeons = json.load(f)["dungeons"]

    for d in dungeons:
        floor_c = hex_rgb(d["floorColor"])
        wall_c = hex_rgb(d["wallColor"])
        nxt = seeded(d["id"])

        # Floor: base + speckle + soft inset border.
        img = canvas(T, T, (*floor_c, 255))
        for _ in range(26):
            x, y = nxt(1, T - 2), nxt(1, T - 2)
            put(img, x, y, shade(floor_c, 0.9 if nxt(0, 1) else 1.08))
        for i in range(T):
            put(img, i, 0, shade(floor_c, 0.92))
            put(img, 0, i, shade(floor_c, 0.92))
        write_png(os.path.join(OUT_ROOT, "Tiles", f"{d['id']}_floor.png"), T, T, img)

        # Wall top: dark base with brick joints.
        img = canvas(T, T, (*shade(wall_c, 0.9)[:3], 255))
        for y in range(0, T, 8):
            for x in range(T):
                put(img, x, y, shade(wall_c, 0.6))
        for y in range(0, T, 8):
            off = 0 if (y // 8) % 2 == 0 else 8
            for x in range(off, T, 16):
                for yy in range(y, min(T, y + 8)):
                    put(img, x, yy, shade(wall_c, 0.6))
        write_png(os.path.join(OUT_ROOT, "Tiles", f"{d['id']}_wall.png"), T, T, img)

        # Wall face (used when floor is below): brighter cap + shaded front.
        img = canvas(T, T, (*shade(wall_c, 1.15)[:3], 255))
        for y in range(12, T):
            f = 0.85 - (y - 12) * 0.012
            for x in range(T):
                put(img, x, y, shade(wall_c, f))
        for x in range(T):
            put(img, x, 11, shade(wall_c, 0.5))
        write_png(os.path.join(OUT_ROOT, "Tiles", f"{d['id']}_wall_face.png"), T, T, img)


def gen_props():
    # Stairs: descending steps on transparency (drawn over the floor).
    img = canvas(T, T)
    for i, top in enumerate((6, 12, 18, 24)):
        g = 170 - i * 26
        step = tuple(min(255, v) for v in (g, g + 10, g + 40)) + (255,)
        lip = tuple(min(255, v) for v in (g + 50, g + 60, g + 90)) + (255,)
        rect(img, 4 + i * 2, top, T - 8 - i * 4, 6, step)
        rect(img, 4 + i * 2, top, T - 8 - i * 4, 1, lip)
    write_png(os.path.join(OUT_ROOT, "Props", "stairs.png"), T, T, img)

    # Chest.
    img = canvas(T, T)
    rect(img, 5, 10, 22, 16, (60, 40, 22, 255))
    rect(img, 6, 11, 20, 14, (130, 88, 46, 255))
    rect(img, 6, 16, 20, 1, (60, 40, 22, 255))
    rect(img, 14, 13, 4, 7, (235, 200, 90, 255))
    rect(img, 15, 15, 2, 2, (120, 90, 30, 255))
    write_png(os.path.join(OUT_ROOT, "Props", "chest.png"), T, T, img)

    # Shopkeeper: a round teal merchant under a tiny awning.
    img = canvas(T, T)
    for y in range(-7, 8):
        span = int((49 - y * y) ** 0.5)
        for x in range(-span, span + 1):
            edge = abs(x) >= span
            put(img, 16 + x, 18 + y, (30, 90, 84, 255) if edge else (64, 170, 158, 255))
    put(img, 13, 15, (255, 255, 255, 255)); put(img, 13, 16, (20, 20, 30, 255))
    put(img, 19, 15, (255, 255, 255, 255)); put(img, 19, 16, (20, 20, 30, 255))
    for i in range(4):
        rect(img, 4 + i * 6, 4 + (i % 2), 6, 3, (220, 80, 80, 255) if i % 2 == 0 else (240, 240, 240, 255))
    write_png(os.path.join(OUT_ROOT, "Props", "shopkeeper.png"), T, T, img)

    # Coin.
    img = canvas(16, 16)
    for y in range(-5, 6):
        span = int((25 - y * y) ** 0.5)
        for x in range(-span, span + 1):
            edge = abs(x) >= span
            put(img, 8 + x, 8 + y, (150, 120, 30, 255) if edge else (235, 200, 90, 255))
    put(img, 6, 5, (255, 245, 200, 255))
    write_png(os.path.join(OUT_ROOT, "Props", "coin.png"), 16, 16, img)

    # Player marker: white corner brackets drawn over the player's tile.
    img = canvas(T, T)
    w = (255, 255, 255, 230)
    for i in range(6):
        put(img, i, 0, w); put(img, 0, i, w)
        put(img, T - 1 - i, 0, w); put(img, T - 1, i, w)
        put(img, i, T - 1, w); put(img, 0, T - 1 - i, w)
        put(img, T - 1 - i, T - 1, w); put(img, T - 1, T - 1 - i, w)
    write_png(os.path.join(OUT_ROOT, "Props", "marker_player.png"), T, T, img)


def gen_icons():
    tiers = {
        "common": (240, 240, 240, 255),
        "uncommon": (110, 210, 100, 255),
        "legendary": (226, 80, 80, 255),
        "active": (240, 160, 64, 255),
    }
    outline = (25, 25, 32, 255)

    # Common: circle.
    img = canvas(16, 16)
    for y in range(-5, 6):
        span = int((26 - y * y) ** 0.5)
        for x in range(-span, span + 1):
            put(img, 8 + x, 8 + y, outline if abs(x) >= span else tiers["common"])
    write_png(os.path.join(OUT_ROOT, "Icons", "common.png"), 16, 16, img)

    # Uncommon: diamond.
    img = canvas(16, 16)
    for y in range(-6, 7):
        span = 6 - abs(y)
        for x in range(-span, span + 1):
            put(img, 8 + x, 8 + y, outline if abs(x) == span else tiers["uncommon"])
    write_png(os.path.join(OUT_ROOT, "Icons", "uncommon.png"), 16, 16, img)

    # Legendary: star (diamond + cross spikes).
    img = canvas(16, 16)
    for y in range(-4, 5):
        span = 4 - abs(y)
        for x in range(-span, span + 1):
            put(img, 8 + x, 8 + y, tiers["legendary"])
    for i in range(7):
        put(img, 8, 1 + i, tiers["legendary"]); put(img, 8, 14 - i, tiers["legendary"])
        put(img, 1 + i, 8, tiers["legendary"]); put(img, 14 - i, 8, tiers["legendary"])
    write_png(os.path.join(OUT_ROOT, "Icons", "legendary.png"), 16, 16, img)

    # Active: flask.
    img = canvas(16, 16)
    rect(img, 6, 2, 4, 3, tiers["active"])
    for y in range(5, 13):
        span = min(5, 1 + (y - 5))
        for x in range(-span, span + 1):
            put(img, 8 + x, y, outline if abs(x) == span or y == 12 else tiers["active"])
    write_png(os.path.join(OUT_ROOT, "Icons", "active.png"), 16, 16, img)


if __name__ == "__main__":
    print("Generating placeholder sprites...")
    gen_species()
    gen_tiles()
    gen_props()
    gen_icons()
    print("Done.")
