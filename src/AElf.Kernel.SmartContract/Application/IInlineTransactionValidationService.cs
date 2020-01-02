using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IInlineTransactionValidationService
    {
        bool Validate(Transaction transaction);
    }
}