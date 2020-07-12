using System.Linq;
using Acs4;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContract : AEPoWContractImplContainer.AEPoWContractImplBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(input.SupposedProduceSeconds > 0, "Invalid input.");
            State.SupposedProduceSeconds.Value = input.SupposedProduceSeconds;
            return new Empty();
        }

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
            return CalculateNonce().ToBytesValue();
        }

        // TODO: Cache nonce in Kernel code.
        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            var nonce = new Hash();
            nonce.MergeFrom(input.Value);
            return new TransactionList
            {
                Transactions =
                {
                    GenerateTransaction(nameof(CoinBase), new CoinBaseInput
                    {
                        Nonce = nonce,
                        Producer = Context.Sender
                    })
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
            return resultHash.Value.Take(State.CurrentDifficulty.Value).All(b => b == 0);
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

        private Hash CalculateNonce()
        {
            var blockHash = Context.PreviousBlockHash;
            var nonceNumber = 1L;
            var nonce = HashHelper.ComputeFrom(new Int64Value {Value = nonceNumber});
            var resultHash = HashHelper.ConcatAndCompute(blockHash, nonce);
            while (!IsValid(resultHash))
            {
                nonceNumber++;
                nonce = HashHelper.ComputeFrom(new Int64Value {Value = nonceNumber});
                resultHash = HashHelper.ConcatAndCompute(blockHash, nonce);
            }

            return nonce;
        }
    }
}