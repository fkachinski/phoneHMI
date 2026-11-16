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

Earlier experiments on a managed Windows laptop did not establish a physical
phone connection. Local HTTP worked, but LAN access failed; connectable Windows
BLE advertising also failed. The exact causes were not established. A personal
laptop is the next test environment, not a guaranteed fix. Its OS and Bluetooth
capabilities still need to be checked.

## Current scope

- Wi-Fi proof: Python standard-library HTTP server, simulated counter, and
  Safari-compatible page. No third-party Python packages or frontend framework.
- BLE proof: independent Windows C# GATT peripheral with one read/notify-only
  simulated counter characteristic. The iPhone uses an existing BLE utility.
- No writes, remote controls, real telemetry, machine connection, native phone
  app, deployment packaging, or automatic system configuration.

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

## Evidence at the initial handoff (2026-11-16)

- Six Python tests passed.
- BLE build succeeded without warnings or errors.
- Local browser checks observed an advancing counter, stale detection,
  rejection of an invalid response, reconnection, and disconnection after the
  server stopped.
- The BLE runtime test on the original laptop reported advertising `Aborted`
  with error `Success` and exited nonzero.
- Neither physical iPhone pass criterion has been verified.

A passing build, adapter capability report, or local browser check is not a
phone connectivity result. Do not describe the Wi-Fi or BLE proof as complete
until its physical-phone acceptance test passes.

## Next steps, in order

1. Inspect this checkout and the personal laptop's OS and available runtimes.
   Preserve existing user changes. Do not assume the previous laptop's setup.
2. Run the existing Wi-Fi unit tests, then run the server on loopback and open
   the page locally. Use README commands; no dependency installation is needed
   if Python is already available.
3. Bind to the laptop's actual Wi-Fi IPv4 address on a trusted private network.
   Open the printed URL in iPhone Safari. Observe changing values for at least
   30 seconds; verify stale detection and server-stop disconnection.
4. On Windows, build and run the independent BLE publisher. Wait for successful
   advertising, then use an iPhone BLE utility to connect, read, and subscribe.
   Observe notifications for at least 30 seconds and verify reconnecting.
   On another OS, report the platform limitation before proposing a different
   publisher; do not silently replace the implementation.
5. Record what actually happened: OS, runtime versions, date, exact commands,
   error/status text, and observed acceptance criteria. Redact network/device
   identifiers. Keep unverified items explicitly pending.
6. Only after transport evidence, discuss the next increment with the user.
   Do not automatically start machine integration or select a phone framework.

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

The intended remote is https://github.com/fkachinski5/phoneHMI.git.
At this handoff, the starter implementation was committed locally as `87e5ae5`,
but pushing was rejected with HTTP 403 because the authenticated account lacked
write permission. Do not assume this commit or this document is on GitHub:
check current Git status and remote history.

Use the normal GitHub authorization flow or an explicitly granted repository
permission to resolve access. Never request tokens/passwords in chat, reuse
unrelated credentials, or bypass access controls. Verify a successful push and
a clean worktree before telling the user the other laptop can pull the work.
