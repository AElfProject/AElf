using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IRequiredAcsInContractsProvider
    {
        Task<RequiredAcsDto> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight);
    }

    public class RequiredAcsInContractsProvider : IRequiredAcsInContractsProvider, ISingletonDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        
        private Address ConfigurationContractAddress => _smartContractAddressService.GetAddressByContractName(
            ConfigurationSmartContractAddressNameProvider.Name);
        
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        public RequiredAcsInContractsProvider(ISmartContractAddressService smartContractAddressService, 
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        public async Task<RequiredAcsDto> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight)
        {
            var result = await GetRequiredAcsAsync(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });

            var returned = RequiredAcsInContracts.Parser.ParseFrom(result);

            return new RequiredAcsDto
            {
                AcsList = returned.AcsList.ToList(),
                RequireAll = returned.RequireAll
            };
        }

        private async Task<ByteString> GetRequiredAcsAsync(IChainContext chainContext)
        {
            var tx = new Transaction
            {
                From = FromAddress,
                To = ConfigurationContractAddress,
                MethodName = nameof(ConfigurationContainer.ConfigurationStub.GetBlockTransactionLimit),
                Params = new Empty().ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };

            var transactionTrace =
                await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, TimestampHelper.GetUtcNow());

            return transactionTrace.ReturnValue;
        }
    }
}