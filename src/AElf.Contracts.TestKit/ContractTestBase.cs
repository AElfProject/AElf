using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Deployer;
using AElf.Contracts.Genesis;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
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
        private IReadOnlyDictionary<string, byte[]> _codes;

        public IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ?? (_codes = ContractsDeployer.GetContractCodes<TModule>());

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
                TransactionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
            });
            if (res.TransactionResult.Status != TransactionResultStatus.Mined)
            {
                throw new Exception($"DeploySystemSmartContract failed: {res.TransactionResult}");
            }
            return res.Output;
        }

        protected async Task<List<Address>> DeploySystemSmartContract(int category, Dictionary<Hash,byte[]> nameCodeDic,
            ECKeyPair senderKey)
        {
            var addressList = new List<Address>();
            var zeroStub = GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, senderKey);
            foreach (var kv in nameCodeDic)
            {
                var res = await zeroStub.DeploySystemSmartContract.SendAsync(new SystemContractDeploymentInput()
                {
                    Category = category,
                    Code = ByteString.CopyFrom(kv.Value),
                    Name = kv.Key,
                    TransactionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                });
                if (res.TransactionResult.Status != TransactionResultStatus.Mined)
                {
                    throw new Exception($"DeploySystemSmartContract failed: {res.TransactionResult}");
                }
                addressList.Add(res.Output);
            }

            return addressList;
        }

        public T GetTester<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new()
        {
            var factory = Application.ServiceProvider.GetRequiredService<IContractTesterFactory>();
            return factory.Create<T>(contractAddress, senderKey);
        }
        
        public T GetTester<T>(Dictionary<string,Address> contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new()
        {
            var factory = Application.ServiceProvider.GetRequiredService<IContractTesterFactory>();
            return factory.Create<T>(contractAddress, senderKey);
        } 
    }
}