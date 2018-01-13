using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    [Serializable]
    public class Block : IBlock
    {
        public int MagicNumber => 0xAE1F;

        /// <summary>
        /// Magic Number: 4B
        /// BlockSize: 4B
        /// BlockHeader: 84B
        /// </summary>
        public int BlockSize => 92;

        private BlockHeader BlockHeader { get; set; }

        private BlockBody BlockBody { get; set; } = new BlockBody();

        public Block(Hash<IBlock> preBlockHash)
        {
            BlockHeader = new BlockHeader(preBlockHash);
        }

        public bool AddTransaction(ITransaction tx)
        {
            if (BlockBody.AddTransaction(tx))
            {
                BlockHeader.AddTransaction(tx.GetHash());
                return true;
            }
            return false;
        }

        public IBlockBody GetBody()
        {
            return BlockBody;
        }

        public IBlockHeader GetHeader()
        {
            return BlockHeader;
        }

        public IHash GetHash()
        {
            return new Hash<IBlock>(this.GetSHA256Hash());
        }
    }
}
