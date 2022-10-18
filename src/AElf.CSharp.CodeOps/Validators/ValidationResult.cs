namespace AElf.CSharp.CodeOps.Validators;

public abstract class ValidationResult
{
    protected ValidationResult(string message)
    {
        Message = message;
    }

    public string Message { get; }

    public Info Info { get; private set; }

    public ValidationResult WithInfo(string referencingMethod, string nm, string type, string member)
    {
        Info = new Info(referencingMethod, nm, type, member);
        return this;
    }

    public override string ToString()
    {
        return $"[{GetType().Name}] {Message} " + Info;
    }
}

public class Info
{
    public readonly string Member;

    public readonly string Namespace;
    public readonly string ReferencingMethod;

    public readonly string Type;

    public Info(string referencingMethod, string nm, string type, string member)
    {
        Namespace = nm;
        Type = type;
        ReferencingMethod = referencingMethod;
        Member = member;
    }

    public override string ToString()
    {
        return $"{ReferencingMethod} > {Namespace} | {Type} | {Member}";
    }
}