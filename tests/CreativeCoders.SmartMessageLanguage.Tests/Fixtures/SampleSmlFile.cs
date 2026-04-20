namespace CreativeCoders.SmartMessageLanguage.Tests.Fixtures;

/// <summary>
/// Builds a canonical <c>SML_GetList.Res</c> payload usable both by the parser
/// and (after wrapping via <see cref="FrameBuilder"/>) by the detector.
/// </summary>
internal static class SampleSmlFile
{
    // OBIS 1-0:1.8.0*255 — Positive active energy total.
    public static readonly byte[] ObisEnergy = [0x01, 0x00, 0x01, 0x08, 0x00, 0xFF];

    // OBIS 1-0:16.7.0*255 — Sum active instantaneous power.
    public static readonly byte[] ObisPower = [0x01, 0x00, 0x10, 0x07, 0x00, 0xFF];

    public const ulong EnergyRaw = 123456UL;   // → 12345.6 Wh with scaler -1
    public const int PowerRaw = 567;           // → 567 W with scaler 0
    public const byte UnitWattHour = 30;
    public const byte UnitWatt = 27;

    public static byte[] BuildGetListResponsePayload()
    {
        var valList = new TlvBuilder()
            .List(2)
                // Entry 1: energy.
                .List(7)
                    .OctetString(ObisEnergy)
                    .Null()          // status
                    .Null()          // valTime
                    .UInt8(UnitWattHour)
                    .Int8(-1)
                    .UInt64(EnergyRaw)
                    .Null()          // valueSignature
                // Entry 2: power.
                .List(7)
                    .OctetString(ObisPower)
                    .Null()
                    .Null()
                    .UInt8(UnitWatt)
                    .Int8(0)
                    .Int32(PowerRaw)
                    .Null()
            .ToArray();

        var getListBody = new TlvBuilder()
            .List(7)
                .OctetString([0x01])        // clientId
                .OctetString([0x02])        // serverId
                .Null()                     // listName
                .Null()                     // actSensorTime
            .ToArray();

        var getListTail = new TlvBuilder()
            .Null()                         // listSignature
            .Null()                         // actGatewayTime
            .ToArray();

        var body = new List<byte>();
        body.AddRange(getListBody);
        body.AddRange(valList);
        body.AddRange(getListTail);

        var message = new TlvBuilder()
            .List(6)
                .OctetString([0xAA, 0xBB])  // transactionId
                .UInt8(0x00)                // groupNo
                .UInt8(0x00)                // abortOnError
                .List(2)
                    .UInt32(0x00000701)     // messageBodyType = GetList.Res
            .ToArray();

        var afterBody = new TlvBuilder()
            .UInt8(0x00)                    // crc16 placeholder
            .ToArray();

        var full = new List<byte>();
        full.AddRange(message);
        full.AddRange(body);
        full.AddRange(afterBody);
        full.Add(0x00);                     // endOfSmlMsg

        return full.ToArray();
    }
}
