using AElf.Standards.ACS1;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.A
{
    public class AContract : AContractContainer.AContractBase
    {
        #region Action
        public override Empty InitializeA(Address input)
        {
            State.AState = new MappedState<Address, StringValue>();
            State.BContract.Value = input;

            return new Empty();
        }

        public override Empty ExecuteAA(StringValue input)
        {
            State.AState[Context.Sender] = new StringValue
            {
                Value = ConcatMsg(input)
            };

            return new Empty();
        }

        public override Empty ExecuteAB(StringValue input)
        {
            State.BContract.ExecuteBB.Send(new StringValue
            {
                Value = ConcatMsg(input)
            });

            return new Empty();
        }

        public override Empty ExecuteAC(StringValue input)
        {
            State.BContract.ExecuteBC.Send(new StringValue
            {
                Value = ConcatMsg(input)
            });

            return new Empty();
        }

        public override Empty ExecuteLoopABC(StringValue input)
        {
            State.BContract.ExecuteLoopABC.Send(new StringValue
            {
                Value = ConcatMsg(input)
            });

            return new Empty();
        }

        public override MethodFees GetMethodFee(StringValue input)
        {
            return new MethodFees
            {
                Fees =
                {
                    new MethodFee
                        {Symbol = Context.Variables.NativeSymbol, BasicFee = 1000_0000} //default 0.1 native symbol
                }
            };
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            return new Empty();
        }
        
        #endregion
        
        #region View

        public override StringValue CallAA(Address input)
        {
            return State.AState[input] ?? new StringValue();
        }

        public override StringValue CallAB(Address input)
        {
            return State.BContract.CallBB.Call(input);
        }

        public override StringValue CallAC(Address input)
        {
            return State.BContract.CallBC.Call(input);
        }

        public override StringValue CallLoopABC(Address input)
        {
            return State.BContract.CallLoopABC.Call(input);
        }

        #endregion

        private static string ConcatMsg(StringValue input)
        {
            return AElfString.Concat("A", input.Value);
        }
    }
}