using System.Collections.Generic;

namespace AElf.WebApp.Application.Chain.Dto
{
    public class MerklePathDto
    {
        public List<MerklePathNode> MerklePathNodes;
    }

    public class MerklePathNode
    {
        public string Hash { get; set; }
        public bool IsLeftChildNode { get; set; }
    }
}