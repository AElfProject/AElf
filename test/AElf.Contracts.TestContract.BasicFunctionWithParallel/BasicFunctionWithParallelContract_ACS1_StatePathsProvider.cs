using AElf.Standards.ACS1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicFunctionWithParallel
{
    public partial class BasicFunctionWithParallelContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            return new MethodFees();
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            return new Empty();
        }

    }
}