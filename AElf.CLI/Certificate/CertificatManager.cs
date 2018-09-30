using System;
using System.IO;
using System.Linq;
using AElf.CLI.Command;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.Common.Application;
using AElf.Cryptography.Certificate;

namespace AElf.CLI.Certificate
{
    public class CertificatManager
    {
        private readonly CertificateStore _certificateStore =
            new CertificateStore(ApplicationHelpers.GetDefaultDataDir());

        private const string GenCmd = "gen";
        private ScreenManager _screenManager;

        public CertificatManager(ScreenManager screenManager)
        {
            _screenManager = screenManager;
        }

        public void ProcCmd(CmdParseResult parsedCmd)
        {
            string validationError = Validate(parsedCmd);

            if (validationError != null)
            {
                _screenManager.PrintError(validationError);
                return;
            }

            string subCommand = parsedCmd.Args.ElementAt(0);
            if (subCommand.Equals(GenCmd, StringComparison.OrdinalIgnoreCase))
            {
                GenerateCertificate(parsedCmd.Args.ElementAt(1), parsedCmd.Args.ElementAt(2));
            }
        }
        private void GenerateCertificate(string chainId, string ip)
        {
            var keyPair = _certificateStore.WriteKeyAndCertificate(chainId, ip);
            var certPath = Path.Combine(ApplicationHelpers.GetDefaultDataDir(), _certificateStore.FolderName,
                chainId + _certificateStore.CertExtension);
            if (File.Exists(certPath))
                _screenManager.PrintLine("[Certificate] " + certPath);
            var keyPath = Path.Combine(ApplicationHelpers.GetDefaultDataDir(), _certificateStore.FolderName,
                chainId + _certificateStore.KeyExtension);
            if (File.Exists(keyPath))
                _screenManager.PrintLine("[Key] " + keyPath);
        }

        private string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args.Count == 0)
                return CliCommandDefinition.InvalidParamsError;

            if (parsedCmd.Args.ElementAt(0).Equals(GenCmd, StringComparison.OrdinalIgnoreCase))
            {
                if (parsedCmd.Args.Count < 3)
                    return CliCommandDefinition.InvalidParamsError;
            }
            
            return null;
        }
    }
}