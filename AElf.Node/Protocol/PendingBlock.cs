using System.Collections.Generic;
using System.Linq;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Extensions;
using AElf.Kernel;
using AElf.Network.Peers;

namespace AElf.Node.Protocol
{
    public class PendingBlock
    {
        public bool IsRequestInProgress { get; set; } = false;
        
        public Block Block { get; }
        public IPeer Peer { get; set; }

        public List<PendingTx> MissingTxs { get; private set; }

        public byte[] BlockHash { get; }

        public bool IsSynced => MissingTxs.Count == 0;

        public PendingBlock(byte[] blockHash, Block block, List<Hash> missing)
        {
            Block = block;
            BlockHash = blockHash;

            MissingTxs = missing == null ? new List<PendingTx>() : missing.Select(m => new PendingTx {Hash = m.Value.ToByteArray()}).ToList();
        }

        public void RemoveTransaction(byte[] txid)
        {
            MissingTxs.RemoveAll(ptx => ptx.Hash.BytesEqual(txid));
        }

        public override string ToString()
        {
            return "{ " + BlockHash.ToHex() + ", " + IsSynced + ", " + Block?.Header?.Index + " }";
        }

        public class PendingTx
        {
            public byte[] Hash { get; set; }
            public bool IsRequestInProgress { get; set; } = false;
        }
    }
}