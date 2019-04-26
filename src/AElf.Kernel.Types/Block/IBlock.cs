namespace AElf.Kernel
{
    public interface IBlock : IHashProvider
    {
        BlockHeader Header { get; set; }
        BlockBody Body { get; set; }
        long Height { get; }
        byte[] GetHashBytes();
        Block Clone();
    }
}