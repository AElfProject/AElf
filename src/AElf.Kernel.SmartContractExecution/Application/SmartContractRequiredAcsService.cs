using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class SmartContractRequiredAcsService : ISmartContractRequiredAcsService
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        //TODO: strange way
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        public SmartContractRequiredAcsService(ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        public async Task<RequiredAcs> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight)
        {
            var chainContext = new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            };
            var configurationContractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                    ConfigurationSmartContractAddressNameProvider.StringName);
            var tx = new Transaction
            {
                From = FromAddress,
                To = configurationContractAddress,
                MethodName = nameof(ConfigurationContainer.ConfigurationStub.GetConfiguration),
                Params = new StringValue {Value = RequiredAcsInContractsConfigurationNameProvider.Name}.ToByteString(),
                Signature = ByteString.CopyFromUtf8(KernelConstants.SignaturePlaceholder)
            };

            var returned = await _transactionReadOnlyExecutionService.ExecuteAsync<BytesValue>(
                chainContext, tx, TimestampHelper.GetUtcNow(), false);

            var requiredAcsInContracts = new RequiredAcsInContracts();
            requiredAcsInContracts.MergeFrom(returned.Value);
            return new RequiredAcs
            {
                AcsList = requiredAcsInContracts.AcsList.ToList(),
                RequireAll = requiredAcsInContracts.RequireAll
            };
        }
    }
}