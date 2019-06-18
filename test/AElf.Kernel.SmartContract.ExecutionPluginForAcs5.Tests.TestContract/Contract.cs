using System;
using System.Linq;
using Acs5;
using AElf.Contracts.Profit;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using CreateProfitItemInput = Acs5.CreateProfitItemInput;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests.TestContract
{
    public class Contract : ContractContainer.ContractBase
    {
        public override Empty CreateProfitItem(CreateProfitItemInput input)
        {
            State.ProfitContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            State.ProfitContract.CreateTreasuryProfitItem.Send(new Contracts.Profit.CreateProfitItemInput
            {
                IsReleaseAllBalanceEveryTimeByDefault = true
            });

            return new Empty();
        }

        public override Empty SetProfitReceivers(ProfitReceivers input)
        {
            var profitItems = State.ProfitContract.GetCreatedProfitItems.Call(new GetCreatedProfitItemsInput
            {
                Creator = Context.Self
            });
            Assert(profitItems.ProfitIds.Any(), "Profit item not found.");
            var profitId = profitItems.ProfitIds.First();
            State.ProfitId.Value = profitId;
            State.ProfitContract.AddWeights.Send(new AddWeightsInput
            {
                ProfitId = profitId,
                EndPeriod = long.MaxValue,
                Weights = {input.Value.Select(i => new WeightMap {Receiver = i.Address, Weight = i.Weight})}
            });

            return new Empty();
        }

        public override Address GetProfitVirtualAddress(Empty input)
        {
            return Address.FromPublicKey(State.ProfitContract.Value.Value.Concat(
                State.ProfitId.Value.Value.ToByteArray().CalculateHash()).ToArray());
        }

        public override Empty SetMethodProfitFee(SetMethodProfitFeeInput input)
        {
            State.MethodProfitFees[input.MethodName] = input.MethodProfitFee;
            return new Empty();
        }

        public override Empty SetMethodProfitFees(SetMethodProfitFeesInput input)
        {
            foreach (var methodProfitFee in input.MethodProfitFees)
            {
                State.MethodProfitFees[methodProfitFee.Key] = methodProfitFee.Value;
            }

            return new Empty();
        }

        public override MethodProfitFee GetMethodProfitFee(StringValue input)
        {
            return State.MethodProfitFees[input.Value] ?? new MethodProfitFee();
        }

        public override Empty ReceiveProfits(Empty input)
        {
            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.ProfitId.Value,
                Period = State.ReleasedTimes.Value.Add(1)
            });

            State.ReleasedTimes.Value = State.ReleasedTimes.Value.Add(1);

            return new Empty();
        }

        public override Hash GetProfitId(Empty input)
        {
            return State.ProfitId.Value;
        }
    }
}