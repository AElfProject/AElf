using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
{
    public class Miner : IMiner
    {
        public byte[] Mine(IBlockHeader blockheader)
        {
            //TODO: Use blockheader to produce a new block.
            var header = blockheader as BlockHeader;
            int nonce = header.Nonce;
            while (true)
            {
                nonce++;
                if (true)
                {
                }
            }
        }
    }
}
