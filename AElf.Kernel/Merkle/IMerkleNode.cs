using System.Collections.Generic;

namespace AElf.Kernel
{
    public interface IMerkleNode: IEnumerable<IMerkleNode>
    {
        IMerkleNode LeftNode { get; set; }
        IMerkleNode RightNode { get; set; }
        IMerkleNode ParentNode { get; set; }
        IHash<IMerkleNode> Hash { get; set; }
        bool VerifyHash();
    }
}