using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IInlineTransactionValidationProvider
    {
        bool Validate(Transaction transaction);
    }
}