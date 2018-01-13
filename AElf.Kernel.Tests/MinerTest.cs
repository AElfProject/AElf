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
            Block block = new Block(new Hash<IBlock>("aelf".GetSHA256Hash()));
            Miner miner = new Miner();

            MerkleTree<ITransaction> tree = new MerkleTree<ITransaction>();
            CreateLeaves(new string[] { "a", "e", "l", "f" }).ForEach(l => block.BlockHeader.AddTransaction(l));

            Assert.NotNull(miner.Mine(block.BlockHeader));
            //Assert.NotNull(block.BlockHeader.Nonce);
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
}
