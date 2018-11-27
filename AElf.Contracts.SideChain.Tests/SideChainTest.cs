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
using NLog;
using AElf.Configuration.Config.Chain;
using AElf.Miner.TxMemPool;

namespace AElf.Contracts.SideChain.Tests
{
    [UseAutofacTestFramework]
    public class SideChainTest
    {
        private SideChainContractShim _contract;
        private ILogger _logger;
        private MockSetup Mock;

        public SideChainTest(ILogger logger)
        {
            _logger = logger;
        }

        private void Init()
        {
            Mock = new MockSetup(_logger);
        }

        [Fact]
        public async Task SideChainLifetime()
        {
            Init();
            _contract = new SideChainContractShim(Mock, ContractHelpers.GetSideChainContractAddress(Mock.ChainId1));
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
            Init();
            var chainId = Mock.ChainId1;
            _contract = new SideChainContractShim(Mock, ContractHelpers.GetSideChainContractAddress(chainId));
            ulong pHeight = 1;
            ParentChainBlockRootInfo parentChainBlockRootInfo = new ParentChainBlockRootInfo
            {
                ChainId = chainId,
                Height = pHeight,
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
            
            ChainConfig.Instance.ChainId = chainId.DumpHex();
            var crossChainInfo = new CrossChainInfo(Mock.StateStore);
            var merklepath = crossChainInfo.GetTxRootMerklePathInParentChain(0);
            Assert.NotNull(merklepath);
            Assert.Equal(parentChainBlockInfo.IndexedBlockInfo[0], merklepath);

            var parentHeight = crossChainInfo.GetParentChainCurrentHeight();
            Assert.Equal(pHeight, parentHeight);
            var boundHeight = crossChainInfo.GetBoundParentChainHeight(0);
            Assert.Equal(parentChainBlockRootInfo.Height, boundHeight);

            var boundBlockInfo = crossChainInfo.GetBoundParentChainBlockInfo(parentChainBlockRootInfo.Height);
            Assert.Equal(parentChainBlockInfo, boundBlockInfo);
        }

        [Fact]
        public async Task VerifyTransactionTest()
        {
            Init();
            var chainId = Mock.ChainId1;
            ChainConfig.Instance.ChainId = chainId.DumpHex();
            _contract = new SideChainContractShim(Mock, ContractHelpers.GetSideChainContractAddress(chainId));
            ulong pHeight = 1;
            ParentChainBlockRootInfo pcbr1 = new ParentChainBlockRootInfo
            {
                ChainId = chainId,
                Height = pHeight,
                SideChainTransactionsRoot = Hash.Generate(),
                SideChainBlockHeadersRoot = Hash.Generate()
            };
            ParentChainBlockInfo pcb1 = new ParentChainBlockInfo
            {
                Root = pcbr1
            };
            pcb1.IndexedBlockInfo.Add(0, new MerklePath
            {
                Path = {Hash.FromString("Block1"), Hash.FromString("Block2"), Hash.FromString("Block3")}
            });
            await _contract.WriteParentChainBLockInfo(pcb1);
            var crossChainInfo = new CrossChainInfo(Mock.StateStore);
            var parentHeight = crossChainInfo.GetParentChainCurrentHeight();
            Assert.Equal(pHeight, parentHeight);

            var sig = new Sig
            {
                P = ByteString.Empty,
                R = ByteString.Empty,
            };
            Transaction t1 = new Transaction
            {
                From = Address.FromString("1"),
                To = Address.FromString("2"),
                MethodName = "test",
                
                Params = ByteString.Empty,
                RefBlockNumber = 0,
                RefBlockPrefix = ByteString.Empty
            };
            t1.Sigs.Add(sig);
            var hashCount = 10;

            var list1 = new List<Hash> {t1.GetHash()};
            for (int i = 0; i < hashCount; i++)
            {
                list1.Add(Hash.Generate());
            }
            
            var bmt1 = new BinaryMerkleTree();
            bmt1.AddNodes(list1);
            var root1 = bmt1.ComputeRootHash();
            var sc1BlockInfo = new SideChainBlockInfo
            {
                Height = pHeight,
                BlockHeaderHash = Hash.Generate(),
                ChainId = Hash.Generate(),
                TransactionMKRoot = root1
            };

            var sig2 = new Sig
            {
                P = ByteString.Empty,
                R = ByteString.Empty,
            };
            Transaction t2 = new Transaction
            {
                From = Address.FromString("3"),
                To = Address.FromString("4"),
                MethodName = "test",
                
                Params = ByteString.Empty,
                RefBlockNumber = 1,
                RefBlockPrefix = ByteString.Empty
            };
            t2.Sigs.Add(sig2);
            var list2 = new List<Hash> {t2.GetHash(), Hash.FromString("d"), Hash.FromString("e"), Hash.FromString("f"), Hash.FromString("a"), Hash.FromString("b"), Hash.FromString("c")};
            var bmt2 = new BinaryMerkleTree();
            bmt2.AddNodes(list2);
            var root2 = bmt2.ComputeRootHash();
            var sc2BlockInfo = new SideChainBlockInfo
            {
                Height = pHeight,
                BlockHeaderHash = Hash.Generate(),
                ChainId = Hash.Generate(),
                TransactionMKRoot = root2
            };
            
            var block = new Block
            {
                Header = new BlockHeader(),
                Body = new BlockBody()
            };
            block.Body.IndexedInfo.Add(new List<SideChainBlockInfo>{sc1BlockInfo, sc2BlockInfo});
            block.Body.CalculateMerkleTreeRoots();
            
            pHeight = 2;
            ParentChainBlockRootInfo parentChainBlockRootInfo = new ParentChainBlockRootInfo
            {
                ChainId = chainId,
                Height = pHeight,
                SideChainTransactionsRoot = block.Body.SideChainTransactionsRoot,
                SideChainBlockHeadersRoot = Hash.FromString("SideChainBlockHeadersRoot")
            };
            
            ParentChainBlockInfo parentChainBlockInfo = new ParentChainBlockInfo
            {
                Root = parentChainBlockRootInfo
            };
            var tree = block.Body.BinaryMerkleTreeForSideChainTransactionRoots;
            var pathForTx1 = bmt1.GenerateMerklePath(0);
            Assert.Equal(root1, pathForTx1.ComputeRootWith(t1.GetHash()));
            var pathForSc1Block = tree.GenerateMerklePath(0);
            pathForTx1.Path.AddRange(pathForSc1Block.Path);
            
            var pathForTx2 = bmt2.GenerateMerklePath(0);
            var pathForSc2Block = tree.GenerateMerklePath(1);
            pathForTx2.Path.AddRange(pathForSc2Block.Path);
            
            //parentChainBlockInfo.IndexedBlockInfo.Add(1, tree.GenerateMerklePath(0));
            await _contract.WriteParentChainBLockInfo(parentChainBlockInfo);
            //crossChainInfo = new CrossChainInfo(Mock.StateStore);
            parentHeight = crossChainInfo.GetParentChainCurrentHeight();
            Assert.Equal(pHeight, parentHeight);
            
            var b = await _contract.VerifyTransaction(t1.GetHash(), pathForTx1, parentChainBlockRootInfo.Height);
            Assert.True(b);
            
            b = await _contract.VerifyTransaction(t2.GetHash(), pathForTx2, parentChainBlockRootInfo.Height);
            Assert.True(b);
        }
    }
}