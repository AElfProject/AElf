namespace AElf.Kernel.CodeCheck;

public class CodeCheckOptions
{
    public bool CodeCheckEnabled { get; set; }
    public int PoolLimit { get; set; } = 5120;
    public int PoolParallelismDegree { get; set; } = 5;
}