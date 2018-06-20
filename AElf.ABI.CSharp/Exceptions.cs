namespace AElf.ABI.CSharp
{
    public class InvalidInputException : System.Exception
    {
        public InvalidInputException(string message) : base(message)
        {
        }
    }
}