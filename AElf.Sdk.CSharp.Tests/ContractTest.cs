using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Contexts;
using Xunit;
using Shouldly;
using Moq;
using AElf.Sdk.CSharp.Tests.TestContract;

namespace AElf.Sdk.CSharp.Tests
{
    public class ContractTest:SdkCSharpTestBase
    {
        private List<Address> AddressList { get; } = new[] {"a", "b", "c", "d"}.Select(Address.FromString).ToList();
        private TokenContract Contract { get; } = new TokenContract();
        private IStateProvider StateProvider { get; }

        public ContractTest()
        {
            StateProvider = GetRequiredService<IStateProviderFactory>().CreateStateProvider();
            Contract.SetStateProvider(StateProvider);
            Contract.SetSmartContractContext(new SmartContractContext()
            {
                ContractAddress = AddressList[0],
                ChainService = new Mock<IBlockchainService>().Object,
            });
            Contract.SetTransactionContext(new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = AddressList[1],
                    To = AddressList[0]
                }
            });
            Contract.SetContractAddress(AddressList[0]);
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
            Should.Throw<AssertionError>(() => { Contract.Initialize("ELF", "elf test token again", 1000000, 0); });
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
            Should.Throw<AssertionError>(() => { Contract.Transfer(AddressList[3], 200UL); });
        }

        [Fact]
        public void Transfer_To_Null_User_Test()
        {
            Init_Test();
            Should.Throw<Exception>(() =>
            {
                Contract.Transfer(null, 100UL);
            });
        }

        [Fact]
        public void Approve_Test()
        {
            Init_Test();
            Contract.Approve(AddressList[2], 1000UL);
            var allownce = Contract.Allowance(AddressList[1], AddressList[2]);
            allownce.ShouldBe(1000UL);
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
            Should.Throw<AssertionError>(() => { Contract.TransferFrom(AddressList[1], AddressList[2], 1000UL); });
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
            Should.Throw<AssertionError>(() => { Contract.Burn(100000000UL); });
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

        private void SwitchOwner(Address address)
        {
            Contract.SetTransactionContext(new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = address,
                    To = AddressList[0]
                }
            });
        }
    }
}