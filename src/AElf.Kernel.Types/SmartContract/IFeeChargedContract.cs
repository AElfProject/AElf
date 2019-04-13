namespace AElf.Kernel.Types.SmartContract
{
    public interface IFeeChargedContract
    {
        ulong GetMethodFee(string methodName);
        void SetMethodFee(string methodName, ulong fee);
    }
}