namespace AElf.Kernel.BlockPruning;

public class BlockPruningOptions
{
    public bool Enabled { get; set; }
    public long RetainDistance { get; set; } = 5184000;
    public int BatchSize { get; set; } = 100;
    public int PruneThreshold { get; set; } = 256;
    public int BatchDelayMilliseconds { get; set; } = 50;
}
