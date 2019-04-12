using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataProvider
    {
        /// <summary>
        /// Get extra data from corresponding services.
        /// </summary>
        /// <param name="blockHeader"></param>
        /// <returns></returns>
        Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader);
    }
}