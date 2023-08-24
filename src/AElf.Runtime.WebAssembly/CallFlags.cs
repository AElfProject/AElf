namespace AElf.Runtime.WebAssembly;

[Flags]
public enum CallFlags
{
    ForwardInput = 0b0000_0001,
    CloneInput = 0b0000_0010,
    TailCall = 0b0000_0100,
    AllowReentry = 0b0000_1000
}