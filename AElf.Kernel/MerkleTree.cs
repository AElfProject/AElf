using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    public class MerkleTree
    {
        public Dictionary<int, MerkleNode> MerkleProofDict { get; set; }
        public string MerkleRoot { get; set; }

        private MerkleNode lastChildProof;
        private int currentCode = 0;

        /// <summary>
        /// Constructor Method
        /// </summary>
        /// <param name="baseChildValues">a list of leaf nodes</param>
        public MerkleTree(List<string> baseChildValues)
        {
            MerkleProofDict = new Dictionary<int, MerkleNode>();

            if (baseChildValues.Count < 1)
                return;

            for (int i = 0; i < baseChildValues.Count; i++)
            {
                MerkleNode proof = new MerkleNode()
                {
                    ProofHashValue = baseChildValues[i].GetMerkleHash(),
                    Code = i + 1,
                    ChildrenList = new List<int>()
                };
                MerkleProofDict.Add(i + 1, proof);
                lastChildProof = proof;
            }
            currentCode = baseChildValues.Count + 1;
            CreatTree();
        }

        /// <summary>
        /// Create a Merkle Tree.
        /// </summary>
        public void CreatTree()
        {
            List<MerkleNode> proofList = MerkleProofDict.Values.ToList();
            CalculateMerkle(proofList);
        }

        /// <summary>
        /// Recursively calculate every nodes.
        /// </summary>
        /// <param name="proofList"></param>
        private void CalculateMerkle(List<MerkleNode> proofList)
        {
            if (proofList.Count == 1)
            {
                MerkleRoot = proofList[0].ProofHashValue;
                return;
            }
            else
            {
                List<MerkleNode> newProofList = new List<MerkleNode>();
                for (int i = 0; i < proofList.Count; i += 2)
                {
                    MerkleNode newProof;
                    if (i + 1 >= proofList.Count)
                    {
                        newProof = NewProofBySelf(proofList[i]);
                    }
                    else
                    {
                        newProof = CombineTwoProofs(proofList[i], proofList[i + 1]);
                    }
                    newProofList.Add(newProof);
                    MerkleProofDict.Add(newProof.Code, newProof);

                }
                CalculateMerkle(newProofList);
            }
        }


        /// <summary>
        /// Verification reliability.
        /// </summary>
        /// <returns></returns>
        public bool CheckProof(MerkleNode checkedProof, string rootValue)
        {
            if (checkedProof.NodeList.Count != checkedProof.Distance)
                return false;
            string src = checkedProof.ProofHashValue;
            for (int i = 0; i < checkedProof.Distance; i++)
            {
                src = checkedProof.NodeList[i].Side == 1 ? src + checkedProof.NodeList[i].HashValue : checkedProof.NodeList[i].HashValue + src;
                src = src.GetMerkleHash();
            }
            return src == rootValue;
        }

        /// <summary>
        /// Add a node.
        /// </summary>
        /// <param name="newChildValue"></param>
        public void AddChildValue(string newChildValue)
        {
            MerkleNode proof = new MerkleNode()
            {
                ProofHashValue = newChildValue.GetMerkleHash(),
                Code = currentCode++,
                ChildrenList = new List<int>()
            };
            MerkleProofDict.Add(proof.Code, proof);
            AddMerkleProof(lastChildProof, proof);
            lastChildProof = proof;
        }

        /// <summary>
        /// Add Merkle Proof.
        /// </summary>
        /// <param name="previProof"></param>
        /// <param name="proof"></param>
        public void AddMerkleProof(MerkleNode previProof, MerkleNode proof)
        {
            if (previProof.BrotherNode > 0)
            {
                MerkleNode newProof = NewProofBySelf(proof);
                MerkleProofDict.Add(newProof.Code, newProof);
                AddMerkleProof(MerkleProofDict[previProof.ParentProofCode], newProof);
            }
            else
            {
                if (previProof.ParentProofCode == 0)
                {
                    MerkleNode newProof = CombineTwoProofs(previProof, proof);
                    MerkleProofDict.Add(newProof.Code, newProof);
                    MerkleRoot = newProof.ProofHashValue;
                    return;
                }
                else
                {
                    previProof.AddBrotherNode(new Node() { Side = 1, HashValue = proof.ProofHashValue }, MerkleProofDict);
                    proof.AddBrotherNode(new Node() { Side = 2, HashValue = previProof.ProofHashValue }, MerkleProofDict);
                    proof.ParentProofCode = previProof.ParentProofCode;
                    MerkleProofDict[previProof.ParentProofCode].ChildrenList.Add(proof.Code);

                    string newStr = previProof.ProofHashValue + proof.ProofHashValue;
                    newStr = newStr.GetMerkleHash();
                    ModifyChild(MerkleProofDict[previProof.ParentProofCode], newStr);
                }
            }
        }

        /// <summary>
        /// Combine Two proofs.
        /// </summary>
        /// <param name="previProof"></param>
        /// <param name="proof"></param>
        public MerkleNode CombineTwoProofs(MerkleNode previProof, MerkleNode proof)
        {
            MerkleNode newProof = new MerkleNode() { Code = currentCode++, ChildrenList = new List<int>() };
            string newStr = previProof.ProofHashValue + proof.ProofHashValue;
            newProof.ProofHashValue = newStr.GetMerkleHash();
            newProof.ChildrenList.Add(previProof.Code);
            newProof.ChildrenList.Add(proof.Code);
            previProof.AddBrotherNode(new Node() { Side = 1, HashValue = proof.ProofHashValue }, MerkleProofDict, proof.Code);
            previProof.ParentProofCode = newProof.Code;
            proof.AddBrotherNode(new Node() { Side = 2, HashValue = previProof.ProofHashValue }, MerkleProofDict, previProof.Code);
            proof.ParentProofCode = newProof.Code;
            return newProof;
        }

        /// <summary>
        /// The situation when a node dose not have a brother.
        /// </summary>
        /// <param name="proof"></param>
        /// <returns></returns>
        public MerkleNode NewProofBySelf(MerkleNode proof)
        {
            MerkleNode newProof = new MerkleNode()
            {
                ProofHashValue = proof.ProofHashValue.GetMerkleHash(),
                Code = currentCode++,
                ChildrenList = new List<int>()
            };
            newProof.ChildrenList.Add(proof.Code);
            proof.ParentProofCode = newProof.Code;
            proof.Distance++;
            return newProof;
        }

        /// <summary>
        /// Modify node.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="newValue"></param>
        public void ModifyChild(int code, string newValue)
        {
            if (MerkleProofDict.ContainsKey(code))
            {
                ModifyChild(MerkleProofDict[code], newValue);
            }
        }

        /// <summary>
        /// Modify node.
        /// </summary>
        /// <param name="proof"></param>
        /// <param name="newValue"></param>
        public void ModifyChild(MerkleNode proof, string newValue)
        {
            proof.ProofHashValue = newValue;
            //Update the value of its brother.
            if (proof.BrotherNode > 0)
            {
                MerkleNode brotherProof = MerkleProofDict[proof.BrotherNode];
                brotherProof.ModifyBrotherNode(newValue, 0, MerkleProofDict);
            }
            if (proof.ParentProofCode > 0)
            {
                //Calculate the hash value of its parent node.
                if (proof.BrotherNode > 0)
                {
                    Node friendNode = proof.NodeList[0];
                    newValue = friendNode.Side == 1 ? newValue + friendNode.HashValue : friendNode.HashValue + newValue;
                }
                newValue = newValue.GetMerkleHash();
                //Update its parent node.
                ModifyChild(MerkleProofDict[proof.ParentProofCode], newValue);
            }
            else
            {
                MerkleRoot = newValue;
            }
        }
    }
}