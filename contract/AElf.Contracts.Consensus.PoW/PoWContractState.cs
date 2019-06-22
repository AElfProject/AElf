using Acs4;
using AElf.Sdk.CSharp.State;
using AElf.Types;
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
            var cmd = new ConsensusCommand
            {
                ExpectedMiningTime = Context.CurrentBlockTime,
                NextBlockMiningLeftMilliseconds = 0,
                LimitMillisecondsOfMiningBlock = int.MaxValue
            };

            return cmd;
        }

        public override BytesValue GetInformationToUpdateConsensus(BytesValue input)
        {
            return GetPowBlockHeaderExtraData(new Empty()).ToBytesValue();
        }

        public override PowBlockHeaderExtraData GetPowBlockHeaderExtraData(Empty input)
        {
            PowBlockHeaderExtraData extraData = new PowBlockHeaderExtraData()
            {
                Nonce = long.MaxValue - 1,
                Bits = 1 //
            };

            return extraData;
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            return new TransactionList();
        }

        public override ValidationResult ValidateConsensusAfterExecution(BytesValue input)
        {
            return new ValidationResult() {Success = true};
        }

        public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
        {
            //TODO: validate NBits and Hash 

            var data = PowBlockHeaderExtraData.Parser.ParseFrom(input.Value);
            
            //first: validate NBits 
            
            return new ValidationResult() {Success = true};
        }
    }
}