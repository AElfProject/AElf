using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.CrossChain;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using AElf.Configuration;

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
            _contract = new SideChainContractShim(_mock, AddressHelpers.GetSystemContractAddress(_mock.ChainId1, SmartContractType.SideChainContract.ToString()));
        }

        [Fact]
        public async Task SideChainLifetime()
        {
//            var chainId = Hash.Generate();
            var chainId = Hash.FromString("Chain1");
            var lockedAddress = Address.FromRawBytes(Hash.FromString("LockedAddress1").ToByteArray());
            ulong lockedToken = 10000;
            // create new chain
            var bytes = await _contract.CreateSideChain(chainId, lockedAddress, lockedToken);
            Assert.Equal(chainId.DumpByteArray(), bytes);

            // check status
            var status = await _contract.GetChainStatus(chainId);
            Assert.Equal(1, status);

            var sn = await _contract.GetCurrentSideChainSerialNumber();
            Assert.Equal(1, (int) sn);

            var tokenAmount = await _contract.GetLockedToken(chainId);
            Assert.Equal(lockedToken, tokenAmount);

            var address = await _contract.GetLockedAddress(chainId);
            Assert.Equal(lockedAddress.DumpByteArray(), address);
            
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
            var chainId = Hash.FromString("Chain1");
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
                Path = {Hash.FromString("Block1"), Hash.FromString("Block2"), Hash.FromString("Block3")}
            });
            await _contract.WriteParentChainBLockInfo(parentChainBlockInfo);
            NodeConfig.Instance.ChainId = _mock.ChainId1.DumpHex();
            var crossChainInfo = new CrossChainInfo(_mock.StateStore);
            var merklepath = crossChainInfo.GetTxRootMerklePathInParentChain(0);
            Assert.NotNull(merklepath);
            Assert.Equal(parentChainBlockInfo.IndexedBlockInfo[0], merklepath);

            var boundHeight = crossChainInfo.GetBoundParentChainHeight(0);
            Assert.Equal(parentChainBlockRootInfo.Height, boundHeight);

            var boundBlockInfo = crossChainInfo.GetBoundParentChainBlockInfo(parentChainBlockRootInfo.Height);
            Assert.Equal(parentChainBlockInfo, boundBlockInfo);
        }

        [Fact]
        public async Task VerifyTransactionTest()
        {
            Transaction t = new Transaction
            {
                From = Address.FromString("1"),
                To = Address.FromString("2"),
                MethodName = "test",
                Sig = new Signature
                {
                    P = ByteString.Empty,
                    R = ByteString.Empty,
                },
                Params = ByteString.Empty,
                RefBlockNumber = 0,
                RefBlockPrefix = ByteString.Empty
            };
            var list = new List<Hash> {t.GetHash(), Hash.FromString("a"), Hash.FromString("b"), Hash.FromString("c")};
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(list);
            var root = bmt.ComputeRootHash();
            var chainId = Hash.Generate();
            ParentChainBlockRootInfo parentChainBlockRootInfo = new ParentChainBlockRootInfo
            {
                ChainId = chainId,
                Height = 2,
                SideChainTransactionsRoot = root,
                SideChainBlockHeadersRoot = Hash.FromString("SideChainBlockHeadersRoot")
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