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
        TurnLeftAndWait(false);

        DriveUntilGreenGround();

        Console.WriteLine("Green ground reached");
        PlayGroundReachedEffect();
        TurnRightAndWait(false);

        Console.WriteLine("Driving out of labyrinth");
        DriveAndWait(200, 50, 25);
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
                TurnRightAndWait(true);
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
                TurnLeftAndWait(true);
            }
        }
    }

    private void DriveOutOfStart()
    {
        Zumo.Instance.Drive.DriveTurnCalib(138);
        DriveAndWait(200, 50, 25);
        TurnRightAndWait(true);
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
            >= 60 and <= 140 => Color.Green,
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
                DriveAndWait(50, 50, 25);
                stopReason = StopReason.RedGround;
                break;
            }

            if (groundColor == Color.Green)
            {
                DriveAndWait(50, 50, 25);
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

    private void TurnAndWait(int angle, int velocity, int acceleration, bool wallLeft, bool calibrate)
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
                if (calibrate)
                {
                    var distance = 0;
                    if (wallLeft)
                    {
                        distance = Zumo.Instance.Lidar[260].Distance - Zumo.Instance.Lidar[280].Distance;
                        Console.WriteLine($"Wall left detected, distance: {distance}");
                    }
                    else
                    {
                        distance = Zumo.Instance.Lidar[80].Distance - Zumo.Instance.Lidar[100].Distance;
                        Console.WriteLine($"Wall right detected, distance: {distance}");
                    }

                    if (distance > 1)
                        TurnAndWait(1, 50, 50, wallLeft, true);
                    else if (distance < -1)
                        TurnAndWait(-1, 50, 50, wallLeft, true);
                }
            }
            finally
            {
                Zumo.Instance.Drive.DriveFinished -= OnDriveFinished;
            }
        }
    }

    private void TurnAndWait(int angle, bool wallLeft, bool calibrate)
    {
        TurnAndWait(angle, 50, 25, wallLeft, calibrate);
    }

    private void TurnLeftAndWait(bool calibrate)
    {
        TurnAndWait(-90, 50, 25, false, calibrate);
    }

    private void TurnRightAndWait(bool calibrate)
    {
        TurnAndWait(90, 50, 25, true, calibrate);
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