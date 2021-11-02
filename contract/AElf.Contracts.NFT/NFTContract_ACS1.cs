using AElf.Standards.ACS1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFT
{
    public partial class NFTContract
    {
        #region Views

        public override MethodFees GetMethodFee(StringValue input)
        {
            return new MethodFees
            {
                MethodName = input.Value,
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = Context.Variables.NativeSymbol,
                        BasicFee = 10000_00000000
                    }
                }
            };
        }

        public override AuthorityInfo GetMethodFeeController(Empty input)
        {
            return new AuthorityInfo();
        }

        #endregion

        public override Empty SetMethodFee(MethodFees input)
        {
            return new Empty();
        }

        public override Empty ChangeMethodFeeController(AuthorityInfo input)
        {
            return new Empty();
        }
    }
}