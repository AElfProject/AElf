using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.CrossChain;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using Xunit;

using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.Types;
using AElf.Miner.TxMemPool;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Contracts.SideChain.Tests
{
public class SideChainTest : SideChainContractTestBase
    {
        private SideChainContractShim _contract;
        private MockSetup Mock;

        public SideChainTest()
        {
            Mock = this.GetRequiredService<MockSetup>();
        }

        [Fact(Skip = "TBD, side chain lifetime needed.")]
        public async Task SideChainLifetime()
        {
            _contract = new SideChainContractShim(Mock, ContractHelpers.GetCrossChainContractAddress(Mock.ChainId1));
//            var chainId = Hash.Generate();
            var chainId = ChainHelpers.GetChainId(123);
            var lockedAddress = Address.Generate();
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

        [Fact(Skip = "TBD, side chain lifetime needed.")]
        public async Task MerklePathTest()
        {
            var chainId = Mock.ChainId1;
            _contract = new SideChainContractShim(Mock, ContractHelpers.GetCrossChainContractAddress(chainId));
            ulong pHeight = 1;
            ParentChainBlockRootInfo parentChainBlockRootInfo = new ParentChainBlockRootInfo
            {
                ChainId = chainId,
                Height = pHeight,
                SideChainTransactionsRoot = Hash.Generate(),
                SideChainBlockHeadersRoot = Hash.Generate()
            };
            ParentChainBlockData parentChainBlockData = new ParentChainBlockData
            {
                Root = parentChainBlockRootInfo
            };
            parentChainBlockData.IndexedMerklePath.Add(pHeight, new MerklePath
            {
                Path = {Hash.FromString("Block1"), Hash.FromString("Block2"), Hash.FromString("Block3")}
            });
            await _contract.WriteParentChainBLockInfo(new []{parentChainBlockData});
            
            ChainConfig.Instance.ChainId = chainId.DumpBase58();
            var crossChainInfo = new CrossChainInfoReader(Mock.StateManager);
            var merklepath = await crossChainInfo.GetTxRootMerklePathInParentChainAsync(pHeight);
            Assert.NotNull(merklepath);
            Assert.Equal(parentChainBlockData.IndexedMerklePath[pHeight], merklepath);

            var parentHeight = await crossChainInfo.GetParentChainCurrentHeightAsync();
            Assert.Equal(pHeight, parentHeight);
            var boundHeight = await crossChainInfo.GetBoundParentChainHeightAsync(pHeight);
            Assert.Equal(parentChainBlockRootInfo.Height, boundHeight);

            var boundBlockInfo = await crossChainInfo.GetBoundParentChainBlockInfoAsync(parentChainBlockRootInfo.Height);
            Assert.Equal(parentChainBlockData, boundBlockInfo);
        }

        /*[Fact(Skip = "TBD, side chain lifetime needed.")]
        public async Task VerifyTransactionTest()
        {
            var chainId = Mock.ChainId1;
            ChainConfig.Instance.ChainId = chainId.DumpBase58();
            //Mock.StateManager.SetAsync()
            _contract = new SideChainContractShim(Mock, ContractHelpers.GetCrossChainContractAddress(chainId));
            ulong pHeight = 1;
            ParentChainBlockRootInfo pcbr1 = new ParentChainBlockRootInfo
            {
                ChainId = chainId,
                Height = pHeight,
                SideChainTransactionsRoot = Hash.Generate(),
                SideChainBlockHeadersRoot = Hash.Generate()
            };
            ParentChainBlockData pcb1 = new ParentChainBlockData
            {
                Root = pcbr1
            };
            pcb1.IndexedMerklePath.Add(pHeight, new MerklePath
            {
                Path = {Hash.FromString("Block1"), Hash.FromString("Block2"), Hash.FromString("Block3")}
            });
            await _contract.WriteParentChainBLockInfo(new []{pcb1});
            var crossChainInfo = new CrossChainInfoReader(Mock.StateManager);
            var parentHeight = await crossChainInfo.GetParentChainCurrentHeightAsync();
            Assert.Equal(pHeight, parentHeight);
            Transaction t1 = new Transaction
            {
                From = Address.FromString("1"),
                To = Address.FromString("2"),
                MethodName = "test",
                Params = ByteString.Empty,
                RefBlockNumber = 1,
                RefBlockPrefix = ByteString.Empty
            };
            
            var hashCount = 10;

            var list1 = new List<Hash> {t1.GetHash()};
            for (int i = 0; i < hashCount; i++)
            {
                list1.Add(Hash.Generate());
            }
            
            var bmt1 = new BinaryMerkleTree();
            bmt1.AddNodes(list1);
            var root1 = bmt1.ComputeRootHash();
            var sc1BlockInfo = new SideChainBlockData
            {
                Height = pHeight,
                BlockHeaderHash = Hash.Generate(),
                ChainId = ChainHelpers.GetChainId(123),
                TransactionMKRoot = root1
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
            
            var list2 = new List<Hash> {t2.GetHash(), Hash.FromString("d"), Hash.FromString("e"), Hash.FromString("f"), Hash.FromString("a"), Hash.FromString("b"), Hash.FromString("c")};
            var bmt2 = new BinaryMerkleTree();
            bmt2.AddNodes(list2);
            var root2 = bmt2.ComputeRootHash();
            var sc2BlockInfo = new SideChainBlockData
            {
                Height = pHeight,
                BlockHeaderHash = Hash.Generate(),
                ChainId = ChainHelpers.GetChainId(123),
                TransactionMKRoot = root2
            };
            
            var block = new Block
            {
                Header = new BlockHeader(),
                Body = new BlockBody()
            };
            
            var binaryMerkleTree = new BinaryMerkleTree();
            binaryMerkleTree.AddNodes(new[] {sc1BlockInfo.TransactionMKRoot, sc2BlockInfo.TransactionMKRoot});
            block.Header.SideChainTransactionsRoot = binaryMerkleTree.ComputeRootHash();
            //block.Body.IndexedInfo.Add(new List<SideChainBlockData>{sc1BlockInfo, sc2BlockInfo});
            block.Body.CalculateMerkleTreeRoots();
            
            pHeight = 2;
            ParentChainBlockRootInfo parentChainBlockRootInfo = new ParentChainBlockRootInfo
            {
                ChainId = chainId,
                Height = pHeight,
                SideChainTransactionsRoot = block.Header.SideChainTransactionsRoot,
            };
            
            ParentChainBlockData parentChainBlockData = new ParentChainBlockData
            {
                Root = parentChainBlockRootInfo
            };
            
            var pathForTx1 = bmt1.GenerateMerklePath(0);
            Assert.Equal(root1, pathForTx1.ComputeRootWith(t1.GetHash()));
            var pathForSc1Block = binaryMerkleTree.GenerateMerklePath(0);
            pathForTx1.Path.AddRange(pathForSc1Block.Path);
            
            var pathForTx2 = bmt2.GenerateMerklePath(0);
            var pathForSc2Block = binaryMerkleTree.GenerateMerklePath(1);
            pathForTx2.Path.AddRange(pathForSc2Block.Path);
            
            //parentChainBlockData.IndexedMerklePath.Add(1, tree.GenerateMerklePath(0));
            await _contract.WriteParentChainBLockInfo(new []{parentChainBlockData});
            //crossChainInfoReader = new CrossChainInfoReader(Mock.StateStore);
            parentHeight = await crossChainInfo.GetParentChainCurrentHeightAsync();
            Assert.Equal(pHeight, parentHeight);
            
            var b = await _contract.VerifyTransaction(t1.GetHash(), pathForTx1, parentChainBlockRootInfo.Height);
            Assert.True(b);
            
            b = await _contract.VerifyTransaction(t2.GetHash(), pathForTx2, parentChainBlockRootInfo.Height);
            Assert.True(b);
        }*/
    }
}