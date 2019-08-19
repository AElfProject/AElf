using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using AElf.Types;
using AElf.Contracts.Configuration;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Miner.Application
{
    public class BlockTransactionLimitProvider : IBlockTransactionLimitProvider, ISingletonDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        public ILogger<BlockTransactionLimitProvider> Logger { get; set; }

        private Address ConfigurationContractAddress => _smartContractAddressService.GetAddressByContractName(
            ConfigurationSmartContractAddressNameProvider.Name);

        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        private int _limit = -1;

        public BlockTransactionLimitProvider(ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IBlockchainService blockchainService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            Logger = NullLogger<BlockTransactionLimitProvider>.Instance;
        }

        public async Task<int> GetLimitAsync()
        {
            if (_limit != -1)
            {
                return _limit;
            }

            // Call ConfigurationContract GetBlockTransactionLimit()
            try
            {
                _limit = Int32Value.Parser.ParseFrom(await CallContractMethodAsync(
                    ConfigurationContractAddress,
                    nameof(ConfigurationContainer.ConfigurationStub.GetBlockTransactionLimit),
                    new Empty())).Value;
                Logger.LogInformation($"Get blockTransactionLimit: {_limit} by ConfigurationStub");
                return _limit;
            }
            catch (InvalidOperationException e)
            {
                Logger.LogWarning($"Invalid ConfigurationContractAddress :{e.Message}");
                return _limit = 0;
            }
        }

        public void SetLimit(int limit)
        {
            _limit = limit;
        }

        #region GetLimit

        private async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            IMessage input)
        {
            var tx = new Transaction
            {
                From = FromAddress,
                To = contractAddress,
                MethodName = methodName,
                Params = input.ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
            var preBlock = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var transactionTrace = await _transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
            {
                BlockHash = preBlock.GetHash(),
                BlockHeight = preBlock.Height
            }, tx, TimestampHelper.GetUtcNow());

            return transactionTrace.ReturnValue;
        }

        #endregion
    }
}