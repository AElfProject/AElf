using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public interface ICrossChainExtraDataExtractor
    {
        CrossChainExtraData ExtractCrossChainData(BlockHeader header);
        Hash ExtractTransactionStatusMerkleTreeRoot(BlockHeader header);
        Dictionary<string, ByteString> ExtractCommonExtraDataForExchange(BlockHeader header);
    }

    public class CrossChainExtraDataExtractor : ICrossChainExtraDataExtractor, ITransientDependency
    {
        private readonly IBlockExtraDataService _blockExtraDataService;

        public CrossChainExtraDataExtractor(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public CrossChainExtraData ExtractCrossChainData(BlockHeader header)
        {
            var bytes = _blockExtraDataService.GetExtraDataFromBlockHeader("CrossChain", header);
            return bytes == ByteString.Empty || bytes == null ? null : CrossChainExtraData.Parser.ParseFrom(bytes);
        }

        public Hash ExtractTransactionStatusMerkleTreeRoot(BlockHeader header)
        {
            return Hash.Parser.ParseFrom(_blockExtraDataService.GetMerkleTreeRootExtraDataForTransactionStatus(header));
        }

        public Dictionary<string, ByteString> ExtractCommonExtraDataForExchange(BlockHeader header)
        {
            var res = new Dictionary<string, ByteString>();
            foreach (var symbol in CrossChainConsts.SymbolsOfExchangedExtraData)
            {
                var extraData = _blockExtraDataService.GetExtraDataFromBlockHeader(symbol, header);
                if(extraData != null)
                    res.Add(symbol, extraData);
            }
            return res;
        }

    }
}