using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
{
    /// <summary>
    /// Not real for now.
    /// </summary>
    public class Network
    {
        private static Queue<ITransaction> _transactions = new Queue<ITransaction>();

        public static int TransactionCount => _transactions.Count;

        public static Chain Chain { get; set; } = new Chain();

        public static ChainManager ChainManager { get; set; } = new ChainManager();
        ///// <summary>
        ///// Add some transactions.
        ///// </summary>
        //public void Initialazation()
        //{
        //    new List<string> { "a", "e", "l", "f" }.ForEach(
        //        str => _transactions.Enqueue(new Transaction() { Data = str }));
        //}

        static Network()
        {
            Block block = new Block(new Hash<IBlock>("aelf".GetHash()));
            Miner miner = new Miner();
            MerkleTree<ITransaction> tree = new MerkleTree<ITransaction>();
            CreateLeaves(new string[] { "a", "e", "l", "f" }).ForEach(l => block.GetHeader().AddTransaction(l));

            ChainManager.AddBlockAsync(Chain, block);
        }

        /// <summary>
        /// Simulation of sending a transaction.
        /// </summary>
        /// <param name="tx"></param>
        public static void BroadcastTransaction(ITransaction tx)
        {
            _transactions.Enqueue(tx);
        }

        /// <summary>
        /// Simulation of receiving a transaction.
        /// </summary>
        /// <returns></returns>
        public static ITransaction ReceiveTransaction()
        {
            ITransaction tx;
            if (_transactions.TryDequeue(out tx))
            {
                return tx;
            }
            return null;
        }


        private static List<IHash<ITransaction>> CreateLeaves(string[] buffers)
        {
            List<IHash<ITransaction>> leaves = new List<IHash<ITransaction>>();
            foreach (var buffer in buffers)
            {
                IHash<ITransaction> hash = new Hash<ITransaction>(buffer.GetHash());
                leaves.Add(hash);
            }
            return leaves;
        }
    }
}
