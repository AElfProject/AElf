namespace AElf.WebApp.Application
{
    public class WebAppOutput<TResult>
    {
        public long Code { get; set; }

        public string Message { get; set; } = "Success";
        
        public TResult Result { get; set; }
    }
}