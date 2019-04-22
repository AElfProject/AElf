using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Basic1
{
    /// <summary>
    /// View methods
    /// </summary>
    public partial class Basic1Contract
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
    }
}