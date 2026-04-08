using System.Globalization;

namespace ZumoLib;

public class Drive : ComDevice
{
    public event EventHandler DriveFinished;
    
    public Drive(ICom com) : base(com, 0x24)
    {
        
    }


    public void DriveTrack(int distance, int velocity, int acceleration)
    {
        SetRequest($"2{ToStr(distance)}{ToStr(velocity)}{ToStr(acceleration)}");
    }

    public void DriveTurn(int angle, int velocity, int acceleration)
    {
        SetRequest($"A{ToStr(angle)}{ToStr(velocity)}{ToStr(acceleration)}");
    }

    public void DriveTurn(int angle, int radius, int velocity, int acceleration)
    {
        SetRequest($"9{ToStr(angle)}{ToStr(radius)}{ToStr(velocity)}{ToStr(acceleration)}");
    }

    public void DriveConstant(int leftVelocity, int rightVelocity)
    {
        SetRequest($"1{ToStr(leftVelocity)}{ToStr(rightVelocity)}");
    }

    public void Stop()
    {
        DriveConstant(0, 0);
    }

    private string ToStr(int value)
    {
        var str = value.ToString("X4");
        while (str.Length < 4)
            str = "0" + str;
        if (str.Length > 4) str = str.Substring(str.Length - 4);

        return str;
    }
    
    public int DriveGetRemainingDistance()
    {
        var msg = GetRequest("2");
        var dist = int.Parse(msg[4..], NumberStyles.HexNumber);
        return dist;
    }

    
    public bool DriveIsRunning()
    {
        var msg = GetRequest("7");
        var running = byte.Parse(msg[4..], NumberStyles.HexNumber) == 1;
        return running;
    }

    protected override bool ProcessEvent(string message)
    {
        if (message == "5!24FF")
        {
            DriveFinished?.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }
}