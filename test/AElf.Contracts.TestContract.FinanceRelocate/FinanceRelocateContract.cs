using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS12;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.FinanceRelocate
{
    public class FinanceRelocateContract : FinanceRelocateContainer.FinanceRelocateBase
    {
        public override Empty Stake(StakeInput input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            Assert(input.Symbol == Context.Variables.NativeSymbol, "Only receive native symbol.");

            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender));
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                Symbol = input.Symbol,
                Amount = input.Amount,
                From = Context.Sender,
                To = virtualAddress
            });
            State.LockedMap[input.Symbol] = State.LockedMap[input.Symbol].Add(input.Amount);
            var amount = State.StakeAddressAmount.Value.Add(1);
            State.StakeAddressAmount.Value = amount;
            State.StakeAddressMap[amount] = Context.Sender;
            return new Empty();
        }

        public override Empty Shuffle(Empty input)
        {
            var randomNumber = Context.ConvertHashToInt64(Context.PreviousBlockHash, 1, State.StakeAddressAmount.Value);
            var luckGuy = State.StakeAddressMap[randomNumber];
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(luckGuy));
            var luckAmount = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = virtualAddress,
                Symbol = Context.Variables.NativeSymbol
            }).Balance;
            var parameter = new TransferInput
            {
                To = Context.Self,
                Amount = luckAmount,
                Symbol = Context.Variables.NativeSymbol
            };
            Context.SendVirtualInline(HashHelper.ComputeFrom(luckGuy), State.TokenContract.Value,
                nameof(State.TokenContract.Transfer), parameter.ToByteString());
            return new Empty();
        }

        public override Empty Harvest(HarvestInput input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender));
            var originAmount = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = virtualAddress,
                Symbol = Context.Variables.NativeSymbol
            }).Balance;
            var parameter = new TransferInput
            {
                To = Context.Sender,
                Amount = originAmount,
                Symbol = Context.Variables.NativeSymbol
            };
            Context.SendVirtualInline(HashHelper.ComputeFrom(Context.Sender), State.TokenContract.Value,
                nameof(State.TokenContract.Transfer), parameter.ToByteString());
            var shuffleAmount = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = Context.Self,
                Symbol = Context.Variables.NativeSymbol
            }).Balance;
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = Context.Sender,
                Amount = shuffleAmount.Div(State.StakeAddressAmount.Value),
                Symbol = Context.Variables.NativeSymbol
            });
            return new Empty();
        }
    }
}