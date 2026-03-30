//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//

namespace ZumoLib;

public class RedLedRear : ComDevice
{
    private bool enabled;

    public RedLedRear(ICom com, LedRear ledRear) : base(com, 0x14)
    {
        LedRear = ledRear;
        Update();
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