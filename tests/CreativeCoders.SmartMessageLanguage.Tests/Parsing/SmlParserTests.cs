using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Framing;
using CreativeCoders.SmartMessageLanguage.Parsing;
using CreativeCoders.SmartMessageLanguage.Tests.Fixtures;
using CreativeCoders.SmartMessageLanguage.Tlv;
using CreativeCoders.SmartMessageLanguage.Units;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CreativeCoders.SmartMessageLanguage.Tests.Parsing;

public class SmlParserTests
{
    private static SmlParser CreateSut() => new(NullLogger<SmlParser>.Instance);

    [Fact]
    public void Parse_GetListResponsePayload_ExtractsAllObisValues()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().BeEmpty();
        result.Values.Should().HaveCount(2);

        var energy = result.Values.Single(v => v.ObisCode == "1-0:1.8.0*255");
        energy.Unit.Should().Be(SmlUnit.WattHour);
        energy.Scaler.Should().Be((sbyte)-1);
        energy.Value.Should().Be(12345.6m);

        var power = result.Values.Single(v => v.ObisCode == "1-0:16.7.0*255");
        power.Unit.Should().Be(SmlUnit.Watt);
        power.Scaler.Should().Be((sbyte)0);
        power.Value.Should().Be(567m);
    }

    [Fact]
    public void Parse_EmptyPayload_ReturnsEmptyResult()
    {
        var result = CreateSut().Parse([]);

        result.Values.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Frame_DelegatesToPayloadOverload()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frame = new SmlFrame([], payload, true, 0);

        var result = CreateSut().Parse(frame);

        result.Values.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_TopLevelPrimitive_ReportsEnvelopeWarning()
    {
        // Top-level primitive (UInt8) instead of a List → warning + skipped element.
        var payload = new TlvBuilder().UInt8(0x42).ToArray();

        var result = CreateSut().Parse(payload);

        result.Values.Should().BeEmpty();
        result.Warnings.Should().ContainSingle()
            .Which.Should().Contain("Unexpected top-level TLV type");
    }

    [Fact]
    public void Parse_EndOfMessageAtTopLevel_IsIgnored()
    {
        // 0x00 end-of-message marker at top level must not produce warnings.
        var payload = new TlvBuilder().EndOfMessage().ToArray();

        var result = CreateSut().Parse(payload);

        result.Values.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MessageWithWrongEntryCount_SkipsMessageSilently()
    {
        // Top-level List(3) instead of the expected List(6): parser just skips the entries.
        var payload = new TlvBuilder()
            .List(3)
                .OctetString([0x01])
                .OctetString([0x02])
                .OctetString([0x03])
            .ToArray();

        var result = CreateSut().Parse(payload);

        result.Values.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MessageBodyWithWrongWrapperArity_AddsWarning()
    {
        // messageBody (field 4) must be List(2); supply List(1) → "Malformed SML_Message body wrapper".
        var payload = new TlvBuilder()
            .List(6)
                .OctetString([0xAA])     // transactionId
                .UInt8(0)                // groupNo
                .UInt8(0)                // abortOnError
                .List(1)                 // malformed body wrapper
                    .UInt32(0x00000701)
            .ToArray();

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().Contain(w => w.Contains("Malformed SML_Message body wrapper"));
    }

    [Fact]
    public void Parse_MessageBodyMissingTypeTag_AddsWarning()
    {
        // messageBody type tag must be Unsigned; supply OctetString instead.
        var payload = new TlvBuilder()
            .List(6)
                .OctetString([0xAA])
                .UInt8(0)
                .UInt8(0)
                .List(2)
                    .OctetString([0x01])      // should be Unsigned type tag
                    .List(0)
            .ToArray();

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().Contain(w => w.Contains("Missing messageBody type tag"));
    }

    [Fact]
    public void Parse_UnknownMessageBodyType_IsSkippedWithoutWarning()
    {
        // A message body type other than 0x701 is simply skipped; no warning expected.
        var payload = BuildSingleMessage(messageBodyType: 0x12345678u, valListEntryBuilder: null);

        var result = CreateSut().Parse(payload);

        result.Values.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Parse_GetListResponseWithWrongFieldCount_AddsWarning()
    {
        // GetListResponse must be List(7); inject List(3).
        var payload = new TlvBuilder()
            .List(6)
                .OctetString([0xAA])
                .UInt8(0)
                .UInt8(0)
                .List(2)
                    .UInt32(0x00000701)
                    .List(3)
                        .OctetString([0x01])
                        .OctetString([0x02])
                        .OctetString([0x03])
                .UInt8(0)
                .EndOfMessage()
            .ToArray();

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().Contain(w => w.Contains("GetListResponse with unexpected field count"));
    }

    [Fact]
    public void Parse_ValListAbsent_ProducesNoValuesAndNoWarning()
    {
        // valList encoded as Null (absent optional) → no warning, no values.
        var payload = BuildGetListResponseWithValList(b => b.Null());

        var result = CreateSut().Parse(payload);

        result.Values.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ValListWithUnexpectedType_AddsWarning()
    {
        // valList as Unsigned instead of List or OctetString → warning.
        var payload = BuildGetListResponseWithValList(b => b.UInt8(0x42));

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().Contain(w => w.Contains("Unexpected valList type"));
    }

    [Fact]
    public void Parse_ValListEntryNotAList_AddsWarning()
    {
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .UInt8(0x42)); // entry should itself be a List, not an Unsigned.

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().Contain("valList entry is not a list");
    }

    [Fact]
    public void Parse_ValListEntryTooShort_AddsWarning()
    {
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(3)
                    .OctetString(SampleSmlFile.ObisEnergy)
                    .Null()
                    .Null());

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().Contain(w => w.Contains("valList entry too short"));
    }

    [Fact]
    public void Parse_ValListEntryMissingObjName_AddsWarning()
    {
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(6)
                    .UInt8(0x42)   // objName must be OctetString
                    .Null()
                    .Null()
                    .UInt8(0)
                    .Int8(0)
                    .UInt8(0));

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().Contain("valList entry missing objName");
    }

    [Fact]
    public void Parse_UnknownUnitCode_AddsWarningButKeepsValue()
    {
        // Unit code 99 is not defined in SmlUnit → warning, Unit=Unknown, value still parsed.
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString(SampleSmlFile.ObisEnergy)
                    .Null().Null()
                    .UInt8(99)
                    .Int8(0)
                    .UInt8(42)
                    .Null());

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().Contain(w => w.Contains("Unknown unit code 99"));
        result.Values.Should().ContainSingle().Which.Unit.Should().Be(SmlUnit.Unknown);
        result.Values[0].Value.Should().Be(42m);
    }

    [Fact]
    public void Parse_UnitCodeZero_IsTreatedAsUnknownWithoutWarning()
    {
        // Unit code 0 is defined as SmlUnit.Unknown; no warning should be raised.
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString(SampleSmlFile.ObisEnergy)
                    .Null().Null()
                    .UInt8(0)
                    .Int8(0)
                    .UInt8(1)
                    .Null());

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().BeEmpty();
        result.Values.Should().ContainSingle().Which.Unit.Should().Be(SmlUnit.Unknown);
    }

    [Fact]
    public void Parse_BooleanValue_IsMappedToOneOrZero()
    {
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString(SampleSmlFile.ObisEnergy)
                    .Null().Null()
                    .UInt8(0)
                    .Int8(0)
                    .Bool(true)
                    .Null());

        var result = CreateSut().Parse(payload);

        var value = result.Values.Should().ContainSingle().Which;
        value.Value.Should().Be(1m);
        value.RawType.Should().Be(SmlMessageValueType.Boolean);
    }

    [Fact]
    public void Parse_OctetStringValue_KeepsRawButNullNumeric()
    {
        // Server ID-like value: OctetString → RawValue populated, Value null, no warning.
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString(SampleSmlFile.ObisEnergy)
                    .Null().Null()
                    .UInt8(0)
                    .Int8(0)
                    .OctetString([0x01, 0x02, 0x03])
                    .Null());

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().BeEmpty();
        var value = result.Values.Should().ContainSingle().Which;
        value.Value.Should().BeNull();
        value.RawType.Should().Be(SmlMessageValueType.OctetString);
        value.RawValue.Should().Equal(0x01, 0x02, 0x03);
    }

    [Fact]
    public void Parse_UnsupportedValueType_AddsWarningAndNullValue()
    {
        // A List as value is not supported by ComputeDecimalValue → warning.
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString(SampleSmlFile.ObisEnergy)
                    .Null().Null()
                    .UInt8(0)
                    .Int8(0)
                    .List(0)
                    .Null());

        var result = CreateSut().Parse(payload);

        result.Warnings.Should().Contain(w => w.Contains("Unsupported value type"));
        result.Values.Should().ContainSingle().Which.Value.Should().BeNull();
    }

    [Theory]
    [InlineData((sbyte)-2, 100, 1.00)]
    [InlineData((sbyte)1, 42, 420.0)]
    [InlineData((sbyte)0, 17, 17.0)]
    public void Parse_ScaledUnsignedValue_AppliesPowerOfTen(sbyte scaler, byte raw, double expected)
    {
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString(SampleSmlFile.ObisEnergy)
                    .Null().Null()
                    .UInt8(SampleSmlFile.UnitWattHour)
                    .Int8(scaler)
                    .UInt8(raw)
                    .Null());

        var result = CreateSut().Parse(payload);

        result.Values.Should().ContainSingle().Which.Value.Should().Be((decimal)expected);
    }

    [Fact]
    public void Parse_SignedValue_IsSignExtended()
    {
        // Signed -5 with scaler 0 → -5.
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString(SampleSmlFile.ObisPower)
                    .Null().Null()
                    .UInt8(SampleSmlFile.UnitWatt)
                    .Int8(0)
                    .Int8(-5)
                    .Null());

        var result = CreateSut().Parse(payload);

        result.Values.Should().ContainSingle().Which.Value.Should().Be(-5m);
    }

    [Fact]
    public void Parse_MissingScaler_DefaultsToZero()
    {
        // Scaler is OctetString (unexpected) → parser keeps scaler = 0.
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString(SampleSmlFile.ObisPower)
                    .Null().Null()
                    .UInt8(SampleSmlFile.UnitWatt)
                    .OctetString([0x00])      // unexpected scaler type
                    .UInt8(7)
                    .Null());

        var result = CreateSut().Parse(payload);

        var value = result.Values.Should().ContainSingle().Which;
        value.Scaler.Should().Be((sbyte)0);
        value.Value.Should().Be(7m);
    }

    [Fact]
    public void Parse_ShortObisBytes_FallsBackToHexRepresentation()
    {
        // 3-byte objName is below OBIS minimum length → formatted as uppercase hex.
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString([0xDE, 0xAD, 0xBE])
                    .Null().Null()
                    .UInt8(0)
                    .Int8(0)
                    .UInt8(1)
                    .Null());

        var result = CreateSut().Parse(payload);

        result.Values.Should().ContainSingle().Which.ObisCode.Should().Be("DEADBE");
    }

    [Fact]
    public void Parse_FiveByteObis_DefaultsFTo255()
    {
        // 5-byte objName defaults the optional F to 255.
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(7)
                    .OctetString([1, 0, 1, 8, 0])
                    .Null().Null()
                    .UInt8(0).Int8(0).UInt8(0)
                    .Null());

        var result = CreateSut().Parse(payload);

        result.Values.Should().ContainSingle().Which.ObisCode.Should().Be("1-0:1.8.0*255");
    }

    [Fact]
    public void Parse_ValListEntryWithExtraTrailingFields_StillParsesValue()
    {
        // Entry with 8 fields (valid: minimum is 6, the parser skips anything beyond field 6).
        var payload = BuildGetListResponseWithValList(b => b
            .List(1)
                .List(8)
                    .OctetString(SampleSmlFile.ObisPower)
                    .Null().Null()
                    .UInt8(SampleSmlFile.UnitWatt)
                    .Int8(0)
                    .UInt8(42)
                    .Null()         // valueSignature
                    .Null());        // extra field

        var result = CreateSut().Parse(payload);

        result.Values.Should().ContainSingle().Which.Value.Should().Be(42m);
    }

    // Builds: List(6)[transactionId, groupNo, abortOnError, List(2)[bodyType, bodyChoice], crc, endOfMsg]
    // with bodyChoice = Null when valListEntryBuilder is null.
    private static byte[] BuildSingleMessage(uint messageBodyType, Action<TlvBuilder>? valListEntryBuilder)
    {
        var body = new TlvBuilder().List(2).UInt32(messageBodyType);

        if (valListEntryBuilder is null)
        {
            body.Null();
        }
        else
        {
            valListEntryBuilder(body);
        }

        var message = new TlvBuilder()
            .List(6)
                .OctetString([0xAA])
                .UInt8(0)
                .UInt8(0)
            .Append(body)
            .UInt8(0)
            .EndOfMessage();

        return message.ToArray();
    }

    // Wraps a valList (field 5 of GetListResponse) into a full single-message SML payload.
    private static byte[] BuildGetListResponseWithValList(Action<TlvBuilder> valListBuilder)
    {
        var inner = new TlvBuilder()
            .List(7)
                .OctetString([0x01])
                .OctetString([0x02])
                .Null()
                .Null();
        valListBuilder(inner);
        inner.Null().Null();

        return BuildSingleMessage(0x00000701u, b => b.Append(inner));
    }
}
