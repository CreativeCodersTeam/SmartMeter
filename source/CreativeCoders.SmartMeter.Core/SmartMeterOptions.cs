namespace CreativeCoders.SmartMeter.Core;

public class SmartMeterOptions
{
    /// <summary>
    /// OS-level device path of the serial port that connects to the smart meter's optical coupler.
    /// The same device is shared by the data producer and the unlock procedure; only one of them
    /// keeps it open at a time.
    /// </summary>
    public string PortName { get; set; } = "/dev/ttyUSB0";

    public decimal SoldEnergyOffset { get; set; } = 23_367_605;

    public decimal PurchasedEnergyOffset { get; set; } = 18_261_046;
}
