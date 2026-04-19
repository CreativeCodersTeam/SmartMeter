# SmartMeter

A .NET-based server application that reads energy data from smart meters via the [Smart Message Language (SML)](https://de.wikipedia.org/wiki/Smart_Message_Language) protocol and publishes the values over MQTT. It runs as a systemd daemon on Linux and connects to the meter through a serial optical coupler (e.g. `/dev/ttyUSB0`).

## Features

- **SML Protocol Parser** -- streaming detector and parser for the Smart Message Language protocol, available as a standalone [NuGet package](https://www.nuget.org/packages/CreativeCoders.SmartMessageLanguage)
- **MQTT Publishing** -- forwards meter readings (purchased/sold energy, current power, grid balance) to an MQTT broker with configurable topics
- **Linux Daemon** -- runs as a systemd service under a dedicated user with automatic service registration
- **CLI Tool** -- command-line interface (`smc`) for diagnostics and manual data retrieval

## Project Structure

| Project | Description |
|---|---|
| `CreativeCoders.SmartMessageLanguage` | SML framing, TLV parsing, CRC validation, OBIS value extraction |
| `CreativeCoders.SmartMeter.Core` | Serial port abstraction and configuration options |
| `CreativeCoders.SmartMeter.DataProcessing` | Value processing pipeline and MQTT publisher |
| `CreativeCoders.SmartMeter.Server.Core` | Server logic and daemon host builder |
| `CreativeCoders.SmartMeter.Server.Linux` | Linux systemd daemon entry point |
| `CreativeCoders.SmartMeter.Cli` | CLI tool for manual interaction |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for building from source)
- .NET 10 Runtime (on the target Linux machine)
- A smart meter with an SML-compatible infrared interface
- An optical reading head (IR coupler) connected via USB serial (e.g. `/dev/ttyUSB0`)
- An MQTT broker (e.g. [Mosquitto](https://mosquitto.org/))

## Building

```bash
./build.sh
```

Or using the .NET CLI directly:

```bash
dotnet build
dotnet test
```

## Linux Server Installation

The recommended way to install or update the SmartMeter server on Linux is by using the provided `install-smartmeter.sh` script. It downloads the distribution package, extracts it, and runs the bundled installer.

### Quick Install (Latest Release)

Download and run the installer in one step:

```bash
curl -fsSL https://raw.githubusercontent.com/CreativeCodersTeam/SmartMeter/main/install-smartmeter.sh -o install-smartmeter.sh
chmod +x install-smartmeter.sh
./install-smartmeter.sh
```

This will:

1. Fetch the latest stable release from GitHub
2. Download the `SmartMeter.Server.Linux.tar.gz` asset
3. Extract and run the bundled `install.sh` with `sudo`

### Install from CI Build

To install from the latest successful CI build instead of a release (requires the [GitHub CLI](https://cli.github.com/) with authentication):

```bash
./install-smartmeter.sh --workflows main.yml
```

### Non-Interactive Install

Skip the confirmation prompt with `-y`:

```bash
./install-smartmeter.sh -y
```

### Required Tools

| Tool | Required for |
|---|---|
| `curl` | Downloading the archive |
| `tar` | Extracting the package |
| `jq` | Parsing GitHub API responses |
| `sudo` | Running the privileged installer |
| `gh` | Only when using `--workflows` |

### What the Installer Does

The bundled `install.sh` performs the following steps:

1. Stops and disables the existing `smartmeter-server` systemd service (if running)
2. Backs up the current installation at `/opt/smartmetersrv` to `/opt/smartmetersrv.bak`
3. Copies the new files to `/opt/smartmetersrv`
4. Creates a dedicated `smartmeter-user` system user (if it doesn't exist) and adds it to the `dialout` group for serial port access
5. Sets file ownership to `smartmeter-user`
6. Registers and starts the systemd service via `dotnet smartmetersrv.dll --install`

### Updating

To update an existing installation, simply run `install-smartmeter.sh` again. The installer automatically backs up the previous installation before deploying the new version.

```bash
./install-smartmeter.sh
```

> [!TIP]
> After an update, check that the service is running correctly:
> ```bash
> sudo systemctl status smartmeter-server
> sudo journalctl -u smartmeter-server -f
> ```

### Service Management

Once installed, the server runs as a systemd service:

```bash
# Check service status
sudo systemctl status smartmeter-server

# Stop the service
sudo systemctl stop smartmeter-server

# Start the service
sudo systemctl start smartmeter-server

# View logs
sudo journalctl -u smartmeter-server -f
```

### Installation Paths

| Path | Description |
|---|---|
| `/opt/smartmetersrv` | Application directory |
| `/opt/smartmetersrv.bak` | Backup of the previous installation |

## Configuration

The server reads its configuration at startup. Key options include:

| Option | Default | Description |
|---|---|---|
| `PortName` | `/dev/ttyUSB0` | Serial port device path for the optical coupler |
| `MqttServer` | -- | URI of the MQTT broker |
| `MqttClientName` | `SmartMeterClient` | MQTT client identifier |
| `MqttTopicTemplate` | `smartmeter/values/{0}` | Topic template (`{0}` is replaced by the value type) |

## Published MQTT Values

The server publishes the following meter values:

| Value | Description |
|---|---|
| `TotalPurchasedEnergy` | Cumulative energy purchased from the grid |
| `TotalSoldEnergy` | Cumulative energy sold to the grid |
| `CurrentPurchasingPower` | Current power being drawn |
| `CurrentSellingPower` | Current power being fed in |
| `GridPowerBalance` | Net power balance |
