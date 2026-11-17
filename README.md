# phoneHMI: simulated phone HMI experiments

Simulated counters only. No machine connection, machine source code, commands,
or real telemetry. The two proofs do not depend on one another.

## Current status and next direction (2026-11-16)

- Wi-Fi on iPad, BLE counter read/notify on iPhone, and the original Bluefy
  counter dashboard with reconnect have been observed working.
- The preferred visual direction is now the custom
  [Bluefy dashboard](ble/dashboard/index.html): actual Rexroth image,
  red/blue instruments, no RexLink heading or demo banners, and a small
  "Simulated data" footer. The user approved its appearance on the phone.
  Final-layout BLE updates/reconnect and physical stale/link-loss checks still
  need explicit confirmation; visual approval alone is not a transport test.
- RemoteXY gauge rendering and updates were observed on iPhone; it is retained
  as an alternative experiment, not the current migration path.
- Next: move to the work laptop and qualify **DI5 peripheral/GATT-server
  support**, then publish the same simulated counter from the target.
  See the [DI5 migration plan](DI5-MIGRATION.md) for gates and handoff.
- Historical DI4 screenshots reportedly show `hci0`, iPhone discovery and
  enumeration of iPhone GATT services with BlueZ 5.50. This is encouraging
  central/client evidence, not proof of DI5 connectable advertising or a local
  GATT server. No target code or configuration has been changed.

## RemoteXY BLE experiment (Windows laptop)

This alternative was explored after the verified nRF Connect and Bluefy tests.
It is a limited, unofficial C# compatibility adapter based on RemoteXY's
MIT-licensed library protocol, not an official Windows RemoteXY SDK or a port
of the full library. It does not change the existing BLE counter or dashboard.

Requires Windows and .NET 10 SDK. Run one publisher and one phone connection
at a time; disconnect Bluefy/nRF Connect before testing RemoteXY:

```text
dotnet run --project remotexy/RemoteXY.csproj
```

Default runtime is 15 minutes. Ctrl+C stops advertising; a bounded check is:

```text
dotnet run --project remotexy/RemoteXY.csproj -- --seconds 6
dotnet run --project remotexy-tests/ProtocolTests.csproj
```

On the phone, install the [RemoteXY app](https://remotexy.com/en/download/),
allow Bluetooth, and add a **Bluetooth LE** connection to the laptop. The
advertised name may be the laptop's name, not RemoteXY. No pairing in phone
Settings is required. The experimental screen is a five-element portrait
instrument panel: a SIMULATED / READ ONLY banner, large RPM dial, digital RPM,
and smaller coolant-temperature and hydraulic-pressure dials. The instruments vary with
simulated time; they are not machine measurements or safe operating limits.
The editor-generated portrait layout uses a charcoal background, teal RPM arc,
amber needle/accent, white scale labels, and generous spacing without buttons
or setpoints. Five elements and four float32 output variables stay within the
documented free personal-test counts; the editor reported zero PRO elements.
This does not waive RemoteXY's separate commercial-use license requirements.
The layout and
values both travel over BLE; no website, cloud, or machine Wi-Fi is used.

**Final instrument layout:** the user confirmed gauge rendering and moving
values on iPhone. A separately timed 30-second run and reconnect with this
final layout remain pending. Record
the app's actual behavior when the publisher stops; freshness/disconnection UX
belongs to RemoteXY and is not yet verified. Do not regard a last value as live.
Earlier text layouts also rendered on the iPhone app in the tests below;
Android compatibility is not yet phone-verified.
Stop and record an app error rather than assuming compatibility.

First phone attempt (2026-11-16): the iPhone app rendered the simulated label,
but it overflowed the screen and subsequently flashed a red disconnect warning.
The publisher was no longer running when checked; the failure reason was not
captured. Text-field height was reduced from 20 to 8 layout units to reduce the
font size. A new run captures protocol diagnostics locally; layout fit and the
disconnect cause required a phone reconnect. The captured log then showed the
app's device-time query (`0x20`) stopped the publisher because it was not
implemented. The adapter now replies with 64-bit little-endian uptime
milliseconds, matching the reference library. The next attempt displayed an
advancing counter; reconnect still failed as described below.

The user subsequently observed an advancing counter. On disconnect, the log
showed zero subscribers followed by a notification delivery failure that
terminated advertising. The adapter now tags queued requests by subscription
session, discards old-session work, and targets replies to the requesting
subscriber. A disconnected/unreachable subscriber is logged without stopping
advertising; other notification failures still terminate with an error.
The updated publisher builds and all 31 protocol checks pass, but those checks
do not exercise WinRT disconnect races.

Physical follow-up (2026-11-16): the user cycled the RemoteXY connection and
confirmed the counter worked on both connections. The publisher remained
running with ongoing output requests after reconnect. This verifies basic
screen delivery, live updates, and manual reconnect on iPhone 15. A separately
timed 30-second run on the final version, publisher-stop/freshness behavior,
Android, and DI5 deployment remain unverified. These results do not establish
production readiness or compatibility with arbitrary RemoteXY layouts.

The first panel attempt was rejected by the phone with "GUI has unknown
element". The layout builder had incorrectly changed the third legacy header
byte from 1 to 5 as though it were a widget count. The panel now preserves the
phone-verified `16,27,1` header; tests check that header separately from the five
text records. The user then confirmed the corrected five-field text panel
displayed its header, moving RPM/temperature/pressure, and read-only footer.

The subsequent gauge revision was designed and visually inspected in the
RemoteXY editor, rather than hand-encoding widget records. Its generated V19
configuration is in [Dashboard.cs](remotexy/Dashboard.cs). The editable project
is [dashboard.remotexy.json](remotexy/dashboard.remotexy.json); import it with
the editor's Open project command. Its Arduino/HC-05 connection settings are
unused editor defaults, not a deployment prescription. Only GUI bytes and the
four packed output floats are used by the Windows BLE adapter.

The generated code lists iOS app 1.14.3+ / Android 4.19.03+ as prerequisites.
The user confirmed gauge rendering and updates on the phone. Local tests validate the exact
configuration length/header and wire payload, plus finite in-range float32
outputs at every second from 0 through the 24-hour publisher runtime. The
digital RPM uses the same value as the dial. Re-export GUI configuration and
the matching variable structure together when changing widgets; do not edit
binary widget records by guessing their meaning.

The transport uses UART service `0000ffe0-0000-1000-8000-00805f9b34fb` and
characteristic `0000ffe1-0000-1000-8000-00805f9b34fb`. Unlike the original
read/notify-only proof, **protocol requests require BLE writes** (layout/value
queries). There are zero input variables, no controls, and nonempty input
writes are rejected. The adapter implements only the minimal
configuration/value exchange, not authentication, encryption, general
fragmented requests, complex widgets, or multiple clients. Unsupported frames,
CRC errors, and notification failures are reported and stop the publisher.
Trusted local simulated experiments only.

The protocol checks cover CRC, framing, split/coalesced requests, layout/output
sizes, handshake, reset, client IDs, and rejecting control payloads. Build and
bounded advertising passed on this laptop on 2026-11-16; the earlier text
screen was phone-tested, followed by gauge rendering/updates. Final-gauge
reconnect, Android, and DI5 deployment remain pending.

Library attribution is in [third-party notices](remotexy/THIRD-PARTY-NOTICES.txt).
RemoteXY mobile app licensing is separate: its
[current terms](https://remotexy.com/en/pro/) require PRO for commercial use.
Do not purchase a license or hardware for this experiment without first
confirming the free personal-test screen works.
Before a work/management demonstration, verify the commercial-use terms and
obtain the required license; staying below the free widget count alone does
not grant commercial-use permission.

## 1. Wi-Fi: open a counter on your phone

Requires Python 3.9+ on Windows, macOS, or Linux. No Python packages are needed.
Connect the laptop and phone to the same trusted private Wi-Fi.

From the repository folder, first test on the laptop:

```text
python wifi/server.py
```

Open http://127.0.0.1:8000/ locally. On macOS/Linux, use `python3` if needed.
Stop with Ctrl+C, find the laptop's Wi-Fi IPv4 address (Windows: `ipconfig`;
macOS: System Settings > Wi-Fi > Details > TCP/IP), then bind to that address:

```text
python wifi/server.py --host 192.168.1.25
```

Replace the example address with yours. On the iPhone, open the printed URL in
Safari. `localhost` on the phone means the phone, not the laptop.

**Pass:** Safari displays SIMULATED, CONNECTED, and a counter increasing once
per second for at least 30 seconds. Stop the server: the page must report
DISCONNECTED rather than presenting the last value as live.

To check stale detection, restart with the same host and `--freeze-after 5`.
The page should show STALE after the counter stops advancing.

The server defaults to loopback; it does not change firewall, VPN, or network
settings. If prompted on a personal laptop, permit only the access appropriate
to your trusted private network. Do not disable protections. Guest-network
client isolation, VPN policy, and a wrong host address can prevent access.
On a managed laptop, resolve access with the administrator.

This is unencrypted HTTP without authentication. Use only a trusted local
network; do not port-forward it, expose it to the Internet, or put sensitive
data in it. Stop with Ctrl+C when finished.

## 2. Bluetooth: read/subscribe using an iPhone BLE utility

This publisher requires **Windows 10 version 2004+ or Windows 11**, the **.NET
10 SDK**, and a working BLE adapter/driver with peripheral-role support.
The Wi-Fi proof works on other operating systems; this BLE publisher does not.
Do not install this publisher on macOS/Linux expecting it to run.

Enable Bluetooth normally. From the repository folder:

```text
dotnet run --project ble/Counter.csproj
```

The first build restores Windows SDK reference packages from NuGet. Wait for
`Advertising confirmed: Started` (or `StartedWithoutAllAdvertisementData`). Merely
building or seeing "peripheral support" is not a successful BLE proof.

On the iPhone:

1. Open a BLE utility such as nRF Connect or LightBlue and grant Bluetooth
   permission. This proof does not use Safari, Wi-Fi, or a custom phone app.
2. Scan without a service filter. The advertised name may be the laptop's
   device name, not `phoneHMI`. Connect and inspect its services.
3. Find service `7f4a0001-6f9c-47ac-9f65-1c4db9870021`.
4. Read characteristic `7f4a0002-6f9c-47ac-9f65-1c4db9870021`, then subscribe
   to notifications. Display its bytes as UTF-8 text, not hex.
5. Observe increasing decimal values for at least 30 seconds. Disconnect,
   reconnect, and confirm reading/subscribing works again.

**Pass:** the phone connects, reads, and receives advancing notifications.
An advertisement alone is not a pass. No Windows Settings pairing is required.
The characteristic is read/notify only, public and unencrypted. Do not use
real data. Stop with Ctrl+C; a bounded diagnostic can use:

```text
dotnet run --project ble/Counter.csproj -- --seconds 15
```

The duration starts after advertising starts. Startup has its own 10-second
timeout. Failures are printed and exit nonzero. Adapter capability reports do
not guarantee connectable advertising; Windows, drivers, policy, and radio
resource availability still matter. If startup aborts, record the exact output
and stop rather than repeatedly changing system settings. This starter does
not install a package, certificate, service, driver, or firewall rule.

Advertisement event lines include both the event and the current provider state.
Windows can deliver an old `Aborted` event after the provider is already `Started`.
The publisher allows up to ten seconds for the current state to become started,
then checks it once per second while running. Startup timeout or a subsequent
aborted/stopped state exits nonzero. Event lines alone are not evidence of a
working phone connection.

## Local checks

### BLE dashboard: iPhone updates and reconnect verified

The latest visual revision is a responsive dark instrument cluster with a blue
RPM arc, digital RPM, coolant-temperature and hydraulic-pressure cards, and
explicit connection/freshness indicators. The gauge values are calculated on
the phone from the received simulated BLE counter, not independent telemetry.
No new BLE characteristic or machine command was added. Stale/disconnected
readings are dimmed and retain their last values; animation follows received
samples only. A real, locally served Rexroth logo image heads the page; the
RexLink heading, overview title, BLE sample counter, and demo banners have
been removed. A small simulation footer remains, and connection/freshness
status sits beside the transport buttons. Routine live-data detail is hidden
while fresh; setup/fault messages remain visible. There are no external fonts,
scripts, or frontend dependencies. See [dashboard notes](ble/dashboard/README.md)
for logo provenance and connection diagnostics.

Local mocked-BLE tests verified numeric outputs (sample 30: 2150 RPM, 76.2 C,
199 bar), stale dimming, recovery, invalid responses, reconnect, and link loss.
Responsive checks found no horizontal overflow at 320, 390, or 768 pixels.
The user approved the final appearance in Bluefy. Explicit BLE acceptance
checks on this final layout remain pending; the physical transport results
below refer to the earlier counter-only design. The intervening fancy layout
had a reported connection hang; per-stage 12-second timeouts and retry
diagnostics were added, but the cause and physical resolution are unverified.

The machine target has no Wi-Fi; future HMI experiments use BLE. The Wi-Fi
proof is retained as historical evidence, not as the intended machine transport.

[BLE dashboard](ble/dashboard/index.html) is a standalone, simulated, read-only
Web Bluetooth page for the existing C# publisher. It reads and subscribes to
the same characteristic as nRF Connect; no write/command interface is added.
It shows CONNECTED, STALE after three seconds without an advancing value,
and DISCONNECTED after a link loss or invalid response. Reconnection is manual.
It does not mirror a Qt screen or implement the RemoteXY protocol.

Phone prerequisites:

- iPhone/iPad: a Web Bluetooth browser such as
  [Bluefy](https://apps.apple.com/us/app/id1492822055); Safari is unsupported.
- Android: a browser supporting Web Bluetooth, such as Chrome.
- The page must be delivered over HTTPS. The experimental dashboard is hosted
  at https://fkachinski.github.io/phoneHMI/ble/dashboard/ using GitHub Pages
  (deploy from `main`, repository root). Do not use the laptop's HTTP URL.
  Initial page delivery requires phone network access; counter data then travels
  locally over BLE, not Wi-Fi or the Internet. Offline startup is not implemented.

Run the BLE publisher, open the HTTPS dashboard on the phone, tap
**Connect**, and choose the laptop. Confirm advancing values for
30 seconds, disconnect/reconnect, and stop the publisher to check disconnection.
If notifications cease without a disconnection, the page must show STALE.
Keep the page foregrounded; background operation is not claimed.

Local browser validation (2026-11-16) used a mocked BLE device to exercise
reads, notifications, the three-second stale threshold, recovery, invalid
responses, link loss, manual disconnection, reconnection, chooser errors, and
unsupported-browser messaging. These checks passed.

Physical dashboard validation (2026-11-16): the user opened the GitHub Pages
dashboard in Bluefy on an iPhone 15 and confirmed an advancing counter,
automatic updates, and successful disconnect/reconnect with updates resuming.
The publisher was started with
`dotnet run --no-build --project ble/Counter.csproj -- --seconds 900`.
Telemetry used BLE; HTTPS was used only to deliver the page. Android, offline
startup, and physical dashboard stale/link-loss behavior remain unverified.
The earlier nRF Connect test was a separate transport test, not a dashboard test.

Local UI inspection only (not a phone BLE test):

```text
python -m http.server 8001 --bind 127.0.0.1 --directory ble/dashboard
```

Open http://localhost:8001/ and stop the server with Ctrl+C afterward.
The same laptop is not a substitute for testing a separate BLE central.

RemoteXY remains a candidate, but its
[documented library ecosystem](https://docs.remotexy.com/docs/) targets
microcontroller/Arduino-style boards. No documented ready-to-run Windows or
Qt/Linux BLE publisher was established in this investigation. This is not a
claim that a port is impossible. A port, extra hardware, or native phone app
would be a separate implementation choice. No device integration is included.
The separate Windows compatibility experiment above implements a limited
subset of that protocol. It is not the selected DI5 migration path.
Neither Windows publisher can be deployed to Linux unchanged.

### Existing simulator tests and publisher build

```text
python -m unittest discover -s wifi -v
dotnet build ble/Counter.csproj
dotnet run --project remotexy-tests/ProtocolTests.csproj
dotnet build remotexy/RemoteXY.csproj
```

Local tests/builds do **not** prove either physical iPhone connection.

Handoff validation (2026-11-16, personal Windows laptop, .NET SDK 10.0.404):
the commands above passed: six Wi-Fi tests, 31 RemoteXY protocol checks, and
both publishers built with zero warnings/errors. Final dashboard mocked-BLE
checks passed for numeric rendering, stale indication/recovery, manual
disconnect/reconnect and widths 320/390/768. No publishers or temporary HTTP
servers were left running at handoff.

Initial validation (2026-11-16): six Python tests passed and the .NET build
completed without warnings. A local browser showed an advancing counter,
STALE for a frozen response, DISCONNECTED for an invalid response or stopped
server, and recovery after valid responses resumed. The Windows BLE startup
test reported `Aborted` with error `Success` and exited nonzero. Those initial
checks did not verify physical-device connectivity; see the later results below.

Physical-device results (2026-11-16, personal Windows 11 Home laptop, Python
3.10.11, trusted home Wi-Fi, no VPN):

- **Wi-Fi proof: PASSED** on an iPad (7th gen, iPadOS 15.4.1, Safari) using
  `python wifi/server.py --host <laptop IPv4>`. Observed CONNECTED with the
  counter advancing past 175, DISCONNECTED after the server stopped, and STALE
  at 5 with `--freeze-after 5`. No firewall or network settings were changed.
- **Bluetooth proof: PASSED on iPhone 15 using nRF Connect.** The user confirmed
  connection, counter reads, automatic notification updates, and successful
  disconnection/reconnection with updates working again. Notifications were
  reported working across the walkthrough; no separately timed 30-second
  observation was recorded. Bluetooth on the iPad and Android remain untested.

Bluetooth follow-up (2026-11-16): .NET SDK 10.0.404 installed; after restarting
Windows, the adapter reported healthy and peripheral-role support. A bounded
diagnostic observed an `Aborted` event while the current provider state was
already `Started`, followed by ten seconds in `Started`. The publisher now
checks current state rather than treating delayed events as authoritative.
The updated publisher built without warnings or errors and completed
`dotnet run --project ble/Counter.csproj -- --seconds 12` with confirmed
advertising, an advancing local counter, and exit code 0.
The subsequent physical iPhone test is recorded above.

## Moving to another laptop / Copilot handoff

AI assistants should read [AGENTS.md](AGENTS.md) for boundaries and
[DI5-MIGRATION.md](DI5-MIGRATION.md) for the current ordered plan.

Clone this repository, open it in VS Code, and read the observed results above.
No previous chat or files are required. Tell the next session:

> Read README.md and AGENTS.md. Wi-Fi is historical; the intended transport is
> BLE. iPhone counter reads/notifications and Bluefy dashboard updates/reconnect
> have been observed on earlier versions. The approved visual direction is
> the custom Bluefy instrument dashboard with the actual logo. Read
> DI5-MIGRATION.md: qualify the actual DI5's peripheral/server APIs first,
> then prove the same simulated read/notify counter. DI4 central/client
> screenshots and BlueZ 5.50 are encouraging, not DI5 peripheral proof.
> Keep Android, offline startup, and unobserved acceptance checks pending.
> Do not copy company code here, integrate real telemetry, or change security
> settings. Record observed outcomes, not just successful builds.

Record the OS, test date, exact commands, errors, and which pass criteria were
observed. Redact network identifiers before sharing logs. Do not commit machine
data, company code, credentials, signing files, or internal diagnostics.
