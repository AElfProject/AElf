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

        static Network()
        {
            Block block = new Block(new Hash<IBlock>("aelf".GetSHA256Hash()), new Hash<IAccount>("2018".GetSHA256Hash()));
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
                IHash<ITransaction> hash = new Hash<ITransaction>(buffer.GetSHA256Hash());
                leaves.Add(hash);
            }
            return leaves;
        }
    }

    public static class QueueExtensions
    {
        public static bool TryDequeue<T>(this Queue<T> queue, out T t)
        {
            if (queue.Count > 0)
            {
                t = queue.Dequeue();
                return true;
            }
            else
            {
                t = default(T);
                return false;
            }
        }
    }
}
