using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicFunctionWithParallel
{
    /// <summary>
    /// View methods
    /// </summary>
    public partial class BasicFunctionWithParallelContract
    {
        public override StringValue GetContractName(Empty input)
        {
            return new StringValue
            {
                Value = nameof(BasicFunctionWithParallelContract)
            };
        }

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
                Int64Value = State.WinnerHistory[address]
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
                FirstInt64Value = State.WinnerHistory[input.First],
                SecondInt64Value = State.WinnerHistory[input.Second]
            };
        }

        public override GetValueOutput GetValue(GetValueInput input)
        {
            return new GetValueOutput
            {
                StringValue = State.StringValueMap[input.Key] ?? string.Empty,
                Int64Value = State.LongValueMap[input.Key],
                MessageValue = State.MessageValueMap[input.Key]
            };
        }
    }
}