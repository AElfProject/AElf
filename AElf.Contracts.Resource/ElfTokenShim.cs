using AElf.Common;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;

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

        public void TransferByUser(Address to, ulong amount)
        {
            Api.SendInline(Address, "Transfer", to, amount);
        }

        public void TransferByContract(Address to, ulong amount)
        {
            Api.SendInlineFromSelf(Address, "Transfer", to, amount);
        }
    }
}