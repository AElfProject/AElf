namespace AElf.WebApp.Application.Chain.Dto
{
    public class LogEventDto
    {
        public string Address { get; set; }
        
        public string Name { get; set; }
        
        public string[] Indexed { get; set; }
        
        public string NonIndexed { get; set; }
    }
}