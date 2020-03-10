using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions
{
    // TODO: IChargeFeeStrategy makes dependency on this plugin project
    public interface IChargeFeeStrategy
    {
        Address ContractAddress { get; }
        string MethodName { get; }
        bool IsFree(Transaction transaction);
    }
}