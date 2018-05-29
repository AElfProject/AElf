namespace AElf.Kernel
{
    public interface ISmartContractWithContext: ISmartContract
    {
        void SetDataProvider(IDataProvider dataProvider);
        void SetContext(TransactionContext context);
    }
}