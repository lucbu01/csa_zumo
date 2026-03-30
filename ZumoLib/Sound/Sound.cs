namespace ZumoLib;

public class Sound : ComDevice
{
    public Sound(ICom com) : base(com, 0x50)
    {
    }

    public void Play(SoundItem item)
    {
        SetRequest($"1{(int)item}");
    }

    public void Play(int frequency, int duration)
    {
        SetRequest($"0{frequency:X4}{duration:X4}");
    }
}