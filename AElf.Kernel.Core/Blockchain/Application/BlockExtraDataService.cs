using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockExtraDataService : IBlockExtraDataService
    {
        private readonly List<IBlockExtraDataProvider> _blockExtraDataProviders;
        public BlockExtraDataService(IEnumerable<IBlockExtraDataProvider> blockExtraDataProviders)
        {
            _blockExtraDataProviders = blockExtraDataProviders.ToList();
        }

        //TODO: Add FillBlockExtraData test case [Case]
        public async Task FillBlockExtraData(BlockHeader blockHeader)
        {
            foreach (var blockExtraDataProvider in _blockExtraDataProviders)
            {
                var extraData = await blockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(blockHeader);
                if (extraData != null) 
                {
                    // Actually extraData cannot be NULL if it is mining processing, as the index in BlockExtraData is fixed.
                    // So it can be ByteString.Empty but not NULL.
                    blockHeader.BlockExtraDatas.Add(extraData);
                }
            }
        }

        public ByteString GetExtraDataFromBlockHeader(string blockExtraDataProviderSymbol, BlockHeader blockHeader)
        {
            for (var i = 0; i < _blockExtraDataProviders.Count; i++)
            {
                var blockExtraDataProviderName = _blockExtraDataProviders[i].GetType().Name;
                if (blockExtraDataProviderName.Contains(blockExtraDataProviderSymbol) && i < blockHeader.BlockExtraDatas.Count)
                {
                    return blockHeader.BlockExtraDatas[i];
                }
            }
            return null;
        }

        public async Task FillMktRootExtraDataForTransactionStatusAsync(BlockHeader blockHeader,
            IEnumerable<(Hash, TransactionResultStatus)> blockExecutionReturnSet)
        {
            var extraDataCount = blockHeader.BlockExtraDatas.Count;
            if( extraDataCount != _blockExtraDataProviders.Count && extraDataCount != _blockExtraDataProviders.Count + 1)
                throw new Exception("Incorrect filled extra data");
            
            var nodes = new List<Hash>();
            foreach (var (transactionId, status) in blockExecutionReturnSet)
            {
                nodes.Add(await GetHashCombiningTransactionAndStatus(transactionId, status));
            }
            var rootByteString = new BinaryMerkleTree().AddNodes(nodes).ComputeRootHash().ToByteString();
            if (extraDataCount == _blockExtraDataProviders.Count)
                blockHeader.BlockExtraDatas.Add(rootByteString); // not filled.
            else
                blockHeader.BlockExtraDatas[_blockExtraDataProviders.Count] = rootByteString; //reset it since already filled
        }
        
        private async Task<Hash> GetHashCombiningTransactionAndStatus(Hash txId,
            TransactionResultStatus executionReturnStatus)
        {
            return Hash.FromTwoHashes(txId, Hash.FromString(executionReturnStatus.ToString()));
        }
        
    }
}