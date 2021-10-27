namespace AElf.WebApp.Application.Chain.Dto
{
    public class BlockDto
    {
        public string BlockHash { get; set; }
        
        public BlockHeaderDto Header { get; set; }
        
        public BlockBodyDto Body { get; set; }
        
        public int BlockSize { get; set; }
    }
}