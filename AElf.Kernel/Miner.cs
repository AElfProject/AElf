using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
{
    public class Miner : IMiner
    {
        public byte[] Mine(IBlockHeader blockheader)
        {
            int nonce = (blockheader as BlockHeader).Nonce;
            while (true)
            {
                nonce++;

            }
        }
    }
}
