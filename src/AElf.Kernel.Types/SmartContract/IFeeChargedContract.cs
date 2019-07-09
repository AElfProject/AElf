namespace AElf.Kernel.Types.SmartContract
{
    // Done: move to project
    public interface IFeeChargedContract
    {
        ulong GetMethodFee(string methodName);
        void SetMethodFee(string methodName, ulong fee);
    }
}