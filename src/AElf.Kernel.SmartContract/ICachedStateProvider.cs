namespace AElf.Kernel.SmartContract
{
    public interface ICachedStateProvider : IStateProvider
    {
        IStateCache Cache { get; set; }
    }
}