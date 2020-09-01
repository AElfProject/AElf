using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public sealed class TransactionReadOnlyExecutionServiceTests : SmartContractTestBase
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly SmartContractHelper _smartContractHelper;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
        
        public TransactionReadOnlyExecutionServiceTests()
        {
            _transactionReadOnlyExecutionService = GetRequiredService<ITransactionReadOnlyExecutionService>();
            _smartContractHelper = GetRequiredService<SmartContractHelper>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _defaultContractZeroCodeProvider = GetRequiredService<IDefaultContractZeroCodeProvider>();
            _smartContractExecutiveProvider = GetRequiredService<ISmartContractExecutiveProvider>();
        }

        [Fact]
        public async Task ExecuteAsync_Test()
        {
            var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var transaction = new Transaction
            {
                From = SampleAddress.AddressList[0],
                To = SampleAddress.AddressList[0],
                MethodName = "NotExistMethod",
                Params = ByteString.Empty
            };
            _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction, TimestampHelper.GetUtcNow())
                .ShouldThrow<SmartContractFindRegistrationException>();
            _transactionReadOnlyExecutionService.ExecuteAsync<Address>(chainContext, transaction, TimestampHelper.GetUtcNow(),false)
                .ShouldThrow<SmartContractFindRegistrationException>();

            transaction = new Transaction
            {
                From = SampleAddress.AddressList[0],
                To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                MethodName = "NotExistMethod",
                Params = ByteString.Empty
            };
            var trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction,
                TimestampHelper.GetUtcNow());
            trace.Error.ShouldContain("Failed to find handler for NotExistMethod");
            trace.ExecutionStatus.ShouldBe(ExecutionStatus.SystemError);
            var hash = await _transactionReadOnlyExecutionService.ExecuteAsync<Hash>(chainContext, transaction,
                TimestampHelper.GetUtcNow(), false);
            hash.ShouldBeNull();
            _transactionReadOnlyExecutionService.ExecuteAsync<Hash>(chainContext, transaction,
                TimestampHelper.GetUtcNow(), true).ShouldThrow<SmartContractExecutingException>();

            _smartContractExecutiveProvider.GetPool(_defaultContractZeroCodeProvider.ContractZeroAddress).Single()
                .ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
            transaction = new Transaction
            {
                From = SampleAddress.AddressList[0],
                To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                MethodName = nameof(ACS0Container.ACS0Stub.GetSmartContractRegistrationByAddress),
                Params = _defaultContractZeroCodeProvider.ContractZeroAddress.ToByteString()
            };
            trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction,
                TimestampHelper.GetUtcNow());
            trace.ExecutionStatus.ShouldBe(ExecutionStatus.Executed);
            var smartContractRegistration = SmartContractRegistration.Parser.ParseFrom(trace.ReturnValue);
            CheckSmartContractRegistration(smartContractRegistration);

            _smartContractExecutiveProvider.GetPool(_defaultContractZeroCodeProvider.ContractZeroAddress).Single()
                .ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);

            smartContractRegistration = await _transactionReadOnlyExecutionService.ExecuteAsync<SmartContractRegistration>(chainContext, transaction,
                TimestampHelper.GetUtcNow(), true);

            CheckSmartContractRegistration(smartContractRegistration);
            
            _smartContractExecutiveProvider.GetPool(_defaultContractZeroCodeProvider.ContractZeroAddress).Single()
                .ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
        }

        private void CheckSmartContractRegistration(SmartContractRegistration smartContractRegistration)
        {
            smartContractRegistration.Category.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration
                .Category);
            smartContractRegistration.Code.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration
                .Code);
            smartContractRegistration.CodeHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration
                .CodeHash);
        }

        [Fact]
        public async Task GetFileDescriptorSetAsync_Test()
        {
            var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            _transactionReadOnlyExecutionService.GetFileDescriptorSetAsync(chainContext, SampleAddress.AddressList[0])
                .ShouldThrow<SmartContractFindRegistrationException>();

            _smartContractExecutiveProvider.GetPool(_defaultContractZeroCodeProvider.ContractZeroAddress).Single()
                .ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
            var bytes = await _transactionReadOnlyExecutionService.GetFileDescriptorSetAsync(chainContext,
                _defaultContractZeroCodeProvider.ContractZeroAddress);
            var fileDescriptorSet = FileDescriptorSet.Parser.ParseFrom(bytes);
            fileDescriptorSet.ShouldNotBeNull();
            _smartContractExecutiveProvider.GetPool(_defaultContractZeroCodeProvider.ContractZeroAddress).Single()
                .ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
        }
        
        [Fact]
        public async Task GetFileDescriptorsAsync_Test()
        {
            var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            _transactionReadOnlyExecutionService.GetFileDescriptorsAsync(chainContext, SampleAddress.AddressList[0])
                .ShouldThrow<SmartContractFindRegistrationException>();

            _smartContractExecutiveProvider.GetPool(_defaultContractZeroCodeProvider.ContractZeroAddress).Single()
                .ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
            var fileDescriptors = await _transactionReadOnlyExecutionService.GetFileDescriptorsAsync(chainContext,
                _defaultContractZeroCodeProvider.ContractZeroAddress);
            fileDescriptors.Count().ShouldBeGreaterThan(0);
            _smartContractExecutiveProvider.GetPool(_defaultContractZeroCodeProvider.ContractZeroAddress).Single()
                .ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
        }

        [Fact]
        public async Task GetTransactionParametersAsync_Test()
        {
            var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var transaction = new Transaction
            {
                From = SampleAddress.AddressList[0],
                To = SampleAddress.AddressList[0],
                MethodName = "NotExistMethod",
                Params = ByteString.Empty
            };
            _transactionReadOnlyExecutionService.GetTransactionParametersAsync(chainContext, transaction)
                .ShouldThrow<SmartContractFindRegistrationException>();

            transaction = new Transaction
            {
                From = SampleAddress.AddressList[0],
                To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                MethodName = "NotExistMethod",
                Params = ByteString.Empty
            };
            var parameters = await _transactionReadOnlyExecutionService.GetTransactionParametersAsync(chainContext, transaction);
            parameters.ShouldBeEmpty();

            _smartContractExecutiveProvider.GetPool(_defaultContractZeroCodeProvider.ContractZeroAddress).Single()
                .ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
            transaction = new Transaction
            {
                From = SampleAddress.AddressList[0],
                To = _defaultContractZeroCodeProvider.ContractZeroAddress,
                MethodName = nameof(ACS0Container.ACS0Stub.GetSmartContractRegistrationByAddress),
                Params = _defaultContractZeroCodeProvider.ContractZeroAddress.ToByteString()
            };
            parameters = await _transactionReadOnlyExecutionService.GetTransactionParametersAsync(chainContext, transaction);
            parameters.Trim('"').ShouldBe(_defaultContractZeroCodeProvider.ContractZeroAddress.ToBase58());

            _smartContractExecutiveProvider.GetPool(_defaultContractZeroCodeProvider.ContractZeroAddress).Single()
                .ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
        }
    }
}