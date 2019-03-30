namespace AElf.WebApp.Application.Chain.Dto
{
    public class LogEventDto
    {
        public string Address { get; set; }
        
        public string[] Topics { get; set; }
        
        public string Data { get; set; }
    }
}