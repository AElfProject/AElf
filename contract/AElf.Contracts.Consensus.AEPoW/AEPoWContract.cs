using System;
using Acs4;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContract : AEPoWContractImplContainer.AEPoWContractImplBase
    {
        public override ConsensusCommand GetConsensusCommand(BytesValue input)
        {
            return base.GetConsensusCommand(input);
        }

        public override BytesValue GetConsensusExtraData(BytesValue input)
        {
            return base.GetConsensusExtraData(input);
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            return base.GenerateConsensusTransactions(input);
        }

        public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
        {
            return base.ValidateConsensusBeforeExecution(input);
        }

        public override ValidationResult ValidateConsensusAfterExecution(BytesValue input)
        {
            return base.ValidateConsensusAfterExecution(input);
        }
    }
}