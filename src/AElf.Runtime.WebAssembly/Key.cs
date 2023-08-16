namespace AElf.Runtime.WebAssembly;

public class Key
{
    public KeyType KeyType { get; set; }
    public byte[] KeyValue { get; set; }
}

public enum KeyType
{
    Fix,
    Var
}