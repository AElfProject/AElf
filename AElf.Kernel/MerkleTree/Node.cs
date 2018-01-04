namespace AElf.Kernel
{
    /// <summary>
    /// Base class of Merkle nodes.
    /// </summary>
    public class Node
    {
        #region Properties

        /// <summary>
        /// String to calculate hash value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Hash value of this node.
        /// </summary>
        public string HashValue
        {
            get
            {
                return Value.GetMerkleHash();
            }
            set { }
        }

        public int Side { get; set; }
        #endregion

        /// <summary>
        /// Constructor Method.
        /// </summary>
        /// <param name="str">String to calculate hash value.</param>
        public Node(string str) => Value = str;
        public Node() { }

    }
}