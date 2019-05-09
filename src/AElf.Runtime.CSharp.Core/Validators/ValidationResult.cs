using System;
using System.Dynamic;

namespace AElf.Runtime.CSharp.Validators
{
    public abstract class ValidationResult
    {
        public string Message { get; }

        public Info Info;

        protected ValidationResult(string message)
        {
            Message = message;
        }

        public ValidationResult WithInfo(string nm, string type, string method, string member)
        {
            Info = new Info(nm, type, method, member);
            return this;
        }

        public override string ToString()
        {
            return $"[{GetType().Name}] {Message}" + Info;
        }
    }
    
    public class Info
    {
        public readonly string Namespace;

        public readonly string Type;

        public readonly string Method;

        public readonly string Member;

        public Info(string nm, string type, string method, string member)
        {
            Namespace = nm;
            Type = type;
            Method = method;
            Member = member;
        }

        public override string ToString()
        {
            return $"{Namespace} > {Type} > {Method} > {Member}";
        }
    }
}
