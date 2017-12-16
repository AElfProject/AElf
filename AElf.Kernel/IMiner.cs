using System;
namespace AElf.Kernel
{
    public interface IMiner
    {
        byte[] Mine(IBlockHeader blockheader);
    }
}
