using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain;

public interface IContractFileDescriptorSetAppService
{
    Task<byte[]> GetContractFileDescriptorSetAsync(string address);
}

public partial class ContractFileDescriptorSetAppService : ApplicationService, IContractFileDescriptorSetAppService
{
    private static IBlockchainService _blockchainService;
    private static ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

    public ContractFileDescriptorSetAppService(IBlockchainService blockchainService,
        ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
    {
        _blockchainService = blockchainService;
        _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
    }

    public ILogger<ContractFileDescriptorSetAppService> Logger { get; set; }

    /// <summary>
    ///     Get the protobuf definitions related to a contract
    /// </summary>
    /// <param name="address">contract address</param>
    /// <returns></returns>
    [ExceptionHandler(typeof(Exception), TargetType = typeof(ContractFileDescriptorSetAppService),
        MethodName = nameof(HandleExceptionWhileGettingContractFileDescriptorSet))]
    public async Task<byte[]> GetContractFileDescriptorSetAsync(string address)
    {
        return await GetFileDescriptorSetAsync(Address.FromBase58(address));
    }

    private async Task<byte[]> GetFileDescriptorSetAsync(Address address)
    {
        var chain = await _blockchainService.GetChainAsync();
        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };

        return await _transactionReadOnlyExecutionService.GetFileDescriptorSetAsync(chainContext, address);
    }
}