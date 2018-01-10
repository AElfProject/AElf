using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
{
    public class Block : IBlock
    {
        public int MagicNumber => 0xAE1F;

        /// <summary>
        /// Magic Number: 4B
        /// BlockSize: 4B
        /// BlockHeader: 84B
        /// </summary>
        public int BlockSize => 92;

        public BlockHeader BlockHeader { get; set; }

        public BlockBody BlockBody { get; set; }


        public bool AddTransaction(ITransaction tx)
        {
            throw new NotImplementedException();
        }

        public IBlockBody GetBody()
        {
            return BlockBody;
        }

        public IHash GetHash()
        {
            throw new NotImplementedException();
        }

        public IBlockHeader GetHeader()
        {
            return BlockHeader;
        }
    }
}
