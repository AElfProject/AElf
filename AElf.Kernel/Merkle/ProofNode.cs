using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
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
        public Hash Hash { get; set; }

        public override string ToString() => Hash.ToString();
    }
}
