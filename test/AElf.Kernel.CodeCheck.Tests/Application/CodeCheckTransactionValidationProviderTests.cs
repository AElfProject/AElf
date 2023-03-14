using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Tests;
using AElf.Kernel.Txn.Application;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.CodeCheck.Application;

public class CodeCheckTransactionValidationProviderTests : CodeCheckTestBase
{
    private readonly ITransactionValidationProvider _transactionValidationProvider;
    private readonly KernelTestHelper _kernelTestHelper;
    private readonly ILocalEventBus _eventBus;

    public CodeCheckTransactionValidationProviderTests()
    {
        _transactionValidationProvider = GetRequiredService<ITransactionValidationProvider>();
        _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        _eventBus = GetRequiredService<ILocalEventBus>();
    }

    [Fact]
    public async Task ValidateTransaction_NotInterestedMethod_Test()
    {
        TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
        _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
        {
            transactionValidationStatusChangedEventData = d;
            return Task.CompletedTask;
        });
        
        var transaction = _kernelTestHelper.GenerateTransaction();
        var result = await _transactionValidationProvider.ValidateTransactionAsync(transaction);
        result.ShouldBeTrue();
        
        transactionValidationStatusChangedEventData.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateTransaction_InterestedMethod_Test()
    {
        TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
        _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
        {
            transactionValidationStatusChangedEventData = d;
            return Task.CompletedTask;
        });

        var chainContext = new ChainContext
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash"),
            BlockHeight = 100
        };
        
        var transaction = _kernelTestHelper.GenerateTransaction();
        transaction.MethodName = nameof(ACS0Container.ACS0Stub.DeployUserSmartContract);
        transaction.Params = (new ContractDeploymentInput
        {
            Category = 0,
            Code = ByteString.CopyFrom(new byte[10]),
        }).ToByteString();
        
        var result = await _transactionValidationProvider.ValidateTransactionAsync(transaction, chainContext);
        result.ShouldBeTrue();
        transactionValidationStatusChangedEventData.ShouldBeNull();
        
        transaction.MethodName = nameof(ACS0Container.ACS0Stub.UpdateUserSmartContract);
        transaction.Params = (new ContractUpdateInput
        {
            Code = ByteString.CopyFrom(new byte[10]),
            Address = NormalAddress
        }).ToByteString();
        
        result = await _transactionValidationProvider.ValidateTransactionAsync(transaction, chainContext);
        result.ShouldBeTrue();
        transactionValidationStatusChangedEventData.ShouldBeNull();
    }
    
    [Fact]
    public async Task ValidateTransaction_Update_ContractNotExist_Test()
    {
        TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
        _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
        {
            transactionValidationStatusChangedEventData = d;
            return Task.CompletedTask;
        });
        
        var chainContext = new ChainContext
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash"),
            BlockHeight = 100
        };
        
        var transaction = _kernelTestHelper.GenerateTransaction();

        transaction.To = ZeroContractFakeAddress;
        transaction.MethodName = nameof(ACS0Container.ACS0Stub.UpdateUserSmartContract);
        transaction.Params = (new ContractUpdateInput
        {
            Code = ByteString.Empty,
            Address = SampleAddress.AddressList[1]
        }).ToByteString();
        
        var result = await _transactionValidationProvider.ValidateTransactionAsync(transaction, chainContext);
        result.ShouldBeFalse();
        transactionValidationStatusChangedEventData.ShouldNotBeNull();
        transactionValidationStatusChangedEventData.TransactionId.ShouldBe(transaction.GetHash());
    }

    [Fact]
    public async Task ValidateTransaction_Update_NotZeroContract_Test()
    {
        TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
        _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
        {
            transactionValidationStatusChangedEventData = d;
            return Task.CompletedTask;
        });
        
        var chainContext = new ChainContext
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash"),
            BlockHeight = 100
        };
        
        var transaction = _kernelTestHelper.GenerateTransaction();

        transaction.To = NormalAddress;
        transaction.MethodName = nameof(ACS0Container.ACS0Stub.UpdateUserSmartContract);
        transaction.Params = (new ContractUpdateInput
        {
            Code = ByteString.Empty,
            Address = SampleAddress.AddressList[1]
        }).ToByteString();
        
        var result = await _transactionValidationProvider.ValidateTransactionAsync(transaction, chainContext);
        result.ShouldBeTrue();
        transactionValidationStatusChangedEventData.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateTransaction_WrongTransaction_Test()
    {
        TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
        _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
        {
            transactionValidationStatusChangedEventData = d;
            return Task.CompletedTask;
        });
        
        var chainContext = new ChainContext
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash"),
            BlockHeight = 100
        };
        
        var transaction = _kernelTestHelper.GenerateTransaction();

        transaction.MethodName = nameof(ACS0Container.ACS0Stub.UpdateUserSmartContract);
        transaction.Params = (new ContractUpdateInput
        {
            Code = ByteString.CopyFrom(new byte[10]),
            Address = SampleAddress.AddressList.First()
        }).ToByteString();
        
        var result = await _transactionValidationProvider.ValidateTransactionAsync(transaction, chainContext);
        result.ShouldBeFalse();
        transactionValidationStatusChangedEventData.ShouldNotBeNull();
        transactionValidationStatusChangedEventData.TransactionId.ShouldBe(transaction.GetHash());
    }
}