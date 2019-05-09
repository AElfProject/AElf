using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc
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
        private static readonly List<string> _symbolsOfExchangedExtraData = new List<string>{"Consensus"};

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
            foreach (var symbol in _symbolsOfExchangedExtraData)
            {
                var extraData = _blockExtraDataService.GetExtraDataFromBlockHeader(symbol, header);
                if(extraData != null)
                    res.Add(symbol, extraData);
            }
            return res;
        }

    }
}