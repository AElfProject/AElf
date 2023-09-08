namespace AElf.Runtime.WebAssembly;

/// <summary>
/// Inspired by https://github.com/paritytech/substrate/blob/master/frame/contracts/src/exec.rs#L65
/// </summary>
public class Key
{
    public KeyType KeyType { get; set; }
    public byte[] KeyValue { get; set; } = Array.Empty<byte>();
}

public enum KeyType
{
    Fix,
    Var
}