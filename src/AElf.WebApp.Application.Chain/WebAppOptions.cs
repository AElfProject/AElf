namespace AElf.WebApp.Application.Chain
{
    public class WebAppOptions
    {
        public int TransactionResultStatusCacheSeconds { get; set; } = 180;

        public bool IsDebugMode { get; set; }
    }
}