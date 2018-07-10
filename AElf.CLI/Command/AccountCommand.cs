using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using AElf.Cryptography;

namespace AElf.CLI.Command
{
    public class AccountCommand : ComposedCommand
    {
        public AccountCommand()
        {
            SubCommands = new Dictionary<string, ICommand>
            {
                ["list"] = new ListAccountCommand(),
                ["new"] = new NewAccountCommand(),
                ["unlock"] = new UnlockAccountCommand()
            };
            CurrentCommandName = "account";
        }

        public const string MsgAccountCreated = "account successfully created!";

        #region ListAccount

        private class ListAccountCommand : ICommand
        {
            public string Process(IEnumerable<string> args, AElfClientProgramContext context)
            {
                if (args.Count() != 0)
                {
                    throw new CommandException("account list does not need to take any params");
                }

                var accounts = context.KeyStore.ListAccounts();
                if (accounts.Count != 0)
                {
                    return string.Join("\n",
                        accounts.Zip(Enumerable.Range(0, accounts.Count),
                            (account, id) => $"account #{id} : {account}"));
                }
                else
                {
                    return "no accounts available";
                }
            }

            public string Usage { get; } = "account list";
        }

        #endregion

        #region NewAccount

        private class NewAccountCommand : ICommand
        {
            public string Process(IEnumerable<string> args, AElfClientProgramContext context)
            {
                string pwd;
                var argc = args.Count();
                switch (argc)
                {
                    case 0:
                        pwd = context.ScreenManager.AskInvisible("Password:");
                        break;
                    case 1:
                        pwd = args.First();
                        break;
                    default:
                        throw new InvalidNumberArgumentsException();
                }

                var pair = context.KeyStore.Create(pwd);
                if (pair != null)
                {
                    return MsgAccountCreated;
                }
                else
                {
                    throw new CommandException("Cannot create new account");
                }
            }

            public string Usage { get; } = "account new [<password>]";
        }

        #endregion

        #region UnlockAccount

        private class UnlockAccountCommand : ICommand
        {
            public string Process(IEnumerable<string> args, AElfClientProgramContext context)
            {
                var argc = args.Count();
                switch (argc)
                {
                    case 1:
                        return Process(args.First(), context);
                    case 2:
                        return Process(args.First(), context, false);
                    default:
                        throw new InvalidNumberArgumentsException();
                }
            }

            private static string Process(string username, AElfClientProgramContext context, bool timeout = true)
            {
                var accounts = context.KeyStore.ListAccounts();

                if (!(accounts?.Contains(username) ?? false))
                {
                    throw new CommandException($"the account '{username}' does not exist.");
                }

                var password = context.ScreenManager.AskInvisible("password: ");
                var tryOpen = context.KeyStore.OpenAsync(username, password, timeout);

                switch (tryOpen)
                {
                    case AElfKeyStore.Errors.WrongPassword:
                        throw new CommandException("incorrect password!");
                    case AElfKeyStore.Errors.AccountAlreadyUnlocked:
                        throw new CommandException("account already unlocked!");
                    default:
                        return "account successfully unlocked!";
                }
            }

            public string Usage { get; } = "account unlock <address> <timeout>";
        }

        #endregion
    }
}