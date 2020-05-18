using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Deployer;
using AElf.Contracts.Genesis;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Testing;
using Volo.Abp.Threading;
using Xunit;
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


        public IReadOnlyDictionary<string, byte[]> Codes => _codes ??= ContractsDeployer.GetContractCodes<TModule>();

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

        public ContractTestBase()
        {
            var blockchainService = Application.ServiceProvider.GetService<IBlockchainService>();
            var chain = AsyncHelper.RunSync(() => blockchainService.GetChainAsync());
            var block = AsyncHelper.RunSync(()=>blockchainService.GetBlockByHashAsync(chain.GenesisBlockHash));
            var transactionResultManager = Application.ServiceProvider.GetService<ITransactionResultManager>();
            var transactionResults = AsyncHelper.RunSync(() =>
                transactionResultManager.GetTransactionResultsAsync(block.Body.TransactionIds, block.GetHash()));
            foreach (var transactionResult in transactionResults)
            {
                Assert.True(transactionResult.Status == TransactionResultStatus.Mined, transactionResult.Error);
            }
        }

        protected async Task<Address> DeployContractAsync(int category, byte[] code, Hash name, ECKeyPair senderKey)
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
            var res = await zeroStub.DeploySystemSmartContract.SendAsync(new SystemContractDeploymentInput
            {
                Category = category,
                Code = ByteString.CopyFrom(code),
                Name = name,
                TransactionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
            });
            if (res.TransactionResult == null || res.TransactionResult.Status != TransactionResultStatus.Mined)
            {
                throw new Exception($"DeploySystemSmartContract failed: {res.TransactionResult}");
            }
            
            var address = await zeroStub.GetContractAddressByName.CallAsync(name);	
            await ContractAddressService.SetSmartContractAddressAsync(new BlockIndex
            {
                BlockHash = res.TransactionResult.BlockHash,
                BlockHeight = res.TransactionResult.BlockNumber
            }, name.ToStorageKey(), address);

            return res.Output;
        }

        protected T GetTester<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new()
        {
            var factory = Application.ServiceProvider.GetRequiredService<IContractTesterFactory>();
            return factory.Create<T>(contractAddress, senderKey);
        }

        protected IReadOnlyDictionary<string, byte[]> GetPatchedCodes(string dir)
        {
            return ContractsDeployer.GetContractCodes<TModule>(dir, true);
        }

        // byte[] ReadPatchedContractCode(Type contractType)
        // {
        //     return ReadCode(ContractPatchedDllDir + contractType.Module + ".patched");
        // }
        //
        // byte[] ReadCode(string path)
        // {
        //     return File.Exists(path)
        //         ? File.ReadAllBytes(path)
        //         : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
        // }
    }
}