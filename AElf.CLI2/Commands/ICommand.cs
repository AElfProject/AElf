using System;
using CommandLine;

namespace AElf.CLI2.Commands
{
    public interface ICommand
    {
        void Execute();
    }

    public class BaseOption
    {
        [Option("server_addr", HelpText = "AElf jsonrpc server address.", Default = "")]
        public string ServerAddr { get; set; }

        public void ParseEnvVars()
        {
            if (ServerAddr.Length == 0)
            {
                var servAddr = Environment.GetEnvironmentVariable("AELF_SERVER_ADDR");
                if (servAddr != null)
                {
                    ServerAddr = servAddr;
                }
            }
        }
    }
}