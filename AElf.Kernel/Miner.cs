namespace AElf.Kernel
{
    /// <summary>
    /// An embeded dummy miner 
    /// </summary>
    public class Miner : IMiner
    {
        /// <summary>
        /// Mine the specified blockheader.
        /// </summary>
        /// <returns>does not but serilize the block</returns>
        /// <param name="blockheader">Blockheader.</param>
        public byte[] Mine(IBlockHeader blockheader)
        {
            // TODO: return a serlized block header
            return null;
        }
    }
}
