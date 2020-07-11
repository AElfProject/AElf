using System;
using System.Linq;
using Acs4;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContract : AEPoWContractImplContainer.AEPoWContractImplBase
    {
        public override ConsensusCommand GetConsensusCommand(BytesValue input)
        {
            return new ConsensusCommand
            {
                ArrangedMiningTime = Context.CurrentBlockTime,
                LimitMillisecondsOfMiningBlock = int.MaxValue,
                MiningDueTime = TimestampHelper.MaxValue,

            };
        }

        public override BytesValue GetConsensusExtraData(BytesValue input)
        {
            return input;
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            return new TransactionList
            {
                Transactions =
                {
                    GenerateTransaction(nameof(CoinBase), Context.Sender)
                }
            };
        }

        public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
        {
            var nonce = new Hash();
            nonce.MergeFrom(input.ToByteString());
            return new ValidationResult
            {
                Success = IsValid(HashHelper.ConcatAndCompute(Context.PreviousBlockHash, nonce))
            };
        }

        public override ValidationResult ValidateConsensusAfterExecution(BytesValue input)
        {
            return new ValidationResult {Success = true};
        }

        private bool IsValid(Hash resultHash)
        {
            var prefix = Enumerable.Range(0, State.CurrentDifficulty.Value).Select(x => "0")
                .Aggregate("0", (s1, s2) => s1 + s2);
            return resultHash.ToHex().StartsWith(prefix);
        }

        private Transaction GenerateTransaction(string methodName, IMessage parameter) => new Transaction
        {
            From = Context.Sender,
            To = Context.Self,
            MethodName = methodName,
            Params = parameter.ToByteString(),
            RefBlockNumber = Context.CurrentHeight,
            RefBlockPrefix = BlockHelper.GetRefBlockPrefix(Context.PreviousBlockHash)
        };
    }
}