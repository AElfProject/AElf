using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataService
    {
        Task FillBlockExtraDataAsync(BlockHeader blockHeader);

        /// <summary>
        /// Get extra data from block header.
        /// </summary>
        /// <param name="blockHeaderExtraDataKey"></param>
        /// <param name="blockHeader"></param>
        /// <returns></returns>
        ByteString GetExtraDataFromBlockHeader(string blockHeaderExtraDataKey, BlockHeader blockHeader);
    }
}