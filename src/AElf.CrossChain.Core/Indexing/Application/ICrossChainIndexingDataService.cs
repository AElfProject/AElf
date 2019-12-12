using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Indexing.Infrastructure;
using AElf.Types;
using Google.Protobuf;

namespace AElf.CrossChain.Indexing.Application
{
    public interface ICrossChainIndexingDataService
    {
        Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash blockHash, long blockHeight);

        Task<IndexedSideChainBlockData> GetIndexedSideChainBlockDataAsync(Hash blockHash, long blockHeight);

        Task<CrossChainTransactionInput> GetCrossChainTransactionInputForNextMiningAsync(Hash blockHash,
            long blockHeight);

        Task<ByteString> PrepareExtraDataForNextMiningAsync(Hash blockHash, long blockHeight);

        ByteString ExtractCrossChainExtraDataFromCrossChainBlockData(CrossChainBlockData crossChainBlockData);
        void UpdateCrossChainDataWithLib(Hash blockHash, long blockHeight);
    }
}