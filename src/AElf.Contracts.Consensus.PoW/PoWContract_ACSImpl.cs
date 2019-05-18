using System.Linq;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
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
            var nonce = 0L;
            while (!IsHashValid(Hash.FromRawBytes(input.Value.Concat(nonce.DumpByteArray()).ToArray())))
            {
                nonce = nonce.Add(1);
            }

            Context.LogDebug(() => $"Find nonce {nonce}");

            return new BytesValue {Value = ByteString.CopyFrom(new SInt64Value {Value = nonce}.ToByteArray())};
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            var nonceBytes = new SInt64Value();
            nonceBytes.MergeFrom(ByteString.CopyFrom(input.Value.ToByteArray()));
            return new TransactionList {Transactions = {GenerateSetNonceTransaction(nonceBytes)}};
        }

        public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
        {
            return new ValidationResult {Success = true};
        }

        public override ValidationResult ValidateConsensusAfterExecution(BytesValue input)
        {
            return new ValidationResult {Success = true};
        }

        private bool IsHashValid(Hash hash)
        {
            var currentDifficulty = State.Difficulty.Value;
            return hash.ToHex().Take(currentDifficulty).All(c => c == '0');
        }

        private Transaction GenerateSetNonceTransaction(IMessage parameter)
        {
            var tx = new Transaction
            {
                From = Context.Sender,
                To = Context.Self,
                MethodName = nameof(SetNonce),
                Params = parameter.ToByteString()
            };

            return tx;
        }
    }
}