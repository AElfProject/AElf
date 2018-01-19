using System.Collections.Generic;
using System.Threading.Tasks;
namespace AElf.Kernel
{
    public class Miner :  IMiner, IBlockProducer
    {
        private List<ITransaction> _transactions = new List<ITransaction>();

        private TransactionExecutingManager txExecutingManager = new TransactionExecutingManager();

        public async Task<IBlock> CreateBlockAsync()
        {
            if (_transactions.Count < 1)
            {
                return null;
            }

            Block block = new Block(Network.Chain.CurrentBlockHash, Network.Chain.CurrentBlockStateHash);

            foreach (var tx in _transactions)
            {
                await txExecutingManager.ExecuteAsync(tx);
            }

            txExecutingManager.Scheduler();



            MerkleTree<ITransaction> tree = new MerkleTree<ITransaction>();
            return block;
        }

        public void GetTransactions()
        {
            int count = _transactions.Count;
            for (int i = 0; i < count; i++)
            {
                _transactions.Add(Network.ReceiveTransaction());
            }
        }

        public byte[] Mine(IBlockHeader blockheader)
        {
            int bits = (blockheader as BlockHeader).Bits;
            while (true)
            {
                //Change the Nonce
                (blockheader as BlockHeader).Nonce++;
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
