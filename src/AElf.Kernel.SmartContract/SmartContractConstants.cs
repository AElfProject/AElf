namespace AElf.Kernel.SmartContract;

public class SmartContractConstants
{
    public const int ExecutionCallThreshold = 15000;

    public const int ExecutionBranchThreshold = 15000;

    public const int StateSizeLimit = 128 * 1024;

    // The prefix `vs` occupies 2 lengths.
    public const int StateKeyMaximumLength = 255 - 2;
}