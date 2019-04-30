using System;

namespace AElf.CLI.JS.Crypto
{
    /// <summary>
    /// A pseudo random.
    /// </summary>
    /// TODO(yuyang): Does C# have a random device API will generate true random number?
    public class PseudoRandomGenerator : IRandomGenerator
    {
        private readonly Random _random;

        public PseudoRandomGenerator()
        {
            _random = new Random();
        }
        
        public int NextInt()
        {
            return _random.Next();
        }
    }
}