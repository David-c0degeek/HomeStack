# HomeStack

HomeStack is a self-hosted tool for homelab management that helps you discover, organize, and configure your homelab services in one place.

## Overview

Running a homelab environment can be empowering but also challenging. HomeStack addresses common pain points:

- **Docker networking complexity** - Automatically discovers and maps container networks
- **Scattered DNS entries** - Generates DNS overrides for pfSense
- **Configuration management** - Creates reverse proxy configs for Nginx and Caddy
- **Service discovery** - Creates bookmarks for Flame and Homepage dashboards
- **VPN container isolation** - Detects and manages VPN-isolated containers

## Features

- 🔍 **Network scanning**
  - Docker containers
  - Unraid servers
  - pfSense routers
- 🗺️ **Service mapping**
  - Hostname → IP → Port → Domain
- ⚙️ **Configuration generation**
  - pfSense DNS host overrides
  - Nginx/Caddy reverse proxy configs
  - Flame/Homepage bookmark configs
- 🔄 **Health checking**
  - Detects service connectivity issues
  - Maps VPN-isolated services

## Getting Started

### Prerequisites

- .NET 9.0 SDK (Preview 8 or later)
- Docker (for container scanning)
- pfSense (optional)
- Unraid (optional)

### Installation

```bash
# Clone the repository
git clone https://github.com/c0degeek/homestack.git

# Build the project
cd homestack
dotnet build

# Run the CLI tool
dotnet run --project src/HomeStack.Cli scan --docker
```

### CLI Usage

Scan your network:

```bash
# Scan Docker containers
dotnet run --project src/HomeStack.Cli scan --docker

# Scan Unraid server
dotnet run --project src/HomeStack.Cli scan --unraid --unraid-host 192.168.1.2 --unraid-user root --unraid-pass password

# Scan pfSense router
dotnet run --project src/HomeStack.Cli scan --pfsense --pfsense-host 192.168.1.1 --pfsense-user admin --pfsense-pass password

# Scan all
dotnet run --project src/HomeStack.Cli scan --docker --unraid --pfsense --unraid-host 192.168.1.2 --pfsense-host 192.168.1.1

# Save results to a file
dotnet run --project src/HomeStack.Cli scan --docker --output scan-results.json
```

Generate configurations:

```bash
# Generate DNS overrides
dotnet run --project src/HomeStack.Cli config --input scan-results.json --types dns

# Generate Nginx proxy configuration
dotnet run --project src/HomeStack.Cli config --input scan-results.json --types nginx

# Generate multiple configuration types
dotnet run --project src/HomeStack.Cli config --input scan-results.json --types dns,nginx,caddy,flame,homepage

# Set domain suffix
dotnet run --project src/HomeStack.Cli config --input scan-results.json --types dns --domain home.local

# Save to a directory
dotnet run --project src/HomeStack.Cli config --input scan-results.json --types dns,nginx --output-dir configs
```

## Roadmap

- Web UI for service visualization
- Auto-update pfSense DNS entries
- Support for Traefik reverse proxy
- Synology DSM integration
- TrueNAS integration
- K8s/k3s cluster scanning

## License

MIT License

Copyright (c) 2025 C0deGeek (David)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
