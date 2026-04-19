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
                    TestingAndConfiguration.Start();
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
}