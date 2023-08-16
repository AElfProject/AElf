namespace AElf.Runtime.WebAssembly;

public interface IExternalEnvironment
{
    Dictionary<string, byte[]> Storage { get; set; }
    WriteOutcome SetStorage(byte[] key, byte[] value, bool takeOld);
    bool TryGetStorage(byte[] key, out byte[] value);
}