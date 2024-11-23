using CreativeCoders.Core.Collections;

namespace CreativeCoders.SmartMeter.Sml;

public class SmlMessage
{
    public SmlMessage(byte[] data)
    {
        Data = data;
    }

    public ushort CalcCrc16()
    {
        var dataForCalc = new List<byte>();
        dataForCalc.AddRange([0x1b, 0x1b, 0x1b, 0x1b]);
        dataForCalc.AddRange([0x01, 0x01, 0x01, 0x01]);
        dataForCalc.AddRange(Data);
        dataForCalc.AddRange([0x1b, 0x1b, 0x1b, 0x1b]);
        dataForCalc.Add(0x1a);
        dataForCalc.Add(FillByteCount);

        return CalcCrc16X25(dataForCalc.ToArray(), dataForCalc.Count);
    }

    public byte[] GetCompleteData()
    {
        var completeData = new List<byte>();

        completeData.AddRange([0x1b, 0x1b, 0x1b, 0x1b]);
        completeData.AddRange([0x01, 0x01, 0x01, 0x01]);
        completeData.AddRange(Data);
        completeData.AddRange([0x1b, 0x1b, 0x1b, 0x1b]);
        completeData.Add(0x1a);
        completeData.Add(FillByteCount);
        BitConverter.GetBytes(Crc16Checksum).Reverse().ForEach(x => completeData.Add(x));

        return completeData.ToArray();
    }

    public bool IsValid()
    {
        return CalcCrc16() == Crc16Checksum;
    }

    private ushort CalcCrc16X25(IReadOnlyList<byte> data, int len)
    {
        ushort crc = 0xffff;
        for (var i = 0; i < len; i++)
        {
            crc ^= data[i];
            for (uint k = 0; k < 8; k++)
                crc = (ushort)((crc & 1) != 0 ? (crc >> 1) ^ 0x8408 : crc >> 1);
        }

        return (ushort)~crc;
    }

    public byte[] Data { get; }

    public byte FillByteCount { get; set; }

    public ushort Crc16Checksum { get; set; }
}
