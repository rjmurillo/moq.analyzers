namespace DataTransferContracts;

public class Measurement
{
    public string IterationStage { get; set; }
    public int LaunchIndex { get; set; }
    public int IterationIndex { get; set; }
    public long Operations { get; set; }
    public double Nanoseconds { get; set; }
}