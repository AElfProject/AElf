using System.Collections.Generic;

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
            
        public List<ValidationError> ValidationErrors { get; set; }
    }
    
    public class ValidationError
    {
        public string Message { get; set; }
            
        public List<string> Members { get; set; }
    }
}