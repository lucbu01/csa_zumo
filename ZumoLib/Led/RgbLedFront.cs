namespace ZumoLib;

public class RgbLedFront : ComDevice
{
    private bool enabled;

    public RgbLedFront(ICom com) : base(com, 0x11)
    {
    }

    public bool Enabled
    {
        get => enabled;
        set
        {
            if (value != enabled)
            {
                enabled = value;
                SetRequest($"{(value ? "1" : "0")}F");
            }
        }
    }

    public void SetValue(byte r, byte g, byte b)
    {
        SetRequest($"FF{r:X2}{g:X2}{b:X2}");
    }

    public void Toggle()
    {
        Enabled = !Enabled;
    }
}