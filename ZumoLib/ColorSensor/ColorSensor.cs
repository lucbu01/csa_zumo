using System.Globalization;

namespace ZumoLib;

public class ColorSensor : ComDevice
{
    public ColorSensor(ICom com) : base(com, 0x31)
    {
    }

    public int Read()
    {
        return int.Parse(GetRequest("0"), NumberStyles.HexNumber);
    }

    public void CalibrateBlack()
    {
        SetRequest("600");
    }

    public void CalibrateWhite()
    {
        SetRequest("601");
    }
}