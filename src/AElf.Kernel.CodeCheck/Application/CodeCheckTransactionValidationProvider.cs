using System;
using System.Linq;
using System.Threading;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Standards.ACS0;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.CodeCheck.Application;

internal class CodeCheckTransactionValidationProvider : ITransactionValidationProvider
{
    private readonly ICodeCheckService _codeCheckService;
    private readonly IContractReaderFactory<ACS0Container.ACS0Stub> _contractReaderFactory;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public CodeCheckTransactionValidationProvider(
        ICodeCheckService codeCheckService, IContractReaderFactory<ACS0Container.ACS0Stub> contractReaderFactory,
        ISmartContractAddressService smartContractAddressService)
    {
        _codeCheckService = codeCheckService;
        _contractReaderFactory = contractReaderFactory;
        _smartContractAddressService = smartContractAddressService;
        LocalEventBus = NullLocalEventBus.Instance;
    }

    public ILocalEventBus LocalEventBus { get; set; }

    public bool ValidateWhileSyncing { get; } = false;

    public async Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext)
    {
        var genesisContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();
        if (transaction.To != genesisContractAddress)
        {
            return true;
        }

        var executionValidationResult = true;
        switch (transaction.MethodName)
        {
            case nameof(ACS0Container.ACS0Stub.ProposeNewContract):
            case nameof(ACS0Container.ACS0Stub.DeployUserSmartContract):
                var deployInput = ContractDeploymentInput.Parser.ParseFrom(transaction.Params);
                executionValidationResult = await _codeCheckService.PerformCodeCheckAsync(
                    deployInput.Code.ToByteArray(), chainContext.BlockHash, chainContext.BlockHeight,
                    deployInput.Category, false, IsUserContract(transaction.MethodName));
                break;
            case nameof(ACS0Container.ACS0Stub.ProposeUpdateContract):
            case nameof(ACS0Container.ACS0Stub.UpdateUserSmartContract):
                var updateInput = ContractUpdateInput.Parser.ParseFrom(transaction.Params);
                var contractInfo = await _contractReaderFactory.Create(new ContractReaderContext
                {
                    BlockHash = chainContext.BlockHash,
                    BlockHeight = chainContext.BlockHeight,
                    ContractAddress = genesisContractAddress
                }).GetContractInfo.CallAsync(updateInput.Address);

                if (contractInfo == null || contractInfo.Author == null)
                {
                    executionValidationResult = false;
                }
                else
                {
                    executionValidationResult = await _codeCheckService.PerformCodeCheckAsync(
                        updateInput.Code.ToByteArray(), chainContext.BlockHash, chainContext.BlockHeight,
                        contractInfo.Category, contractInfo.IsSystemContract, IsUserContract(transaction.MethodName));
                }
                break;
        }

        if (!executionValidationResult)
        {
            var transactionId = transaction.GetHash();
            await LocalEventBus.PublishAsync(new TransactionValidationStatusChangedEvent
            {
                TransactionId = transactionId,
                TransactionResultStatus = TransactionResultStatus.NodeValidationFailed,
                Error = "Contract code check failed."
            });
        }

        return executionValidationResult;
    }

    private bool IsUserContract(string methodName)
    {
        return methodName == nameof(ACS0Container.ACS0Stub.DeployUserSmartContract) ||
               methodName == nameof(ACS0Container.ACS0Stub.UpdateUserSmartContract);
    }
}