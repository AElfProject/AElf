namespace AElf.Kernel.SmartContract.Application
{
    public abstract class SmartContractAcsPluginBase
    {
        private readonly string _acsSymbol;

        protected SmartContractAcsPluginBase(string acsSymbol)
        {
            _acsSymbol = acsSymbol;
        }

        protected string GetAcsSymbol()
        {
            return _acsSymbol;
        }
    }
}