namespace AElf.Kernel.CodeCheck.Application;

public interface ICodePatchService
{
    bool PerformCodePatch(byte[] code, int category, bool isSystemContract, out byte[] patchedCode);
}