using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProviderTest : CrossChainTestBase
    {
//        private LogEvent CreateCrossChainLogEvent(byte[] topic, byte[] data)
//        {
//            return new LogEvent
//            {
//                Address = ContractHelpers.GetCrossChainContractAddress(0),
//                Topics =
//                {
//                    ByteString.CopyFrom(topic)
//                },
//                Data = ByteString.CopyFrom(data)
//            };
//        }
//
//        private TransactionResult CreateFakeTransactionResult(Hash txId, IEnumerable<LogEvent> logEvents)
//        {
//            return new TransactionResult
//            {
//                TransactionId = txId,
//                Logs = { logEvents }
//            };
//        }
//        
//        [Fact]
//        public async Task FillExtraData_NoEvent()
//        {
//            var block = new Block
//            {
//                Header = new BlockHeader(),
//                Body = new BlockBody()
//            };
//            var fakeTransactionResultManager = TransactionResultManager;
//            //    CrossChainTestHelper.FakeTransactionResultManager(new List<TransactionResult>());
//            var crossChainBlockExtraDataProvider = new CrossChainBlockExtraDataProvider(fakeTransactionResultManager);
//            await crossChainBlockExtraDataProvider.FillExtraDataAsync( block);
//            Assert.Null(block.Header.BlockExtraData);
//        }
//        
//        [Fact]
//        public async Task FillExtraData_NotFoundEvent()
//        {            
//            Hash txId1 = Hash.FromString("tx1");
//            var txRes1 =
//                CreateFakeTransactionResult(txId1, new []{CreateCrossChainLogEvent(new byte[0], new byte[0])});
//            txRes1.UpdateBloom();
//            
//            var block = new Block
//            {
//                Header = new BlockHeader
//                {
//                    Bloom = ByteString.CopyFrom(Bloom.AndMultipleBloomBytes(new[] {txRes1.Bloom.ToByteArray()}))
//                },
//                Body = new BlockBody()
//            };
//            block.Body.Transactions.AddRange(new []{ txId1});
//            var transactionResultManager = TransactionResultManager;
//            await transactionResultManager.AddTransactionResultAsync(txRes1);
//            var crossChainBlockExtraDataProvider = new CrossChainBlockExtraDataProvider(transactionResultManager);
//            await crossChainBlockExtraDataProvider.FillExtraDataAsync(block);
//            Assert.Null(block.Header.BlockExtraData);
//        }
//        
//        [Fact]
//        public async Task FillExtraData_OneEvent()
//        {
//
//            var fakeMerkleTreeRoot = Hash.FromString("SideChainTransactionsMerkleTreeRoot");
//            var publicKey = CrossChainTestHelper.GetPubicKey();
//            var data = ParamsPacker.Pack(fakeMerkleTreeRoot, new CrossChainBlockData(),
//                Address.FromPublicKey(publicKey));
//            var logEvent =
//                CreateCrossChainLogEvent(CrossChainConsts.CrossChainIndexingEventName.CalculateHash(), data);
//            
//            Hash txId1 = Hash.FromString("tx1");
//            var txRes1 = CreateFakeTransactionResult(txId1, new []{logEvent});
//            txRes1.UpdateBloom();
//            Hash txId2 = Hash.FromString("tx2");
//            var txRes2 = CreateFakeTransactionResult(txId2,
//                new[] {CreateCrossChainLogEvent(new byte[0], new byte[0])});
//            
//            Hash txId3 = Hash.FromString("tx3");
//            var txRes3 = CreateFakeTransactionResult(txId3,
//                new[] {CreateCrossChainLogEvent(new byte[0], new byte[0])});
//            
//            var block = new Block
//            {
//                Header = new BlockHeader
//                {
//                    Bloom = ByteString.CopyFrom(Bloom.AndMultipleBloomBytes(new[] {txRes1.Bloom.ToByteArray()}))
//                },
//                Body = new BlockBody()
//            };
//            block.Body.Transactions.AddRange(new []{ txId1, txId2, txId3});
//            block.Sign(publicKey, b => Task.FromResult(CrossChainTestHelper.Sign(b)));
//
//            var transactionResultManager = TransactionResultManager;
//            await transactionResultManager.AddTransactionResultAsync(txRes1);
//            await transactionResultManager.AddTransactionResultAsync(txRes2);
//            await transactionResultManager.AddTransactionResultAsync(txRes3);
//            var crossChainBlockExtraDataProvider = new CrossChainBlockExtraDataProvider(transactionResultManager);
//            await crossChainBlockExtraDataProvider.FillExtraDataAsync(block);
//            Assert.Equal(fakeMerkleTreeRoot, block.Header.BlockExtraData.SideChainTransactionsRoot);
//        }
//        
//        [Fact]
//        public async Task FillExtraData_MultiEventsInOneTransaction()
//        {            
//            var fakeMerkleTreeRoot = Hash.FromString("SideChainTransactionsMerkleTreeRoot");
//            var publicKey = CrossChainTestHelper.GetPubicKey();
//            var data = ParamsPacker.Pack(fakeMerkleTreeRoot, new CrossChainBlockData(),
//                Address.FromPublicKey(publicKey));
//            var interestedLogEvent =
//                CreateCrossChainLogEvent(CrossChainConsts.CrossChainIndexingEventName.CalculateHash(), data);
//            
//            Hash txId = Hash.FromString("tx1");
//            var txRes = CreateFakeTransactionResult(txId,
//                new[] {CreateCrossChainLogEvent(new byte[0], new byte[0]), interestedLogEvent});
//            txRes.UpdateBloom();
//            
//            var fakeBlock = new Block
//            {
//                Header = new BlockHeader
//                {
//                    Bloom = ByteString.CopyFrom(Bloom.AndMultipleBloomBytes(new[] {txRes.Bloom.ToByteArray()}))
//                },
//                Body = new BlockBody()
//            };
//            
//            fakeBlock.Body.Transactions.AddRange(new []{ txId});
//            fakeBlock.Sign(publicKey, b => Task.FromResult(CrossChainTestHelper.Sign(b)));
//
//            var fakeTransactionResultManager = TransactionResultManager;
//            await fakeTransactionResultManager.AddTransactionResultAsync(txRes);
//            var crossChainBlockExtraDataProvider = new CrossChainBlockExtraDataProvider(fakeTransactionResultManager);
//            await crossChainBlockExtraDataProvider.FillExtraDataAsync(fakeBlock);
//            Assert.Equal(fakeMerkleTreeRoot, fakeBlock.Header.BlockExtraData.SideChainTransactionsRoot);
//        }
//        
//        [Fact]
//        public async Task FillExtraData_OneEvent_WithWrongData()
//        {
//            int wrongHash = 123; // which should be Hash type
//            var data = ParamsPacker.Pack(wrongHash, new CrossChainBlockData());
//            var logEvent =
//                CreateCrossChainLogEvent(CrossChainConsts.CrossChainIndexingEventName.CalculateHash(), data);
//            
//            Hash txId1 = Hash.FromString("tx1");
//            var txRes1 = CreateFakeTransactionResult(txId1, new []{logEvent});
//            txRes1.UpdateBloom();
//            
//            var block = new Block
//            {
//                Header = new BlockHeader
//                {
//                    Bloom = ByteString.CopyFrom(Bloom.AndMultipleBloomBytes(new[] {txRes1.Bloom.ToByteArray()}))
//                },
//                Body = new BlockBody()
//            };
//            block.Body.Transactions.AddRange(new[] {txId1});
//            var transactionResultManager = TransactionResultManager;
//            await transactionResultManager.AddTransactionResultAsync(txRes1);
//            var crossChainBlockExtraDataProvider = new CrossChainBlockExtraDataProvider(transactionResultManager);
//            await crossChainBlockExtraDataProvider.FillExtraDataAsync(block);
//            Assert.Null(block.Header.BlockExtraData);
//        }
    }
}