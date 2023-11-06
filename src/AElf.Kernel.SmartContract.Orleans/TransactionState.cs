namespace AElf.Kernel.SmartContract.Orleans;

public class TransactionState
{
    public Guid Id    { get; set; }
    public List<ExecutionReturnSet> ExecutionReturnSets { get; set; }

}