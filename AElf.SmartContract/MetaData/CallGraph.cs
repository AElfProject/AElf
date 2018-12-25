using System;
using System.Linq;
using Google.Protobuf.Collections;
using QuickGraph;


namespace AElf.SmartContract.MetaData
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

namespace AElf.SmartContract
{
    public sealed partial class SerializedCallGraph
    {
        bool IEquatable<SerializedCallGraph>.Equals(SerializedCallGraph other)
        {
            var edges = new RepeatedField<GraphEdge>();
            edges.AddRange(Edges.OrderBy(e => e.Source).ThenBy(e => e.Target));
            var otherEdges = new RepeatedField<GraphEdge>();
            otherEdges.AddRange(other.Edges.OrderBy(e => e.Source).ThenBy(e => e.Target));
            
            var vertices = new RepeatedField<string>();
            vertices.AddRange(Vertices.OrderBy(v => v));
            var otherVertices = new RepeatedField<string>();
            otherVertices.AddRange(other.Vertices.OrderBy(v => v));
            
           
            return edges.Equals(otherEdges) && vertices.Equals(otherVertices);
        }
    }
}