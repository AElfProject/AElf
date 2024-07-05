using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Standards.ACS4;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.PoA;

public partial class PoAContract : PoAContractImplContainer.PoAContractImplBase
{
    public override ConsensusCommand GetConsensusCommand(BytesValue input)
    {
        Context.LogDebug(() => "Getting consensus command from PoA Contract.");

        var arrangedMiningTime = Context.CurrentBlockTime.AddMilliseconds(State.MiningInterval.Value);
        var dueTime = arrangedMiningTime.AddMilliseconds(State.MiningInterval.Value);
        var limit = ((int)State.MiningInterval.Value).Sub(250);
        return new ConsensusCommand
        {
            ArrangedMiningTime = arrangedMiningTime,
            MiningDueTime = dueTime,
            LimitMillisecondsOfMiningBlock = limit
        };
    }

    public override BytesValue GetConsensusExtraData(BytesValue input)
    {
        return new BytesValue
        {
            Value = Context.Sender.Value
        };
    }

    public override TransactionList GenerateConsensusTransactions(BytesValue input)
    {
        return new TransactionList
        {
            Transactions =
            {
                GenerateTransaction(nameof(Mine), new MineInput())
            }
        };
    }

    public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
    {
        return new ValidationResult
        {
            Success = true
        };
    }

    public override ValidationResult ValidateConsensusAfterExecution(BytesValue input)
    {
        return new ValidationResult
        {
            Success = true
        };
    }

    private Transaction GenerateTransaction(string methodName, IMessage parameter)
    {
        return new Transaction
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