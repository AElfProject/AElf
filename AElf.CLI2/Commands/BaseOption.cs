using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Newtonsoft.Json;

namespace AElf.CLI2.Commands
{
    public class BaseOption
    {
        [JsonProperty("datadir")]
        [LoadDefaultFromEnvironmentVariable("AELF_CLI_DATADIR")]
        [Option('d', "datadir", HelpText = "The directory that contains the files.")]
        public string DataDir { get; set; } = ".";

        [JsonProperty("endpoint")]
        [LoadDefaultFromEnvironmentVariable("AELF_CLI_ENDPOINT")]
        [Option('e', "endpoint", HelpText = "The endpoint for the rpc service.")]
        public string Endpoint { get; set; } = "";

        [JsonProperty("account")]
        [LoadDefaultFromEnvironmentVariable("AELF_CLI_ACCOUNT")]
        [Option('a', "account", HelpText = "The account to be used to interact with the blockchain.")]
        public string Account { get; set; } = "";

        [JsonProperty("accountsdir")]
        public string AccountsDir
        {
            get
            {
                var dir = Path.GetFullPath(Path.Combine(DataDir, "accounts"));
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception)
                {
                    // Ignore
                }

                return dir;
            }
        }

        public string GetPathForAccount(string address)
        {
            return Path.GetFullPath(Path.Combine(AccountsDir, address + ".xml"));
        }

        public BaseOption()
        {
            InitFromEnv();
        }

        private void InitFromEnv()
        {
            // Currently only supports string type of property
            var props = GetType().GetProperties().Where(
                p => Attribute.IsDefined(p, typeof(OptionAttribute)) &&
                     Attribute.IsDefined(p, typeof(LoadDefaultFromEnvironmentVariableAttribute)) &&
                     p.PropertyType == typeof(string)
            );

            foreach (var p in props)
            {
                var val = Environment.GetEnvironmentVariable(
                    p.GetCustomAttribute<LoadDefaultFromEnvironmentVariableAttribute>().VariableName
                );
                if (val == null)
                {
                    continue;
                }

                p.SetValue(this, val);
            }
        }
    }
}