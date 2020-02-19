namespace AElf.Kernel.SmartContract.Application
{
    //TODO: why in a SmartContract package, you put a Token class? if you create a door, can put a clock on the door, 
    //And tell people, "I don't care about whether you need a clock or not, but I MADE the clock inside the door?"
    public class AvailableTokenInfoInCache
    {
        public string TokenSymbol { get; set; }
        public int BaseTokenWeight { get; set; }
        public int AddedTokenWeight { get; set; }
    }
}