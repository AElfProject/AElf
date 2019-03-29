using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.WebApp.Application.Chain.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IChainAppService : IApplicationService
    {
        Task<GetChainInformationOutput> GetChainInformation();
    }
    
    public class ChainAppService : IChainAppService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        public ILogger<ChainAppService> Logger { get; set; }

        public ChainAppService(IBlockchainService blockchainService,
            ISmartContractAddressService smartContractAddressService
            )
        {
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            
            Logger = NullLogger<ChainAppService>.Instance;
        }
        
        public Task<GetChainInformationOutput> GetChainInformation()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            return Task.FromResult(new GetChainInformationOutput
            {
                GenesisContractAddress = basicContractZero?.GetFormatted(),
                ChainId = ChainHelpers.ConvertChainIdToBase58(_blockchainService.GetChainId())
            });
        }
    }
}