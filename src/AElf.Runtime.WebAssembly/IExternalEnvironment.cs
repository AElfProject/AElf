namespace AElf.Runtime.WebAssembly;

public interface IExternalEnvironment
{
    Dictionary<string, byte[]> Storage { get; set; }
    void SetStorage(byte[] key, byte[] value, bool takeOld);
    bool TryGetStorage(byte[] key, out byte[] value);
}