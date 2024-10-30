using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using Scale;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class BalancesTest : SolidityContractTestBase
{
    public BalancesTest()
    {
        ContractPath = "contracts/balances.contract";
    }

    [Fact(DisplayName = "Test balances contract.")]
    public async Task TestBalances()
    {
        var contractAddress = await DeployContractAsync();

        const long initAmount = 1000000_00000000;
        const long payAmount = 100000_00000000;
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            To = contractAddress,
            Symbol = "ELF",
            Amount = initAmount
        });

        // Query balance.
        {
            var balance = await QueryAsync(contractAddress, "get_balance");
            UInt128Type.From(balance.ToByteArray()).Value.ShouldBe(initAmount);
        }

        // Test pay_me method.
        {
            var txResult = await ExecuteTransactionAsync(contractAddress, "pay_me", value: payAmount);
            var print = txResult.Logs.First(l => l.Name == "Print").NonIndexed.ToByteArray();
            Encoding.UTF8.GetString(print).ShouldContain($"print: Thank you very much for {payAmount}");
        }

        // Query balance.
        {
            var balance = await QueryAsync(contractAddress, "get_balance");
            UInt128Type.From(balance.ToByteArray()).Value.ShouldBe(initAmount + payAmount);
        }

        // Test send method.
        {
            var txResult = await ExecuteTransactionAsync(contractAddress, "send",
                TupleType<AddressType, UInt128Type>.GetByteStringFrom(
                    AddressType.From(SampleAddress.AddressList[1].ToByteArray()),
                    UInt128Type.From(payAmount)
                ));
            BoolType.From(txResult.ReturnValue.ToByteArray()).Value.ShouldBeTrue();
        }

        // Query balance.
        {
            var balance = await QueryAsync(contractAddress, "get_balance");
            UInt128Type.From(balance.ToByteArray()).Value.ShouldBe(initAmount);
            var balanceOfReceiver = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = SampleAddress.AddressList[1]
            });
            balanceOfReceiver.Balance.ShouldBe(payAmount);
        }

        // Test send method.
        {
            await ExecuteTransactionAsync(contractAddress, "transfer",
                TupleType<AddressType, UInt128Type>.GetByteStringFrom(
                    AddressType.From(SampleAddress.AddressList[1].ToByteArray()),
                    UInt128Type.From(payAmount)
                ));
        }

        // Query balance.
        {
            var balance = await QueryAsync(contractAddress, "get_balance");
            UInt128Type.From(balance.ToByteArray()).Value.ShouldBe(initAmount - payAmount);
            var balanceOfReceiver = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = SampleAddress.AddressList[1]
            });
            balanceOfReceiver.Balance.ShouldBe(payAmount * 2);
        }
    }
}