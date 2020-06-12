using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.WebApp.Application.Chain.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using AutoMapper;
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

        private readonly IMapper _mapper;

        public ChainStatusAppService(ISmartContractAddressService smartContractAddressService,
            IBlockchainService blockchainService, IMapper mapper)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainService = blockchainService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get the current status of the block chain.
        /// </summary>
        /// <returns></returns>
        public async Task<ChainStatusDto> GetChainStatusAsync()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            var chain = await _blockchainService.GetChainAsync();

            var result = _mapper.Map<Kernel.Chain, ChainStatusDto>(chain);
            result.GenesisContractAddress = basicContractZero?.ToBase58();

            return result;
        }
    }
}