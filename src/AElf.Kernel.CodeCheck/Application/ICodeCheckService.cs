namespace AElf.Kernel.CodeCheck.Application;

public interface ICodeCheckService
{
    Task<bool> PerformCodeCheckAsync(byte[] code, Hash blockHash, long blockHeight, int category,
        bool isSystemContract, bool isUserContract);
}