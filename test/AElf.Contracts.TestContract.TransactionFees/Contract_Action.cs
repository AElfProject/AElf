using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests.TestContract;

namespace AElf.Contracts.TestContract.TransactionFees
{
    public partial class TransactionFeesContract : TransactionFeesContractContainer.TransactionFeesContractBase
    {
        public override Empty InitializeFeesContract(Address input)
        {
            State.Acs8Contract.Value = input;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            //init state
            for (var i = 0; i <= 100; i++)
            {
                State.TestInfo[i] = 5 * i;
            }
            
            return new Empty();
        }
        
        public override Empty MessCpuNetConsuming(NetBytesInput input)
        {
            State.Acs8Contract.CpuConsumingMethod.Send(new Empty());
            
            State.Acs8Contract.TrafficConsumingMethod.Send(new TrafficConsumingMethodInput
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
            State.Acs8Contract.TrafficConsumingMethod.Send(new TrafficConsumingMethodInput
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
            State.Acs8Contract.TrafficConsumingMethod.Send(new TrafficConsumingMethodInput
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

        public override Empty ReadCpuCountTest(Int32Value input)
        {
            Assert(input.Value>0, $"Invalid read state count {input.Value}");
            var sum = 0;
            for (var i = 0; i <= input.Value % 100; i++)
            {
                sum = sum.Add(State.TestInfo[i]);
            }
            
            return new Empty();
        }

        public override Empty WriteRamCountTest(Int32Value input)
        {
            Assert(input.Value>0, $"Invalid write state count {input.Value}");
            for (var i = 0; i <= input.Value % 100; i++)
            {
                State.TestInfo[i] = input.Value * i;
            }
            
            return new Empty();
        }

        public override Empty ComplexCountTest(ReadWriteInput input)
        {
            Assert(input.Read>0 && input.Write>0, "Invalid write/read state count input.");
            WriteRamCountTest(new Int32Value
            {
                Value = input.Write
            });
            ReadCpuCountTest(new Int32Value
            {
                Value = input.Read
            });
            
            return new Empty();
        }

        public override Empty NoReadWriteCountTest(StringValue input)
        {
            return new Empty();
        }
    }
}