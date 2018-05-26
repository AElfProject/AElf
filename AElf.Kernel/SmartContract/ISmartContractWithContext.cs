namespace AElf.Kernel
{
    public interface IContextedSmartContract: ISmartContract
    {
        void SetDataProvider(IDataProvider dataProvider);
        void SetContext(SmartContractRuntimeContext context);
    }
}