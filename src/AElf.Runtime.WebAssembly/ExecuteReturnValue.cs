namespace AElf.Runtime.WebAssembly;

public class ExecuteReturnValue
{
    public ReturnFlags Flags { get; set; }
    public byte[] Data { get; set; }
}

public enum ReturnFlags
{
    Empty = 0,
    Revert = 1
}