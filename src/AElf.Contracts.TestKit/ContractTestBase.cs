using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Xunit.Abstractions;

namespace AElf.Contracts.TestKit
{
    public class ContractTestBase : ContractTestBase<ContractTestModule>
    {
    }

    public class ContractTestBase<TModule> : AbpIntegratedTest<TModule>
        where TModule : ContractTestModule
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            options.UseAutofac();
        }

        protected void SetTestOutputHelper(ITestOutputHelper testOutputHelper)
        {
            GetRequiredService<ITestOutputHelperAccessor>().OutputHelper = testOutputHelper;
        }

        protected ISmartContractAddressService ContractAddressService =>
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();

        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

        protected async Task<Address> DeployContractAsync(int category, byte[] code, ECKeyPair senderKey)
        {
            var zeroStub = GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, senderKey);
            var res = await zeroStub.DeploySmartContract.SendAsync(new ContractDeploymentInput()
            {
                Category = category,
                Code = ByteString.CopyFrom(code)
            });
            return res.Output;
        }

        protected async Task<Address> DeploySystemSmartContract(int category, byte[] code, Hash name,
            ECKeyPair senderKey)
        {
            var zeroStub = GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, senderKey);
            var res = await zeroStub.DeploySystemSmartContract.SendAsync(new SystemContractDeploymentInput()
            {
                Category = category,
                Code = ByteString.CopyFrom(code),
                Name = name,
                TransactionMethodCallList = new SystemTransactionMethodCallList()
            });
            return res.Output;
        }

        public T GetTester<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new()
        {
            var factory = Application.ServiceProvider.GetRequiredService<IContractTesterFactory>();
            return factory.Create<T>(contractAddress, senderKey);
        }
    }
}