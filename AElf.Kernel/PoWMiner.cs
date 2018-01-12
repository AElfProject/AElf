using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
{
    public class PoWMiner : IMiner
    {
        public byte[] Mine(IBlockHeader blockheader)
        {
            while (true)
            {
                int bits = (blockheader as BlockHeader).Bits;
                //Do mining
                var result = (blockheader as BlockHeader).GetHash();
                if (result.Value.NumberOfZero() == bits)
                {
                    //Get the proper hash value.
                    return result.Value;
                }
            }
        }
    }
}
