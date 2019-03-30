namespace AElf.WebApp.Application.Chain.Dto
{
    public class ParamsDto
    {
        public string OutValue { get; set; }
        
        public string Signature { get; set; }
        
        public string RoundId { get; set; }
        
        public string PromiseTinyBlocks { get; set; }
        
        public string PreviousInValue { get; set; }
        
        public string ActualMiningTime { get; set; }
        
        public string OrderOfNextRound { get; set; }
    }
}