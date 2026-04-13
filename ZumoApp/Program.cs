//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//

using ZumoLib;

namespace ZumoApp;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Zumo starting...");
        while (true)
        {
            Console.WriteLine("Choose Program to use: ");
            Console.WriteLine("1 - Testing/Configuration");
            Console.WriteLine("2 - Testat 1");
            Console.WriteLine();
            Console.WriteLine("(Please calibrate color and drive turn using program 1 before starting program 2)");

            var key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.D1:
                    TestingProgram();
                    break;
                case ConsoleKey.D2:
                    Testat1.Start();
                    break;
                default:
                    Console.WriteLine("Stopping Program");
                    Zumo.Instance.Drive.Stop();
                    Zumo.Instance.Lidar.SetPower(false);
                    return;
            }
        }
    }

    private static void TestingProgram()
    {
        Zumo.Instance.Cm4Button.ButtonChanged += ButtonChanged;
        Zumo.Instance.Cm4Led.LedStateChanged += LedChanged;


        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("F1     Track +1000 mm");
            Console.WriteLine("F2     Track -1000 mm");
            Console.WriteLine("F3     Turn +90°");
            Console.WriteLine("F4     Turn -90°");
            Console.WriteLine("F5     Lidar On");
            Console.WriteLine("F6     Lidar Off");
            Console.WriteLine("F8     Ping Zumo");
            Console.WriteLine("F9     Toggle Led");
            Console.WriteLine("F10    Calibrate Black");
            Console.WriteLine("F12    Calibrate White and write calibration");
            Console.WriteLine("C      Read Color with Color Sensor");
            Console.WriteLine("D      Drive Forward");
            Console.WriteLine("B      Drive Backword");
            Console.WriteLine("S      Stop Driving");
            Console.WriteLine("P(0-9) Play Sound");
            Console.WriteLine("T(...) Calibrate Drive Turn");
            Console.WriteLine("Esc    Exit");
            var redir = Console.IsInputRedirected;
            var key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.F1:
                    Zumo.Instance.Drive.DriveTrack(500, 100, 100);
                    break;

                case ConsoleKey.F2:
                    Zumo.Instance.Drive.DriveTrack(-500, 100, 100);
                    break;

                case ConsoleKey.F3:
                    Zumo.Instance.Drive.DriveTurn(90, 100, 100);
                    break;

                case ConsoleKey.F4:
                    Zumo.Instance.Drive.DriveTurn(-90, 100, 100);
                    break;

                case ConsoleKey.F5:
                    Zumo.Instance.Lidar.SetPower(true);
                    while (!Console.KeyAvailable)
                    {
                        var p = Zumo.Instance.Lidar[45];
                        Console.SetCursorPosition(0, 0);
                        Console.WriteLine(
                            $"Speed {Zumo.Instance.Lidar.Speed} °/sec \tDistance: {p.Distance / 1000f} m    ");
                        Thread.Sleep(200);
                    }

                    break;
                case ConsoleKey.F6:
                    Zumo.Instance.Lidar.SetPower(false);
                    break;

                case ConsoleKey.F8:
                    var result = Zumo.Instance.Ping.DoPing();
                    Console.WriteLine("Ping " + (result ? "OK" : "timeout"));
                    break;

                case ConsoleKey.F9:
                    var random = new Random();
                    Zumo.Instance.RgbLedRearLeft.SetValue((byte)random.Next(255), (byte)random.Next(255),
                        (byte)random.Next(255));
                    Zumo.Instance.RgbLedRearLeft.Toggle();
                    Zumo.Instance.RgbLedRearRight.SetValue((byte)random.Next(255), (byte)random.Next(255),
                        (byte)random.Next(255));
                    Zumo.Instance.RgbLedRearRight.Toggle();
                    Zumo.Instance.RgbLedFront.SetValue((byte)random.Next(255), (byte)random.Next(255),
                        (byte)random.Next(255));
                    Zumo.Instance.RgbLedFront.Toggle();
                    Zumo.Instance.Cm4Led.Toggle();
                    break;

                case ConsoleKey.F10:
                    Zumo.Instance.ColorSensor.CalibrateBlack();
                    break;

                case ConsoleKey.F12:
                    Zumo.Instance.ColorSensor.CalibrateWhite();
                    break;

                case ConsoleKey.C:
                    Console.WriteLine($"Color: {Zumo.Instance.ColorSensor.Read()}°");
                    break;

                case ConsoleKey.D:
                    Zumo.Instance.Drive.DriveConstant(100, 100);
                    break;

                case ConsoleKey.B:
                    Zumo.Instance.Drive.DriveConstant(-100, -100);
                    break;

                case ConsoleKey.S:
                    Zumo.Instance.Drive.Stop();
                    break;

                case ConsoleKey.P:
                    if (int.TryParse(Console.ReadLine(), out var si) && si >= 0 && si <= 8)
                        Zumo.Instance.Sound.Play((SoundItem)si);
                    break;

                case ConsoleKey.T:
                    if (int.TryParse(Console.ReadLine(), out var cal) && cal >= 0 && cal <= 200)
                        Zumo.Instance.Drive.DriveTurnCalib(cal);
                    break;

                case ConsoleKey.Escape:
                    Zumo.Instance.Lidar.SetPower(false);
                    return;
            }
        }
    }

    private static void ButtonChanged(object? sender, ButtonStateChangedEventArgs args)
    {
        Console.WriteLine("Button State: " + args.Pressed);
    }

    private static void LedChanged(object? sender, LedStateChangedEventArgs args)
    {
        Console.WriteLine("Led State: " + args.Enabled);
    }
}