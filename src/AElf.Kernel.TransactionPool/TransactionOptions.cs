namespace AElf.Kernel.TransactionPool;

public class TransactionOptions
{
    /// <summary>
    ///     Transaction pool limit.
    /// </summary>
    public int PoolLimit { get; set; } = 5120;

    /// <summary>
    ///     Transaction processing data flow MaxDegreeOfParallelism for transaction pool.
    /// </summary>
    public int PoolParallelismDegree { get; set; } = 5;

    /// <summary>
    ///     Bp Node can disable this flag to make best performance.
    ///     But common node needs to enable it to prevent transaction flood attack
    /// </summary>
    public bool EnableTransactionExecutionValidation { get; set; } = true;
    
    
    /// <summary>
    ///     Configuration whether to save failed transaction results
    /// </summary>
    public bool StoreInvalidTransactionResultEnabled { get; set; }

    
}