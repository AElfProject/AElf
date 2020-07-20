using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContract
    {
        public override StringValue GetDifficulty(Empty input)
        {
            return new StringValue {Value = State.CurrentDifficulty.Value};
        }
    }
}