using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicFunctionWithParallel
{
    /// <summary>
    /// View methods
    /// </summary>
    public partial class BasicFunctionWithParallelContract
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

        public override TwoUserMoneyOut QueryTwoUserWinMoney(QueryTwoUserWinMoneyInput input)
        {
            return new TwoUserMoneyOut
            {
                FirstInt64Value = State.WinerHistory[input.First],
                SecondInt64Value = State.WinerHistory[input.First]
            };
        }
    }
}