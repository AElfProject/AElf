using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Genesis
{
    public partial class ContractInfo
    {
        private Address _address = null;

        public Address Address
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

        private Address GetAddress()
        {
            return Address.FromBytes(
                Hash.FromTwoHashes(
                    Api.GetChainId(),
                    Hash.FromMessage(
                        new UInt64Value()
                        {
                            Value = SerialNumber
                        })
                ).ToByteArray()
            );
        }
    }
}