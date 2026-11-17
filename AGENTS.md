# AI assistant handoff

## Context and goal

The user wants to explore using a phone as a read-only HMI for an existing
embedded application. Start by proving communication with simulated data on a
laptop, without touching the actual application or machine. The first phone is
an iPhone; Android is a later goal.

This repository is a fresh, standalone experiment intended to move between
computers through GitHub. It deliberately contains no company source, machine
data, or internal environment details. No previous chat or external project
folder is needed to run it.

The personal Windows laptop has now demonstrated Wi-Fi on iPad and BLE
read/notify/reconnect on iPhone, including the earlier Bluefy counter page.
The user approved the custom logo/instrument dashboard's appearance.
Read [README.md](README.md) for evidence and pending acceptance checks and
[DI5-MIGRATION.md](DI5-MIGRATION.md) for the current migration gates.

## Current scope

- Wi-Fi proof: Python standard-library HTTP server, simulated counter, and
  Safari-compatible page. No third-party Python packages or frontend framework.
- BLE proof: independent Windows C# read/notify counter; iPhone uses nRF
  Connect or the custom Bluefy Web Bluetooth dashboard.
- RemoteXY: separate Windows compatibility experiment with protocol writes
  for layout/value requests, zero input variables and no machine commands.
- Next: qualify actual DI5 peripheral/GATT-server capabilities, then prove a
  standalone simulated publisher before separately scoped application work.
- No remote controls, real telemetry, machine connection, native phone app,
  deployment packaging, or automatic system configuration.

Read [README.md](README.md) for prerequisites, exact commands, UUIDs, pass
criteria, and cleanup. The Wi-Fi proof supports Python 3.9+ on Windows, macOS,
and Linux. The current BLE publisher requires Windows and the .NET 10 SDK.
Do not assume it works on macOS/Linux.

## Repository map

- [wifi/server.py](wifi/server.py): loopback-by-default server and counter;
  accepts a local bind address and optional freeze time.
- [wifi/index.html](wifi/index.html): counter and connected/stale/disconnected UI.
- [wifi/test_server.py](wifi/test_server.py): focused simulator and HTTP tests.
- [ble/Program.cs](ble/Program.cs): read/notify counter and bounded advertising
  startup; Ctrl+C stops it.
- [ble/Counter.csproj](ble/Counter.csproj): Windows .NET project.
- [ble/dashboard/index.html](ble/dashboard/index.html): preferred custom UI,
  actual locally served logo, instruments derived from the simulated counter.
- [ble/dashboard/README.md](ble/dashboard/README.md): connection diagnostics.
- [remotexy/](remotexy/): alternative Windows RemoteXY compatibility publisher.
- [remotexy-tests/](remotexy-tests/): package-free protocol tests.
- [DI5-MIGRATION.md](DI5-MIGRATION.md): work-laptop handoff and target gates.

## Evidence at the current handoff (2026-11-16)

- Six Python tests and 31 RemoteXY protocol checks passed; both Windows
  publishers built without warnings/errors.
- Wi-Fi advancing/stale/disconnected UI observed on iPad Safari.
- iPhone nRF Connect counter reads/notifications and manual reconnect observed;
  original Bluefy counter updates/reconnect observed.
- RemoteXY text reconnect and subsequent gauge rendering/updates observed.
- Final custom dashboard appearance approved on phone; mocked BLE layout,
  freshness, recovery and reconnect checks passed locally. Final-layout
  physical BLE acceptance and an earlier reported connection hang remain
  unresolved by explicit observation.
- Historical DI4 screenshots reportedly show hci0, iPhone discovery and
  iPhone service enumeration, apparently on BlueZ 5.50. This is central/client
  evidence only, not DI5 peripheral/server proof.
- Android, offline startup and all actual DI5 migration gates remain pending.

A passing build, adapter capability report, or local browser check is not a
phone connectivity result. Keep unobserved timed/freshness checks explicit.

## Next steps, in order

1. Inspect the work checkout and runtimes; preserve user changes. Follow
   [DI5-MIGRATION.md](DI5-MIGRATION.md), not the old Wi-Fi-first sequence.
2. Inventory the actual DI5 through approved access. Confirm local GATT and
   advertising managers, controller support, permissions and toolchain.
   Do not infer DI5 capability from DI4 or BlueZ's version alone.
3. Qualify a standalone simulated read/notify peripheral with the same UUIDs
   and payload as the Windows counter. Choose Qt/backend only after inventory.
4. Verify actual phone reads, timed notifications, repeated reconnect, stale
   detection and publisher-stop behavior before application integration.
5. Keep real telemetry and company source outside this repository. Later
   integration needs a separately agreed contract and authorized work scope.
6. Record redacted facts, commands and observed results. Stop temporary
   services and advertising after tests; do not change managed settings.

If a test fails, isolate the smallest failing layer before changing code.
For Wi-Fi, distinguish loopback, LAN access, and phone display. For BLE,
distinguish adapter reports, connectable advertising, discovery, reading, and
notifications. Do not spend an unbounded session repeating unsuccessful
Windows packaging or configuration experiments.

## Boundaries for future work

- Keep all published data simulated and clearly labeled. No command interfaces.
- Do not copy company code, internal docs, real data, certificates, credentials,
  or network diagnostics into this repository.
- These proofs have no authentication or encryption. Trusted local experiments
  only; no Internet exposure, port forwarding, or real-machine deployment.
- Do not disable protections or change VPN routes, managed settings, drivers,
  certificate trust, or firewall rules automatically.
- Prefer small changes and existing tools. Validate changed behavior with the
  focused tests/build and, when needed, actual phone observations.
- Stop temporary servers and advertising when finished. Do not leave services
  running or alter the machine without clear user approval.
- Any later embedded publisher should expose selected decoded state read-only,
  with freshness information. Do not recreate machine protocol decoding or
  introduce control paths in this experiment. Such integration requires a
  separate approved scope; the necessary source is not in this repository.

## Git handoff

The intended remote is https://github.com/fkachinski/phoneHMI.git.
An earlier destination under a different account rejected pushes; the user
subsequently created this repository under the account available to Git.
Check current Git status and remote history rather than assuming all local
commits have been published.

Use the normal GitHub authorization flow or an explicitly granted repository
permission to resolve access. Never request tokens/passwords in chat, reuse
unrelated credentials, or bypass access controls. Verify a successful push and
a clean worktree before telling the user the other laptop can pull the work.
