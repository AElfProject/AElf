using System.Collections.Generic;

namespace AElf.Kernel
{
    public class Miner : ITransactionReceiver, IMiner, IBlockProducer
    {
        private List<ITransaction> _transactions = new List<ITransaction>();

        public IBlock CreateBlock()
        {
            if (_transactions.Count < 1)
            {
                return null;
            }

            Block block = new Block(Network.Chain.CurrentBlockHash);

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
