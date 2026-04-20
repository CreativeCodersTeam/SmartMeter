# SmartMeter

A .NET 10 server application that reads energy data from smart meters via the SML (Smart Message Language) protocol and publishes values over MQTT. It runs as a systemd daemon on Linux and connects to the meter through a serial optical coupler.

## Project Structure

```
SmartMeter.sln
source/
  CreativeCoders.SmartMessageLanguage/      # SML framing, TLV parsing, CRC validation, OBIS value extraction
  CreativeCoders.SmartMeter.Core/           # Serial port abstraction, reactive data pipeline, configuration
  CreativeCoders.SmartMeter.DataProcessing/ # Value processing (power calculation, throttling) and MQTT publishing
  CreativeCoders.SmartMeter.Server.Core/    # Daemon host builder, server lifecycle orchestration
  CreativeCoders.SmartMeter.Server.Linux/   # Linux systemd daemon entry point
  CreativeCoders.SmartMeter.Cli/            # CLI tool for diagnostics and manual data retrieval
tests/
  CreativeCoders.SmartMessageLanguage.Tests/
  CreativeCoders.SmartMeter.Core.Tests/
  CreativeCoders.SmartMeter.DataProcessing.Tests/
  CreativeCoders.SmartMeter.Server.Core.Tests/
build/                                      # Cake build project (CreativeCoders.CakeBuild)
```

## Tech Stack

- **Runtime:** .NET 10 (`net10.0`), C# with nullable reference types and implicit usings
- **Reactive Streams:** `System.Reactive` for the entire data pipeline
- **MQTT:** `MQTTnet` for broker communication
- **Serial I/O:** `System.IO.Ports` for optical coupler access
- **Daemon:** `CreativeCoders.Daemon.Linux` for systemd integration
- **Logging:** Serilog with console sink
- **CLI:** Spectre.Console (`IAnsiConsole`)
- **Build:** Cake via `CreativeCoders.CakeBuild`, scripts: `build.sh` / `build.ps1` / `build.cmd`
- **Package Management:** Central version management via `Directory.Packages.props`

## Testing Stack

- **Framework:** xUnit
- **Mocking:** FakeItEasy
- **Assertions:** AwesomeAssertions
- **Coverage:** coverlet.collector
- **Time Testing:** `Microsoft.Extensions.TimeProvider.Testing` (FakeTimeProvider)

## Data Flow

```
Serial Port (byte[])
    |  ReactiveSerialPort
    v
SML Frame Detection
    |  SmlMessageDetector (start/end escape sequences, CRC-16/X-25 validation)
    v
SML Parsing
    |  SmlParser (TLV parsing, OBIS value extraction with decimal scaling)
    v
Reactive Data Pipeline
    |  SmartMeterReactiveDataPipeline
    |  Filters OBIS codes: 1-0:1.8.0 (purchased), 1-0:2.8.0 (sold)
    |  Applies configurable energy offsets
    v
Value Processing
    |  SmlValueProcessor
    |  Calculates instantaneous power from energy differentials
    |  Derives GridPowerBalance from current power values
    |  Throttles unchanged values (30s minimum, 5m window)
    v
MQTT Publishing
    |  MqttValuePublisher (background worker, auto-reconnect)
    |  Topic: smartmeter/values/{ValueType}
    |  Payload: JSON or plain decimal (GridPowerBalance uses plain)
    v
MQTT Broker
```

## Key Types

| Type | File | Purpose |
|------|------|---------|
| `SmlMessageDetector` | `SmartMessageLanguage/Framing/SmlMessageDetector.cs` | Streaming SML frame boundary detection |
| `SmlParser` | `SmartMessageLanguage/Parsing/SmlParser.cs` | TLV-based SML protocol parser |
| `SmartMeterReactiveDataPipeline` | `SmartMeter.Core/SmlData/SmartMeterReactiveDataPipeline.cs` | OBIS code filtering and value conversion |
| `SmlValueProcessor` | `SmartMeter.DataProcessing/SmlValueProcessor.cs` | Power calculation, throttling, balance derivation |
| `MqttValuePublisher` | `SmartMeter.DataProcessing/MqttValuePublisher.cs` | MQTT client with queued publishing |
| `SmartMeterServer` | `SmartMeter.Server.Core/SmartMeterServer.cs` | Daemon lifecycle (`IDaemonService`) |
| `SmartMeterDaemonHostBuilder` | `SmartMeter.Server.Core/SmartMeterDaemonHostBuilder.cs` | Host configuration, DI, Serilog setup |

## Published MQTT Values

| SmartMeterValueType | Description | Format |
|---------------------|-------------|--------|
| `TotalPurchasedEnergy` | Cumulative energy purchased from grid | JSON |
| `TotalSoldEnergy` | Cumulative energy sold to grid | JSON |
| `CurrentPurchasingPower` | Current power drawn from grid | JSON |
| `CurrentSellingPower` | Current power fed into grid | JSON |
| `GridPowerBalance` | Net power balance (positive = purchasing, negative = selling) | Plain decimal |

## Dependency Injection

Services are registered in layers:

1. `AddSml()` -- registers `ISmlParser`, `ISmlMessageDetector`
2. `AddSmartMeterServer()` -- calls `AddSml()`, registers `IReactiveSerialPortFactory`, `ISmartMeterReactiveDataPipeline`, `ISmartMeterUnlocker`
3. `SmartMeterDaemonHostBuilder.ConfigureServices()` -- registers `IMqttValuePublisher`, `ISmartMeterDataProducer`

## Configuration

The server reads `/etc/smartmeter.conf`. Key options:

| Option | Default | Description |
|--------|---------|-------------|
| `PortName` | `/dev/ttyUSB0` | Serial port device path |
| `Mqtt:Server` | -- | MQTT broker URI (required) |
| `Mqtt:ClientName` | `SmartMeterClient` | MQTT client identifier |
| `Mqtt:TopicTemplate` | `smartmeter/values/{0}` | Topic template (`{0}` = value type) |

## Common Commands

```bash
# Build
./build.sh
# or
dotnet build

# Run tests
dotnet test

# Run specific test project
dotnet test tests/CreativeCoders.SmartMeter.DataProcessing.Tests

# Install on Linux
./install-smartmeter.sh

# Service management
sudo systemctl status smartmeter-server
sudo journalctl -u smartmeter-server -f
```

## CI/CD

Workflows in `.github/workflows/`:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `main.yml` | Push to `main` | Build, test, publish NuGet, create GitHub release |
| `pull-request.yml` | PR events | PR validation |
| `integration.yml` | -- | Integration testing |
| `release.yml` | -- | Release management |
