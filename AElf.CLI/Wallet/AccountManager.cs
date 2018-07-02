using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.CLI.Command;
using AElf.CLI.Data.Protobuf;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.CLI.Wallet.Exceptions;
using AElf.Common.ByteArrayHelpers;
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
                if (parsedCmd.Args.Count == 2)
                {
                    UnlockAccount(parsedCmd.Args.ElementAt(1));
                }
                else if (parsedCmd.Args.Count == 3)
                {
                    UnlockAccount(parsedCmd.Args.ElementAt(1), false);
                }
                else
                {
                    _screenManager.PrintError("wrong arguments.");
                }
            }
        }


        private void UnlockAccount(string address, bool timeout = true)
        {
            var accounts = _keyStore.ListAccounts();

            if (accounts == null || accounts.Count <= 0)
            {
                _screenManager.PrintError("error: the account '" + address + "' does not exist.");
                return;
            }
            
            if (!accounts.Contains(address))
            {
                _screenManager.PrintError("account does not exist!");
                return;
            }
                
            var password = _screenManager.AskInvisible("password: ");
            var tryOpen = _keyStore.OpenAsync(address, password, timeout);
            
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
            var keypair = _keyStore.Create(password);
            if(keypair!=null)
                _screenManager.PrintLine("account successfully created!");
        }

        private void ListAccounts()
        {
            List<string> accnts = _keyStore.ListAccounts();

            for (int i = 0; i < accnts.Count; i++)
            {
                _screenManager.PrintLine("account #" + i + " : " + accnts.ElementAt(i));
            }
            
            if (accnts.Count == 0)
                _screenManager.PrintLine("no accounts available");
        }

        public ECKeyPair GetKeyPair(string addr)
        {
            ECKeyPair kp = _keyStore.GetAccountKeyPair(addr);
            return kp;
        }

        /*public Transaction SignTransaction(JObject t)
        {
            Transaction tr = new Transaction();

            try
            {
                tr.From = ByteArrayHelpers.FromHexString(addr);
                tr.To = Convert.FromBase64String(t["to"].ToString());
                tr.IncrementId = t["incr"].ToObject<ulong>();
                tr.MethodName = t["method"].ToObject<string>();
                var p = Convert.FromBase64String(t["params"].ToString());
                tr.Params = p.Length == 0 ? null : p;

                SignTransaction(tr);

                return tr;
            }
            catch (Exception e)
            {
                ;
            }
            
            return null;
        }*/

        public Transaction SignTransaction(Transaction tx)
        {
            string addr = BitConverter.ToString(tx.From.Value).Replace("-", string.Empty).ToLower();
            
            ECKeyPair kp = _keyStore.GetAccountKeyPair(addr);

            if (kp == null)
                throw new AccountLockedException(addr);
            
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);
    
            byte[] b = ms.ToArray();
            byte[] toSig = SHA256.Create().ComputeHash(b);
                
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(kp, toSig);
                
            // Update the signature
            tx.R = signature.R;
            tx.S = signature.S;
                
            tx.P = kp.PublicKey.Q.GetEncoded();

            return tx;
        }
    }
}