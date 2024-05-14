using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Vote;

public class VoteContract : VoteContractContainer.VoteContractBase
{
    private const int SsdbMaxKeyLength = 200;
    public override Empty AddOption(AddOptionInput input)
    {
        var longItem = RepeatStringMultipleTimes(input.VotingItemId.ToHex(), 5);
        var longOption = RepeatStringMultipleTimes(input.Option.ToHex(), 5);
        Assert(longItem.Length > SsdbMaxKeyLength);
        Assert(longOption.Length > SsdbMaxKeyLength);
        State.State[longItem][longOption] = longOption;
        return new Empty();
    }

    public override StringValue GetState(AddOptionInput input)
    {
        var longItem = RepeatStringMultipleTimes(input.VotingItemId.ToHex(), 5);
        var longOption = RepeatStringMultipleTimes(input.Option.ToHex(), 5);
        return new StringValue
        {
            Value = State.State[longItem][longOption]
        };
    }

    private string RepeatStringMultipleTimes(string input, int times)
    {
        var result = "";
        for (var i = 0; i < times; i++)
        {
            result += input;
        }

        return result;
    }
}