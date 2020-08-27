using Shouldly;
using Xunit;

namespace AElf.Sdk.CSharp.Tests
{
    public class ExecutionObserverTests
    {
        [Fact]
        public void CallCount_Test()
        {
            var observer = new ExecutionObserver(-1, 5);
            observer.GetCallCount().ShouldBe(0);
            observer.CallCount();
            observer.GetCallCount().ShouldBe(1);

            observer = new ExecutionObserver(5, 5);
            
            for (int i = 0; i < 5; i++)
            {
                observer.CallCount();
                observer.GetCallCount().ShouldBe(i + 1);
            }

            Assert.Throws<RuntimeCallThresholdExceededException>(() => observer.CallCount());
        }
        
        [Fact]
        public void BranchCount_Test()
        {
            var observer = new ExecutionObserver(5, -1);
            observer.GetBranchCount().ShouldBe(0);
            observer.BranchCount();
            observer.GetBranchCount().ShouldBe(1);

            observer = new ExecutionObserver(5, 5);
            
            for (int i = 0; i < 5; i++)
            {
                observer.BranchCount();
                observer.GetBranchCount().ShouldBe(i + 1);
            }

            Assert.Throws<RuntimeBranchThresholdExceededException>(() => observer.BranchCount());
        }
    }
}