namespace DataTransferContracts;

public class ConfidenceInterval
{
    public int N { get; set; }
    public double Mean { get; set; }
    public double StandardError { get; set; }
    public int Level { get; set; }
    public double Margin { get; set; }
    public double Lower { get; set; }
    public double Upper { get; set; }
}
