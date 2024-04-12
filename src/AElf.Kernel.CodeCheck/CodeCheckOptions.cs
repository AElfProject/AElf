namespace AElf.Kernel.CodeCheck;

public class CodeCheckOptions
{
    public bool CodeCheckEnabled { get; set; }
    public int MaxBoundedCapacity { get; set; } = 5120;
    public int MaxDegreeOfParallelism { get; set; } = 5;
}