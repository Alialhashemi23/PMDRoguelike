# Project PMD-Rogue — MVP Roadmap

A phased plan for building the game described in the project brief: a 2D top-down, turn-based
roguelike inspired by Pokémon Mystery Dungeon, with a Risk-of-Rain-style stacking item system,
built in C# / MonoGame.

Each phase ends in a state that **builds, passes the headless smoke test, and is playable**, so we
can course-correct between phases. Update the checkboxes as we go.

---

## MVP Definition

A complete run is: pick a starter → clear **3 dungeons of 4–5 floors each**, each capped by a
**boss** → victory screen. Dying anywhere ends the run (permadeath, no meta-progression).
A run should take roughly 30–60 minutes.

### Scope decisions (locked)

| Decision | Choice |
|---|---|
| Roster size | Curated: ~10–15 species, ~25–30 moves, all data-driven (JSON) |
| Growth | EXP/levels + **move learning on level-up**; no evolution in MVP |
| Run structure | 3 dungeons × 4–5 floors, boss per dungeon, victory after the third |
| Item sources | Enemy drops, chests (Poké currency), **shops**, boss rewards — all in MVP |
| UI extras | Message log, minimap w/ fog of war, title & game-over/victory screens |
| Audio | SFX + music in MVP (placeholder-quality assets) |
| Graphics | Placeholders until the final sprite-integration phase |

### Post-MVP (explicitly out of scope for now)

Evolution, partner/team Pokémon, meta-progression/unlocks, save-and-resume mid-run, weather,
abilities, traps, hunger/belly, ranged move projectiles beyond straight lines, controller support.

---

## ✅ Phase 1: Project Setup & Grid Movement Engine — DONE

Procgen rooms-and-corridors floors, 8-directional grid movement (no corner cutting), turn
controller (player acts → enemies act, parallel slide animations), dummy chase/wander AI,
placeholder rendering + camera, constants pipeline, headless smoke test (`--dump-map`).

---

## ✅ Phase 2: Run Skeleton — Stairs, Floors & Game States — DONE

**Goal:** the *shape* of a full run exists before any combat: descend floors, cross dungeons,
reach a stubbed victory; a stubbed game-over exists for later.

- [x] `GameState` scene machine (replaces the old empty SceneManager idea): `DungeonState`,
      placeholder `GameOverState` / `VictoryState`; game class delegates Update/Draw to the active state
- [x] Stairs tile: generator places stairs in a room far from spawn; stepping on them + confirm key descends
- [x] Dungeon definitions (JSON in `Content/Data/`): name, floor count, tile palette, enemy table stub — 3 dungeons defined
- [x] `RunManager`: tracks current dungeon/floor/turn count; floor transition regenerates the floor; last floor → next dungeon; dungeon 3 → VictoryState stub
- [x] Minimal HUD: dungeon name + floor number (bitmap font or SpriteFont in content pipeline)
- [x] Smoke test: simulate descending through all floors of all 3 dungeons headlessly

**Playable check:** walk to stairs, descend through 3 dungeons, hit the victory stub. Press R still regenerates.

---

## ✅ Phase 3: Pokémon Data & Combat Foundation — DONE

**Goal:** real Pokémon on the grid hitting each other with real math. The biggest phase — the heart of the game.

- [x] Data pipeline: JSON loading for species (base stats, types, learnset), moves (power, type,
      physical/special, PP, accuracy, effects), and the full 18-type effectiveness chart
- [x] Curated content: ~12 species, ~25 moves authored
- [x] Stat system: level-based stat calculation from base stats; Actor gets a real stat block (HP/Atk/Def/SpA/SpD/Spe)
- [x] Damage formula: standard Pokémon mainline formula with STAB, type effectiveness, crit chance, damage roll
- [x] Moves & PP: player has up to 4 moves; move-selection UI (e.g. hold a modifier + 1–4); depleted PP blocks the move; basic "Struggle" fallback
- [x] Attack resolution as `TurnAction`s: `AttackAction` joins Move/Wait; facing-based targeting (melee + straight-line ranged); simple hit flash/lunge animation
- [x] **Message log UI**: scrolling combat log ("X used Ember! It's super effective!") — required to read combat
- [x] Enemy combat AI: use moves when in range, approach otherwise; enemies drawn from the current dungeon's spawn table with levels
- [x] Fainting: enemies die and are removed; player faints → GameOverState (permadeath)
- [x] EXP & leveling: EXP on kill (scaled by level), level-ups with stat growth, **move learning on level-up** with a replace-move prompt when at 4
- [x] Smoke test: scripted battle assertions (damage math golden tests, PP depletion, faint, level-up)

**Playable check:** a real fight — pick moves, exploit type matchups, watch PP, kill things, level up, learn a move, die and see game over.

---

## ✅ Phase 4: Combat Depth — Status Conditions & Smarter Dungeon AI — DONE

**Goal:** combat gets the required status layer and enemies stop being trivial.

- [x] Status conditions: Burn, Poison, Paralysis, Sleep (durations from GameConstants; already configured) — tick timing, damage-over-time, action-skip/attack-drop effects, log messages, HUD indicators
- [x] Moves can inflict statuses (effect data on move JSON); a few status-focused moves added to the roster
- [x] Status cures groundwork (used by berries/items in Phase 5)
- [x] AI upgrades: line-of-sight awareness, room-based aggro (notice you when you enter their room), don't path through other enemies' reserved tiles (already handled) but do path around obstacles better
- [x] Enemy spawn scaling: level ranges per floor from the dungeon definition; moderate stat scaling across floors/dungeons
- [x] Smoke test: status tick assertions (burn damage per turn, sleep skips actions, paralysis proc rate)

**Playable check:** get poisoned, watch it tick in the log; put an enemy to sleep and take free turns.

---

## ✅ Phase 5: The Risk of Rain Item System — DONE

**Goal:** the signature mechanic — infinitely stacking passives and manually triggered actives.

- [x] Item architecture: `PassiveItem` / `ActiveItem` under the existing `Item` base; **stat-hook pipeline**
      (OnFloorStart, OnTurnEnd, OnDealDamage, OnTakeDamage, OnMoveUsed, ModifyStats) so item effects compose;
      stack count multiplies effect with **hard/soft caps** where needed (evasion, crit)
- [x] Inventories: passive inventory (unbounded stacks, grouped by item) + limited active/relic slots; inventory UI screen showing stacks and tiers
- [x] Ground items: item entities on the floor, walk-over pickup, drop tables (low-rate enemy drops)
- [x] Concrete items — at least: Oran Berry, Silk Scarf (Common); Eviolite, Rocky Helmet (Uncommon);
      Choice Band (move-lock until floor transition), Life Orb, Focus Sash (once per floor) (Legendary);
      plus 2–3 more per tier for variety; 2–3 actives (e.g. Escape Rope, Blast Seed, status-cure)
- [x] Tier colors in UI (white/green/red/orange) and weighted rarity rolls
- [x] Smoke test: stacking math golden tests (5× Leftovers = 5%/turn, caps clamp correctly, Focus Sash resets per floor, Choice Band lock/unlock)

**Playable check:** stack 4 of the same passive and feel the difference; trigger an active; Choice Band locks you into a move until the stairs.

---

## ✅ Phase 6: Economy — Poké, Chests & Shops — DONE

**Goal:** the acquisition loop: earn currency, spend it on chests and shops.

- [x] Poké currency: drops from enemies/on the floor, HUD counter, persists across floors within a run
- [x] Chests: generator places 0–2 per floor; opening costs Poké (price scales by depth); rolls a tier-weighted item
- [x] Shop rooms: occasional special room with a shopkeeper tile and priced items on display; browse/buy UI; stealing = not a thing (MVP)
- [x] Item pools: per-dungeon/depth weighting so later floors trend rarer
- [x] Smoke test: chest/shop generation counts over many seeds, price scaling assertions

**Playable check:** hoard Poké on floor 1, buy something green on floor 3, regret not saving for the chest.

---

## ✅ Phase 7: Bosses, Difficulty Curve & the Complete Run — DONE

**Goal:** the full MVP loop closes: 3 dungeons, 3 bosses, victory or permadeath.

- [x] Boss floors: final floor of each dungeon is a handcrafted-ish arena room (generator special-case)
- [x] 3 bosses authored: higher stats, unique movesets, simple behavior patterns (e.g. power-up turn, summon 2 minions once)
- [x] Boss rewards: guaranteed Legendary-tier drop + Poké shower; defeating the boss reveals the stairs to the next dungeon
- [x] Difficulty curve pass: tune enemy levels/stats/spawn counts across all 15-ish floors so dungeon 1 is gentle and dungeon 3 threatens
- [x] Run stats tracking: turns taken, kills, items collected, damage dealt — for the end screens
- [x] Permadeath finalized: death anywhere → run summary; no state carries over
- [x] Smoke test: full-run simulation (bot descends all floors with combat resolved headlessly) completes without invariant violations

**Playable check:** a full 3-dungeon run, win or die, is genuinely fun at least once.

---

## Phase 8: Game Shell — Title, Starter Select, Endings, Minimap

**Goal:** it frames itself like a finished game instead of a debug build.

- [ ] Title screen (new game / quit) wired into the GameState machine
- [ ] Starter selection screen: pick from 3–4 of the curated species
- [ ] Game-over & victory screens: run stats (from Phase 7), cause of death, "play again" loop back to title
- [ ] Fog of war: explored-tiles layer on `DungeonMap`; unexplored = black, explored-but-unseen = dimmed
- [ ] Minimap: corner overlay showing explored layout, stairs, enemies in sight, chests/shops
- [ ] Pause/menu screen: resume, controls reference, abandon run
- [ ] HUD polish: HP bar, EXP bar, status icons, Poké counter, floor label unified

**Playable check:** boot → title → pick starter → run → die → stats → play again, all without touching a debug key.

---

## Phase 9: Audio

**Goal:** the game makes noise.

- [ ] Audio manager: SFX (SoundEffect) + music (looping Song/SoundEffectInstance) through the content pipeline, volumes in GameConstants
- [ ] SFX hooks on existing events: move used, hit (normal/super/not-very), faint, pickup, stairs, chest, buy, level-up, menu navigation
- [ ] Music: per-dungeon track, boss track, title track (placeholder/CC0 audio to start)
- [ ] Mute/volume in the pause menu

**Playable check:** play with sound on; super-effective hits should feel chunky.

---

## Phase 10: Sprites & Visual Polish

**Goal:** swap placeholders for real art via the sprite-key indirection built in Phase 1.

- [ ] Sprite-sheet loader: directional idle/walk frames per species; animation timing tied to the existing slide tween
- [ ] Attack/hurt flash frames; faint animation; damage numbers popup
- [ ] Tile sets: per-dungeon floor/wall textures (autotile-lite: walls pick edge variants); stairs/chest/shop/item sprites
- [ ] Item & UI icons by tier; portrait in HUD
- [ ] Juice pass: screen shake on big hits, hit-stop frames, smooth camera zoom on boss intro
- [ ] Final balance & bug-fix pass across the whole run

**Playable check:** it looks like a PMD-like. Ship the MVP tag. 🎉

---

## Working Agreement

- Every phase: `dotnet build` clean, `dotnet run -- --dump-map` (and phase-specific headless tests) passing, committed and pushed before moving on.
- Game data lives in JSON under `Content/Data/`; adding a species/move/item should never require a code change.
- Placeholder visuals/audio are fine until Phases 9–10; never block a mechanic on assets.
