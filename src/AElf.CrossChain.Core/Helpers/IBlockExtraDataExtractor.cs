using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public interface IBlockExtraDataExtractor
    {
        CrossChainExtraData ExtractCrossChainExtraData(BlockHeader header);
        Hash ExtractTransactionStatusMerkleTreeRoot(BlockHeader header);
        ByteString ExtractOtherExtraData(string symbol, BlockHeader header);
    }

    public class BlockExtraDataExtractor : IBlockExtraDataExtractor, ITransientDependency
    {
        private readonly IBlockExtraDataService _blockExtraDataService;

        public BlockExtraDataExtractor(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public CrossChainExtraData ExtractCrossChainExtraData(BlockHeader header)
        {
            var bytes = _blockExtraDataService.GetExtraDataFromBlockHeader("CrossChain", header);
            return bytes == ByteString.Empty || bytes == null ? null : CrossChainExtraData.Parser.ParseFrom(bytes);
        }

        public Hash ExtractTransactionStatusMerkleTreeRoot(BlockHeader header)
        {
            return Hash.Parser.ParseFrom(_blockExtraDataService.GetMerkleTreeRootExtraDataForTransactionStatus(header));
        }

        public ByteString ExtractOtherExtraData(string symbol, BlockHeader header)
        {
            return _blockExtraDataService.GetExtraDataFromBlockHeader(symbol, header);
        }

    }
}