using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class GenesisBlock : IBlock
    {
        private readonly BlockHeader _blockHeader = new BlockHeader(Hash<IBlock>.Zero);
        private readonly BlockBody _blockBody = new BlockBody();
        public ITransaction Transaction { get; set; }

        
        /// <summary>
        /// Returns the block hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public IHash<IBlock> GetHash()
        {
            return new Hash<IBlock>(this.CalculateHash());
        }


        public IBlockHeader GetHeader()
        {
            return _blockHeader;
        }

        public IBlockBody GetBody()
        {
            return _blockBody;
        }

        
        public bool AddTransaction(ITransaction tx)
        {
            if (!_blockBody.AddTransaction(tx)) return false;
            _blockHeader.AddTransaction(tx.GetHash());
            return true;
        }
    }
}