using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicFunction
{
    /// <summary>
    /// View methods
    /// </summary>
    public partial class BasicFunctionContract
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
        
        public override Address GetOriginAddress(Empty input)
        {
            return State.Origin;
        }
        
        public override Address GetSenderAddress(Empty input)
        {
            return State.Sender;
        }
    }
}