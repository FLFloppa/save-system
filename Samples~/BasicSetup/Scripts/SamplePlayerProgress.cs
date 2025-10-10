using System;
using FLFloppa.SaveSystem;

namespace FLFloppa.SaveSystem.Samples
{
    [Serializable]
    public struct SamplePlayerProgress : ISaveReadable
    {
        public int levelIndex;
        public int coins;
        public string difficulty;

        public string ToReadableString()
        {
            return $"Level {levelIndex} · {coins} coins · {difficulty}";
        }
    }
}
