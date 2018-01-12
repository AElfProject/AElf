namespace AElf.Kernel
{
    public class PoWMiner : IMiner
    {
        public byte[] Mine(IBlockHeader blockheader)
        {
            int bits = (blockheader as BlockHeader).Bits;
            while (true)
            {
                //Change the nonce
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
