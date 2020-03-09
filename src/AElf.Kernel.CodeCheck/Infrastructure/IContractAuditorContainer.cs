namespace AElf.Kernel.CodeCheck.Infrastructure
{
    public interface IContractAuditorContainer
    {
        IContractAuditor GetContractAuditor(int category);
    }
}