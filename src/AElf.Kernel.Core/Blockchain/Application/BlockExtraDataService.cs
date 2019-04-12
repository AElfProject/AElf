using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (blockHeader.Height == KernelConstants.GenesisBlockHeight)
                return null;
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

        public void FillMerkleTreeRootExtraDataForTransactionStatus(BlockHeader blockHeader,
            IEnumerable<(Hash, TransactionResultStatus)> blockExecutionReturnSet)
        {
            var extraDataCount = blockHeader.BlockExtraDatas.Count;
            if( blockHeader.Height != KernelConstants.GenesisBlockHeight 
                && extraDataCount != _blockExtraDataProviders.Count 
                && extraDataCount != _blockExtraDataProviders.Count + 1)
                throw new Exception("Incorrect filled extra data");
            
            var nodes = new List<Hash>();
            foreach (var (transactionId, status) in blockExecutionReturnSet)
            {
                nodes.Add(GetHashCombiningTransactionAndStatus(transactionId, status));
            }
            var rootByteString = new BinaryMerkleTree().AddNodes(nodes).ComputeRootHash().ToByteString();
            if (blockHeader.Height == KernelConstants.GenesisBlockHeight || extraDataCount == _blockExtraDataProviders.Count)
                blockHeader.BlockExtraDatas.Add(rootByteString); // not filled.
            else
                blockHeader.BlockExtraDatas[_blockExtraDataProviders.Count] = rootByteString; //reset it since already filled
        }

        /// <summary>
        /// Extract merkle tree root from header extra data.
        /// </summary>
        /// <param name="blockHeader"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">The size of header extra data is incorrect.</exception>
        public ByteString GetMerkleTreeRootExtraDataForTransactionStatus(BlockHeader blockHeader)
        {
            var index = blockHeader.Height == KernelConstants.GenesisBlockHeight ? 0 : _blockExtraDataProviders.Count;
            return blockHeader.BlockExtraDatas[index];
        }

        private Hash GetHashCombiningTransactionAndStatus(Hash txId,
            TransactionResultStatus executionReturnStatus)
        {
            // combine tx result status
            var rawBytes = txId.DumpByteArray().Concat(Encoding.UTF8.GetBytes(executionReturnStatus.ToString()))
                .ToArray();
            return Hash.FromRawBytes(rawBytes);
        }
    }
}