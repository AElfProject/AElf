using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.WebApp.Application.Chain.Dto;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.Application.Chain;

public interface IChainStatusAppService
{
    Task<ChainStatusDto> GetChainStatusAsync();
}

public class ChainStatusAppService : AElfAppService, IChainStatusAppService
{
    private readonly IBlockchainService _blockchainService;

    private readonly IObjectMapper<ChainApplicationWebAppAElfModule> _objectMapper;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public ChainStatusAppService(ISmartContractAddressService smartContractAddressService,
        IBlockchainService blockchainService,
        IObjectMapper<ChainApplicationWebAppAElfModule> objectMapper)
    {
        _smartContractAddressService = smartContractAddressService;
        _blockchainService = blockchainService;
        _objectMapper = objectMapper;
    }

    /// <summary>
    ///     Get the current status of the block chain.
    /// </summary>
    /// <returns></returns>
    public async Task<ChainStatusDto> GetChainStatusAsync()
    {
        var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

        var chain = await _blockchainService.GetChainAsync();

        var result = _objectMapper.Map<Kernel.Chain, ChainStatusDto>(chain);
        result.GenesisContractAddress = basicContractZero?.ToBase58();

        return result;
    }
}