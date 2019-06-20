using Acs4;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.PoW
{
    public class PoWContractState : ContractState
    {
    }

    public class PowContract : PoWContractImplContainer.PoWContractImplBase
    {
        public override ConsensusCommand GetConsensusCommand(BytesValue input)
        {
            var cmd = new ConsensusCommand();
            return cmd;
        }
    }
}