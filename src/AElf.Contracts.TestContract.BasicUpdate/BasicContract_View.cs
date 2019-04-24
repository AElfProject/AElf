using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicUpdate
{
    /// <summary>
    /// View methods
    /// </summary>
    public partial class BasicUpdateContract
    {
        public override MoneyOutput QueryWinMoney(Empty input)
        {
            return new MoneyOutput
            {
                Int64Value = State.TotalBetBalance.Value
            };
        }

        public override MoneyOutput QueryRewardMoney(Empty input)
        {
            return new MoneyOutput
            {
                Int64Value = State.RewardBalance.Value
            };
        }

        public override MoneyOutput QueryUserWinMoney(Address address)
        {
            return new MoneyOutput
            {
                Int64Value = State.WinerHistory[address]
            };
        }

        public override MoneyOutput QueryUserLoseMoney(Address address)
        {
            return new MoneyOutput
            {
                Int64Value = State.LoserHistory[address]
            };
        }

        public override BetStatus QueryBetStatus(Empty input)
        {
            return new BetStatus
            {
                BoolValue = (State.MinBet.Value == 0 && State.MaxBet.Value == 0) 
            };
        }
    }
}