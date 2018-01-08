using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel.Merkle
{
    public class ProofNode
    {
        /// <summary>
        /// This node donnot have right node -> Root
        /// </summary>
        public enum NodeSide { Left, Right, Root}
        /// <summary>
        /// The side of this node.
        /// </summary>
        public NodeSide Side { get; set; }
        public MerkleHash Hash { get; set; }

        public override string ToString() => Hash.ToString();
    }
}
