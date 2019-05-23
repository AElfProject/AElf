using AElf.Sdk.CSharp;

namespace AElf.Contracts.TokenConverter
{
    public class InvalidValueException : BaseAElfException
    {
        public InvalidValueException(string message) : base(message)
        {
        }
    }
}