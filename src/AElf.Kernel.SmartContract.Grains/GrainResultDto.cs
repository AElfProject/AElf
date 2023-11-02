namespace AElf.Kernel.SmartContract.Grains;

public class GrainResultDto<T> : GrainResultDto
{
    public T Data { get; set; }
}

public class GrainResultDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}