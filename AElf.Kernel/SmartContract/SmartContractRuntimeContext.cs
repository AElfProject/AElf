namespace AElf.Kernel
{
    public class SmartContractRuntimeContext
    {
        public IDataProvider DataProvider;
        public Hash ChainId;
        public Hash ContractAddress;
        public Hash PreviousBlockHash;
        public Transaction Transaction;
    }
}