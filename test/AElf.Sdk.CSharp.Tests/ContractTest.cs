using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using Xunit;
using Shouldly;
using AElf.Sdk.CSharp.Tests.TestContract;
using AElf.Types;

namespace AElf.Sdk.CSharp.Tests
{
    public sealed class ContractTest : SdkCSharpTestBase
    {
        private List<Address> AddressList { get; } = SampleAddress.AddressList.ToList();
        private TokenContract Contract { get; } = new TokenContract();
        private IStateProvider StateProvider { get; }
        private IHostSmartContractBridgeContext BridgeContext { get; }

        public ContractTest()
        {
            StateProvider = GetRequiredService<IStateProviderFactory>().CreateStateProvider();
            BridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
            Contract.InternalInitialize(BridgeContext);
            //Contract.SetStateProvider(StateProvider);

            var transactionContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = AddressList[1],
                    To = AddressList[0]
                }
            };

            BridgeContext.TransactionContext = transactionContext;
            //StateProvider.TransactionContext = transactionContext;
        }

        [Fact]
        public void Init_Test()
        {
            Contract.Initialize("ELF", "elf test token", 1000000, 9);
            Contract.Symbol().ShouldBe("ELF");
            Contract.TokenName().ShouldBe("elf test token");
            Contract.TotalSupply().ShouldBe(1000000UL);
            Contract.Decimals().ShouldBe(9U);
            var balance = Contract.BalanceOf(AddressList[1]);
            balance.ShouldBe(1000000UL);
        }

        [Fact(Skip = "Symbol name format should be checked.")]
        public void Init_Invalid_Symbol_Test()
        {
            Contract.Initialize("eLf Symbol", "elf test token", 1000000, 9);
            Contract.Symbol().ShouldBe("eLf Symbol");
            Contract.TokenName().ShouldBe("elf test token");
            Contract.TotalSupply().ShouldBe(1000000UL);
            Contract.Decimals().ShouldBe(9U);
        }

        [Fact]
        public void Init_Again_Test()
        {
            Init_Test();
            Should.Throw<AssertionException>(() => { Contract.Initialize("ELF", "elf test token again", 1000000, 0); });
        }

        [Fact]
        public void Transfer_With_Enough_Token()
        {
            Init_Test();
            Contract.Transfer(AddressList[2], 100UL);
            var balance = Contract.BalanceOf(AddressList[2]);

            balance.ShouldBe(100UL);
        }

        [Fact]
        public void Transfer_Without_Enough_Token()
        {
            Init_Test();
            Contract.Transfer(AddressList[2], 100UL);

            SwitchOwner(AddressList[2]);
            Should.Throw<AssertionException>(() => { Contract.Transfer(AddressList[3], 200UL); });
        }

        [Fact]
        public void Transfer_To_Null_User_Test()
        {
            Init_Test();
            Should.Throw<Exception>(() => { Contract.Transfer(null, 100UL); });
        }

        [Fact]
        public void Approve_Test()
        {
            Init_Test();
            Contract.Approve(AddressList[2], 1000UL);
            var allowance = Contract.Allowance(AddressList[1], AddressList[2]);
            allowance.ShouldBe(1000UL);
        }

        [Fact]
        public void UnApprove_Test()
        {
            Init_Test();
            Contract.Approve(AddressList[2], 1000UL);
            Contract.UnApprove(AddressList[2], 500UL);
            var allowance = Contract.Allowance(AddressList[1], AddressList[2]);
            allowance.ShouldBe(500UL);
        }

        [Fact]
        public void UnApprove_Available_Token_Test()
        {
            Init_Test();
            Contract.Approve(AddressList[2], 500UL);
            Contract.UnApprove(AddressList[2], 1000UL);
            var allowance = Contract.Allowance(AddressList[1], AddressList[2]);
            allowance.ShouldBe(0UL);
        }

        [Fact]
        public void TransferFrom_Available_Token_Test()
        {
            Init_Test();
            Contract.Approve(AddressList[2], 500UL);
            SwitchOwner(AddressList[2]);
            Contract.TransferFrom(AddressList[1], AddressList[2], 500UL);
            var balance1 = Contract.BalanceOf(AddressList[1]);
            var balance2 = Contract.BalanceOf(AddressList[2]);
            balance1.ShouldBe(Contract.TotalSupply() - 500UL);
            balance2.ShouldBe(500UL);
        }

        [Fact]
        public void TransferFrom_Over_Token_Test()
        {
            Init_Test();
            Contract.Approve(AddressList[2], 500UL);
            SwitchOwner(AddressList[2]);
            Should.Throw<AssertionException>(() => { Contract.TransferFrom(AddressList[1], AddressList[2], 1000UL); });
        }

        [Fact]
        public void TransferFrom_MultipleTimes_Available_Token_Test()
        {
            Init_Test();
            Contract.Approve(AddressList[2], 1000UL);
            SwitchOwner(AddressList[2]);
            Contract.TransferFrom(AddressList[1], AddressList[2], 300UL);
            Contract.TransferFrom(AddressList[1], AddressList[2], 500UL);
            var balance2 = Contract.BalanceOf(AddressList[2]);
            balance2.ShouldBe(800UL);
        }

        [Fact]
        public void Burn_Test()
        {
            Init_Test();
            Contract.Burn(1000UL);

            var balance = Contract.BalanceOf(AddressList[1]);
            Contract.TotalSupply().ShouldBe(999000UL);
            balance.ShouldBe(999000UL);
        }

        [Fact]
        public void Burn_Over_Token_Test()
        {
            Init_Test();
            Should.Throw<AssertionException>(() => { Contract.Burn(100000000UL); });
        }

        [Fact]
        public void SetMethodFee_Test()
        {
            Init_Test();
            var fee = Contract.GetMethodFee("Transfer");
            fee.ShouldBe(0UL);

            Contract.SetMethodFee("Transfer", 10UL);
            var fee1 = Contract.GetMethodFee("Transfer");
            fee1.ShouldBe(10UL);

            Contract.SetMethodFee("Transfer", 20UL);
            var fee2 = Contract.GetMethodFee("Transfer");
            fee2.ShouldBe(20UL);
        }

        [Fact]
        public void GetVirtualAddressHash_Test()
        {
            var hash = Contract.GetVirtualAddressHash(10);
            hash.ShouldNotBeNull();
            
            var hash1 = Contract.GetVirtualAddressHash(10);
            hash1.ShouldBe(hash);
            
            var hash2 = Contract.GetVirtualAddressHash(100);
            hash2.ShouldNotBe(hash);
        }


        [Fact]
        public void GetVirtualAddress_Test()
        {
            var address = Contract.GetVirtualAddress(10);
            address.Value.ShouldNotBeNull();
            
            var address1 = Contract.GetVirtualAddress(10);
            address1.ShouldBe(address);
            
            var address2 = Contract.GetVirtualAddress(100);
            address2.ShouldNotBe(address);
        }
        private void SwitchOwner(Address address)
        {
            var transactionContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = address,
                    To = AddressList[0]
                }
            };

            BridgeContext.TransactionContext = transactionContext;
            //StateProvider.TransactionContext = transactionContext;
        }
    }
}