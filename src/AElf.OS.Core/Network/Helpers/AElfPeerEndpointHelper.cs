using System.Linq;
using System.Net;
using AElf.OS.Network.Types;

namespace AElf.OS.Network.Helpers;

public static class AElfPeerEndpointHelper
{
    public static bool TryParse(string endpointString, out DnsEndPoint endpoint,
        int defaultPort = NetworkConstants.DefaultPeerPort)
    {
        endpoint = null;

        if (string.IsNullOrEmpty(endpointString) || endpointString.Trim().Length == 0)
            return false;

        if (defaultPort != -1 && (defaultPort < IPEndPoint.MinPort || defaultPort > IPEndPoint.MaxPort))
            return false;

        var values = endpointString.Split(new[] { ':' });
        string host;
        var port = -1;

        if (values.Length <= 2)
        {
            // ipv4 or hostname
            host = values[0];

            if (values.Length == 1)
            {
                port = defaultPort;
            }
            else
            {
                var parsedPort = GetPort(values[1]);

                if (parsedPort == 0)
                    return false;

                port = parsedPort;
            }
        }
        else
        {
            //ipv6
            //could be [a:b:c]:d
            if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
            {
                host = string.Join(":", values.Take(values.Length - 1).ToArray());
                var parsedPort = GetPort(values[values.Length - 1]);

                if (parsedPort == 0)
                    return false;

                port = parsedPort;
            }
            else // [a:b:c] or a:b:c
            {
                host = endpointString;
                port = defaultPort;
            }
        }

        if (port == -1)
            return false;

        // we leave semantic analysis of the ip/hostname to lower levels.
        endpoint = new AElfPeerEndpoint(host, port);

        return true;
    }

    public static int GetEndpointPort(string endpointString)
    {
        var values = endpointString.Split(new[] { ':' });
        if (values.Length == 1)
        {
            return -1;
        }

        if (!int.TryParse(values[values.Length - 1], out var port))
        {
            return -1;
        }

        return port;
    }

    private static int GetPort(string p)
    {
        if (!int.TryParse(p, out var port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            return 0;

        return port;
    }
}