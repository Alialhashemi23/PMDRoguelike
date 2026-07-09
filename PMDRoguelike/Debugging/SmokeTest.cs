using Microsoft.Xna.Framework;
using PMDRoguelike.Combat;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Run;
using PMDRoguelike.Turns;
using PMDRoguelike.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PMDRoguelike.Debugging
{
    /// <summary>
    /// Headless verification of procgen, turn logic, and combat (no window needed).
    /// Prints the generated floor as ASCII, flood-fills to prove every floor tile is
    /// reachable, simulates turns through the real TurnController, walks a full run
    /// to victory, and asserts combat math golden values. Exit code 0 = pass.
    /// </summary>
    public static class SmokeTest
    {
        public static int Run(int seed)
        {
            GameConstants.Instance.LoadConstants();
            GameData.Load();

            var rng = new Rng(seed);
            GeneratedFloor floor = new DungeonGenerator(rng).Generate();
            DungeonMap map = floor.Map;

            var player = new Player(floor.PlayerSpawn, GameData.GetSpecies("charmander"), 5);
            map.Actors.Add(player);
            foreach (Point spawn in floor.EnemySpawns)
            {
                map.Actors.Add(new Enemy(spawn, GameData.GetSpecies("rattata"), 3));
            }

            Console.WriteLine($"Seed {seed}: {map.Width}x{map.Height} floor, {map.Rooms.Count} rooms, {floor.EnemySpawns.Count} enemies");
            PrintMap(map);

            bool ok = CheckConnectivity(map, floor.PlayerSpawn);
            ok &= SimulateTurns(map, player, rng, turns: 40);
            ok &= SimulateFullRun(seed);
            ok &= CombatGoldenTests();
            ok &= StatusGoldenTests();
            ok &= ItemGoldenTests();
            ok &= EconomyGoldenTests(seed);

            Console.WriteLine(ok ? "SMOKE TEST PASSED" : "SMOKE TEST FAILED");
            return ok ? 0 : 1;
        }

        // ------------------------------------------------------------------
        // Combat golden tests
        // ------------------------------------------------------------------

        private static bool CombatGoldenTests()
        {
            bool ok = TypeChartTests();
            ok &= StatCalcTests();
            ok &= BattleScriptTest();
            return ok;
        }

        private static bool Expect(bool condition, string what)
        {
            if (!condition) Console.WriteLine($"Combat: FAIL — {what}");
            return condition;
        }

        private static bool TypeChartTests()
        {
            TypeChart chart = GameData.TypeChart;
            bool ok = true;
            ok &= Expect(chart.Effectiveness(PokemonType.Fire, PokemonType.Grass) == 2f, "Fire vs Grass should be 2x");
            ok &= Expect(chart.Effectiveness(PokemonType.Fire, PokemonType.Water) == 0.5f, "Fire vs Water should be 0.5x");
            ok &= Expect(chart.Effectiveness(PokemonType.Electric, PokemonType.Ground) == 0f, "Electric vs Ground should be 0x");
            ok &= Expect(chart.Effectiveness(PokemonType.Normal, PokemonType.Ghost) == 0f, "Normal vs Ghost should be 0x");
            ok &= Expect(chart.Effectiveness(PokemonType.Water, PokemonType.Normal) == 1f, "Water vs Normal should be 1x");

            // Dual typing multiplies: Grass move vs Geodude (Rock/Ground) = 2 * 2 = 4.
            var geodude = GameData.GetSpecies("geodude");
            ok &= Expect(chart.Effectiveness(PokemonType.Grass, geodude.Types) == 4f, "Grass vs Rock/Ground should be 4x");

            if (ok) Console.WriteLine("Combat: type chart golden values — OK");
            return ok;
        }

        private static bool StatCalcTests()
        {
            // Charmander at L50 with no IV/EV: HP = 2*39*50/100 + 50 + 10 = 99, Atk = 2*52*50/100 + 5 = 57.
            StatBlock stats = StatBlock.AtLevel(GameData.GetSpecies("charmander").BaseStats, 50);
            bool ok = Expect(stats.HP == 99, $"Charmander L50 HP expected 99, got {stats.HP}");
            ok &= Expect(stats.Attack == 57, $"Charmander L50 Atk expected 57, got {stats.Attack}");
            ok &= Expect(stats.Speed == 70, $"Charmander L50 Spe expected 70, got {stats.Speed}");

            if (ok) Console.WriteLine("Combat: stat formulas golden values — OK");
            return ok;
        }

        /// <summary>
        /// Scripted duel on a tiny handmade arena: player Charmander vs a Caterpie one
        /// tile east. Exercises PP spend, damage application, faint/removal, EXP gain,
        /// level-up, and the Struggle fallback — all through the real TurnController.
        /// </summary>
        private static bool BattleScriptTest()
        {
            var map = new DungeonMap(9, 9);
            for (int x = 1; x < 8; x++)
                for (int y = 1; y < 8; y++)
                    map.SetTile(new Point(x, y), TileType.Floor);
            map.StairsPosition = new Point(7, 7);
            map.SetTile(map.StairsPosition, TileType.Stairs);

            var rng = new Rng(1234);
            var log = new MessageLog();
            var player = new Player(new Point(3, 3), GameData.GetSpecies("charmander"), 5);
            var enemy = new Enemy(new Point(4, 3), GameData.GetSpecies("caterpie"), 2);
            map.Actors.Add(player);
            map.Actors.Add(enemy);

            var controller = new TurnController(map, player, rng, log);
            bool ok = true;

            // Face the enemy (bump east = turn, costs no turn) then use Ember (slot 1) until it faints.
            ok &= Expect(!controller.ExecuteTurn(new MoveAction(Direction.East)), "bump into enemy should not consume a turn");
            ok &= Expect(player.Facing == Direction.East, "bump should set facing east");

            int emberPP = player.Moves[1].CurrentPP;
            int startExp = player.Exp;
            int startLevel = player.Level;

            for (int i = 0; i < 10 && map.Actors.Contains(enemy); i++)
            {
                ok &= Expect(controller.ExecuteTurn(new AttackAction(1)), "attack turn should execute");
            }

            ok &= Expect(!map.Actors.Contains(enemy), "caterpie should faint and be removed");
            ok &= Expect(player.Moves[1].CurrentPP < emberPP, "Ember PP should be spent");
            ok &= Expect(player.Exp > startExp || player.Level > startLevel, "EXP should be awarded on faint");
            ok &= Expect(log.Messages.Any(m => m.Contains("super effective")), "Ember vs Caterpie should log super effective");
            ok &= Expect(log.Messages.Any(m => m.Contains("fainted")), "faint should be logged");

            // Level-up check: feed EXP directly until a level passes.
            int before = player.Level;
            player.AddExp(Player.ExpRequired(player.Level) + 5, log);
            ok &= Expect(player.Level > before, "AddExp past threshold should level up");
            ok &= Expect(log.Messages.Any(m => m.Contains("grew to level")), "level-up should be logged");

            // Struggle fallback: drain all PP, attack again with a fresh target.
            foreach (MoveSlot slot in player.Moves) slot.CurrentPP = 0;
            var punchingBag = new Enemy(new Point(player.GridPosition.X + 1, player.GridPosition.Y),
                GameData.GetSpecies("geodude"), 2);
            map.Actors.Add(punchingBag);
            player.Facing = Direction.East;
            int hpBefore = player.CurrentHP;
            ok &= Expect(controller.ExecuteTurn(new AttackAction(0)), "attack with no PP should still execute (Struggle)");
            ok &= Expect(log.Messages.Any(m => m.Contains("Struggle")), "Struggle should be used when out of PP");
            ok &= Expect(player.CurrentHP <= hpBefore, "Struggle recoil should not heal the attacker");

            if (ok) Console.WriteLine("Combat: scripted battle (PP, faint, EXP, level-up, Struggle) — OK");
            return ok;
        }

        // ------------------------------------------------------------------
        // Economy tests (Phase 6)
        // ------------------------------------------------------------------

        private static bool EconomyGoldenTests(int seed)
        {
            bool ok = true;

            // --- Price scaling with depth.
            ok &= Expect(Items.Economy.BaseChestPrice(1) == 55, $"chest price at depth 1 should be 55, got {Items.Economy.BaseChestPrice(1)}");
            ok &= Expect(Items.Economy.BaseChestPrice(13) == 235, $"chest price at depth 13 should be 235, got {Items.Economy.BaseChestPrice(13)}");
            ok &= Expect(Items.Economy.BaseItemPrice(Items.ItemTier.Legendary, 1) > Items.Economy.BaseItemPrice(Items.ItemTier.Common, 1),
                "legendaries should cost more than commons");
            ok &= Expect(Items.Economy.BaseItemPrice(Items.ItemTier.Common, 10) > Items.Economy.BaseItemPrice(Items.ItemTier.Common, 1),
                "prices should grow with depth");

            // --- Tier weights shift with depth (and always sum to 1).
            var shallow = Items.ItemRegistry.TierWeights(1);
            var deep = Items.ItemRegistry.TierWeights(13);
            ok &= Expect(Math.Abs(shallow.common - 0.55f) < 0.001f && Math.Abs(shallow.legendary - 0.10f) < 0.001f,
                $"depth-1 weights should be 0.55/0.10 common/legendary, got {shallow.common}/{shallow.legendary}");
            ok &= Expect(deep.common < shallow.common && deep.legendary > shallow.legendary,
                "deep floors should trend rarer");
            float sum = deep.common + deep.uncommon + deep.legendary + deep.active;
            ok &= Expect(Math.Abs(sum - 1f) < 0.001f, $"tier weights must sum to 1, got {sum}");

            // --- Generation legality across many seeds.
            int shopsSeen = 0, chestsSeen = 0;
            for (int s = 0; s < 30; s++)
            {
                var genRng = new Rng(seed + s * 101);
                GeneratedFloor floor = new DungeonGenerator(genRng).Generate();
                DungeonMap map = floor.Map;
                EconomyPopulator.Populate(map, floor.PlayerSpawn, genRng, depth: 1 + s % 13);

                ok &= Expect(map.Chests.Count <= EconomyPopulator.MaxChestsPerFloor,
                    $"seed {s}: too many chests ({map.Chests.Count})");
                ok &= Expect(map.MoneyPiles.Count >= 1 && map.MoneyPiles.Count <= 3,
                    $"seed {s}: money piles out of range ({map.MoneyPiles.Count})");
                chestsSeen += map.Chests.Count;

                foreach (var chest in map.Chests)
                    ok &= Expect(map.IsWalkable(chest.Position) && chest.Position != map.StairsPosition,
                        $"seed {s}: chest on illegal tile");
                foreach (var pile in map.MoneyPiles)
                    ok &= Expect(map.IsWalkable(pile.Position), $"seed {s}: money pile on illegal tile");

                bool hasKeeper = false;
                for (int x = 0; x < map.Width && !hasKeeper; x++)
                    for (int y = 0; y < map.Height && !hasKeeper; y++)
                        hasKeeper = map.GetTile(new Point(x, y)).Type == TileType.Shopkeeper;

                if (map.ShopItems.Count > 0)
                {
                    shopsSeen++;
                    ok &= Expect(hasKeeper, $"seed {s}: shop items without a shopkeeper");
                    foreach (var stock in map.ShopItems)
                        ok &= Expect(map.IsWalkable(stock.Position) && stock.Price > 0, $"seed {s}: bad shop stock");
                }

                // The shopkeeper pillar must never cut off the stairs.
                ok &= Expect(FindPath(map, floor.PlayerSpawn, map.StairsPosition) != null,
                    $"seed {s}: stairs unreachable after economy population");
            }
            ok &= Expect(shopsSeen > 0, "expected at least one shop across 30 seeds");
            ok &= Expect(chestsSeen > 0, "expected at least one chest across 30 seeds");

            // --- Buy and chest flows.
            var arena = new DungeonMap(7, 7);
            for (int x = 1; x < 6; x++)
                for (int y = 1; y < 6; y++)
                    arena.SetTile(new Point(x, y), TileType.Floor);
            arena.StairsPosition = new Point(5, 5);

            var log = new MessageLog();
            var rng = new Rng(seed);
            var buyer = new Player(new Point(2, 2), GameData.GetSpecies("charmander"), 5);
            arena.Actors.Add(buyer);

            var stockItem = new Items.ShopItem(new Point(2, 2), Items.ItemRegistry.Get("oran-berry"), 80);
            arena.ShopItems.Add(stockItem);
            ok &= Expect(!Items.Economy.TryBuy(buyer, arena, stockItem, log), "buying with 0 Poké should fail");
            buyer.AddPoke(100);
            ok &= Expect(Items.Economy.TryBuy(buyer, arena, stockItem, log), "buying with enough Poké should succeed");
            ok &= Expect(buyer.Poke == 20, $"purchase should deduct the price, wallet has {buyer.Poke}");
            ok &= Expect(buyer.Inventory.StacksOf("oran-berry") == 1, "purchased item should be owned");
            ok &= Expect(arena.ShopItems.Count == 0, "sold stock should leave the display");

            var chestToOpen = new Items.Chest(new Point(3, 3), 50);
            arena.Chests.Add(chestToOpen);
            ok &= Expect(!Items.Economy.TryOpenChest(buyer, arena, chestToOpen, log, rng, depth: 3),
                "opening a chest without enough Poké should fail");
            buyer.AddPoke(200);
            int passivesBefore = TotalPassives(buyer);
            int activesBefore = buyer.Inventory.Actives.Count;
            ok &= Expect(Items.Economy.TryOpenChest(buyer, arena, chestToOpen, log, rng, depth: 3),
                "opening a paid chest should succeed");
            bool gotSomething = TotalPassives(buyer) > passivesBefore ||
                                buyer.Inventory.Actives.Count > activesBefore ||
                                arena.GroundItems.Count > 0;
            ok &= Expect(gotSomething, "chest should always yield an item");
            ok &= Expect(arena.Chests.Count == 0, "opened chest should be removed");

            if (ok) Console.WriteLine("Economy: pricing, depth-weighted tiers, generation legality, buy/chest flows — OK");
            return ok;
        }

        private static int TotalPassives(Player player)
        {
            int total = 0;
            foreach (var item in player.Inventory.Passives) total += player.Inventory.StacksOf(item.Id);
            return total;
        }

        // ------------------------------------------------------------------
        // RoR item system tests (Phase 5)
        // ------------------------------------------------------------------

        private static bool ItemGoldenTests()
        {
            bool ok = true;

            // Small arena with stairs for the pickup/active walkthrough.
            var map = new DungeonMap(9, 9);
            for (int x = 1; x < 8; x++)
                for (int y = 1; y < 8; y++)
                    map.SetTile(new Point(x, y), TileType.Floor);
            map.StairsPosition = new Point(7, 7);
            map.SetTile(map.StairsPosition, TileType.Stairs);

            var rng = new Rng(77);
            var log = new MessageLog();
            var player = new Player(new Point(2, 2), GameData.GetSpecies("charmander"), 50);
            map.Actors.Add(player);
            var controller = new TurnController(map, player, rng, log);
            Items.ItemContext ctx = controller.ItemContext;
            Items.Inventory inv = player.Inventory;

            // --- Oran Berry: +10 max HP per stack, current HP rises with it.
            int baseMax = player.Stats.HP;
            for (int i = 0; i < 3; i++) inv.AddItem(Items.ItemRegistry.Get("oran-berry"), log);
            ok &= Expect(player.Stats.HP == baseMax + 30, $"3x Oran Berry: expected {baseMax + 30} max HP, got {player.Stats.HP}");
            ok &= Expect(player.CurrentHP == player.Stats.HP, "Oran Berry max-HP gain should heal by the increase");

            // --- Leftovers: 5 stacks heal 5% of max HP per turn.
            for (int i = 0; i < 5; i++) inv.AddItem(Items.ItemRegistry.Get("leftovers"), log);
            player.TakeDamage(50);
            int expectedHeal = Items.Leftovers.HealAmount(player.Stats.HP, 5);
            ok &= Expect(expectedHeal == (int)(player.Stats.HP * 0.05f),
                $"5x Leftovers heal should be 5% of max HP ({(int)(player.Stats.HP * 0.05f)}), got {expectedHeal}");
            int hpBeforeTurn = player.CurrentHP;
            controller.ExecuteTurn(new WaitAction());
            // Walking regen may add 1 on multiples of 4; turn 1 isn't one.
            ok &= Expect(player.CurrentHP == hpBeforeTurn + expectedHeal,
                $"Leftovers should heal {expectedHeal} on turn end, got {player.CurrentHP - hpBeforeTurn}");

            // --- Scope Lens: +5%/stack hard-capped at +50%.
            for (int i = 0; i < 20; i++) inv.AddItem(Items.ItemRegistry.Get("scope-lens"), log);
            ok &= Expect(Math.Abs(inv.CritChanceBonus() - Items.ScopeLens.HardCap) < 0.001f,
                $"20x Scope Lens should cap at +{Items.ScopeLens.HardCap:P0}, got {inv.CritChanceBonus():P1}");

            // --- Silk Scarf: +15%/stack on Normal moves only.
            inv.AddItem(Items.ItemRegistry.Get("silk-scarf"), log);
            inv.AddItem(Items.ItemRegistry.Get("silk-scarf"), log);
            int boosted = inv.ModifyOutgoingDamage(100, GameData.GetMove("tackle"));
            int unboosted = inv.ModifyOutgoingDamage(100, GameData.GetMove("ember"));
            ok &= Expect(boosted == 130, $"2x Silk Scarf on Normal 100 dmg should be 130, got {boosted}");
            ok &= Expect(unboosted == 100, $"Silk Scarf must not boost non-Normal moves, got {unboosted}");

            // --- Focus Sash: lethal hit leaves 1 HP, once per floor, resets on floor start.
            inv.AddItem(Items.ItemRegistry.Get("focus-sash"), log);
            int lethal = inv.ModifyLethalDamage(9999, player.CurrentHP, ctx);
            ok &= Expect(lethal == player.CurrentHP - 1, "Focus Sash should reduce lethal damage to HP-1");
            int secondLethal = inv.ModifyLethalDamage(9999, player.CurrentHP, ctx);
            ok &= Expect(secondLethal == 9999, "second lethal hit on the same floor should not be blocked");
            inv.OnFloorStart(ctx);
            ok &= Expect(inv.ModifyLethalDamage(9999, player.CurrentHP, ctx) == player.CurrentHP - 1,
                "Focus Sash charge should reset on floor start");

            // --- Choice Band: locks first move; other slots rejected; floor start unlocks.
            inv.AddItem(Items.ItemRegistry.Get("choice-band"), log);
            player.Facing = Direction.East; // empty tile — attacking air still locks
            ok &= Expect(controller.ExecuteTurn(new AttackAction(0)), "attack should execute with Choice Band");
            ok &= Expect(inv.LockedMoveId == player.Moves[0].Move.Id, "Choice Band should lock the first move used");
            ok &= Expect(!controller.ExecuteTurn(new AttackAction(1)), "Choice Band should reject other moves");
            ok &= Expect(log.Messages.Any(m => m.Contains("only allows")), "lock rejection should be logged");
            inv.OnFloorStart(ctx);
            ok &= Expect(inv.LockedMoveId == null, "Choice Band lock should clear on floor start");

            // --- Rocky Helmet: melee attacker takes flat reflect damage.
            inv.AddItem(Items.ItemRegistry.Get("rocky-helmet"), log);
            var biter = new Enemy(new Point(2, 3), GameData.GetSpecies("rattata"), 5);
            map.Actors.Add(biter);
            biter.Facing = Direction.North;
            var resolver = new CombatResolver(map, log, rng, player);
            int biterHP = biter.CurrentHP;
            resolver.ExecuteAttack(biter, biter.Moves[0]); // tackle (melee)
            ok &= Expect(biter.CurrentHP <= biterHP - Items.RockyHelmet.DamagePerStack,
                $"Rocky Helmet should reflect {Items.RockyHelmet.DamagePerStack} damage, biter went {biterHP} -> {biter.CurrentHP}");
            map.Actors.Remove(biter);

            // --- Ground pickup + Lum Berry active through the real turn loop.
            var pickupSpot = new Point(player.GridPosition.X, player.GridPosition.Y + 1);
            map.GroundItems.Add(new Items.GroundItem(pickupSpot, Items.ItemRegistry.Get("lum-berry")));
            ok &= Expect(controller.ExecuteTurn(new MoveAction(Direction.South)), "move onto item tile should execute");
            ok &= Expect(inv.Actives.Count == 1 && inv.Actives[0].Id == "lum-berry", "walk-over should pick up the Lum Berry");
            ok &= Expect(map.GroundItemAt(pickupSpot) == null, "picked-up item should leave the floor");

            player.ApplyStatus(StatusType.Poison, 5);
            ok &= Expect(controller.ExecuteTurn(new UseItemAction(0)), "using the Lum Berry should consume a turn");
            ok &= Expect(player.StatusType == StatusType.None, "Lum Berry should cure the status");
            ok &= Expect(inv.Actives.Count == 0, "active item should be consumed");

            if (ok) Console.WriteLine("Items: stacking math, caps, Sash/Band per-floor state, Rocky Helmet, pickup & actives — OK");
            return ok;
        }

        // ------------------------------------------------------------------
        // Status condition tests (Phase 4)
        // ------------------------------------------------------------------

        private static bool StatusGoldenTests()
        {
            bool ok = true;

            // Status move data parsed correctly.
            MoveDefinition sleepPowder = GameData.GetMove("sleep-powder");
            ok &= Expect(sleepPowder.Category == MoveCategory.Status, "sleep-powder should be a Status move");
            ok &= Expect(sleepPowder.InflictStatus == StatusType.Sleep, "sleep-powder should inflict Sleep");
            ok &= Expect(GameData.GetMove("ember").InflictStatus == StatusType.Burn, "ember should carry a burn chance");

            // One status at a time; cure works.
            var dummy = new Enemy(new Point(1, 1), GameData.GetSpecies("rattata"), 5);
            ok &= Expect(dummy.ApplyStatus(StatusType.Poison, 5), "applying poison to a clean actor should succeed");
            ok &= Expect(!dummy.ApplyStatus(StatusType.Burn, 5), "second status should be rejected");
            dummy.CureStatus();
            ok &= Expect(dummy.StatusType == StatusType.None, "CureStatus should clear the condition");

            // Poison ticks 1/8 max HP (min 1) and expires after its duration.
            var arena = new DungeonMap(5, 5);
            for (int x = 1; x < 4; x++)
                for (int y = 1; y < 4; y++)
                    arena.SetTile(new Point(x, y), TileType.Floor);
            var victim = new Enemy(new Point(1, 1), GameData.GetSpecies("geodude"), 10);
            var bystanderPlayer = new Player(new Point(3, 3), GameData.GetSpecies("charmander"), 5);
            arena.Actors.Add(bystanderPlayer);
            arena.Actors.Add(victim);

            var resolver = new CombatResolver(arena, new MessageLog(), new Rng(7), bystanderPlayer);
            victim.ApplyStatus(StatusType.Poison, StatusRules.DurationFor(StatusType.Poison));
            int expectedTick = Math.Max(1, victim.Stats.HP / 8);
            int hpBefore = victim.CurrentHP;
            resolver.TickStatuses();
            ok &= Expect(victim.CurrentHP == hpBefore - expectedTick,
                $"poison tick expected {expectedTick}, got {hpBefore - victim.CurrentHP}");

            for (int i = 0; i < StatusRules.DurationFor(StatusType.Poison); i++) resolver.TickStatuses();
            ok &= Expect(victim.StatusType == StatusType.None, "poison should wear off after its duration");

            // Sleep always skips; paralysis skips ~25%.
            victim.ApplyStatus(StatusType.Sleep, 3);
            ok &= Expect(StatusRules.ActionSkipped(victim, new Rng(1), out _), "sleep should always skip the action");
            victim.CureStatus();

            victim.ApplyStatus(StatusType.Paralysis, 999);
            var procRng = new Rng(99);
            int skips = 0;
            const int rolls = 2000;
            for (int i = 0; i < rolls; i++)
            {
                if (StatusRules.ActionSkipped(victim, procRng, out _)) skips++;
            }
            float rate = (float)skips / rolls;
            ok &= Expect(rate > 0.20f && rate < 0.30f, $"paralysis skip rate expected ~0.25, got {rate:F3}");
            victim.CureStatus();

            // Burn halves physical damage (same RNG seed → identical crit/roll).
            var attacker = new Enemy(new Point(2, 2), GameData.GetSpecies("machop"), 20);
            MoveDefinition chop = GameData.GetMove("karate-chop");
            int healthy = DamageCalculator.Calculate(attacker, victim, chop, new Rng(5)).Damage;
            attacker.ApplyStatus(StatusType.Burn, 5);
            int burned = DamageCalculator.Calculate(attacker, victim, chop, new Rng(5)).Damage;
            ok &= Expect(burned <= healthy / 2 + 1 && burned < healthy,
                $"burned physical damage should be ~half ({healthy} -> {burned})");

            if (ok) Console.WriteLine("Status: poison tick, sleep/paralysis skips, burn halving, move data — OK");
            return ok;
        }

        // ------------------------------------------------------------------
        // Procgen / movement checks (Phases 1-2)
        // ------------------------------------------------------------------

        private static void PrintMap(DungeonMap map)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new Point(x, y);
                    Actor actor = map.GetActorAt(p);
                    sb.Append(actor switch
                    {
                        Player => '@',
                        Enemy => 'e',
                        _ => map.GetTile(p).Type switch
                        {
                            TileType.Stairs => '>',
                            TileType.Floor => '.',
                            _ => '#'
                        }
                    });
                }
                sb.AppendLine();
            }
            Console.Write(sb.ToString());
        }

        /// <summary>Every floor tile must be reachable from the player spawn (4-directional BFS).</summary>
        private static bool CheckConnectivity(DungeonMap map, Point start)
        {
            int totalFloor = 0;
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                    if (map.GetTile(new Point(x, y)).IsWalkable) totalFloor++;

            var visited = new HashSet<Point> { start };
            var queue = new Queue<Point>();
            queue.Enqueue(start);
            Point[] cardinals = { new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                foreach (Point offset in cardinals)
                {
                    Point next = current + offset;
                    if (map.IsWalkable(next) && visited.Add(next)) queue.Enqueue(next);
                }
            }

            bool ok = visited.Count == totalFloor;
            Console.WriteLine($"Connectivity: {visited.Count}/{totalFloor} floor tiles reachable from spawn — {(ok ? "OK" : "FAIL")}");
            return ok;
        }

        /// <summary>
        /// Drive the real TurnController with random (legal) player moves and check
        /// that no actor ever ends up inside a wall or sharing a tile. Enemies now
        /// fight back, so the loop also tolerates (and reports) a player defeat.
        /// </summary>
        private static bool SimulateTurns(DungeonMap map, Player player, Rng rng, int turns)
        {
            var controller = new TurnController(map, player, rng, new MessageLog());
            Direction[] allDirections =
            {
                Direction.North, Direction.NorthEast, Direction.East, Direction.SouthEast,
                Direction.South, Direction.SouthWest, Direction.West, Direction.NorthWest
            };

            int executed = 0;
            for (int i = 0; i < turns; i++)
            {
                if (controller.PlayerDefeated)
                {
                    Console.WriteLine($"Turn simulation: player was defeated after {executed} turns (valid outcome)");
                    return true;
                }

                var options = new List<Direction>();
                foreach (Direction dir in allDirections)
                {
                    Point target = player.GridPosition + dir.ToOffset();
                    if (map.CanMove(player.GridPosition, dir) && !map.IsOccupied(target)) options.Add(dir);
                }

                TurnAction action = options.Count > 0 ? new MoveAction(rng.Pick(options)) : new WaitAction();
                if (controller.ExecuteTurn(action)) executed++;

                string violation = FindInvariantViolation(map);
                if (violation != null)
                {
                    Console.WriteLine($"Turn simulation: FAIL on turn {i + 1} — {violation}");
                    return false;
                }
            }

            Console.WriteLine($"Turn simulation: {executed}/{turns} turns executed, invariants held — OK");
            return true;
        }

        private static string FindInvariantViolation(DungeonMap map)
        {
            var seen = new HashSet<Point>();
            foreach (Actor actor in map.Actors)
            {
                if (!map.IsWalkable(actor.GridPosition))
                    return $"{actor.GetType().Name} is standing in a wall at {actor.GridPosition}";
                if (!seen.Add(actor.GridPosition))
                    return $"two actors share tile {actor.GridPosition}";
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Full-run descent (Phase 2)
        // ------------------------------------------------------------------

        /// <summary>
        /// Walk an entire run headlessly: every floor of every defined dungeon,
        /// pathing the player to the stairs through the real TurnController and
        /// advancing through the RunManager until victory. Enemies are omitted so
        /// pathing is deterministic.
        /// </summary>
        private static bool SimulateFullRun(int seed)
        {
            var rng = new Rng(seed + 1);
            var dungeons = DungeonRegistry.Load();
            var run = new RunManager(dungeons);

            int expectedFloors = 0;
            foreach (var d in dungeons) expectedFloors += d.Floors;

            int floorsCleared = 0;
            while (true)
            {
                GeneratedFloor floor = new DungeonGenerator(rng).Generate(run.CurrentDungeon);
                DungeonMap map = floor.Map;
                string where = $"{run.CurrentDungeon.Name} F{run.FloorNumber}";

                if (!map.IsWalkable(map.StairsPosition) || map.GetTile(map.StairsPosition).Type != TileType.Stairs)
                {
                    Console.WriteLine($"Full run: FAIL — no stairs on {where}");
                    return false;
                }

                var player = new Player(floor.PlayerSpawn, GameData.GetSpecies("charmander"), 5);
                map.Actors.Add(player);
                var controller = new TurnController(map, player, rng, new MessageLog());

                List<Direction> path = FindPath(map, floor.PlayerSpawn, map.StairsPosition);
                if (path == null)
                {
                    Console.WriteLine($"Full run: FAIL — stairs unreachable on {where}");
                    return false;
                }

                foreach (Direction step in path)
                {
                    if (!controller.ExecuteTurn(new MoveAction(step)))
                    {
                        Console.WriteLine($"Full run: FAIL — pathing move rejected on {where}");
                        return false;
                    }
                }

                if (player.GridPosition != map.StairsPosition)
                {
                    Console.WriteLine($"Full run: FAIL — path did not end on stairs on {where}");
                    return false;
                }

                run.AddTurns(controller.TurnCount);
                floorsCleared++;

                if (run.Advance() == AdvanceResult.Victory) break;
            }

            bool ok = floorsCleared == expectedFloors;
            Console.WriteLine($"Full run: {floorsCleared}/{expectedFloors} floors cleared across {dungeons.Count} dungeons in {run.TotalTurns} turns — {(ok ? "OK" : "FAIL")}");
            return ok;
        }

        /// <summary>BFS over legal moves (8-directional, honoring corner-cut rules) from start to goal.</summary>
        private static List<Direction> FindPath(DungeonMap map, Point start, Point goal)
        {
            Direction[] allDirections =
            {
                Direction.North, Direction.NorthEast, Direction.East, Direction.SouthEast,
                Direction.South, Direction.SouthWest, Direction.West, Direction.NorthWest
            };

            var cameFrom = new Dictionary<Point, (Point parent, Direction step)>();
            var queue = new Queue<Point>();
            queue.Enqueue(start);
            cameFrom[start] = (start, Direction.None);

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                if (current == goal) break;

                foreach (Direction dir in allDirections)
                {
                    if (!map.CanMove(current, dir)) continue;
                    Point next = current + dir.ToOffset();
                    if (cameFrom.ContainsKey(next)) continue;
                    cameFrom[next] = (current, dir);
                    queue.Enqueue(next);
                }
            }

            if (!cameFrom.ContainsKey(goal)) return null;

            var path = new List<Direction>();
            Point node = goal;
            while (node != start)
            {
                (Point parent, Direction step) = cameFrom[node];
                path.Add(step);
                node = parent;
            }
            path.Reverse();
            return path;
        }
    }
}
