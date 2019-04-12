namespace AElf.Kernel.TransactionPool
{
    public class TransactionOptions
    {
        /// <summary>
        /// Transaction pool limit.
        /// </summary>
        public int PoolLimit { get; set; } = 5120;
    }
}