using System.Collections.Generic;

namespace PMDRoguelike.Core
{
    /// <summary>
    /// Tiny one-way bus from game logic to the audio layer. Logic posts cue names
    /// (matching Content/Audio/Sfx asset names); the AudioManager drains and plays
    /// them each frame. Headless runs post into the capped queue and nothing drains —
    /// no audio hardware ever touches the logic path.
    /// </summary>
    public static class AudioCues
    {
        private const int Capacity = 32;
        private static readonly Queue<string> _cues = new();

        public static void Post(string cue)
        {
            if (_cues.Count < Capacity) _cues.Enqueue(cue);
        }

        public static bool TryDequeue(out string cue)
        {
            if (_cues.Count > 0)
            {
                cue = _cues.Dequeue();
                return true;
            }
            cue = null;
            return false;
        }

        public static int Count => _cues.Count;

        public static void Clear() => _cues.Clear();
    }
}
