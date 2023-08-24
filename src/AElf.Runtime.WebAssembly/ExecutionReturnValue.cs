namespace AElf.Runtime.WebAssembly;

public class ExecutionReturnValue
{
    public ReturnFlags Flags { get; set; }
    public string Data { get; set; }
}

public enum ReturnFlags
{
    Default = 0,
    Revert = 1
}