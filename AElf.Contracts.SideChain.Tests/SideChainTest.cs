using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Node.CrossChain;
using AElf.SmartContract;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.SideChain.Tests
{
    [UseAutofacTestFramework]
    public class SideChainTest
    {
        private SideChainContractShim _contract;
        private MockSetup _mock;

        public SideChainTest(MockSetup mock)
        {
            _mock = mock;
            Init();
        }

        private void Init()
        {
            _contract = new SideChainContractShim(_mock, 
                new Hash(_mock.ChainId1.CalculateHashWith(SmartContractType.SideChainContract.ToString())).ToAccount());
        }

        [Fact]
        public async Task SideChainLifetime()
        {
            var chainId = Hash.Generate();
            var lockedAddress = Hash.Generate().ToAccount();
            ulong lockedToken = 10000;
            // create new chain
            var bytes = await _contract.CreateSideChain(chainId, lockedAddress, lockedToken);
            Assert.Equal(chainId.GetHashBytes(), bytes);

            // check status
            var status = await _contract.GetChainStatus(chainId);
            Assert.Equal(1, status);

            var sn = await _contract.GetCurrentSideChainSerialNumber();
            Assert.Equal(1, (int) sn);

            var tokenAmount = await _contract.GetLockedToken(chainId);
            Assert.Equal(lockedToken, tokenAmount);

            var address = await _contract.GetLockedAddress(chainId);
            Assert.Equal(lockedAddress, address);
            
            // authorize the chain 
            await _contract.ApproveSideChain(chainId);
            Assert.True(_contract.TransactionContext.Trace.IsSuccessful());
            
            status = await _contract.GetChainStatus(chainId);
            Assert.Equal(2, status);
            
            // dispose 
            await _contract.DisposeSideChain(chainId);
            Assert.True(_contract.TransactionContext.Trace.IsSuccessful());
            
            status = await _contract.GetChainStatus(chainId);
            Assert.Equal(3, status);
        }

        [Fact]
        public async Task MerklePathTest()
        {
            var chainId = Hash.Generate();
            ParentChainBlockRootInfo parentChainBlockRootInfo = new ParentChainBlockRootInfo
            {
                ChainId = chainId,
                Height = 1,
                SideChainTransactionsRoot = Hash.Generate(),
                SideChainBlockHeadersRoot = Hash.Generate()
            };
            ParentChainBlockInfo parentChainBlockInfo = new ParentChainBlockInfo
            {
                Root = parentChainBlockRootInfo
            };
            parentChainBlockInfo.IndexedBlockInfo.Add(0, new MerklePath
            {
                Path = {Hash.Generate(), Hash.Generate(), Hash.Generate()}
            });
            await _contract.WriteParentChainBLockInfo(parentChainBlockInfo);
            var crossChainInfo = new CrossChainInfo(_mock.StateDictator);
            var merklepath = crossChainInfo.GetTxRootMerklePathInParentChain(_contract.SideChainContractAddress, 0);
            Assert.NotNull(merklepath);
            Assert.Equal(parentChainBlockInfo.IndexedBlockInfo[0], merklepath);

            var boundHeight = crossChainInfo.GetBoundParentChainHeight(_contract.SideChainContractAddress, 0);
            Assert.Equal(parentChainBlockRootInfo.Height, boundHeight);

            var boundBlockInfo = crossChainInfo.GetBoundParentChainBlockInfo(_contract.SideChainContractAddress,
                parentChainBlockRootInfo.Height);
            Assert.Equal(parentChainBlockInfo, boundBlockInfo);
        }

        [Fact]
        public async Task VerifyTransactionTest()
        {
            Transaction t = new Transaction
            {
                From = Hash.Generate(),
                To = Hash.Generate(),
                MethodName = "test",
                P = ByteString.Empty,
                Params = ByteString.Empty,
                R = ByteString.Empty,
                RefBlockNumber = 0,
                RefBlockPrefix = ByteString.Empty
            };
            var list = new List<Hash> {t.GetHash(), Hash.Generate(), Hash.Generate(), Hash.Generate()};
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(list);
            var root = bmt.ComputeRootHash();
            var chainId = Hash.Generate();
            ParentChainBlockRootInfo parentChainBlockRootInfo = new ParentChainBlockRootInfo
            {
                ChainId = chainId,
                Height = 2,
                SideChainTransactionsRoot = root,
                SideChainBlockHeadersRoot = Hash.Generate()
            };
            ParentChainBlockInfo parentChainBlockInfo = new ParentChainBlockInfo
            {
                Root = parentChainBlockRootInfo
            };
            
            parentChainBlockInfo.IndexedBlockInfo.Add(1, bmt.GenerateMerklePath(0));
            await _contract.WriteParentChainBLockInfo(parentChainBlockInfo);

            var b = await _contract.VerifyTransaction(t.GetHash(), bmt.GenerateMerklePath(0), parentChainBlockRootInfo.Height);
            Assert.True(b);
        }
    }
}