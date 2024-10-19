using System;
using System.IO;
using System.Threading.Tasks;
using AElf.Cryptography.Exceptions;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Nethereum.KeyStore.Crypto;

namespace AElf.OS.Account.Infrastructure;

public partial class AElfKeyStore
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileUnlockingAccount(Exception ex)
    {
        if (ex is InvalidPasswordException)
        {
            Logger.LogError(ex, "Invalid password: ");
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
                ReturnValue = AccountError.WrongPassword
            };
        }

        if (ex is KeyStoreNotFoundException)
        {
            Logger.LogError(ex, "Could not load account:");
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
                ReturnValue = AccountError.AccountFileNotFound
            };
        }

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = AccountError.None
        };
    }

    protected virtual async Task<FlowBehavior> HandleExceptionWhileReadingKeyPair(Exception ex)
    {
        if (ex is FileNotFoundException)
        {
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
                ReturnValue = new KeyStoreNotFoundException("Keystore file not found.", ex)
            };
        }

        if (ex is DirectoryNotFoundException)
        {
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
                ReturnValue = new KeyStoreNotFoundException("Invalid keystore path.", ex)
            };
        }

        if (ex is DecryptionException)
        {
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
                ReturnValue = new InvalidPasswordException("Invalid password.", ex)
            };
        }

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
}