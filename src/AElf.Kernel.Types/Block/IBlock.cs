namespace AElf.Kernel
{
    public interface IBlock : IBlockBase
    {
        BlockHeader Header { get; set; }
        BlockBody Body { get; set; }
        long Height { get; set; }
        byte[] GetHashBytes();
        Block Clone();
    }
}