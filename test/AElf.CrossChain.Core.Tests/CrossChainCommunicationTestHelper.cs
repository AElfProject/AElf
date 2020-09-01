using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS7;
using AElf.CrossChain.Communication.Infrastructure;
using AElf.Types;

namespace AElf.CrossChain
{
    public class CrossChainCommunicationTestHelper
    {
        private readonly Dictionary<int, CrossChainClientCreationContext> _crossChainClientCreationContexts =
            new Dictionary<int, CrossChainClientCreationContext>();
        
        private readonly Dictionary<int, bool> _clientConnected = new Dictionary<int, bool>();

        public IndexedSideChainBlockData IndexedSideChainBlockData { get; set; } = new IndexedSideChainBlockData
        {
            SideChainBlockDataList =
            {
                new SideChainBlockData
                {
                    ChainId = 123, Height = 1,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("fakeTransactionMerkleTree")
                }
            }
        };
        
        public void AddNewCrossChainClient(CrossChainClientCreationContext crossChainClientCreationContext)
        {
            _crossChainClientCreationContexts[crossChainClientCreationContext.RemoteChainId] =
                crossChainClientCreationContext;
        }

        public bool TryGetCrossChainClientCreationContext(int chainId, 
            out CrossChainClientCreationContext crossChainClientCreationContext) 
        {
            return _crossChainClientCreationContexts.TryGetValue(chainId, out crossChainClientCreationContext);
        }
        
        
        public bool CheckClientConnected(int chainId)
        {
            return _clientConnected.TryGetValue(chainId, out var res) && res;
        }

        public void SetClientConnected(int chainId, bool isConnected)
        {
            _clientConnected[chainId] = isConnected;
        }

        public Hash GetBlockHashByHeight(long height)
        {
            var dict = CreateDict();
            return dict.TryGetValue(height, out var hash) ? hash : null;
        }

        public bool TryGetHeightByHash(Hash hash, out long height)
        {
            height = 0;
            var dict = CreateDict();
            if (dict.All(kv => kv.Value != hash)) return false;
            height = dict.First(kv => kv.Value == hash).Key;
            return true;
        }
        
        private Dictionary<long, Hash> CreateDict()
        {
            return new Dictionary<long, Hash>
            {
                {1, HashHelper.ComputeFrom("1")},
                {2, HashHelper.ComputeFrom("2")},
                {
                    3,
                    BinaryMerkleTree
                        .FromLeafNodes(
                            IndexedSideChainBlockData.SideChainBlockDataList.Select(scb =>
                                scb.TransactionStatusMerkleTreeRoot)).Root
                }
            };
        }
    }
}