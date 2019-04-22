using System;
using System.IO;
using AElf.Common.Application;
using Shouldly;
using Xunit;

namespace AElf.Common
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
            
            //Recover to default.
            Directory.Delete(path2, true);
            ApplicationHelper.AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "aelf");
        }
    }
}