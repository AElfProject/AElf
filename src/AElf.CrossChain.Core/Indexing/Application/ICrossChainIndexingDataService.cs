using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Indexing.Infrastructure;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.CrossChain.Indexing.Application
{
    public interface ICrossChainIndexingDataService
    {
        Task<IndexedSideChainBlockData> GetIndexedSideChainBlockDataAsync(Hash blockHash, long blockHeight);

        Task<CrossChainTransactionInput> GetCrossChainTransactionInputForNextMiningAsync(Hash blockHash,
            long blockHeight);

        Task<bool> CheckExtraDataIsNeededAsync(Hash blockHash, long blockHeight, Timestamp timestamp);

        Task<ByteString> PrepareExtraDataForNextMiningAsync(Hash blockHash, long blockHeight);

        // ByteString ExtractCrossChainExtraDataFromCrossChainBlockData(CrossChainBlockData crossChainBlockData);
        void UpdateCrossChainDataWithLib(Hash blockHash, long blockHeight);
        Task<ChainIdAndHeightDict> GetAllChainIdHeightPairsAtLibAsync();
        Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId);
        Task<Block> GetNonIndexedBlockAsync(long requestHeight);
    }
}