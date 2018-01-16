using System;
using System.Collections.Generic;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class PipelineTest
    {
        [Fact]
        public void InterfacesPipeline()
        {
            //At first we need to initial a chain with nothing.
            var chain = new Mock<IChain>();

            //Then add a genesis block to the chain.
            var chainManager = new Mock<IChainManager>();
            chainManager.Setup(manager => manager.AddBlockAsync(
                chain.Object, It.IsAny<IBlock>()));

            //So we can start a loop now:
            //The user create a transaction and broadcast it after verification.
            var txSender = new Mock<ITransactionSender>();
            txSender.Setup(sender => sender.VerifyTransaction(
                It.IsAny<ITransaction>())).Returns(true);
            txSender.Setup(sender => sender.BroadcastTransanction(It.IsAny<ITransaction>()));

            //The tx receiver receive transactions
            var txReceiver = new Mock<ITransactionReceiver>();
            txReceiver.Setup(receiver => receiver.GetTransactions());

            //The block producer use these transactions to produce a block,
            var blockProducer = new Mock<IBlockProducer>();
            var block = new Mock<IBlock>();
            blockProducer.Setup(producer => producer.CreateBlock()).Returns(block.Object);

            //The block producer excute all the transactions.
            var txExcute = new Mock<ITransactionExecutingManager>();
            foreach (var tx in block.Object.GetBody().GetTransactions())
            {
                txExcute.Object.ExecuteAsync(tx);
            }

            //TODO: collect results from workers


            //The block producer (block sender) broadcast the block,
            //and use chain manager to add it to the chain.
            var blockSender = new Mock<IBlockSender>();
            blockSender.Setup(sender => sender.BroadcastBlock(block.Object));
            chainManager.Object.AddBlockAsync(chain.Object, block.Object);

            //Turn to another block producer to start a new loop.
        }


        #region Old version but helpful
        [Fact]
        public void BasicPipeline()
        {
            var blkheader = new Mock<IBlockHeader>();
            var blk = new Mock<IBlock>();
            blk.Setup(b => b.AddTransaction(It.IsAny<ITransaction>())).Returns(true);
            blk.Setup(b => b.GetHeader()).Returns(blkheader.Object);

            var hash = new Mock<IHash>();
            hash.Setup(p => p.GetHashBytes()).Returns(new byte[] { 1, 2, 3 });

            var merkletree = new Mock<IMerkleTree<ITransaction>>();
            merkletree.Setup(m => m.AddNode(It.IsAny<IHash<ITransaction>>()));

            var miner = new Mock<IMiner>();
            miner.Setup(m => m.Mine(It.IsAny<IBlockHeader>())).Returns(new byte[] { 4, 5, 6 });

            var chainmgr = new Mock<IChainManager>();
            chainmgr.Setup(c => c.AddBlockAsync(It.IsAny<IChain>(), It.IsAny<IBlock>()));

            var chain = new Mock<IChain>();

            // basic pipeline
            var tx = new Mock<ITransaction>();
            blk.Object.AddTransaction(tx.Object);
            miner.Object.Mine(blk.Object.GetHeader());
            chainmgr.Object.AddBlockAsync(chain.Object, blk.Object);
        }
        #endregion
    }
    

}
