# phoneHMI: two independent starter proofs

Simulated counters only. No machine connection, machine source code, commands,
or real telemetry. The two proofs do not depend on one another.

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

```text
python -m unittest discover -s wifi -v
dotnet build ble/Counter.csproj
```

Local tests/builds do **not** prove either physical iPhone connection.

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

AI assistants should read [AGENTS.md](AGENTS.md) for project context, boundaries,
current evidence, and the ordered next steps.

Clone this repository on the personal laptop, open it in VS Code, and start
with proof 1. No previous chat or files are required. Tell the next session:

> Read README.md. Help me run the Wi-Fi counter on this laptop and verify it
> in Safari on my iPhone. Then, if this is Windows, try the independent BLE
> counter. Do not add machine integration, frameworks, packaging, certificates,
> or change managed network/security settings. Record observed outcomes, not
> just successful builds.

Record the OS, test date, exact commands, errors, and which pass criteria were
observed. Redact network identifiers before sharing logs. Do not commit machine
data, company code, credentials, signing files, or internal diagnostics.
