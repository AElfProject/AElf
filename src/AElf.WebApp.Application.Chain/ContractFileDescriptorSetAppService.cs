using System;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IContractFileDescriptorSetAppService : IApplicationService
    {
        Task<byte[]> GetContractFileDescriptorSetAsync(string address);
    }

    [ControllerName("BlockChain")]
    public class ContractFileDescriptorSetAppService : IContractFileDescriptorSetAppService
    {
        private static IBlockchainService _blockchainService;
        private static ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        
        public ILogger<ContractFileDescriptorSetAppService> Logger { get; set; }

        public ContractFileDescriptorSetAppService(IBlockchainService blockchainService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _blockchainService = blockchainService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
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
                var result = await GetFileDescriptorSetAsync(Address.FromBase58(address));
                return result;
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Error during GetContractFileDescriptorSetAsync.");
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }
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