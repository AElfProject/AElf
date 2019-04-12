using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataService
    {
        Task FillBlockExtraData(BlockHeader blockHeader);

        /// <summary>
        /// Get extra data from block header.
        /// </summary>
        /// <param name="blockExtraDataProviderSymbol"></param>
        /// <param name="blockHeader"></param>
        /// <returns></returns>
        ByteString GetExtraDataFromBlockHeader(string blockExtraDataProviderSymbol, BlockHeader blockHeader);

        void FillMerkleTreeRootExtraDataForTransactionStatus(BlockHeader blockHeader,
            IEnumerable<(Hash, TransactionResultStatus)> blockExecutionReturnSet);

        ByteString GetMerkleTreeRootExtraDataForTransactionStatus(BlockHeader blockHeader);
    }
}