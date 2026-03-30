using System.Device.Gpio;
using System.Device.Pwm;
using System.IO.Ports;

namespace ZumoLib;

public class Lidar
{
    public Lidar(GpioController gpio)
    {
        Gpio = gpio;
        Gpio.OpenPin(7, PinMode.Output);
        pwm = PwmChannel.Create(0, 0);

        Points = new LidarPoint[360];
        for (var i = 0; i < Points.Length; i++) Points[i] = new LidarPoint();

        Com = new SerialPort("/dev/ttyAMA4", 230400);
        Com.Open();

        var thread = new Thread(Run);
        thread.IsBackground = true;
        thread.Start();
    }

    #region members

    private readonly byte HEADER_BYTE = 0x54;
    private readonly byte HEADER_BYTE_TWO = 0x2C;

    private readonly byte[] crc8 =
    {
        0x00, 0x4d, 0x9a, 0xd7, 0x79, 0x34, 0xe3, 0xae, 0xf2, 0xbf, 0x68, 0x25, 0x8b, 0xc6, 0x11, 0x5c,
        0xa9, 0xe4, 0x33, 0x7e, 0xd0, 0x9d, 0x4a, 0x07, 0x5b, 0x16, 0xc1, 0x8c, 0x22, 0x6f, 0xb8, 0xf5,
        0x1f, 0x52, 0x85, 0xc8, 0x66, 0x2b, 0xfc, 0xb1, 0xed, 0xa0, 0x77, 0x3a, 0x94, 0xd9, 0x0e, 0x43,
        0xb6, 0xfb, 0x2c, 0x61, 0xcf, 0x82, 0x55, 0x18, 0x44, 0x09, 0xde, 0x93, 0x3d, 0x70, 0xa7, 0xea,
        0x3e, 0x73, 0xa4, 0xe9, 0x47, 0x0a, 0xdd, 0x90, 0xcc, 0x81, 0x56, 0x1b, 0xb5, 0xf8, 0x2f, 0x62,
        0x97, 0xda, 0x0d, 0x40, 0xee, 0xa3, 0x74, 0x39, 0x65, 0x28, 0xff, 0xb2, 0x1c, 0x51, 0x86, 0xcb,
        0x21, 0x6c, 0xbb, 0xf6, 0x58, 0x15, 0xc2, 0x8f, 0xd3, 0x9e, 0x49, 0x04, 0xaa, 0xe7, 0x30, 0x7d,
        0x88, 0xc5, 0x12, 0x5f, 0xf1, 0xbc, 0x6b, 0x26, 0x7a, 0x37, 0xe0, 0xad, 0x03, 0x4e, 0x99, 0xd4,
        0x7c, 0x31, 0xe6, 0xab, 0x05, 0x48, 0x9f, 0xd2, 0x8e, 0xc3, 0x14, 0x59, 0xf7, 0xba, 0x6d, 0x20,
        0xd5, 0x98, 0x4f, 0x02, 0xac, 0xe1, 0x36, 0x7b, 0x27, 0x6a, 0xbd, 0xf0, 0x5e, 0x13, 0xc4, 0x89,
        0x63, 0x2e, 0xf9, 0xb4, 0x1a, 0x57, 0x80, 0xcd, 0x91, 0xdc, 0x0b, 0x46, 0xe8, 0xa5, 0x72, 0x3f,
        0xca, 0x87, 0x50, 0x1d, 0xb3, 0xfe, 0x29, 0x64, 0x38, 0x75, 0xa2, 0xef, 0x41, 0x0c, 0xdb, 0x96,
        0x42, 0x0f, 0xd8, 0x95, 0x3b, 0x76, 0xa1, 0xec, 0xb0, 0xfd, 0x2a, 0x67, 0xc9, 0x84, 0x53, 0x1e,
        0xeb, 0xa6, 0x71, 0x3c, 0x92, 0xdf, 0x08, 0x45, 0x19, 0x54, 0x83, 0xce, 0x60, 0x2d, 0xfa, 0xb7,
        0x5d, 0x10, 0xc7, 0x8a, 0x24, 0x69, 0xbe, 0xf3, 0xaf, 0xe2, 0x35, 0x78, 0xd6, 0x9b, 0x4c, 0x01,
        0xf4, 0xb9, 0x6e, 0x23, 0x8d, 0xc0, 0x17, 0x5a, 0x06, 0x4b, 0x9c, 0xd1, 0x7f, 0x32, 0xe5, 0xa8
    };

    private readonly LidarPoint[] Points;
    private readonly PwmChannel pwm;

    #endregion

    #region properites

    internal SerialPort Com { get; }

    internal GpioController Gpio { get; }


    /// <summary>
    ///     Returns degree per second
    /// </summary>
    public int Speed { get; private set; }


    public LidarPoint this[int angle] => Points[angle];

    #endregion


    #region methods

    public void SetPower(bool enable)
    {
        if (enable)
        {
            Gpio.Write(7, true);
            Thread.Sleep(100);
            pwm.Frequency = 1000;
            pwm.DutyCycle = 0.5;
            pwm.Start();
            Thread.Sleep(100);
            pwm.DutyCycle = 0.15;
            Thread.Sleep(100);
            pwm.Stop();
        }
        else
        {
            pwm.Frequency = 7000;
            pwm.DutyCycle = 0.5;
            pwm.Start();
            Thread.Sleep(200);
            pwm.Stop();
            Thread.Sleep(500);
            Gpio.Write(7, false);
        }
    }

    private void Run()
    {
        var data = new byte[47];

        Console.WriteLine("Waiting for Lidar-Sync");
        while (true)
        {
            var first = Com.ReadByte();

            // Erstes Byte prüfen 
            if (first == HEADER_BYTE)
            {
                var second = Com.ReadByte();

                // Zweites Byte prüfen 
                if (second == HEADER_BYTE_TWO)
                {
                    data[0] = HEADER_BYTE;
                    data[1] = HEADER_BYTE_TWO;

                    ReadExactBytes(data, 2, 45);

                    // CRC Prüfsumme über alle 47 Bytes bilden [cite: 2882]
                    // Falls diese 0 ergibt, ist das Paket gültig [cite: 2883]
                    var crc = CalculateCrc(data);
                    if (crc == 0)
                    {
                        ProcessPacket(data);

                        Console.WriteLine("Lidar-Sync hergestellt");
                        ReadSynchronized(data);
                        Console.WriteLine("Lidar-Sync failed!");
                    }
                    else
                    {
                        // Sehr wichtiges Debugging: Wenn er hier landet, 
                        // empfängt er Daten, aber sie sind fehlerhaft!
                        Console.WriteLine($"Header gefunden, aber CRC falsch: {crc}");
                    }
                }
                //  Console.WriteLine($"Header gefunden, aber zweites Byte falsch: {second}");
            }
            // Console.WriteLine($"Header nicht gefunden: {first}");
        }
    }

    private void ReadSynchronized(byte[] data)
    {
        while (true)
        {
            ReadExactBytes(data, 0, 47);

            if (data[0] != HEADER_BYTE || data[1] != HEADER_BYTE_TWO || CalculateCrc(data) != 0) break;

            ProcessPacket(data);
            Console.WriteLine($"Lidar-Abstand vorne: {Points[0].Distance} mm / Intensity: {Points[0].Intensity}");
        }
    }

    private void ReadExactBytes(byte[] buffer, int offset, int count)
    {
        var bytesReadTotal = 0;
        while (bytesReadTotal < count)
            // Com.Read liest bis zu x Bytes, blockiert aber, bis mindestens 1 Byte da ist
            bytesReadTotal += Com.Read(buffer, offset + bytesReadTotal, count - bytesReadTotal);
    }

    private byte CalculateCrc(byte[] data)
    {
        byte crc = 0;
        for (var i = 0; i < 47; i++) crc = crc8[(crc ^ data[i]) & 0xFF];
        return crc;
    }

    private void ProcessPacket(byte[] data)
    {
        Speed = BitConverter.ToUInt16(data, 2);

        var startAngle = BitConverter.ToUInt16(data, 4) / 100.0;
        var endAngle = BitConverter.ToUInt16(data, 42) / 100.0;

        // Distanz zwischen Start und Ende berechnen (mit Wrap-Around Behandlung bei 360°)
        var diff = endAngle - startAngle;
        if (diff < 0) diff += 360; // Passiert, wenn das Paket den Nullpunkt überschreitet (z.B. Start 355°, Ende 5°)

        // Das Paket enthält 12 Punkte. Es gibt also 11 Zwischenschritte zwischen Start und Ende.
        var step = diff / 11.0;

        // Byte 6 bis 41 enthalten die 12 x 3 Bytes (Distanz + Intensität)
        for (var i = 0; i < 12; i++)
        {
            // 6 bytes sind Metadaten: Header (2), Geschwindigkeit (2), Startwinkel (2)
            // 3 Bytes sind Messdaten: Distanz (2), Intensität (1)
            // Ab 6 beginnen die eigentlichen Messdaten + wie viele Punkte bereits gelesen wurden
            var offset = 6 + i * 3;

            // Die Distanz in mm (2 Bytes)
            var distance = BitConverter.ToUInt16(data, offset);

            // Intensität (1 Byte)
            var intensity = data[offset + 2];

            // Winkel für diesen spezifischen Punkt berechnen
            var currentAngle = startAngle + step * i;
            if (currentAngle >= 360) currentAngle -= 360;

            // Winkel in einen Ganzzahl (0-359) umwandeln
            var angleIndex = (int)Math.Round(currentAngle) % 360;

            // Wert in 360 Array speichern
            Points[angleIndex] = new LidarPoint
            {
                Distance = distance,
                Intensity = intensity
            };
        }
    }

    #endregion
}