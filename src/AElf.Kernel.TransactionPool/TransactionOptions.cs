namespace AElf.Kernel.TransactionPool
{
    public class TransactionOptions
    {
        /// <summary>
        /// Transaction pool limit.
        /// </summary>
        public int PoolLimit { get; set; } = 5120;


        /// <summary>
        /// Bp Node can disable this flag to make best performance.
        /// But common node needs to enable it to prevent transaction flood attack
        /// </summary>
        public bool EnableTransactionExecutionValidation { get; set; } = true;
    }
}