namespace DataTransferContracts;

public class Memory
{
    public int Gen0Collections { get; set; }

    public int Gen1Collections { get; set; }

    public int Gen2Collections { get; set; }

    public long TotalOperations { get; set; }

    public long BytesAllocatedPerOperation { get; set; }
}
