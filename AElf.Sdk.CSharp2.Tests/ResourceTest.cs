using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Contracts.Resource;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.SmartContract;
using AElf.SmartContract;
using Shouldly;
using Xunit;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp2.Tests.TestContract;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Sdk.CSharp2.Tests
{
    public class ResourceTest
    {
        private List<Address> ContractAddresses { get; } =
            new[] {"a", "b"}.Select(Address.FromString).ToList();

        private List<Address> UserAddresses { get; } = new[]
            {"d", "e", "f"}.Select(Address.FromString).ToList();

        private IStateManager StateManager = new MockStateManager();
        private TokenContract TokenContract { get; } = new TokenContract();
        private ResourceContract ResourceContract { get; } = new ResourceContract();

        public ResourceTest()
        {
            InitContractsService();
        }

        private void InitContractsService()
        {
            //Init token contract
            var stateProvider = new MockStateProviderFactory(StateManager).CreateStateProvider();
            var chainService = new MockChainService();

            TokenContract.SetStateProvider(stateProvider);
            TokenContract.SetSmartContractContext(new SmartContractContext()
            {
                ContractAddress = ContractAddresses[0],
                ChainService = chainService
            });
            TokenContract.SetTransactionContext(new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = UserAddresses[0],
                    To = ContractAddresses[0]
                }
            });
            TokenContract.SetContractAddress(ContractAddresses[0]);

            //Init resource conotract
            ResourceContract.SetStateProvider(stateProvider);
            ResourceContract.SetSmartContractContext(new SmartContractContext()
            {
                ContractAddress = ContractAddresses[1],
                ChainService = chainService
            });
            ResourceContract.SetTransactionContext(new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = UserAddresses[0],
                    To = ContractAddresses[1]
                }
            });
            ResourceContract.SetContractAddress(ContractAddresses[1]);
        }

        private void InitToken()
        {
            TokenContract.Initialize("ELF", "elf test token", 1000000, 9);
        }

        [Fact]
        public void Init_TokenContract()
        {
            InitToken();
            TokenContract.Symbol().ShouldBe("ELF");
            TokenContract.TokenName().ShouldBe("elf test token");
            TokenContract.TotalSupply().ShouldBe(1000000UL);
            TokenContract.Decimals().ShouldBe(9U);
            var balance = TokenContract.BalanceOf(UserAddresses[0]);
            balance.ShouldBe(1000000UL);
        }

        private void InitResource()
        {
            InitToken();
            ResourceContract.Initialize(ContractAddresses[0], UserAddresses[1], UserAddresses[0]);
        }

        [Fact]
        public void Init_ResourceContract()
        {
            InitResource();

            ResourceContract.GetElfTokenAddress().ShouldBe(ContractAddresses[0].GetFormatted());
            ResourceContract.GetFeeAddress().ShouldBe(UserAddresses[1].GetFormatted());
            ResourceContract.GetResourceControllerAddress().ShouldBe(UserAddresses[0].GetFormatted());

            var cpuConverter = ResourceContract.GetConverter("Cpu");
            var cpuObj = JsonConvert.DeserializeObject<JObject>(cpuConverter);
            cpuObj["ResourceType"].ToString().ShouldBe("Cpu");
            var ramConverter = ResourceContract.GetConverter("Ram");
            ramConverter.ShouldNotBe(string.Empty);
            var netConverter = ResourceContract.GetConverter("Net");
            netConverter.ShouldNotBe(string.Empty);

            var elfBalance = ResourceContract.GetElfBalance("Cpu");
            elfBalance.ShouldBe(1000000UL);
            var exchangeBalance = ResourceContract.GetExchangeBalance("Cpu");
            exchangeBalance.ShouldBe(1000000UL);

        }

        [Fact]
        public void Issue_Cpu_Resource()
        {
            InitResource();

            ResourceContract.IssueResource("CPU", 1000000UL);
            var cpuConverter = ResourceContract.GetConverter("CPU");
            var cpuObj = JsonConvert.DeserializeObject<JObject>(cpuConverter);
            cpuObj["ResBalance"].Value<ulong>().ShouldBe(2000000UL);

            //Check another account
            SwitchOwner(UserAddresses[2]);
            Should.Throw<AssertionError>(() =>
            {
                ResourceContract.IssueResource("CPU", 1000000UL);
            });
        }

        [Fact(Skip = "Due to no execute service, cannot call other contract method from another contract.")]
        public void Buy_Cpu_Resource()
        {
            InitResource();

            //User with enough balance
            ResourceContract.BuyResource("Cpu", 1000UL);
            var result1 = ResourceContract.GetElfBalance("Cpu");
            var result2 = ResourceContract.GetUserBalance(UserAddresses[0], "Cpu");
            var result3 = ResourceContract.GetExchangeBalance("Cpu");

            //User without enough balance
            SwitchOwner(UserAddresses[2]);
            ResourceContract.BuyResource("Cpu", 1000UL);
        }

        private void SwitchOwner(Address address)
        {
            ResourceContract.SetTransactionContext(new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = address,
                    To = ContractAddresses[1]
                }
            });
        }

    }
}