using System.Dynamic;

namespace AElf.Runtime.CSharp.Validators
{
    public abstract class ValidationResult
    {
        public string Subject { get; }

        public string Message { get; }

        protected ValidationResult(string message)
        {
            Message = message;
        }

        protected ValidationResult(string subject, string message) : this(message)
        {
            Subject = subject;
        }

        public override string ToString()
        {
            return $"[{GetType().Name}] {Message}";
        }
    }
}