using ZumoLib;

namespace ZumoApp;

public class Testat1
{
    public static void Start()
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
        Zumo.Instance.Drive.DriveTrack(400, 100, 100);

        
        TearDown();
    }

    private static void TearDown()
    {
        Zumo.Instance.Lidar.SetPower(false);
    }

    private static void PlayGroundReachedEffect()
    {
        Zumo.Instance.Sound.Play(SoundItem.Wasted);
    }

    private static void DriveUntilRedGround()
    {
        var stopReason = StopReason.None;
        while (stopReason != StopReason.RedGround)
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
    
    private static void DriveUntilGreenGround()
    {
        var stopReason = StopReason.None;
        while (stopReason != StopReason.GreenGround)
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

    private static void DriveOutOfStart()
    {
        DriveAndWait(200, 100, 100);
        TurnRightAndWait();
    }

    private static void SetUp()
    {
        // TODO LED's blinken lassen z.B.
        Zumo.Instance.Lidar.SetPower(true);
    }

    private static Color CheckGroundColor()
    {
        var groundColor = Zumo.Instance.ColorSensor.Read();
        Console.WriteLine("Ground Color: " + groundColor);
        return groundColor switch
        {
            <= 20 or >= 340 and <= 360 => Color.Red,
            >= 180 and <= 260 => Color.Green, // TODO LOOKING FOR BLUE NOT GREEN RIGHT NOW BECAUSE OF TESTING ON WOOD FLOOR
            // GREEN WOULD BE 120~~ 
            _ => Color.Irrelevant
        };
    }


    private static StopReason DriveStraightUntilWallOrGround()
    {
        var stopReason = StopReason.None;
        var cts = new CancellationTokenSource();

        Zumo.Instance.Drive.DriveConstant(50, 50);

        var monitorTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Zumo.Instance.Lidar[0].Distance <= 200)
                {
                    stopReason = StopReason.Wall;
                    break;
                }

                var groundColor = CheckGroundColor();
                if (groundColor == Color.Red)  { stopReason = StopReason.RedGround; break; }
                if (groundColor == Color.Green) { stopReason = StopReason.GreenGround; break; }

                await Task.Delay(50, cts.Token);
            }
        }, cts.Token);

        monitorTask.Wait();
        Zumo.Instance.Drive.Stop();

        return stopReason;
    }
    
    private static void DriveAndWait(int distance, int velocity, int acceleration)
    {
        var driveFinished = new ManualResetEventSlim(false);

        void OnDriveFinished(object? sender, EventArgs e) => driveFinished.Set();

        Zumo.Instance.Drive.DriveFinished += OnDriveFinished;
        Zumo.Instance.Drive.DriveTrack(distance, velocity, acceleration);
        driveFinished.Wait();
        Zumo.Instance.Drive.DriveFinished -= OnDriveFinished;
    }
    
    private static void TurnAndWait(int angle, int velocity, int acceleration)
    {
        var driveFinished = new ManualResetEventSlim(false);
        void OnDriveFinished(object? sender, EventArgs e) => driveFinished.Set();

        Zumo.Instance.Drive.DriveFinished += OnDriveFinished;
        Zumo.Instance.Drive.DriveTurn(angle, velocity, acceleration);
        driveFinished.Wait();
        Zumo.Instance.Drive.DriveFinished -= OnDriveFinished;
    }
    
    private static void TurnLeftAndWait()  => TurnAndWait(-90, 100, 100);
    private static void TurnRightAndWait() => TurnAndWait(90, 100, 100);
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
    Red, Green, Irrelevant
}