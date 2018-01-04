namespace AElf.Kernel
{
    /// <summary>
    /// Base class of Merkle nodes.
    /// </summary>
    public class Node
    {
        public int Side { get; set; }//1-right;2-left.
        public string HashValue { get; set; }//The hash value of this node.
    }
}