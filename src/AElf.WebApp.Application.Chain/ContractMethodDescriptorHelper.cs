using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;

namespace AElf.WebApp.Application.Chain
{
    public static class ContractMethodDescriptorHelper
    {
        private static IBlockchainService _blockchainService;
        private static ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        internal static async Task<MethodDescriptor> GetContractMethodDescriptorAsync(
            IBlockchainService blockchainService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, Address contractAddress,
            string methodName)
        {
            IEnumerable<FileDescriptor> fileDescriptors;
            _blockchainService = blockchainService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;

            try
            {
                fileDescriptors = await GetFileDescriptorsAsync(contractAddress);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidContractAddress],
                    Error.InvalidContractAddress.ToString());
            }

            foreach (var fileDescriptor in fileDescriptors)
            {
                var method = fileDescriptor.Services.Select(s => s.FindMethodByName(methodName)).FirstOrDefault();
                if (method == null) continue;
                return method;
            }

            return null;
        }

        private static async Task<IEnumerable<FileDescriptor>> GetFileDescriptorsAsync(Address address)
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return await _transactionReadOnlyExecutionService.GetFileDescriptorsAsync(chainContext, address);
        }
    }
}