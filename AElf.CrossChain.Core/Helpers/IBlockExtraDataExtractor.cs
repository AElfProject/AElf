using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public interface IBlockExtraDataExtractor
    {
        ByteString ExtractExtraData(string symbol, BlockHeader header);
    }

    public class BlockExtraDataExtractor : IBlockExtraDataExtractor, ITransientDependency
    {
        private readonly IBlockExtraDataService _blockExtraDataService;

        public BlockExtraDataExtractor(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public ByteString ExtractExtraData(string symbol, BlockHeader header)
        {
            return _blockExtraDataService.GetExtraDataFromBlockHeader("CrossChain", header);
        }
    }
}