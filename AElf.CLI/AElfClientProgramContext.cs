using AElf.CLI.Screen;
using AElf.Common.Application;
using AElf.Cryptography;

namespace AElf.CLI
{
    public class AElfClientProgramContext
    {
        public AElfClientProgramContext(ScreenManager screenManager) :
            this(screenManager, ApplicationHelpers.GetDefaultDataDir())
        {
        }

        public AElfClientProgramContext(ScreenManager screenManager, string keyStorePath)
        {
            ScreenManager = screenManager;
            KeyStore = new AElfKeyStore(keyStorePath);
        }

        public AElfKeyStore KeyStore { get; }

        public ScreenManager ScreenManager { get; }
    }
}