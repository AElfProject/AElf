using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.Reflection;
using Volo.Abp;

namespace AElf.WebApp.Application.Chain;

public static class TransactionReadOnlyExecutionServiceExtensions
{
    public static async Task<MethodDescriptor> GetContractMethodDescriptorAsync(
        this ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, IChainContext chainContext,
        Address contractAddress, string methodName, bool throwException = true)
    {
        var isValid =
            await IsValidContractAddressAsync(transactionReadOnlyExecutionService, chainContext, contractAddress);
        if (!isValid)
        {
            if (throwException)
                throw new UserFriendlyException(Error.Message[Error.InvalidContractAddress],
                    Error.InvalidContractAddress.ToString());
            return null;
        }

        var fileDescriptors =
            await transactionReadOnlyExecutionService.GetFileDescriptorsAsync(chainContext, contractAddress);

        return fileDescriptors
            .Select(fileDescriptor =>
                fileDescriptor.Services.Select(s => s.FindMethodByName(methodName)).FirstOrDefault())
            .FirstOrDefault(method => method != null);
    }

    [ExceptionHandler(typeof(Exception), typeof(TransactionReadOnlyExecutionServiceExtensions),
        MethodName = nameof(HandleExceptionWhileGettingFileDescriptor))]
    private static async Task<bool> IsValidContractAddressAsync(
        ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, IChainContext chainContext,
        Address contractAddress)
    {
        var fileDescriptors =
            await transactionReadOnlyExecutionService.GetFileDescriptorsAsync(chainContext, contractAddress);
        return fileDescriptors != null;
    }

    internal static Task<FlowBehavior> HandleExceptionWhileGettingFileDescriptor(Exception ex)
    {
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        });
    }
}