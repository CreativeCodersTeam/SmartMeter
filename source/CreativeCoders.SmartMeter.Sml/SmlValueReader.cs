namespace CreativeCoders.SmartMeter.Sml;

public class SmlValueReader : ISmlValueReader
{
    public IEnumerable<SmlValue> Read(byte[] data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            if (data.Skip(i).Take(4).SequenceEqual(new byte[] { 0xFF, 0x01, 0x01, 0x62 }))
            {
                var valueData = data.Skip(i + 8).Take(8).ToArray();

                decimal value = BitConverter.ToUInt64(valueData.Reverse().ToArray());

                yield return new SmlValue(SmlValueType.SoldEnergy) { Value = value / 10 };
            }
            
            //if (data.Skip(i).Take(4).SequenceEqual(new byte[] { 0x59, 0x04, 0x01, 0x62 }))
            if (data.Skip(i).Take(3).SequenceEqual(new byte[] { 0x04, 0x01, 0x62 }))
            {
                var valueData = data.Skip(i + 7).Take(8).ToArray();

                decimal value = BitConverter.ToUInt64(valueData.Reverse().ToArray());

                yield return new SmlValue(SmlValueType.PurchasedEnergy) { Value = value / 10 };
            }
        }
    }
}