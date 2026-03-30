//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//

namespace ZumoLib;

public class RgbLedRear : ComDevice
{
    private bool enabled;

    public RgbLedRear(ICom com, LedRear ledRear) : base(com, 0x12)
    {
        LedRear = ledRear;
    }

    public LedRear LedRear { get; }

    public bool Enabled
    {
        get => enabled;
        set
        {
            if (value != enabled)
            {
                enabled = value;
                SetRequest($"{(value ? "1" : "0")}{(byte)LedRear:X1}");
            }
        }
    }

    public void SetValue(byte r, byte g, byte b)
    {
        SetRequest($"{(byte)LedRear:X2}{r:X2}{g:X2}{b:X2}");
    }

    public void Toggle()
    {
        Enabled = !Enabled;
    }

    public void Update()
    {
        var message = GetRequest();
        if (message.Length == 6)
        {
            var ledStates = int.Parse(message.Substring(4, 2));
            enabled = (ledStates & (int)LedRear) != 0;
        }
    }
}