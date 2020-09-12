using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Runtime.CSharp.Core;
using AElf.Runtime.CSharp.Tests.TestContract;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp
{
    public class ExecutiveTests : CSharpRuntimeTestBase
    {
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
        private readonly IContractPatcher _patcher;

        public ExecutiveTests()
        {
            _hostSmartContractBridgeContextService = GetRequiredService<IHostSmartContractBridgeContextService>();
            _patcher = GetRequiredService<IContractPatcher>();
        }

        [Fact]
        public async Task Apply_ExceededMaxCallDepth_Test()
        {
            var executive = CreateExecutive();
            
            var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(_hostSmartContractBridgeContextService.Create());

            var transactionContext = CreateTransactionContext();
            transactionContext.CallDepth = 16;
            
            await executive.ApplyAsync(transactionContext);

            hostSmartContractBridgeContext.TransactionContext.ShouldBeNull();
            transactionContext.Trace.ExecutionStatus.ShouldBe(ExecutionStatus.ExceededMaxCallDepth);
            transactionContext.Trace.Error.ShouldContain("ExceededMaxCallDepth");
        }

        [Fact]
        public async Task Apply_MethodNotExist_Test()
        {
            var executive = CreateExecutive();

            var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(_hostSmartContractBridgeContextService.Create());

            var transactionContext = CreateTransactionContext();
            transactionContext.Transaction.MethodName = "NotExist";

            await executive.ApplyAsync(transactionContext);
            hostSmartContractBridgeContext.TransactionContext.ShouldBeNull();
            transactionContext.Trace.ExecutionStatus.ShouldBe(ExecutionStatus.SystemError);
            transactionContext.Trace.Error.ShouldContain("Failed to find handler for NotExist");
        }

        [Fact]
        public async Task Apply_Success_Test()
        {
            var executive = CreateExecutive();

            var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(_hostSmartContractBridgeContextService.Create());
            var transactionContext = CreateTransactionContext();
            
            await executive.ApplyAsync(transactionContext);
            
            hostSmartContractBridgeContext.TransactionContext.ShouldBeNull();
            transactionContext.Trace.ExecutionStatus.ShouldBe(ExecutionStatus.Executed);
            transactionContext.Trace.Error.ShouldBeNullOrEmpty();
            transactionContext.Trace.ReturnValue.ShouldBe((new Int32Output {Int32Value = -5}).ToByteString());
            transactionContext.Trace.StateSet.Reads.Count.ShouldBe(1);
            transactionContext.Trace.StateSet.Writes.Count.ShouldBe(1);
        }
        
        [Fact]
        public async Task Apply_ViewMethod_Test()
        {
            var executive = CreateExecutive();

            var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(_hostSmartContractBridgeContextService.Create());
            var transactionContext = CreateTransactionContext();
            transactionContext.Transaction.MethodName = "TestViewMethod";
            transactionContext.Transaction.Params = ByteString.Empty;
            
            await executive.ApplyAsync(transactionContext);
            
            hostSmartContractBridgeContext.TransactionContext.ShouldBeNull();
            transactionContext.Trace.ExecutionStatus.ShouldBe(ExecutionStatus.Executed);
            transactionContext.Trace.Error.ShouldBeNullOrEmpty();
            transactionContext.Trace.ReturnValue.ShouldBe((new Int32Output {Int32Value = 1}).ToByteString());
            transactionContext.Trace.StateSet.Reads.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task Apply_ExecutionObserver_Test()
        {
            var contractCode = File.ReadAllBytes(typeof(TestContract).Assembly.Location);
            var code = _patcher.Patch(contractCode, false);
            var assembly = Assembly.Load(code);
            var executive = new Executive(assembly)
            {
            };

            var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(_hostSmartContractBridgeContextService.Create());
            var transactionContext = CreateTransactionContext();
            
            await executive.ApplyAsync(transactionContext);
            
            hostSmartContractBridgeContext.TransactionContext.ShouldBeNull();
            transactionContext.Trace.ExecutionStatus.ShouldBe(ExecutionStatus.Executed);
            transactionContext.Trace.Error.ShouldBeNullOrEmpty();
            transactionContext.Trace.ReturnValue.ShouldBe((new Int32Output {Int32Value = -5}).ToByteString());
            transactionContext.Trace.StateSet.Reads.Count.ShouldBe(1);
            transactionContext.Trace.StateSet.Writes.Count.ShouldBe(1);
        }

        [Fact]
        public void GetJsonStringOfParameters_Test()
        {
            var executive = CreateExecutive();
            var json = executive.GetJsonStringOfParameters("NotExist", new Int32Input{Int32Value = 5}.ToByteArray());
            json.ShouldBeEmpty();
            
            json = executive.GetJsonStringOfParameters("TestInt32State", new Int32Input{Int32Value = 5}.ToByteArray());
            json.ShouldBe("{ \"int32Value\": 5 }");
        }

        [Fact]
        public void Descriptors_Test()
        {
            var executive = CreateExecutive();
            executive.Descriptors.Count.ShouldBe(1);
            executive.Descriptors[0].File.Name.ShouldBe("test_contract.proto");
            executive.Descriptors[0].FullName.ShouldBe("TestContract");
        }

        [Fact]
        public void GetFileDescriptors_Test()
        {
            var executive = CreateExecutive();
            var fileDescriptors = executive.GetFileDescriptors();

            var descriptors = fileDescriptors.ToList();
            descriptors.Count.ShouldBe(5);
            descriptors.ShouldContain(f => f.Name == "google/protobuf/descriptor.proto");
            descriptors.ShouldContain(f => f.Name == "google/protobuf/empty.proto");
            descriptors.ShouldContain(f => f.Name == "google/protobuf/timestamp.proto");
            descriptors.ShouldContain(f => f.Name == "aelf/options.proto");
            descriptors.ShouldContain(f => f.Name == "test_contract.proto");
            
            var set = new FileDescriptorSet();
            set.File.AddRange(descriptors.Select(x => x.SerializedData));

            var fileDescriptorSet = executive.GetFileDescriptorSet();
            fileDescriptorSet.ShouldBe(set.ToByteArray());
        }

        [Fact]
        public void IsView_Test()
        {
            var executive = CreateExecutive();

            Assert.Throws<RuntimeException>(() => executive.IsView("NotExist"));
            executive.IsView("TestViewMethod").ShouldBeTrue();
            executive.IsView("TestBoolState").ShouldBeFalse();
        }

        private Executive CreateExecutive()
        {
            var contractCode = File.ReadAllBytes(typeof(TestContract).Assembly.Location);
            var assembly = Assembly.Load(contractCode);
            var executive = new Executive(assembly)
            {
            };
            return executive;
        }

        private TransactionContext CreateTransactionContext()
        {
            return new TransactionContext
            {
                Transaction = new Transaction
                {
                    From = SampleAddress.AddressList[0],
                    To = SampleAddress.AddressList[1],
                    MethodName = "TestInt32State",
                    Params = (new Int32Input{Int32Value = 5}).ToByteString()
                },
                Trace = new TransactionTrace(),
                ExecutionObserverThreshold = new ExecutionObserverThreshold
                {
                    ExecutionBranchThreshold = -1,
                    ExecutionCallThreshold = -1
                },
                CallDepth = 1,
                MaxCallDepth = 15,
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                BlockHeight = 1,
                CurrentBlockTime = TimestampHelper.GetUtcNow()
            };
        }
    }
}