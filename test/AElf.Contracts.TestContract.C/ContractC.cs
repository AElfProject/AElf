using AElf.Standards.ACS1;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.C
{
    public class CContract : CContractContainer.CContractBase
    {
        #region Action
        public override Empty InitializeC(Address input)
        {
            State.CState = new MappedState<Address, StringValue>();
            State.AContract.Value = input;

            return new Empty();
        }

        public override Empty ExecuteCC(StringValue input)
        {
            State.CState[Context.Sender] = new StringValue
            {
                Value = ConcatMsg(input)
            };

            return new Empty();
        }

        public override Empty ExecuteCA(StringValue input)
        {
            State.AContract.ExecuteAA.Send(new StringValue
            {
                Value = ConcatMsg(input)
            });

            return new Empty();
        }

        public override Empty ExecuteCB(StringValue input)
        {
            State.AContract.ExecuteAB.Send(new StringValue
            {
                Value = ConcatMsg(input)
            });

            return new Empty();
        }

        public override Empty ExecuteLoopABC(StringValue input)
        {
            State.AContract.ExecuteLoopABC.Send(new StringValue
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

        public override StringValue CallCC(Address input)
        {
            return State.CState[input] ?? new StringValue();
        }

        public override StringValue CallCA(Address input)
        {
            return State.AContract.CallAA.Call(input);
        }

        public override StringValue CallCB(Address input)
        {
            return State.AContract.CallAB.Call(input);
        }

        public override StringValue CallLoopABC(Address input)
        {
            return State.AContract.CallLoopABC.Call(input);
        }

        #endregion
        private static string ConcatMsg(StringValue input)
        {
            return AElfString.Concat("C", input.Value);
        }
    }
}