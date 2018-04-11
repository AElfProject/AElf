namespace AElf.Kernel
{
    public class TxPoolConfig : ITxPoolConfig
    {
        public TxPoolConfig(ulong limitSize)
        {
            LimitSize = limitSize;
        }

        public ulong LimitSize { get; }
    }
}