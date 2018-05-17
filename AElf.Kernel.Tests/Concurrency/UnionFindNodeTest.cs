using AElf.Kernel.Concurrency;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency
{
    public class UnionFindNodeTest
    {
        [Fact]
        public void TestTrivial()
        {
            var n1 = new UnionFindNode();
            var n2 = new UnionFindNode();
            
            Assert.True(n1.IsUnionedWith(n1));
            Assert.False(n1.IsUnionedWith(n2));
        }
        
        
        [Fact]
        public void TestUnion()
        {
            var n1 = new UnionFindNode();
            var n2 = new UnionFindNode();
            var n3 = new UnionFindNode();
            var n4 = new UnionFindNode();
            var n5 = new UnionFindNode();
                      
            
            Assert.True(n1.Union(n2));
            Assert.True(n2.Union(n3));
            Assert.False(n3.Union(n1));
            
            Assert.True(n4.Union(n5));
            Assert.False(n5.Union(n4));
            
            Assert.True(n1.IsUnionedWith(n1));
            Assert.True(n2.IsUnionedWith(n2));
            Assert.True(n1.IsUnionedWith(n2));
            Assert.True(n1.IsUnionedWith(n3));
            
            Assert.False(n4.IsUnionedWith(n1));
            Assert.False(n5.IsUnionedWith(n2));
            
            Assert.True(n4.IsUnionedWith(n5));
     
        }
    }
}