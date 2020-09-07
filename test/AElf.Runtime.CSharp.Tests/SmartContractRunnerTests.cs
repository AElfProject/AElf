using System.IO;
using System.Threading.Tasks;
using AElf.Runtime.CSharp.Tests.TestContract;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp
{
    public sealed class SmartContractRunnerTests: CSharpRuntimeTestBase
    {
        [Fact]
        public async Task Run_Test()
        {
            var contractCode = File.ReadAllBytes(typeof(TestContract).Assembly.Location);
            var smartContractRegistration = new SmartContractRegistration()
            {
                Code = ByteString.CopyFrom(contractCode),
                CodeHash = HashHelper.ComputeFrom(contractCode),
                IsSystemContract = true
            };
            
            var sdkDir = Path.GetDirectoryName(typeof(CSharpSmartContractRunner).Assembly.Location);
            var smartContractRunner = new CSharpSmartContractRunner(sdkDir);
            
            var executive = await smartContractRunner.RunAsync(smartContractRegistration);
            executive.ShouldNotBe(null);
            executive.ContractHash.ShouldBe(smartContractRegistration.CodeHash);
            executive.ContractVersion.ShouldBe("1.0.0.0");
            
            smartContractRunner.ContractVersion.ShouldBe("1.0.0.0");
        }
    }
}