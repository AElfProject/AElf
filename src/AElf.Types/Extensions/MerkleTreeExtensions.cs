using System.Collections.Generic;
using System.Linq;
using AElf.Types;

namespace AElf
{
    public static class MerkleTreeExtensions
    {
        public static Hash ComputeParentWith(this Hash left, Hash right)
        {
            var res = new[] {left, right}.OrderBy(b => b).Select(h => h.DumpByteArray())
                .Aggregate(new byte[0], (rawBytes, bytes) => rawBytes.Concat(bytes).ToArray());

            return Hash.FromRawBytes(res);
        }

        public static Hash ComputeRootHash(this IEnumerable<Hash> hashList)
        {
            var nodes = hashList.ToList();
            if (!nodes.Any())
            {
                return Hash.Empty;
            }

            if (nodes.Count % 2 == 1)
                nodes.Add(nodes.Last());
            var nodeToAdd = nodes.Count / 2;
            var newAdded = 0;
            var i = 0;
            while (i < nodes.Count - 1)
            {
                var left = nodes[i++];
                var right = nodes[i++];
                nodes.Add(left.ComputeParentWith(right));
                if (++newAdded != nodeToAdd)
                    continue;

                // complete this row
                if (nodeToAdd % 2 == 1 && nodeToAdd != 1)
                {
                    nodeToAdd++;
                    nodes.Add(nodes.Last());
                }

                // start a new row
                nodeToAdd /= 2;
                newAdded = 0;
            }

            return nodes.Last();
        }
    }
}