using System;
using System.IO;
using AElf.CLI.Command;
using AElf.CLI.Screen;
using Xunit;

namespace AElf.CLI.Tests
{
    public class CommandTest
    {
        [Fact]
        public void TestListCommands()
        {
            var ctx = new AElfClientProgramContext(new ScreenManager());
            var command = new RootCommand();
            var cmds = command.Process(new string[]
            {
                "get_commands"
            }, ctx);
            Assert.Equal(cmds.Split('\n').Length, RootCommand.Commands.Count);
        }

        [Fact]
        public void TestCreateListAccount()
        {
            string tmpPath = Path.Combine(Path.GetTempPath(), "TestCreateListAccount");
            if (Directory.Exists(tmpPath))
            {
                Directory.Delete(tmpPath, true);
            }
            Directory.CreateDirectory(tmpPath);
            var ctx = new AElfClientProgramContext(new ScreenManager(), tmpPath);
            var command = new RootCommand();
            for (var i = 0; i < 2; ++i)
            {
                var result = command.Process(new string[]
                {
                    "account",
                    "new",
                    "12345"
                }, ctx);
                Assert.Equal(result, AccountCommand.MsgAccountCreated);
            }

            var listResult = command.Process(new string[]
            {
                "account",
                "list"
            }, ctx);
            Assert.Equal(2, listResult.Split('\n').Length);
        }
    }
}