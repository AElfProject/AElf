using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Contracts.Economic.TestBase
{
    public class InlineTransferFromValidationProvider : IInlineTransactionValidationProvider
    {
        public bool Validate(Transaction transaction)
        {
            return true;
        }
    }
}