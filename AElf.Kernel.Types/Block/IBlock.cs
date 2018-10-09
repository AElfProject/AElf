using System.Collections.Generic;
using AElf.Cryptography.ECDSA;
using AElf.Common;

namespace AElf.Kernel
{
    public interface IBlock : IHashProvider, ISerializable
    {
        bool AddTransaction(Hash tx);
        BlockHeader Header { get; set; }
        BlockBody Body { get; set; }
        void FillTxsMerkleTreeRootInHeader();
        Block Complete();
        bool AddTransactions(IEnumerable<Hash> txHashes);
        void Sign(ECKeyPair keyPair);
    }
}