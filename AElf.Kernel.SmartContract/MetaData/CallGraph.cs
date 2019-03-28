using System;
using System.Linq;
using QuickGraph;

//TODO: All code about Metadata without cases covered, including AElf.Kernel.SmartContract and AElf.Runtime.CSharp.Core [Case]
namespace AElf.Kernel.SmartContract.MetaData
{
    public class CallGraph : AdjacencyGraph<string, Edge<string>>, IEquatable<CallGraph>
    {
        bool IEquatable<CallGraph>.Equals(CallGraph other)
        {
            return CallingGraphToString(this) == CallingGraphToString(other);
        }

        private string CallingGraphToString(CallGraph callGraph)
        {
            return
                $"Edge: [{string.Join(", ", callGraph.Edges.OrderBy(a => a.Source).ThenBy(a => a.Target).Select(a => a.ToString()))}] Vertices: [{string.Join(", ", callGraph.Vertices.OrderBy(a => a))}]";
        }
    }
}