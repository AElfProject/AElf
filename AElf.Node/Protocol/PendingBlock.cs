using System.Collections.Generic;
using System.Linq;
using AElf.ChainController;
using AElf.Common;
using AElf.Kernel;
using AElf.Network;
using AElf.Network.Peers;

namespace AElf.Node.Protocol
{
    public class PendingBlock
    {
        public bool IsRequestInProgress { get; set; } = false;
        
        public Block Block { get; }
        public IPeer Peer { get; set; }
        public AElfProtocolMsgType MsgType { get; set; }
        
        public ValidationError ValidationError { get; set; }

        public List<PendingTx> MissingTxs { get; private set; }

        public byte[] BlockHash { get; }

        public bool IsSynced => MissingTxs.Count == 0;

        public PendingBlock(byte[] blockHash, Block block, List<Hash> missing, AElfProtocolMsgType msgType)
        {
            Block = block;
            BlockHash = blockHash;
            MissingTxs = missing == null ? new List<PendingTx>() : missing.Select(m => new PendingTx {Hash = m.Value.ToByteArray()}).ToList();
            MsgType = msgType;
        }

        public void RemoveTransaction(byte[] txId)
        {
            MissingTxs.RemoveAll(ptx => ptx.Hash.BytesEqual(txId));
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