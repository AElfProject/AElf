using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class DonateResourceTokenValidationProvider : IBlockValidationProvider
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly ITotalResourceTokensMapsProvider _totalResourceTokensMapsProvider;

        public DonateResourceTokenValidationProvider(IBlockExtraDataService blockExtraDataService,
            ITotalResourceTokensMapsProvider totalResourceTokensMapsProvider)
        {
            _blockExtraDataService = blockExtraDataService;
            _totalResourceTokensMapsProvider = totalResourceTokensMapsProvider;
        }

        /// <summary>
        /// 直接返回true
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// 判断本地Provider中的数据和收到区块的ExtraData中的数据是否一致
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            return await ValidateEqual(block);
        }

        /// <summary>
        /// 判断区块执行后State中的信息与Provider中的数据是否一致
        /// 得到State中的信息：调用TokenContract.GetLatestTotalResourceTokensMaps即可。
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            throw new System.NotImplementedException();
        }

        private async Task<bool> ValidateEqual(IBlock block)
        {
            var totalResourceTokensMapsFromExtraData =
                _blockExtraDataService.GetExtraDataFromBlockHeader(
                    TotalResourceTokensMapsExtraDataProvider.ExtraDataName, block.Header);
            var totalResourceTokensMapsFromProvider =
                await _totalResourceTokensMapsProvider.GetTotalResourceTokensMapsAsync(new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height
                });
            return totalResourceTokensMapsFromExtraData == totalResourceTokensMapsFromProvider.ToByteString();
        }
    }
}