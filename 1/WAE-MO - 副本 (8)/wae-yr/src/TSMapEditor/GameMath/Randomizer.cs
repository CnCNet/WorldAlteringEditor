using System;

namespace TSMapEditor.GameMath
{
    /// <summary>
    /// A random number generator.
    /// </summary>
    public class Randomizer
    {
        public Randomizer()
        {
            random = new Random();
        }

        private Random random;

        public void Init(int? seed)
        {
            if (seed == null)
                random = new Random();
            else
                random = new Random(seed.Value);
        }

        public int GetRandomNumber()
        {
            return random.Next();
        }

        public int GetRandomNumber(int min, int max) => random.Next(min, max + 1);
    }
}
