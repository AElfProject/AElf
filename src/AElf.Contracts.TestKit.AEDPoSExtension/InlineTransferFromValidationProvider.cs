using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class InlineTransferFromValidationProvider : IInlineTransactionValidationProvider
    {
        public bool Validate(Transaction transaction)
        {
            return true;
        }
    }
}