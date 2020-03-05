using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public interface ICoefficientsCacheProvider
    {
        Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext);
        void UpdateLatestModifiedHeight(long height);
        bool GetUpdateNotification(int tokenType);
    }

    public class CoefficientsCacheProvider : ICoefficientsCacheProvider, ISyncCacheProvider, ISingletonDependency
    {
        private readonly IBlockchainStateService _blockChainStateService;
        private readonly Dictionary<int, IList<int[]>> _coefficientsDicCache;
        private readonly List<int> _updateNotifications;
        private long _latestModifiedHeight;

        public CoefficientsCacheProvider(IBlockchainStateService blockChainStateService)
        {
            _blockChainStateService = blockChainStateService;
            _coefficientsDicCache = new Dictionary<int, IList<int[]>>();
            _updateNotifications = new List<int>();
            _latestModifiedHeight = 0;
        }

        public async Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext)
        {
            if (_latestModifiedHeight == 0)
            {
                if (_coefficientsDicCache.TryGetValue(tokenType, out var coefficientsInCache))
                    return coefficientsInCache;
                coefficientsInCache = await GetFromBlockChainStateAsync(tokenType, chainContext);
                _coefficientsDicCache[tokenType] = coefficientsInCache;
                return coefficientsInCache;
            }

            return await GetFromBlockChainStateAsync(tokenType, chainContext);
        }

        public void UpdateLatestModifiedHeight(long height)
        {
            _latestModifiedHeight = height;
        }

        public bool GetUpdateNotification(int tokenType)
        {
            if (!_updateNotifications.Contains(tokenType)) return false;
            _updateNotifications.Remove(tokenType);
            return true;
        }

        public async Task SyncCacheAsync(IChainContext chainContext)
        {
            var currentLibHeight = chainContext.BlockHeight;
            if (_latestModifiedHeight <= currentLibHeight)
            {
                var allCalculateFeeCoefficients =
                    await _blockChainStateService.GetBlockExecutedDataAsync<AllCalculateFeeCoefficients>(chainContext);
                var tokenTypeList = _coefficientsDicCache.Select(x => x.Key).ToArray();
                foreach (var tokenType in tokenTypeList)
                {
                    var targetTokeData =
                        allCalculateFeeCoefficients.Value.FirstOrDefault(x => x.FeeTokenType == tokenType);
                    if (targetTokeData == null) continue;
                    _coefficientsDicCache[tokenType] = targetTokeData.PieceCoefficientsList.AsEnumerable()
                        .Select(x => (int[]) x.Value.AsEnumerable()).ToList();
                    _updateNotifications.Add(tokenType);
                }

                _latestModifiedHeight = 0;
            }
        }

        private async Task<IList<int[]>> GetFromBlockChainStateAsync(int tokenType, IChainContext chainContext)
        {
            var allCalculateFeeCoefficients =
                await _blockChainStateService.GetBlockExecutedDataAsync<AllCalculateFeeCoefficients>(
                    chainContext);
            var targetTokeData =
                allCalculateFeeCoefficients.Value.SingleOrDefault(x => x.FeeTokenType == tokenType);
            if (targetTokeData == null) return new List<int[]>();
            var coefficientsArray = targetTokeData.PieceCoefficientsList.AsEnumerable()
                .Select(x => (int[]) x.Value.AsEnumerable()).ToList();
            return coefficientsArray;
        }
    }
}