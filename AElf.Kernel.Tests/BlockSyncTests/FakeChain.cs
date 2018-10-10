using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Tests.BlockSyncTests
{
    public class FakeChain
    {
        private byte[] _from = { 1, 2, 3}; 
        private byte[] _to = { 3, 4, 5}; 
        
        private Dictionary<Block, List<Transaction>> Txs { get; set; }
        public List<Block> Chain { get; private set; }

        public int ChainHeight { get; private set; }

        public FakeChain(int height)
        {
            ChainHeight = height;
            Chain = new List<Block>();
            Txs = new Dictionary<Block, List<Transaction>>();
        }

        public void Generate()
        {
            for (ulong i = 0; i < (ulong)ChainHeight; i++)
            {
                // todo gen blocks and transactions
                Block b = BlockSyncHelpers.GenerateValidBlockToSync(i);
                Chain.Add(b);

                var txList = new List<Transaction>();
                Txs.TryAdd(b, txList);

                for (ulong j = 0; j < 3; j++)
                {
                    Transaction t = new Transaction();
                    t.From = Address.FromRawBytes(Hash.FromRawBytes(_from).ToByteArray());
                    t.To = Address.FromRawBytes(Hash.FromRawBytes(_to).ToByteArray());
                    t.IncrementId = j;
                    
                    txList.Add(t);
                    b.AddTransaction(t.GetHash());
                }
            }
        }

        public List<Transaction> GetBlockTransactions(Block b)
        {
            return Txs[b];
        }

        public Block GetAtHeight(int height)
        {
            return Chain.ElementAt(height);
        }
    }
}