using System;
using System.IO;
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
            var appDatePath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "aelf");
            appDatePath1.ShouldBe(appDatePath2);
        }
    }
}