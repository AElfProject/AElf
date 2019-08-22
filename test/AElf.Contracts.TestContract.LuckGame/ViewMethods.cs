using AElf.Types;

namespace AElf.Contracts.TestContract.LuckGame
{
    public partial class LuckGameContract
    {
        public override ExceptReward QueryExceptedReward(BetInput input)
        {
            var result = GetOdds(input.BetNumber, input.BetType) * input.BetAmount;
            
            return new ExceptReward
            {
                Value = result
            };
        }

        public override BetRecord QueryBetResult(Hash hash)
        {
            var result = State.UserBetRecord[hash];
            return result ?? new BetRecord();
        }
        
        public override BetRecords QueryUserBetResults(Address address)
        {
            var result = State.UserBetRecords[address];
            return result ?? new BetRecords();
        }
    }
}