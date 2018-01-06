using System.Collections.Generic;

namespace AElf.Kernel.Merkle
{
    public class MerkleTree
    {
        public MerkleNode MerkleRoot { get; set; }

        protected List<MerkleNode> Nodes { get; set; } = new List<MerkleNode>();
        protected List<MerkleNode> Leaves { get; set; } = new List<MerkleNode>();
        protected SortedList<MerkleHash, MerkleNode> SortedLeaves { get; set; } 
            = new SortedList<MerkleHash, MerkleNode>(new MerkleHashCompare());

        public void AddLeaf(MerkleNode node)
        {
            Nodes.Add(node);
            Leaves.Add(node);
            SortedLeaves.Add(node.Hash, node);
        }

        public void AddLeaves(List<MerkleNode> nodes)
        {
            nodes.ForEach(n => AddLeaf(n));
        }

        public MerkleNode FindLeaf(MerkleHash hash)
        {
            MerkleNode node;
            if (SortedLeaves.TryGetValue(hash, out node))
                return node;
            else
                return null;
        }

        public void GenerateMerkleTree()
        {
            GenerateMerkleTree(Leaves);
        }

        /// <summary>
        /// Generate Merkle Tree with a list of merkle nodes.
        /// </summary>
        /// <param name="nodes"></param>
        public void GenerateMerkleTree(List<MerkleNode> nodes)
        {
            if (nodes.Count < 1)
            {
                throw new MerkleException("Cannot generate merkle tree without any nodes.");
            }

            if (nodes.Count == 1)//Finally
            {
                MerkleRoot = nodes[0];
            }
            else
            {
                List<MerkleNode> parents = new List<MerkleNode>();

                for (int i = 0; i < nodes.Count; i += 2)
                {
                    MerkleNode right = (i + 1 < nodes.Count) ? nodes[i + 1] : null;
                    MerkleNode parent = new MerkleNode(nodes[i], right);
                    parents.Add(parent);
                }

                GenerateMerkleTree(parents);
            }
        }
    }
}
