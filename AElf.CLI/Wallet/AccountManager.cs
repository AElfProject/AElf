using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.CLI.Command;
using AElf.CLI.Data.Protobuf;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Newtonsoft.Json.Linq;
using ProtoBuf;

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

        public readonly List<string> SubCommands = new List<string>()
        {
            NewCmdName,
            ListAccountsCmdName,
            UnlockAccountCmdName
        };

        private string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args.Count == 0)
                return CliCommandDefinition.InvalidParamsError;

            if (parsedCmd.Args.ElementAt(0).Equals(UnlockAccountCmdName, StringComparison.OrdinalIgnoreCase))
            {
                if (parsedCmd.Args.Count < 2)
                    return CliCommandDefinition.InvalidParamsError;
            }

            if (!SubCommands.Contains(parsedCmd.Args.ElementAt(0)))
                return CliCommandDefinition.InvalidParamsError;
            
            return null;
        }
        
        public void ProcessCommand(CmdParseResult parsedCmd)
        {
            string validationError = Validate(parsedCmd);

            if (validationError != null)
            {
                _screenManager.PrintError(validationError);
                return;
            }

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
            if (!_keyStore.ListAccounts().Contains(address))
            {
                _screenManager.PrintError("account does not exist!");
                return;
            }
                
            var password = _screenManager.AskInvisible("password: ");
            var tryOpen = _keyStore.OpenAsync(address, password);
            
            if (tryOpen == AElfKeyStore.Errors.WrongPassword)
                _screenManager.PrintError("incorrect password!");
            else if (tryOpen == AElfKeyStore.Errors.AccountAlreadyUnlocked)
                _screenManager.PrintError("account already unlocked!");
            else if (tryOpen == AElfKeyStore.Errors.None)
                _screenManager.PrintLine("account successfully unlocked!");
        }

        private void CreateNewAccount()
        {
            var password = _screenManager.AskInvisible("password: ");
            _keyStore.Create(password);
            _screenManager.PrintLine("account successfully created!");
        }

        private void ListAccounts()
        {
            List<string> accnts = _keyStore.ListAccounts();

            for (int i = 0; i < accnts.Count; i++)
            {
                _screenManager.PrintLine("account #" + i + " : " + accnts.ElementAt(i));
            }
        }

        public Transaction SignTransaction(JObject t)
        {
            Transaction tr = new Transaction();

            string addr = t["from"].ToString();
            
            //UnlockAccount(addr);
            ECKeyPair kp = _keyStore.GetAccountKeyPair(addr);

            try
            {
                tr.From = Convert.FromBase64String(addr);
                tr.To = Convert.FromBase64String(t["to"].ToString());
                tr.IncrementId = t["incr"].ToObject<ulong>();
                
                MemoryStream ms = new MemoryStream();
                Serializer.Serialize(ms, tr);
    
                byte[] b = ms.ToArray();
                byte[] toSig = SHA256.Create().ComputeHash(b);
                
                // Sign the hash
                ECSigner signer = new ECSigner();
                ECSignature signature = signer.Sign(kp, toSig);
                
                // Update the signature
                tr.R = signature.R;
                tr.S = signature.S;
                
                tr.P = kp.PublicKey.Q.GetEncoded();

                return tr;
            }
            catch (Exception e)
            {
                ;
            }
            
            return null;
        }
    }
}