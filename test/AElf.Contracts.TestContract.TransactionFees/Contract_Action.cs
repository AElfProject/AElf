using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.TransactionFees
{
    public partial class TransactionFeesContract : TransactionFeesContractContainer.TransactionFeesContractBase
    {
        public override Empty InitializeFeesContract(Address input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Acs8Contract.Value = input;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            
            return new Empty();
        }

        public override Empty MessCpuNetConsuming(NetBytesInput input)
        {
            State.Acs8Contract.CpuConsumingMethod.Send(new Empty());
            
            State.Acs8Contract.NetConsumingMethod.Send(new NetConsumingMethodInput
            {
                Blob = input.NetPackage
            });
            
            return new Empty();
        }

        public override Empty MessCpuStoConsuming(Empty input)
        {
            State.Acs8Contract.CpuConsumingMethod.Send(new Empty());
            
            State.Acs8Contract.StoConsumingMethod.Send(new Empty());
            
            return new Empty();
        }

        public override Empty MessNetStoConsuming(NetBytesInput input)
        {
            State.Acs8Contract.NetConsumingMethod.Send(new NetConsumingMethodInput
            {
                Blob = input.NetPackage
            });
            
            State.Acs8Contract.StoConsumingMethod.Send(new Empty());
            
            return new Empty();
        }

        public override Empty FailInlineTransfer(TransferInput input)
        {
            State.TokenContract.Transfer.Send(new MultiToken.TransferInput
            {
                Symbol = "ELF",
                Amount = input.Amount,
                To = input.To,
                Memo = input.Memo
            });
            
            Context.SendInline(State.TokenContract.Value, "Burn", new BurnInput
            {
                Symbol = "ELF",
                Amount = input.Amount.Div(10)
            }.ToByteString());
            
            return new Empty();
        }

        public override Empty FailNetStoConsuming(NetBytesInput input)
        {
            State.Acs8Contract.NetConsumingMethod.Send(new NetConsumingMethodInput
            {
                Blob = input.NetPackage
            });
            
            Context.SendInline(State.Acs8Contract.Value, "InvalidMethod", ByteString.CopyFromUtf8("fail"));
            
            Assert(false, "");
            return new Empty();
        }

        public override Empty FailCpuNetConsuming(NetBytesInput input)
        {
            State.Acs8Contract.CpuConsumingMethod.Send(new Empty());
            
            Context.SendInline(State.TokenContract.Value, "Transfer", new MultiToken.TransferInput
            {
                Symbol = "ELF",
                To = Context.Self,
                Amount = 1000,
                Memo = "failed inline test"
            }.ToByteString());
            
            return new Empty();
        }

        public override Empty FailCpuStoConsuming(Empty input)
        {
            State.Acs8Contract.CpuConsumingMethod.Send(new Empty());
            
            State.Acs8Contract.StoConsumingMethod.Send(new Empty());
            
            Context.SendInline(State.TokenContract.Value, "NotExist", ByteString.CopyFromUtf8("fake parameter"));
            
            return new Empty();
        }
    }
}