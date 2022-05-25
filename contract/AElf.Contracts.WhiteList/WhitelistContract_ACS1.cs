using AElf.Standards.ACS1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract
    {
        #region Views

        public override MethodFees GetMethodFee(StringValue input)
        {
            return new MethodFees();
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