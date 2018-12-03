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
        [Option("endpoint", HelpText = "The endpoint for the rpc service.")]
        public string Endpoint { get; set; }

        public void ParseEnvVars()
        {
            if (string.IsNullOrEmpty(Endpoint))
            {
                var endpoint = Environment.GetEnvironmentVariable("AELF_RPC_ENDPOINT");
                if (endpoint != null)
                {
                    Endpoint = endpoint;
                }
            }

            if (string.IsNullOrEmpty(Endpoint))
            {
                Endpoint = "";
            }
        }
    }
}