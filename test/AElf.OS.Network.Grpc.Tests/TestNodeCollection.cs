using System.Collections.Generic;
using AElf.Cryptography.ECDSA;
using Virgil.Crypto;

namespace AElf.OS.Network
{
    /// <summary>
    /// A helper class that represents multiple nodes.
    /// </summary>
    public class TestNodeCollection
    {
        public List<ECKeyPair> MinerNodes { get; }
        public List<ECKeyPair> OtherNodes { get; }

        public TestNodeCollection()
        {
            MinerNodes = new List<ECKeyPair>();
            OtherNodes = new List<ECKeyPair>();
        }
    }
}