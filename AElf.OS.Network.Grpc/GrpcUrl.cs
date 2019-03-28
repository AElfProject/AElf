namespace AElf.OS.Network.Grpc
{
    public class GrpcUrl
    {
        public string IpAddress { get; set; }
        public string IpVersion { get; set; }

        public int Port { get; set; }

        //TODO: Add Parse test case  [Case]
        public static GrpcUrl Parse(string address)
        {
            var splitRes = address.Split(':');

            if (splitRes.Length != 3)
                return null;

            var url = new GrpcUrl();
            url.IpVersion = splitRes[0];
            url.IpAddress = splitRes[1];
            url.Port = int.Parse(splitRes[2]);

            return url;
        }

        public string ToIpPortFormat()
        {
            return IpAddress + ":" + Port;
        }
    }
}