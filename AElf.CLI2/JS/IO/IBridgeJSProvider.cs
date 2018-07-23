using System.IO;

namespace AElf.CLI2.JS.IO
{
    public interface IBridgeJSProvider
    {
        Stream GetBridgeJSStream();
    }
}