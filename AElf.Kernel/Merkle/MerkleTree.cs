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

        public MerkleTree AddLeaf(MerkleNode node)
        {
            Nodes.Add(node);
            Leaves.Add(node);
            SortedLeaves.Add(node.Hash, node);

            return this;
        }

        public MerkleTree AddLeaves(List<MerkleNode> nodes)
        {
            nodes.ForEach(n => AddLeaf(n));

            return this;
        }

        public MerkleNode FindLeaf(MerkleHash hash)
        {
            if (SortedLeaves.TryGetValue(hash, out MerkleNode node))
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
                    Nodes.Add(parent);
                }

                GenerateMerkleTree(parents);
            }
        }

        public bool VerifyProofList(List<MerkleHash> hashlist)
        {
            List<MerkleHash> t = hashlist.ComputeProofHash();
            return MerkleRoot.Hash.ToString() == hashlist.ComputeProofHash()[0].ToString();
        }
    }
}
