using AElf.Standards.ACS1;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.B
{
    public class BContract : BContractContainer.BContractBase
    {
        #region Action
        public override Empty InitializeB(Address input)
        {
            State.BState = new MappedState<Address, StringValue>();
            State.CContract.Value = input;

            return new Empty();
        }

        public override Empty ExecuteBB(StringValue input)
        {
            State.BState[Context.Sender] = new StringValue
            {
                Value = ConcatMsg(input)
            };

            return new Empty();
        }

        public override Empty ExecuteBC(StringValue input)
        {
            State.CContract.ExecuteCC.Send(new StringValue
            {
                Value = ConcatMsg(input)
            });

            return new Empty();
        }

        public override Empty ExecuteBA(StringValue input)
        {
            State.CContract.ExecuteCA.Send(new StringValue
            {
                Value = ConcatMsg(input)
            });

            return new Empty();
        }

        public override Empty ExecuteLoopABC(StringValue input)
        {
            State.CContract.ExecuteLoopABC.Send(new StringValue
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

        public override StringValue CallBB(Address input)
        {
            return State.BState[input] ?? new StringValue();
        }

        public override StringValue CallBA(Address input)
        {
            return State.CContract.CallCA.Call(input);
        }

        public override StringValue CallBC(Address input)
        {
            return State.CContract.CallCC.Call(input);
        }

        public override StringValue CallLoopABC(Address input)
        {
            return State.CContract.CallLoopABC.Call(input);
        }

        #endregion

        private static string ConcatMsg(StringValue input)
        {
            return AElfString.Concat("B", input.Value);
        }
    }
}