namespace AElf.Kernel.Token.Infrastructure
{
    /// <summary>
    /// Primary token symbol is the token symbol used in validating tx sender's balance.
    /// </summary>
    public interface IPrimaryTokenSymbolProvider
    {
        void SetPrimaryTokenSymbol(string symbol);
        string GetPrimaryTokenSymbol();
    }

    public class PrimaryTokenSymbolProvider : IPrimaryTokenSymbolProvider
    {
        private string _primaryTokenSymbol;

        public void SetPrimaryTokenSymbol(string symbol)
        {
            _primaryTokenSymbol = symbol;
        }

        public string GetPrimaryTokenSymbol()
        {
            return _primaryTokenSymbol;
        }
    }
}