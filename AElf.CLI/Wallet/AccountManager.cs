using System;
using System.Linq;
using AElf.CLI.Command;
using AElf.CLI.Command.Account;
using AElf.CLI.Parsing;
using AElf.Cryptography;
using Org.BouncyCastle.Asn1;

namespace AElf.CLI.Wallet
{
    public class AccountManager
    {
        private AElfKeyStore _keyStore;
        
        public void ProcessCommand(CmdParseResult parsedCmd)
        {
            string subCommand = parsedCmd.Args.ElementAt(0);
            Console.WriteLine("subcommand : " + subCommand);
        }
    }
}