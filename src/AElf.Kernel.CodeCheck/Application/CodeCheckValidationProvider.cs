using System.Diagnostics;
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
    private readonly ActivitySource _activitySource;
    public CodeCheckValidationProvider(ISmartContractAddressService smartContractAddressService,
        IContractReaderFactory<ACS0Container.ACS0Stub> contractReaderFactory,
        ICheckedCodeHashProvider checkedCodeHashProvider,
        IOptionsSnapshot<CodeCheckOptions> codeCheckOptions,
        Instrumentation instrumentation)
    {
        _smartContractAddressService = smartContractAddressService;
        _contractReaderFactory = contractReaderFactory;
        _checkedCodeHashProvider = checkedCodeHashProvider;

        Logger = NullLogger<CodeCheckValidationProvider>.Instance;
        _activitySource = instrumentation.ActivitySource;
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
        using var activity = _activitySource.StartActivity();

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

        if (codeHashList == null || !codeHashList.Value.Any())
        {
            Logger.LogInformation("CodeHashList is empty.");
            return true;
        }

        Logger.LogInformation("block hash: {Block}", block);
        return codeHashList.Value.All(codeHash => _checkedCodeHashProvider.IsCodeHashExists(new BlockIndex
        {
            BlockHash = blockHash,
            BlockHeight = block.Header.Height
        }, codeHash));
    }
}