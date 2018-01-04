using System.Collections.Generic;
namespace AElf.Kernel
{
    /// <summary>
    /// A Node of Merkle Tree.
    /// </summary>
    public class MerkleNode
    {
        /// <summary>
        /// The list of brother nodes which is necessary to calculate the merkle tree.
        /// </summary>
        public List<Node> NodeList { get; set; }
        /// <summary>
        /// The hash code of this node.
        /// </summary>
        public string ProofHashValue { get; set; }
        /// <summary>
        /// Code to identify this node.
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// Children of this nodes.
        /// </summary>
        public List<int> ChildrenList { get; set; }
        /// <summary>
        /// Distance to Merkle Root / Depth - 1.
        /// </summary>
        public int Distance { get; set; }
        /// <summary>
        /// Parent node.
        /// </summary>
        public int ParentProofCode { get; set; }
        /// <summary>
        /// Brother node.
        /// </summary>
        public int BrotherNode { get; set; }

        /// <summary>
        /// Add brother node.
        /// </summary>
        /// <param name="brotherNode"></param>
        /// <param name="MerkleProofDict"></param>
        /// <param name="brotherCode"></param>
        public void AddBrotherNode(Node brotherNode, Dictionary<int, MerkleNode> MerkleProofDict, int brotherCode)
        {
            BrotherNode = brotherCode;
            AddBrotherNode(brotherNode, MerkleProofDict);
        }

        /// <summary>
        /// Add brother node.
        /// </summary>
        /// <param name="brotherNode"></param>
        /// <param name="MerkleProofDict"></param>
        public void AddBrotherNode(Node brotherNode, Dictionary<int, MerkleNode> MerkleProofDict)
        {
            if (NodeList == null)
                NodeList = new List<Node>();
            NodeList.Add(brotherNode);
            Distance++;
            if (ChildrenList != null && ChildrenList.Count > 0)
            {
                foreach (int code in ChildrenList)
                {
                    MerkleProofDict[code].AddBrotherNode(brotherNode, MerkleProofDict);
                }
            }
        }

        /// <summary>
        /// Modify brother node.
        /// </summary>
        /// <param name="newStr"></param>
        /// <param name="nodeIndex"></param>
        /// <param name="MerkleProofDict"></param>
        public void ModifyBrotherNode(string newStr, int nodeIndex, Dictionary<int, MerkleNode> MerkleProofDict)
        {
            if (NodeList != null && NodeList.Count > nodeIndex)
            {
                NodeList[nodeIndex].HashValue = newStr;
                if (ChildrenList != null && ChildrenList.Count > 0)
                {
                    nodeIndex++;
                    foreach (int code in ChildrenList)
                    {
                        MerkleProofDict[code].ModifyBrotherNode(newStr, nodeIndex, MerkleProofDict);
                    }
                }
            }
        }
    }
}
