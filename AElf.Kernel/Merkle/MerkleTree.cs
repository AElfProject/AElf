using System.Collections.Generic;

namespace AElf.Kernel
{
    public class MerkleTree
    {
        public MerkleNode MerkleRoot { get; set; }

        protected List<MerkleNode> Nodes { get; set; } = new List<MerkleNode>();
        protected List<MerkleNode> Leaves { get; set; } = new List<MerkleNode>();
        protected SortedList<Hash, MerkleNode> SortedLeaves { get; set; }
            = new SortedList<Hash, MerkleNode>(new MerkleHashCompare());

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

        public MerkleNode FindLeaf(Hash hash)
        {
            if (SortedLeaves.TryGetValue(hash, out MerkleNode node))
                return node;
            else
                return null;
        }

        public void Generate()
        {
            Generate(Leaves);
        }

        /// <summary>
        /// Generate Merkle Tree with a list of merkle nodes.
        /// </summary>
        /// <param name="nodes"></param>
        public void Generate(List<MerkleNode> nodes)
        {
            if (nodes.Count < 1)
            {
                throw new AELFException("Cannot generate merkle tree without any nodes.");
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

                Generate(parents);
            }
        }

        public bool VerifyProofList(List<Hash> hashlist)
        {
            List<Hash> t = hashlist.ComputeProofHash();
            return MerkleRoot.Hash.ToString() == hashlist.ComputeProofHash()[0].ToString();
        }
    }
}