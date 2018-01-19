using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Xunit;


namespace AElf.Kernel.Tests
{
    public class MinerTest
    {
        [Fact]
        public void MineTest()
        {
            Block block = new Block(new Hash<IBlock>("aelf".GetHash()), new Hash<IAccount>("2018".GetHash()));
            Miner miner = new Miner();

            MerkleTree<ITransaction> tree = new MerkleTree<ITransaction>();
            CreateLeaves(new string[] { "a", "e", "l", "f" }).ForEach(l => block.GetHeader().AddTransaction(l));

            Assert.NotNull(miner.Mine(block.GetHeader()));
        }

        #region Some methods
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
        #endregion
    }
}
