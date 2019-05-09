namespace AElf.Kernel
{
    public interface IBlock : IBlockBase
    {
        BlockHeader Header { get; }
        BlockBody Body { get; }
        long Height { get; }
        byte[] GetHashBytes();
    }
}