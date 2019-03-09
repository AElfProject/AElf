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
            var appDatePath1 = ApplicationHelper.AppDataPath;
            var appDatePath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "aelf");
            appDatePath1.ShouldBe(appDatePath2);
        }

        [Fact]
        public void Set_Path()
        {
            var path1 = "";
            var path2 = "/tmp/not_exist";

            ApplicationHelper.AppDataPath = path1;
            ApplicationHelper.AppDataPath = path2;
            ApplicationHelper.AppDataPath.ShouldBe(path2);
        }
    }
}