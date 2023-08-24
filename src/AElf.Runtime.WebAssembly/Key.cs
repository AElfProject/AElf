namespace AElf.Runtime.WebAssembly;

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