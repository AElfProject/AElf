using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.PoW
{
    public partial class PoWContract : PoWContractImplContainer.PoWContractImplBase
    {
        public override ConsensusCommand GetConsensusCommand(BytesValue input)
        {
            return new ConsensusCommand
            {
                ExpectedMiningTime = Context.CurrentBlockTime.ToTimestamp(),
                LimitMillisecondsOfMiningBlock = int.MaxValue,
                NextBlockMiningLeftMilliseconds = 0
            };
        }

        public override BytesValue GetInformationToUpdateConsensus(BytesValue input)
        {
            return base.GetInformationToUpdateConsensus(input);
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