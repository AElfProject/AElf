using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly;

internal class CSharpContractReader : ICSharpContractReader, ISingletonDependency
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly IBlockchainService _blockchainService;

    private readonly IContractReaderFactory<TokenContractContainer.TokenContractStub> _tokenContractReaderFactory;

    public CSharpContractReader(ISmartContractAddressService smartContractAddressService,
        IBlockchainService blockchainService,
        IContractReaderFactory<TokenContractContainer.TokenContractStub> tokenContractReaderFactory)
    {
        _smartContractAddressService = smartContractAddressService;
        _blockchainService = blockchainService;
        _tokenContractReaderFactory = tokenContractReaderFactory;
    }

    public async Task<long> GetBalanceAsync(Address owner, string? symbol = null)
    {
        var chain = await _blockchainService.GetChainAsync();
        var tokenContractAddress = await _smartContractAddressService.GetAddressByContractNameAsync(new ChainContext
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            },
            TokenSmartContractAddressNameProvider.StringName);
        var tokenContractStub = _tokenContractReaderFactory.Create(new ContractReaderContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight,
            ContractAddress = tokenContractAddress
        });
        var querySymbol = symbol ?? (await tokenContractStub.GetNativeTokenInfo.CallAsync(new Empty())).Symbol;
        var output = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = querySymbol,
            Owner = owner
        });
        return output.Balance;
    }
}