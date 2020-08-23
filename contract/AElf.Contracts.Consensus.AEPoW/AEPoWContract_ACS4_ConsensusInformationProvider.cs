using System.Collections.Generic;
using Acs4;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContract : AEPoWContractImplContainer.AEPoWContractImplBase
    {
        public override ConsensusCommand GetConsensusCommand(BytesValue input)
        {
            Context.LogDebug(() => "Getting consensus command for PoW.");

            return new ConsensusCommand
            {
                ArrangedMiningTime = Context.CurrentBlockTime,
                LimitMillisecondsOfMiningBlock = int.MaxValue,
                MiningDueTime = TimestampHelper.MaxValue,
                Hint = new AdjustDifficultyInfo
                {
                    CurrentDifficulty = State.CurrentDifficulty.Value,
                    SupposedProduceMilliseconds = State.SupposedProduceMilliseconds.Value,
                    AverageProduceMilliseconds = CalculateAverageProduceMilliseconds()
                }.ToByteString()
            };
        }

        public override BytesValue GetConsensusExtraData(BytesValue input)
        {
            Context.LogDebug(() => "Entered GetConsensusExtraData of PoW.");
            var nonce = HashHelper.ComputeFrom(new Int64Value {Value = CalculateNonce()});
            var validateInfo = new PoWValidateInfo
            {
                Nonce = nonce,
                PreviousBlockHash = Context.PreviousBlockHash
            };
            Context.LogDebug(() => $"Extra Data: {validateInfo}");
            return validateInfo.ToBytesValue();
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            Context.LogDebug(() => "Entered GenerateConsensusTransactions of PoW.");
            var nonceNumber = CalculateNonce();

            return new TransactionList
            {
                Transactions =
                {
                    GenerateTransaction(nameof(CoinBase), new CoinBaseInput
                    {
                        NonceNumber = nonceNumber,
                        Producer = Context.Sender,
                        NewDifficulty = StringValue.Parser.ParseFrom(input.Value.ToByteArray()).Value
                    })
                }
            };
        }

        public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
        {
            var validateInfo = PoWValidateInfo.Parser.ParseFrom(input.Value.ToByteArray());
            Context.LogDebug(() => $"Validate Data: {validateInfo}");

            var isValid = IsValid(HashHelper.ConcatAndCompute(validateInfo.PreviousBlockHash, validateInfo.Nonce));
            return new ValidationResult
            {
                // This is not enough, more validations should be in kernel code.
                Success = isValid
            };
        }

        public override ValidationResult ValidateConsensusAfterExecution(BytesValue input)
        {
            var record = State.Records[Context.CurrentHeight.Sub(1)];
            Context.LogDebug(() => $"Record: {record}");
            if (record == null)
            {
                return new ValidationResult
                {
                    Message = $"Record of height {Context.CurrentHeight.Sub(1)} is null."
                };
            }

            return new ValidationResult {Success = true};
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

        private long CalculateNonce()
        {
            Context.LogDebug(() => "Entered CalculateNonce.");

            var blockHash = Context.PreviousBlockHash;
            var nonceNumber = 1L;
            var nonce = HashHelper.ComputeFrom(new Int64Value {Value = nonceNumber});
            var resultHash = HashHelper.ConcatAndCompute(blockHash, nonce);
            Context.LogDebug(() => "Start calculate nonce.");

            while (!IsValid(resultHash))
            {
                nonceNumber++;
                nonce = HashHelper.ComputeFrom(new Int64Value {Value = nonceNumber});
                resultHash = HashHelper.ConcatAndCompute(blockHash, nonce);
                if (nonceNumber % 100000 == 0)
                {
                    var number = nonceNumber;
                    Context.LogDebug(() => $"Nonce increased to: {number}");
                }
            }

            Context.LogDebug(() => $"New nonce: {nonce}, number: {nonceNumber}, result hash: {resultHash}");
            return nonceNumber;
        }
    }
}