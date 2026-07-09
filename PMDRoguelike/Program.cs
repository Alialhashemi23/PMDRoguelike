using System;

// Headless smoke-test mode: generates a dungeon, verifies connectivity, and
// simulates turns without opening a window. Usage: dotnet run -- --dump-map [seed]
if (args.Length > 0 && args[0] == "--dump-map")
{
    int seed = args.Length > 1 && int.TryParse(args[1], out int parsed) ? parsed : Environment.TickCount;
    return PMDRoguelike.Debugging.SmokeTest.Run(seed);
}

using var game = new PMDRoguelike.PMDRogueGame();
game.Run();
return 0;
