# Project PMD-Rogue

A 2D top-down, turn-based roguelike inspired by **Pokémon Mystery Dungeon**, with a
**Risk of Rain**-style infinitely-stacking item system. Built in C# on MonoGame (DesktopGL).

A run: pick a starter → fight through **3 procedurally generated dungeons** (13 floors),
each capped by a **boss** → victory. Fainting anywhere ends the run — permadeath, no
meta-progression.

## Features

- **Grid & turn-based**: 8-directional movement (diagonals can't cut corners), PMD-style
  turn flow — you act, every enemy acts, animations play in parallel
- **Real Pokémon combat**: mainline damage formula (STAB, full 18-type chart, crits,
  damage rolls), 4 moves with PP, Struggle fallback, Burn/Poison/Paralysis/Sleep,
  EXP/levels with move learning — 18 species and 30+ moves, all authored in JSON
- **The RoR item system**: passives stack flatly and infinitely through a hook pipeline
  (5× Leftovers = 5% HP/turn), with caps where needed; Choice Band locks your move until
  the stairs, Focus Sash saves you once per floor; Q/E actives
- **Economy**: Poké from kills and floor piles, chests, shop rooms with a keeper —
  prices and item rarity scale with depth
- **Bosses**: arena floors with hidden stairs; bosses summon minions and enrage
- **Roguelike dressing**: fog of war, minimap, message log, run stats on death/victory
- **Placeholder A/V, real pipelines**: every sprite and sound is procedurally generated
  by scripts in `tools/` — replace any file under `Content/Sprites` or `Content/Audio`
  with real art/audio of the same name and it just works
- **Real PMD-style sprites in one command**: `python3 tools/fetch_pmd_sprites.py` pulls
  community walk sprites for the whole roster from
  [PMDCollab SpriteCollab](https://github.com/PMDCollab/SpriteCollab) and converts them
  to the game's sheet layout (non-commercial use, artist credit required — the script
  writes a credits file)

## Building & Running

Prereqs: [.NET 8 SDK](https://dotnet.microsoft.com/download). MonoGame's content tools
restore automatically on first build.

```bash
cd PMDRoguelike
dotnet run
```

Headless smoke test (procgen, combat math, items, economy, a full bot-played run):

```bash
dotnet run -- --dump-map [seed]
```

## Controls

| Key | Action |
|---|---|
| Arrows / WASD | Move (8 directions; bump to turn in place for free) |
| 1–4 | Use a move along your facing |
| Space | Wait a turn |
| Q / E | Trigger active items |
| Enter / Z | Interact: descend stairs, open chests, buy |
| Shift (hold) | Moves panel |
| Tab (hold) | Items panel |
| Esc | Pause (controls, mute `M`, volume `-`/`+`, abandon `X`) |

## Project layout

- `PMDRoguelike/Content/Data/` — all game data (species, moves, type chart, dungeons)
- `PMDRoguelike/Combat|Items|Dungeon|Turns|Entities/` — game logic (graphics-free, tested headlessly)
- `PMDRoguelike/Rendering|UI|States/` — presentation
- `tools/` — placeholder asset generators (`generate_placeholder_sprites.py`, `generate_placeholder_audio.py`)
- `ROADMAP.md` — the phased plan this was built from, with everything checked off

## License

A non-commercial fan game for educational purposes. Pokémon and all related properties
are owned by Nintendo, Game Freak, and The Pokémon Company. All art and audio in this
repository are original procedural placeholders. Bundled font: DejaVu Sans (see
`Content/Fonts/DejaVuSans-LICENSE.txt`).
