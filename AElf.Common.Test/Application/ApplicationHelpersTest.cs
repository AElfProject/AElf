using Xunit;
using Shouldly;

namespace AElf.Common.Application.Test
{
    public class ApplicationHelpersTest
    {

        [Fact]
        public void Get_Path()
        {
            var appDatePath1 = ApplicationHelpers.AppDataPath;
            appDatePath1.ShouldEndWith("aelf");
        }
    }
}