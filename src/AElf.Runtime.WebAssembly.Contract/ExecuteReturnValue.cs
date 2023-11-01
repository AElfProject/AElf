namespace AElf.Runtime.WebAssembly.Contract;

public class ExecuteReturnValue
{
    public ReturnFlags Flags { get; set; }
    public byte[] Data { get; set; }
}

[Flags]
public enum ReturnFlags
{
    Empty = 0x0000_0000,
    Revert = 0x0000_0001
}