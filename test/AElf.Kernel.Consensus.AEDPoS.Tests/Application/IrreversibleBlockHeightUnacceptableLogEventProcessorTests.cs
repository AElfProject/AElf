using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Cryptography;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class IrreversibleBlockHeightUnacceptableLogEventProcessorTests : AEDPoSTestBase
    {
        [Fact]
        public async Task ProcessorTest()
        {
            var processor = GetRequiredService<IrreversibleBlockHeightUnacceptableLogEventProcessor>();
            var transactionPackingOptionProvider = GetRequiredService<ITransactionPackingOptionProvider>();
            var signer = CryptoHelper.GenerateKeyPair();

            var blockHeader = new BlockHeader
            {
                ChainId = 1,
                Height = 10,
                PreviousBlockHash = HashHelper.ComputeFrom(new byte[] {1, 2, 3}),
                Time = TimestampHelper.GetUtcNow(),
                MerkleTreeRootOfTransactions = Hash.Empty,
                MerkleTreeRootOfWorldState = Hash.Empty,
                MerkleTreeRootOfTransactionStatus = Hash.Empty,
                SignerPubkey = ByteString.CopyFrom(signer.PublicKey)
            };
            var block = new Block
            {
                Header = blockHeader
            };

            {
                var logEvent = new IrreversibleBlockHeightUnacceptable
                {
                    DistanceToIrreversibleBlockHeight = 10
                }.ToLogEvent();
                await processor.ProcessAsync(block, new Dictionary<TransactionResult, List<LogEvent>>
                {
                    {new TransactionResult(), new List<LogEvent> {logEvent}}
                });

                transactionPackingOptionProvider.IsTransactionPackable(new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height
                }).ShouldBeFalse();
            }

            {
                var logEvent = new IrreversibleBlockHeightUnacceptable
                {
                    DistanceToIrreversibleBlockHeight = 0
                }.ToLogEvent();
                await processor.ProcessAsync(block, new Dictionary<TransactionResult, List<LogEvent>>
                {
                    {new TransactionResult(), new List<LogEvent> {logEvent}}
                });

                transactionPackingOptionProvider.IsTransactionPackable(new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height
                }).ShouldBeTrue();
            }
        }
    }
}