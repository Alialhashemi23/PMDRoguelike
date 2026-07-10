using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace PMDRoguelike.Core
{
    public enum PopupKind
    {
        Damage,       // white
        DamageCrit,   // yellow, bigger
        DamagePlayer, // red — the player got hurt
        Miss
    }

    public struct VisualEvent
    {
        public PopupKind Kind;
        public string Text;
        public Point Tile;
        /// <summary>Camera shake strength (0 = none). Intensity >= 5 also causes hit-stop.</summary>
        public float Shake;
    }

    /// <summary>
    /// One-way bus from game logic to the presentation layer (same pattern as
    /// AudioCues): combat posts damage popups and shake requests, DungeonState
    /// drains them each frame. Capped so headless runs never grow it.
    /// </summary>
    public static class VisualEvents
    {
        private const int Capacity = 64;
        private static readonly Queue<VisualEvent> _events = new();

        public static void Post(VisualEvent e)
        {
            if (_events.Count < Capacity) _events.Enqueue(e);
        }

        public static void PostDamage(Point tile, int amount, PopupKind kind, float shake = 0f) =>
            Post(new VisualEvent { Kind = kind, Text = amount.ToString(), Tile = tile, Shake = shake });

        public static void PostMiss(Point tile) =>
            Post(new VisualEvent { Kind = PopupKind.Miss, Text = "MISS", Tile = tile });

        public static bool TryDequeue(out VisualEvent e)
        {
            if (_events.Count > 0)
            {
                e = _events.Dequeue();
                return true;
            }
            e = default;
            return false;
        }

        public static int Count => _events.Count;

        public static void Clear() => _events.Clear();
    }
}
