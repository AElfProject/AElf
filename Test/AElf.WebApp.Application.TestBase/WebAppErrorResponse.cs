namespace AElf.WebApp.Application
{
    public class WebAppErrorResponse
    {
        public WebAppResponseError Error { get; set; }
    }
    
    public class WebAppResponseError
    {
        public string Code { get; set; }
            
        public string Message { get; set; }
            
        public string Details { get; set; }
            
        public string ValidationErrors { get; set; }
    }
}