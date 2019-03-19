using AElf.Common;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    public class StateValueTests
    {
        [Fact]
        public void StateValue_Test()
        {
            var hashArray = Hash.Generate().DumpByteArray();
            var stateValue = StateValue.Create(hashArray);
            var isDirty = stateValue.IsDirty;
            isDirty.ShouldBeFalse();

            var hashArray1 = stateValue.Get();
            hashArray.ShouldBe(hashArray1);

            var hashArray2 = Hash.Generate().DumpByteArray();
            stateValue.Set(hashArray2);

            isDirty = stateValue.IsDirty;
            isDirty.ShouldBeTrue();

            var hashArray3 = stateValue.Get();
            hashArray3.ShouldBe(hashArray2);
            
            stateValue.OriginalValue.ShouldBe(hashArray);
            stateValue.CurrentValue.ShouldBe(hashArray2);
        }
    }
}