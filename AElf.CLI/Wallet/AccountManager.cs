using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CLI.Command;
using AElf.CLI.Command.Account;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.Cryptography;
using Org.BouncyCastle.Asn1;

namespace AElf.CLI.Wallet
{
    public class AccountManager
    {
        private const string NewCmdName = "new";
        private const string ListAccountsCmdName = "list";
        private const string UnlockAccountCmdName = "unlock";
        
        private AElfKeyStore _keyStore;
        private ScreenManager _screenManager;

        public AccountManager(AElfKeyStore keyStore, ScreenManager screenManager)
        {
            _screenManager = screenManager;
            _keyStore = keyStore;
        }
        
        public void ProcessCommand(CmdParseResult parsedCmd)
        {
            string subCommand = parsedCmd.Args.ElementAt(0);

            if (subCommand.Equals(NewCmdName, StringComparison.OrdinalIgnoreCase))
            {
                CreateNewAccount();
            }
            else if (subCommand.Equals(ListAccountsCmdName, StringComparison.OrdinalIgnoreCase))
            {
                ListAccounts();
            }
            else if (subCommand.Equals(UnlockAccountCmdName, StringComparison.OrdinalIgnoreCase))
            {
                UnlockAccount(parsedCmd.Args.ElementAt(1));
            }
        }

        private void UnlockAccount(string address)
        {
            var password = _screenManager.AskInvisible("password: ");
            _keyStore.OpenAsync(address, password);
        }

        private void CreateNewAccount()
        {
            var password = _screenManager.AskInvisible("password: ");
            _keyStore.Create(password);
        }

        private void ListAccounts()
        {
            List<string> accnts = _keyStore.ListAccounts();

            for (int i = 0; i < accnts.Count; i++)
            {
                _screenManager.PrintLine("account #" + i + " : " + accnts.ElementAt(i));
            }
        }
    }
}