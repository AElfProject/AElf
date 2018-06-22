using System;
using AElf.Kernel;

namespace AElf.Test.ChainCreator
{
    public class BlockFactory
    {
        public Block CreateBlock(Hash prev)
        {
            int incrId = 1;
            var block = new Block(prev);

            Transaction t = GetTransaction(incrId++);
            block.AddTransaction(t.GetHash());
            block.FullTransactions.Add(t);

            Transaction t2 = GetTransaction(incrId++);
            block.AddTransaction(t2.GetHash());
            block.FullTransactions.Add(t2);
            
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.ChainId = Convert.FromBase64String("CiCu2grxhpvieFLigl0jyjxSqiy5roUKXT6cAbjYvWBE0g==");
            
            return block;
        }

        public Transaction GetTransaction(int incr)
        {
            Transaction t = new Transaction();
            t.From = new byte[] { 0x01, 0x02 };
            t.To = new byte[] { 0x03, 0x04 };
            t.IncrementId = (ulong)incr;

            return t;
        }
    }
}