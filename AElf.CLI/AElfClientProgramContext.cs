using AElf.CLI.RPC;
using AElf.CLI.Screen;
using AElf.Common.Application;
using AElf.Cryptography;

namespace AElf.CLI
{
    public class AElfClientProgramContext
    {
        public AElfClientProgramContext(ScreenManager screenManager, IRPCClient rpcClient) :
            this(screenManager, ApplicationHelpers.GetDefaultDataDir(), rpcClient)
        {
        }

        public AElfClientProgramContext(ScreenManager screenManager, string keyStorePath, IRPCClient rpcClient)
        {
            ScreenManager = screenManager;
            RPCClient = rpcClient;
            KeyStore = new AElfKeyStore(keyStorePath);
        }

        public AElfKeyStore KeyStore { get; }

        public ScreenManager ScreenManager { get; }
        
        public IRPCClient RPCClient { get; }
    }
}