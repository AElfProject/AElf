namespace AElf.CrossChain
{
    public class CrossChainRequestException : System.Exception
    {
        public CrossChainRequestException(string message) : base(message)
        {
        }

        public CrossChainRequestException(string message, System.Exception innerException) : base(message,
            innerException)
        {
        }
    }
}