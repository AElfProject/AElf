using System;
using System.Collections.Generic;
using AElf.Cryptography.ECDSA;
using AElf.Common;

namespace AElf.Kernel
{
    public interface IBlock : IHashProvider
    {
        byte[] GetHashBytes();
        bool AddTransaction(Transaction tx);
        BlockHeader Header { get; set; }
        BlockBody Body { get; set; }
        void FillTxsMerkleTreeRootInHeader();

        void Complete(DateTime currentBlockTime, SideChainBlockInfo[] indexedSideChainBlockInfo = null,
            HashSet<TransactionResult> results = null);
        bool AddTransactions(IEnumerable<Hash> txHashes);
        void Sign(ECKeyPair keyPair);
        ulong Index { get; set; }
        string BlockHashToHex { get; set; }
        Block Clone();
    }
}