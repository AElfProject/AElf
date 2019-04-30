using System;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract
{
    public class SerializedCallGraphTest
    {
        [Fact]
        public void IEquatable_SerializedCallGraph_Equals_Other_True()
        {
            IEquatable<SerializedCallGraph> serializedCallGraph = new SerializedCallGraph
            {
                Edges =
                {
                    new GraphEdge {Source = "Source1", Target = "Target1"},
                    new GraphEdge {Source = "Source2", Target = "Target2"},
                    new GraphEdge {Source = "Source3", Target = "Target3"}
                },
                Vertices = {"Vertices"}
            };
            
            var otherSerializedCallGraph = new SerializedCallGraph
            {
                Edges =
                {
                    new GraphEdge {Source = "Source1", Target = "Target1"},
                    new GraphEdge {Source = "Source2", Target = "Target2"},
                    new GraphEdge {Source = "Source3", Target = "Target3"}
                },
                Vertices = {"Vertices"}
            };

            serializedCallGraph.Equals(otherSerializedCallGraph).ShouldBeTrue();
        }

        [Fact]
        public void IEquatable_SerializedCallGraph_Equals_Other_False()
        {
            IEquatable<SerializedCallGraph> serializedCallGraph = new SerializedCallGraph
            {
                Edges =
                {
                    new GraphEdge {Source = "Source1", Target = "Target1"},
                    new GraphEdge {Source = "Source2", Target = "Target2"},
                    new GraphEdge {Source = "Source3", Target = "Target3"}
                },
                Vertices = {"Vertices"}
            };
            
            serializedCallGraph.Equals(new SerializedCallGraph()).ShouldBeFalse();
            
            var otherSerializedCallGraph1 = new SerializedCallGraph
            {
                Edges =
                {
                    new GraphEdge {Source = "Source1", Target = "Target1"},
                    new GraphEdge {Source = "Source2", Target = "Target2"},
                    new GraphEdge {Source = "Source3", Target = "Target4"}
                },
                Vertices = {"Vertices"}
            };

            serializedCallGraph.Equals(otherSerializedCallGraph1).ShouldBeFalse();
            
            var otherSerializedCallGraph2 = new SerializedCallGraph
            {
                Edges =
                {
                    new GraphEdge {Source = "Source1", Target = "Target1"},
                    new GraphEdge {Source = "Source2", Target = "Target2"},
                    new GraphEdge {Source = "Source3", Target = "Target3"}
                },
                Vertices = {"OtherVertices"}
            };

            serializedCallGraph.Equals(otherSerializedCallGraph2).ShouldBeFalse();
            
            var otherSerializedCallGraph3 = new SerializedCallGraph
            {
                Edges =
                {
                    new GraphEdge {Source = "Source1", Target = "Target1"},
                    new GraphEdge {Source = "Source2", Target = "Target2"},
                    new GraphEdge {Source = "Source3", Target = "Target4"}
                },
                Vertices = {"OtherVertices"}
            };

            serializedCallGraph.Equals(otherSerializedCallGraph3).ShouldBeFalse();
        }

        [Fact]
        public void IEquatable_SerializedCallGraph_Equals_Null_False()
        {
            IEquatable<SerializedCallGraph> serializedCallGraph = new SerializedCallGraph
            {
                Edges =
                {
                    new GraphEdge {Source = "Source1", Target = "Target1"},
                    new GraphEdge {Source = "Source2", Target = "Target2"},
                    new GraphEdge {Source = "Source3", Target = "Target3"}
                },
                Vertices = {"Vertices"}
            };
            serializedCallGraph.Equals(null).ShouldBeFalse();
        }
    }
}