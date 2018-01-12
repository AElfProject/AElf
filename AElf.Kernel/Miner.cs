namespace AElf.Kernel
{
    public class Miner : IMiner
    {
        public byte[] Mine(IBlockHeader blockheader)
        {
            int bits = (blockheader as BlockHeader).Bits;
            while (true)
            {
                //Change the nonce
                (blockheader as BlockHeader).SetNonce();
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
