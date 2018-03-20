using System;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Merkle;
using Moq;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class PipelineTests
    {
        private ISmartContractZero _smartContractZero;

        public PipelineTests(ISmartContractZero smartContractZero)
        {
            _smartContractZero = smartContractZero;
        }

        [Fact]
        public void BasicPipelineTest()
        {
            //var smartContract = new SmartContractZero();
            var builder = new GenesisBlockBuilder().Build(_smartContractZero);

            //TODO: finish the unit test
            /*var blkheader = new Mock<IBlockHeader>();
            var blk = new Mock<IBlock>();
            blk.Setup(b => b.AddTransaction(It.IsAny<ITransaction>())).Returns(true);
            blk.Setup(b => b.GetHeader()).Returns(blkheader.Object);

            var hash = new Mock<IHash>();
            hash.Setup(p => p.GetHashBytes()).Returns(new byte[] { 1, 2, 3 });

            var merkletree = new Mock<IMerkleTree<ITransaction>>();
            merkletree.Setup(m => m.AddNode(It.IsAny<IHash<ITransaction>>()));

            var miner = new Mock<IMiner>();
            miner.Setup(m => m.Mine(It.IsAny<IBlockHeader>())).Returns(new byte[] {4,5,6});

            var chainmgr = new Mock<IChainManager>();
            chainmgr.Setup(c => c.AddBlockAsync(It.IsAny<IChain>(), It.IsAny<IBlock>()));

            var chain = new Mock<IChain>();

            // basic pipeline
            var tx = new Mock<ITransaction>();
            blk.Object.AddTransaction(tx.Object);
            miner.Object.Mine(blk.Object.GetHeader());
            chainmgr.Object.AddBlockAsync(chain.Object,blk.Object);*/
        }
    }
}
