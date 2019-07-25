using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IChainStatusAppService : IApplicationService
    {
        Task<ChainStatusDto> GetChainStatusAsync();
    }

    [ControllerName("BlockChain")]
    public class ChainStatusAppService : IChainStatusAppService
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly IBlockchainService _blockchainService;

        public ChainStatusAppService(ISmartContractAddressService smartContractAddressService,
            IBlockchainService blockchainService)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainService = blockchainService;
        }

        /// <summary>
        /// Get the current status of the block chain.
        /// </summary>
        /// <returns></returns>
        public async Task<ChainStatusDto> GetChainStatusAsync()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            var chain = await _blockchainService.GetChainAsync();

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