namespace AElf.WebApp.Application.Chain.Dto
{
    public class NotLinkedBlockDto
    {
        public string BlockHash { get; set; }
        
        public long Height { get; set; }
        
        public string PreviousBlockHash { get; set; }
    }
}