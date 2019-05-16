using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public interface INewChainRegistrationService
    {
        Task RegisterNewChainsAsync(Hash blockHash, long blockHeight);
    }

    internal class NewChainRegistrationService : INewChainRegistrationService, ITransientDependency
    {
        private readonly IReaderFactory _readerFactory;
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;

        public NewChainRegistrationService(IReaderFactory readerFactory, IChainCacheEntityProvider chainCacheEntityProvider)
        {
            _readerFactory = readerFactory;
            _chainCacheEntityProvider = chainCacheEntityProvider;
        }

        public async Task RegisterNewChainsAsync(Hash blockHash, long blockHeight)
        {
            var dict = await _readerFactory.Create(blockHash, blockHeight).GetAllChainsIdAndHeight
                .CallAsync(new Empty());

            foreach (var chainIdHeight in dict.IdHeightDict)
            {
                _chainCacheEntityProvider.AddChainCacheEntity(chainIdHeight.Key,
                    new ChainCacheEntity(chainIdHeight.Value + 1));
            }
        }
    }
}