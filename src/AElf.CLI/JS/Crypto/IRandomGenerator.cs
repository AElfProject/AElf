using System;

namespace AElf.CLI.JS.Crypto
{
    /// <summary>
    /// Random generator will generate the random seed.
    /// </summary>
    public interface IRandomGenerator
    {
        int NextInt();
    }
}