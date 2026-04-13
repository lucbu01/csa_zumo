using ZumoLib;

namespace ZumoApp;

public class Testat1
{
    public CancellationTokenSource Cts { get; } = new();

    public static void Start()
    {
        Console.WriteLine("Starting Testat1");
        Console.WriteLine("Press S to stop");

        var testat1 = new Testat1();
        var programTask = Task.Run(testat1.RunTestat1, testat1.Cts.Token);

        while (!testat1.Cts.Token.IsCancellationRequested && !programTask.IsCompleted)
            if (Console.KeyAvailable)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.S)
                {
                    Console.WriteLine("\nAbbruch angefordert. Bitte warten...");
                    testat1.Cts.Cancel();
                }
            }
            else
            {
                Thread.Sleep(50);
            }

        try
        {
            programTask.Wait();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Any(e =>
                                                e is TaskCanceledException || e is OperationCanceledException))
        {
            Console.WriteLine("Der Task wurde erfolgreich abgebrochen.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ein Fehler ist aufgetreten: {ex.Message}");
        }
        finally
        {
            TearDown();
        }
    }

    private void RunTestat1()
    {
        SetUp();
        DriveOutOfStart();
        Console.WriteLine("Driven out of start");

        DriveUntilRedGround();

        Console.WriteLine("Red ground reached");
        PlayGroundReachedEffect();
        TurnLeftAndWait();

        DriveUntilGreenGround();

        Console.WriteLine("Green ground reached");
        PlayGroundReachedEffect();
        TurnRightAndWait();

        Console.WriteLine("Driving out of labyrinth");
        if (!Cts.Token.IsCancellationRequested) Zumo.Instance.Drive.DriveTrack(400, 100, 100);
    }

    private static void TearDown()
    {
        Zumo.Instance.Drive.Stop();
        Zumo.Instance.Lidar.SetPower(false);
    }

    private void PlayGroundReachedEffect()
    {
        Zumo.Instance.Sound.Play(SoundItem.Wasted);
    }

    private void DriveUntilRedGround()
    {
        var stopReason = StopReason.None;
        while (stopReason != StopReason.RedGround && !Cts.Token.IsCancellationRequested)
        {
            stopReason = DriveStraightUntilWallOrGround();
            Console.WriteLine("Stop reason: " + stopReason);
            if (stopReason == StopReason.Wall)
            {
                Console.WriteLine("Wall detected, Turning Right");
                TurnRightAndWait();
            }
        }
    }

    private void DriveUntilGreenGround()
    {
        var stopReason = StopReason.None;
        while (stopReason != StopReason.GreenGround && !Cts.Token.IsCancellationRequested)
        {
            stopReason = DriveStraightUntilWallOrGround();
            Console.WriteLine("Stop reason: " + stopReason);
            if (stopReason == StopReason.Wall)
            {
                Console.WriteLine("Wall detected, Turning Left");
                TurnLeftAndWait();
            }
        }
    }

    private void DriveOutOfStart()
    {
        DriveAndWait(200, 100, 100);
        TurnRightAndWait();
    }

    private void SetUp()
    {
        // TODO LED's blinken lassen z.B.
        Zumo.Instance.Lidar.SetPower(true);
    }

    private Color CheckGroundColor()
    {
        var groundColor = Zumo.Instance.ColorSensor.Read();
        Console.WriteLine("Ground Color: " + groundColor);
        return groundColor switch
        {
            <= 20 or >= 340 and <= 360 => Color.Red,
            >= 80 and <= 140 => Color.Green,
            _ => Color.Irrelevant
        };
    }


    private StopReason DriveStraightUntilWallOrGround()
    {
        var stopReason = StopReason.None;

        Zumo.Instance.Drive.DriveConstant(50, 50);

        while (!Cts.Token.IsCancellationRequested)
        {
            if (Zumo.Instance.Lidar[0].Distance <= 100)
            {
                stopReason = StopReason.Wall;
                break;
            }

            var groundColor = CheckGroundColor();
            if (groundColor == Color.Red)
            {
                stopReason = StopReason.RedGround;
                break;
            }

            if (groundColor == Color.Green)
            {
                stopReason = StopReason.GreenGround;
                break;
            }

            Thread.Sleep(50);
        }

        Zumo.Instance.Drive.Stop();

        return stopReason;
    }

    private void DriveAndWait(int distance, int velocity, int acceleration)
    {
        if (!Cts.Token.IsCancellationRequested)
        {
            var driveFinished = new ManualResetEventSlim(false);

            void OnDriveFinished(object? sender, EventArgs e)
            {
                driveFinished.Set();
            }

            Zumo.Instance.Drive.DriveFinished += OnDriveFinished;
            try
            {
                Zumo.Instance.Drive.DriveTrack(distance, velocity, acceleration);
                driveFinished.Wait(Cts.Token);
                Zumo.Instance.Drive.DriveFinished -= OnDriveFinished;
            }
            finally
            {
                Zumo.Instance.Drive.DriveFinished -= OnDriveFinished;
            }
        }
    }

    private void TurnAndWait(int angle, int velocity, int acceleration, bool wallLeft)
    {
        if (!Cts.Token.IsCancellationRequested)
        {
            var driveFinished = new ManualResetEventSlim(false);

            void OnDriveFinished(object? sender, EventArgs e)
            {
                driveFinished.Set();
            }

            Zumo.Instance.Drive.DriveFinished += OnDriveFinished;
            try
            {
                Zumo.Instance.Drive.DriveTurn(angle, velocity, acceleration);
                driveFinished.Wait(Cts.Token);
                var distance = 0;
                if (wallLeft)
                {
                    if (Zumo.Instance.Lidar[265].Distance < 20 && Zumo.Instance.Lidar[275].Distance < 20)
                    {
                        Console.WriteLine(
                            $"Wall left detected, distance: {Zumo.Instance.Lidar[275].Distance - Zumo.Instance.Lidar[265].Distance}");
                        distance = Zumo.Instance.Lidar[275].Distance - Zumo.Instance.Lidar[265].Distance;
                    }
                }
                else if (Zumo.Instance.Lidar[85].Distance < 20 && Zumo.Instance.Lidar[95].Distance < 20)
                {
                    Console.WriteLine(
                        $"Wall right detected, distance: {Zumo.Instance.Lidar[85].Distance - Zumo.Instance.Lidar[95].Distance}");
                    distance = Zumo.Instance.Lidar[85].Distance - Zumo.Instance.Lidar[95].Distance;
                }

                if (distance > 1)
                    TurnAndWait(1, wallLeft);
                else if (distance < -1)
                    TurnAndWait(-1, wallLeft);
            }
            finally
            {
                Zumo.Instance.Drive.DriveFinished -= OnDriveFinished;
            }
        }
    }

    private void TurnAndWait(int angle, bool wallLeft)
    {
        TurnAndWait(angle, 100, 100, wallLeft);
    }

    private void TurnLeftAndWait()
    {
        TurnAndWait(-90, 100, 100, false);
    }

    private void TurnRightAndWait()
    {
        TurnAndWait(90, 100, 100, true);
    }
}

internal enum StopReason
{
    RedGround,
    GreenGround,
    Wall,
    None
}

internal enum Color
{
    Red,
    Green,
    Irrelevant
}