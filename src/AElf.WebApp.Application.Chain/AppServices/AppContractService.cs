using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain.AppServices
{
    public interface IAppContractService : IApplicationService
    {
        /// <summary>
        /// Get the protobuf definitions related to a contract
        /// </summary>
        /// <param name="address">contract address</param>
        /// <returns></returns>
        Task<byte[]> GetContractFileDescriptorSetAsync(string address);

        /// <summary>
        /// Gets the contract method descriptor asynchronous.
        /// </summary>
        /// <param name="contractAddress">The contract address.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <returns></returns>
        Task<MethodDescriptor> GetContractMethodDescriptorAsync(Address contractAddress,
            string methodName);
    }

    /// <summary>
    /// contract services
    /// </summary>
    /// <seealso cref="IAppContractService" />
    public sealed class AppContractService : IAppContractService
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly IBlockchainService _blockchainService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppContractService"/> class.
        /// </summary>
        /// <param name="transactionReadOnlyExecutionService">The transaction read only execution service.</param>
        /// <param name="blockchainService">The blockchain service.</param>
        public AppContractService(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IBlockchainService blockchainService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _blockchainService = blockchainService;
        }

        /// <summary>
        /// Get the protobuf definitions related to a contract
        /// </summary>
        /// <param name="address">contract address</param>
        /// <returns></returns>
        public async Task<byte[]> GetContractFileDescriptorSetAsync(string address)
        {
            try
            {
                var result = await GetFileDescriptorSetAsync(Address.Parse(address));
                return result;
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }
        }

        /// <summary>
        /// Gets the contract method descriptor asynchronous.
        /// </summary>
        /// <param name="contractAddress">The contract address.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        internal async Task<MethodDescriptor> GetContractMethodDescriptorAsync(Address contractAddress,
            string methodName)
        {
            IEnumerable<FileDescriptor> fileDescriptors;
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

        /// <summary>
        /// Gets the file descriptors asynchronous.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns></returns>
        private async Task<IEnumerable<FileDescriptor>> GetFileDescriptorsAsync(Address address)
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return await _transactionReadOnlyExecutionService.GetFileDescriptorsAsync(chainContext, address);
        }

        private async Task<byte[]> GetFileDescriptorSetAsync(Address address)
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return await _transactionReadOnlyExecutionService.GetFileDescriptorSetAsync(chainContext, address);
        }
    }
}