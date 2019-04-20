using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Basic11
{
    /// <summary>
    /// Action methods
    /// </summary>
    public partial class Basic11Contract : Basic11ContractContainer.Basic11ContractBase
    {
        public override Empty InitialBasic11Contract(InitialBasic11ContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            Assert(input.MinValue >0 && input.MaxValue >0 && input.MaxValue >= input.MinValue, "Invalid min/max value input setting.");
            
            State.Initialized.Value = true;
            State.ContractName.Value = input.ContractName;
            State.ContractManager.Value = Context.Sender;
            State.MinBet.Value = input.MinValue;
            State.MaxBet.Value = input.MaxValue;
            State.MortgageBalance.Value = input.MortgageValue;

            return new Empty();
        }

        public override Empty UpdateBetLimit(BetLimitInput input)
        {
            Assert(Context.Sender == State.ContractManager.Value, "Only manager can perform this action."); 
            Assert(input.MinValue >0 && input.MaxValue >0 && input.MaxValue >= input.MinValue, "Invalid min/max value input setting.");
            
            State.MinBet.Value = input.MinValue;
            State.MaxBet.Value = input.MaxValue;

            return new Empty();
        }

        public override Empty UserPlayBet(BetInput input)
        {
            Assert(input.Int64Value >= State.MinBet.Value && input.Int64Value <=State.MaxBet.Value, $"Input balance not in boundary({State.MinBet.Value}, {State.MaxBet.Value}).");
            Assert(input.Int64Value > State.WinerHistory[Context.Sender]);
            
            var result = WinOrLose(input.Int64Value);
            State.LoserHistory[Context.Sender] += input.Int64Value;

            if (result == 0)
            {
                State.TotalBetBalance.Value += input.Int64Value;
                State.LoserHistory[Context.Sender] += result;
            }
            else
            {
                State.RewardBalance.Value += result;
                State.WinerHistory[Context.Sender] += result;
            }
            
            return new Empty();
        }

        public override Empty UpdateMortgage(BetInput input)
        {
            Assert(Context.Sender == State.ContractManager.Value, "Only manager can perform this action."); 
            Assert(input.Int64Value >0, "Update mortgage value should be more than 0.");

            State.MortgageBalance.Value += input.Int64Value;
            
            return new Empty();
        }

        public override Empty UpdateStopBet(Empty input)
        {
            Assert(Context.Sender == State.ContractManager.Value, "Only manager can perform this action."); 
            State.MinBet.Value = 0;
            State.MaxBet.Value = 0;

            return new Empty();
        }

        private long WinOrLose(long betAmount)
        {
            Random rd = new Random();
            var data = rd.Next(1, 1000);
            if (data % 100 == 1)
                return betAmount * 1000;
            else if (data % 50 == 5)
                return betAmount * 50;
            else
                return 0;
        }
    }
}