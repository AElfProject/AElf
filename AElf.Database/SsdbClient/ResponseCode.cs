namespace AElf.Database.SsdbClient
{
    public static class ResponseCode
    {
        public const string Ok = "ok";
        
        public const string NotFound = "not_found";
        
        public const string Error = "error";
        
        public const string Fail = "fail";
        
        public const string ClientError = "client_error";

        public static readonly byte[] OkByte;

        public static readonly byte[] NotFoundByte;

        static ResponseCode()
        {
            OkByte = Helper.StringToBytes(Ok);
            NotFoundByte = Helper.StringToBytes(NotFound);
        }
    }
}