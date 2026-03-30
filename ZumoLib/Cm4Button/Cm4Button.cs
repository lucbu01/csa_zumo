//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//

using System.Device.Gpio;

namespace ZumoLib.OldButton;

public class Cm4Button : IButton
{
    internal Cm4Button(GpioController gpio, int pin)
    {
        Pin = pin;
        Gpio = gpio;

        gpio.OpenPin(pin, PinMode.Input);

        var t = new Thread(Run);
        t.IsBackground = true;
        t.Start();
    }

    internal GpioController Gpio { get; }
    public int Pin { get; }
    public event EventHandler<ButtonStateChangedEventArgs>? ButtonChanged;

    public bool Pressed => Gpio.Read(Pin) == PinValue.Low; // ToDo: Read GPIO Pin State

    private void Run()
    {
        var lastValue = false;
        while (true)
        {
            var currentValue = Pressed;
            if (currentValue != lastValue)
            {
                lastValue = currentValue;
                ButtonChanged?.Invoke(this, new ButtonStateChangedEventArgs(currentValue));
            }

            Thread.Sleep(50);
        }
    }
}