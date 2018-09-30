using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Genesis
{
    public partial class ContractInfo
    {
        private Hash _address = null;

        public Hash Address
        {
            get
            {
                if (_address == null)
                {
                    _address = GetAddress();
                }

                return _address;
            }
        }

        private Hash GetAddress()
        {
            return new Hash(
                Api.GetChainId()
                    .CalculateHashWith(new UInt64Value()
                    {
                        Value = SerialNumber
                    })
            ).ToAccount();
        }
    }
}