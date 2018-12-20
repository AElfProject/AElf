using AElf.Common;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Resource
{
    public class ElfTokenShim
    {
        private readonly PbField<Address> _addressField;

        private Address Address => _addressField.GetValue();

        public ElfTokenShim(PbField<Address> addressField)
        {
            _addressField = addressField;
        }

        public ulong BalanceOf(Address owner)
        {
            if (Api.Call(Address, nameof(BalanceOf), owner))
            {
                return Api.GetCallResult().DeserializeToPbMessage<UInt64Value>().Value;
            }

            return 0;
        }

        public void Burn(ulong amount)
        {
            Api.SendInlineByContract(Address, nameof(Burn), amount);
        }
        public void TransferByUser(Address to, ulong amount)
        {
            Api.SendInline(Address, "Transfer", to, amount);
        }

        public void TransferByContract(Address to, ulong amount)
        {
            Api.SendInlineByContract(Address, "Transfer", to, amount);
        }
    }
}