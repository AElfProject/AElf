namespace AElf.Kernel.CodeCheck.Application;

public interface ICodeCheckService
{
    Task<bool> PerformCodeCheckAsync(byte[] code, Hash blockHash, long blockHeight, int category,
        bool isSystemContract, bool isUserContract);
    
    bool PerformCodePatch(byte[] code, int category, bool isSystemContract, out byte[] patchedCode);

    Task<byte[]> GetPatchedCodeAsync(Hash codeHash, byte[] code, int category, bool isSystemContract);
}