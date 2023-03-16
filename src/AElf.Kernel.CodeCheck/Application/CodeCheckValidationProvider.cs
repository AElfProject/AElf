using System.Linq;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.CodeCheck.Application;

internal class CodeCheckValidationProvider : IBlockValidationProvider
{
    private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;
    private readonly IContractReaderFactory<ACS0Container.ACS0Stub> _contractReaderFactory;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ICodeCheckService _codeCheckService;

    public CodeCheckValidationProvider(ISmartContractAddressService smartContractAddressService,
        IContractReaderFactory<ACS0Container.ACS0Stub> contractReaderFactory,
        ICheckedCodeHashProvider checkedCodeHashProvider,
        IOptionsSnapshot<CodeCheckOptions> codeCheckOptions, ICodeCheckService codeCheckService)
    {
        _smartContractAddressService = smartContractAddressService;
        _contractReaderFactory = contractReaderFactory;
        _checkedCodeHashProvider = checkedCodeHashProvider;
        _codeCheckService = codeCheckService;

        Logger = NullLogger<CodeCheckValidationProvider>.Instance;
    }

    public ILogger<CodeCheckValidationProvider> Logger { get; set; }

    public Task<bool> ValidateBeforeAttachAsync(IBlock block)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
    {
        return Task.FromResult(true);
    }

    public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
    {
        if (block.Header.Height == AElfConstants.GenesisBlockHeight) return true;

        var genesisContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();
        var deployedBloom = new ContractDeployed().ToLogEvent(genesisContractAddress).GetBloom();
        if (!deployedBloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray()))) return true;

        var blockHash = block.GetHash();
        var codeHashList = await _contractReaderFactory.Create(new ContractReaderContext
        {
            BlockHash = blockHash,
            BlockHeight = block.Header.Height,
            ContractAddress = genesisContractAddress
        }).GetContractCodeHashListByDeployingBlockHeight.CallAsync(new Int64Value { Value = block.Header.Height });
        
        foreach (var codeHash in codeHashList.Value)
        {
            if (_checkedCodeHashProvider.IsCodeHashExists(new BlockIndex
                {
                    BlockHash = blockHash,
                    BlockHeight = block.Header.Height
                }, codeHash))
            {
                continue;
            }

            var contractRegistration = await _contractReaderFactory.Create(new ContractReaderContext
            {
                BlockHash = blockHash,
                BlockHeight = block.Header.Height,
                ContractAddress = genesisContractAddress
            }).GetSmartContractRegistrationByCodeHash.CallAsync(codeHash);

            if (await _codeCheckService.PerformCodeCheckAsync(contractRegistration.Code.ToByteArray(),
                    blockHash, block.Header.Height, contractRegistration.Category,
                    contractRegistration.IsSystemContract, contractRegistration.IsUserContract))
            {
                continue;
            }

            Logger.LogWarning("Code check validate failed. block hash: {BlockHash}, code hash: {CodeHash}", blockHash,
                codeHash.ToHex());
            return false;
        }

        return true;
    }
}