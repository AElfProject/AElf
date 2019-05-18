using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.PoW
{
    public partial class PoWContract
    {
        public override Empty InitialPoWContract(SInt32Value input)
        {
            State.Difficulty.Value = input.Value;
            return new Empty();
        }

        public override Empty SetNonce(SInt64Value input)
        {
            State.Nonces[Context.CurrentHeight] = input.Value;
            return new Empty();
        }
    }
}