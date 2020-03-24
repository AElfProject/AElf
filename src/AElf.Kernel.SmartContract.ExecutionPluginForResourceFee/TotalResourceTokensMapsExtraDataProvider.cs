using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    /// <summary>
    /// 发版前保证ExtraData能写入即可。
    /// </summary>
    public class TotalResourceTokensMapsExtraDataProvider : IBlockExtraDataProvider
    {
        public static string ExtraDataName => nameof(TotalResourceTokensMaps);

        private readonly ITotalResourceTokensMapsProvider _totalResourceTokensMapsProvider;

        public ILogger<TotalResourceTokensMapsExtraDataProvider> Logger { get; set; }

        public TotalResourceTokensMapsExtraDataProvider(
            ITotalResourceTokensMapsProvider totalResourceTokensMapsProvider)
        {
            _totalResourceTokensMapsProvider = totalResourceTokensMapsProvider;

            Logger = NullLogger<TotalResourceTokensMapsExtraDataProvider>.Instance;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            var targetBlockHash = blockHeader.PreviousBlockHash;
            var targetBlockHeight = blockHeader.Height - 1;
            var totalResourceTokensMaps =
                await _totalResourceTokensMapsProvider.GetTotalResourceTokensMapsAsync(new ChainContext
                {
                    BlockHash = targetBlockHash,
                    BlockHeight = targetBlockHeight
                });

            if (totalResourceTokensMaps == null || totalResourceTokensMaps.BlockHash != targetBlockHash ||
                totalResourceTokensMaps.BlockHeight != targetBlockHeight) return ByteString.Empty;

            Logger.LogDebug(
                $"TotalResourceTokensMaps extra data generated. Of size {totalResourceTokensMaps.CalculateSize()}");

            return totalResourceTokensMaps.ToByteString();
        }
    }
}