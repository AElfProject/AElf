using System.IO;
using System.Reflection;

namespace AElf.CLI2.JS.IO
{
    public class BridgeJSProvider : IBridgeJSProvider
    {
        public const string BridgeJSResourceName = "AElf.CLI2.Scripts.bridge.js";
        public Stream GetBridgeJSStream()
        {
            return Assembly.GetEntryAssembly().GetManifestResourceStream(BridgeJSResourceName);
        }
    }
}