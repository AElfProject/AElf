using System;
using System.Threading.Tasks;
using AElf.CrossChain;
using AElf.CrossChain.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Standards.ACS7;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.SideChain;

public class SideChainInitializationDataProvider : ISideChainInitializationDataProvider, ISingletonDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly IChainInitializationDataPlugin _chainInitializationDataPlugin;
    private readonly ChainOptions _chainOptions;

    private ChainInitializationData _chainInitializationData;

    public SideChainInitializationDataProvider(IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<CrossChainConfigOptions> crossChainConfigOptions,
        IChainInitializationDataPlugin chainInitializationDataPlugin, IBlockchainService blockchainService)
    {
        _chainOptions = chainOptions.Value;
        _chainInitializationDataPlugin = chainInitializationDataPlugin;
        _blockchainService = blockchainService;
        ParentChainId = ChainHelper.ConvertBase58ToChainId(crossChainConfigOptions.Value.ParentChainId);
    }

    public async Task<ChainInitializationData> GetChainInitializationDataAsync()
    {
        if (_chainInitializationData != null)
            return _chainInitializationData;

        var chain = await _blockchainService.GetChainAsync();
        if (chain != null)
            return null;

        _chainInitializationData =
            await _chainInitializationDataPlugin.GetChainInitializationDataAsync(_chainOptions.ChainId);
        if (_chainInitializationData == null)
            throw new Exception("Initialization data cannot be null for a new side chain.");

        return _chainInitializationData;
    }

    public int ParentChainId { get; }
}