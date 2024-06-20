using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using FileDescriptorSet = AElf.Runtime.CSharp.FileDescriptorSet;

namespace AElf.WebApp.Application.Chain;

public interface IContractFileDescriptorSetAppService
{
    Task<byte[]> GetContractFileDescriptorSetAsync(string address);
    Task<string[]> GetContractViewMethodListAsync(string address);
}

public class ContractFileDescriptorSetAppService : ApplicationService, IContractFileDescriptorSetAppService
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
    public async Task<byte[]> GetContractFileDescriptorSetAsync(string address)
    {
        try
        {
            var result = await GetFileDescriptorSetAsync(Address.FromBase58(address));
            return result;
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Error during GetContractFileDescriptorSetAsync");
            throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
        }
    }

    /// <summary>
    ///     Get the view method list of a contract
    /// </summary>
    /// <param name="address">contract address</param>
    /// <returns></returns>
    public async Task<string[]> GetContractViewMethodListAsync(string address)
    {
        try
        {
            var set = new FileDescriptorSet();
            var fds = await GetFileDescriptorSetAsync(Address.FromBase58(address));
            set.MergeFrom(ByteString.CopyFrom(fds));
            var fdList = FileDescriptor.BuildFromByteStrings(set.File, new ExtensionRegistry
            {
                OptionsExtensions.IsView,
                // OptionsExtensions.Identity,
                // OptionsExtensions.Base,
                // OptionsExtensions.CsharpState,
                // OptionsExtensions.IsEvent,
                // OptionsExtensions.IsIndexed
            });
            var viewMethodList =
                (from fd in fdList
                    from service in fd.Services
                    from method in service.Methods
                    where method.GetOptions().GetExtension(OptionsExtensions.IsView)
                    select method.Name).ToList();
            return viewMethodList.ToArray();
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Error during GetContractViewMethodListAsync");
            throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
        }
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