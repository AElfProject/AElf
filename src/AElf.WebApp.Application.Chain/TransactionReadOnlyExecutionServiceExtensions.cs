using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.Reflection;
using Volo.Abp;

namespace AElf.WebApp.Application.Chain
{
    public static class TransactionReadOnlyExecutionServiceExtensions
    {
        public static async Task<MethodDescriptor> GetContractMethodDescriptorAsync(
            this ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, IChainContext chainContext,
            Address contractAddress, string methodName, bool throwException = true)
        {
            IEnumerable<FileDescriptor> fileDescriptors;

            try
            {
                fileDescriptors =
                    await transactionReadOnlyExecutionService.GetFileDescriptorsAsync(chainContext, contractAddress);
            }
            catch
            {
                if (throwException)
                    throw new UserFriendlyException(Error.Message[Error.InvalidContractAddress],
                        Error.InvalidContractAddress.ToString());
                return null;
            }

            return fileDescriptors
                .Select(fileDescriptor =>
                    fileDescriptor.Services.Select(s => s.FindMethodByName(methodName)).FirstOrDefault())
                .FirstOrDefault(method => method != null);
        }
    }
}