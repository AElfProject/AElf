namespace AElf.CSharp.CodeOps.Validators.Whitelist.SampleContracts;

public class SampleContract
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}

public class InvalidDependencyContract
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public void InvalidMethod()
    {
        // This method uses a non-existent namespace and should cause a validation error
        SampleInvalidClass obj = new SampleInvalidClass();
    }
}

public class NonWhitelistedTypeContract
{
    public void NonWhitelistedTypeMethod()
    {
        // This method uses a non-whitelisted type and should cause a validation error
        FileStream fs = new FileStream("file.txt", FileMode.Open);
    }
}

public class NonWhitelistedMemberContract
{
    public void NonWhitelistedMemberMethod()
    {
// This method uses a non-whitelisted member and should cause a validation error
        List<int> list = new List<int>();
        list.Capacity = 10;
    }
}

public class SampleInvalidClass
{
    public void InvalidMethod()
    {
        // This method uses a non-whitelisted type and should cause a validation error
    }
}