using System.Linq;
using AElf.Dtos;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.Threading;

namespace AElf.GraphQL.Application.Chain
{
    public interface IChainStatusRepository
    {
        ChainStatusDto GetChainStatus();
    }

    public class ChainStatusRepository : IChainStatusRepository
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ChainStatusRepository(IBlockchainService blockchainService,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
        }

        public ChainStatusDto GetChainStatus()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            var chain = AsyncHelper.RunSync(_blockchainService.GetChainAsync);

            var branches = chain.Branches.ToDictionary(b => HashHelper.Base64ToHash(b.Key).ToHex(), b => b.Value);
            var notLinkedBlocks = chain.NotLinkedBlocks.ToDictionary(b => HashHelper.Base64ToHash(b.Key).ToHex(),
                b => HashHelper.Base64ToHash(b.Value).ToHex());
            return new ChainStatusDto
            {
                ChainId = ChainHelper.ConvertChainIdToBase58(chain.Id),
                GenesisContractAddress = basicContractZero?.GetFormatted(),
                Branches = branches,
                NotLinkedBlocks = notLinkedBlocks,
                LongestChainHeight = chain.LongestChainHeight,
                LongestChainHash = chain.LongestChainHash?.ToHex(),
                GenesisBlockHash = chain.GenesisBlockHash.ToHex(),
                LastIrreversibleBlockHash = chain.LastIrreversibleBlockHash?.ToHex(),
                LastIrreversibleBlockHeight = chain.LastIrreversibleBlockHeight,
                BestChainHash = chain.BestChainHash?.ToHex(),
                BestChainHeight = chain.BestChainHeight
            };
        }
    }
}