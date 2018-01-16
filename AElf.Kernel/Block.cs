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
        #region Seems useless for now, but maybe useful in the future
        public int MagicNumber => 0xAE1F;

        /// <summary>
        /// Magic Number: 4B
        /// BlockSize: 4B
        /// BlockHeader: 84B
        /// </summary>
        public int BlockSize => 92;
        #endregion

        #region Private fields
        private BlockHeader _blockHeader { get; set; }
        private BlockBody _blockBody { get; set; } = new BlockBody();
        #endregion

        /// <summary>
        /// When we want to generate a new block,
        /// we must now know the hash value of previous block.
        /// </summary>
        /// <param name="preBlockHash"></param>
        public Block(Hash<IBlock> preBlockHash)
        {
            _blockHeader = new BlockHeader(preBlockHash);
        }

        /// <summary>
        /// Add a transaction to block body.
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public bool AddTransaction(ITransaction tx)
        {
            if (_blockBody.AddTransaction(tx))
            {
                //If successfully add a transaction to the block body,
                //add the hash value of transaction to block header.
                _blockHeader.AddTransaction(tx.GetHash());
                return true;
            }
            return false;
        }

        public IBlockBody GetBody()
        {
            return _blockBody;
        }

        public IBlockHeader GetHeader()
        {
            return _blockHeader;
        }

        public IHash GetHash()
        {
            return new Hash<IBlock>(ExtensionMethods.GetHash(this));
        }
    }
}
