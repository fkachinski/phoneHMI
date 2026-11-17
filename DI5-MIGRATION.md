# DI5 BLE HMI migration plan

Updated 2026-11-16. Planning and capability qualification only; no DI5
implementation, access, or configuration change has occurred.

## Direction and evidence

Use the custom [instrument dashboard](ble/dashboard/index.html) in Bluefy on
iPhone first; Android Chrome follows later. Preserve the existing BLE contract
so the phone page does not need a rewrite when the publisher moves off Windows.
RemoteXY remains a separate experiment, not a dependency.

| Evidence | What it establishes | What it does not establish |
|---|---|---|
| Windows publisher with iPhone nRF Connect and earlier Bluefy page | Counter reads, notifications and manual reconnect were observed | DI5 support, production readiness |
| User approval of final logo/instrument screen | Visual direction accepted on phone | Final-layout BLE acceptance or resolution of earlier connection hang |
| User-reported historical DI4 screenshots: `hci0`, iPhone discovery, iPhone service enumeration; apparently BlueZ 5.50 | Adapter present and central/GATT-client operation on that DI4 at that time | LE peripheral advertising, local GATT-server support, or any DI5 capability |
| BlueZ 5.50 upstream API documentation | Local GATT registration and peripheral advertisement APIs exist in that release | Enabled vendor build, controller/driver support, available resources or permissions |

The role must reverse: **DI5 advertises and serves; phone connects and reads**.
`hci0` is an adapter identifier, not a peripheral capability flag.

References for the exact upstream release:

- [BlueZ 5.50 GATT API](https://github.com/bluez/bluez/blob/5.50/doc/gatt-api.txt):
  `org.bluez.GattManager1.RegisterApplication`.
- [BlueZ 5.50 advertising API](https://github.com/bluez/bluez/blob/5.50/doc/advertising-api.txt):
  `org.bluez.LEAdvertisingManager1.RegisterAdvertisement`, advertisement
  `Type = "peripheral"`, supported/active instance counts.

The basic APIs are documented without an experimental marker; some optional
advertisement properties are experimental in 5.50. Do not enable experimental
daemon options or upgrade BlueZ as the first troubleshooting step.

## Gate 1: work-laptop and actual DI5 inventory

Pull this repository normally; preserve any existing work-laptop changes.
Do not assume DI4 and DI5 share firmware, radio, drivers or software packages.
With approved target access, establish:

- Exact target model, OS/kernel, architecture, BlueZ runtime version and adapter
  capabilities. Confirm LE advertising, not just scanning or classic Bluetooth.
- Whether `org.bluez.GattManager1` and `org.bluez.LEAdvertisingManager1` are
  exposed on the actual adapter, supported/active advertising instance counts,
  and whether another application already owns radio resources.
- System D-Bus access permissions, existing Bluetooth users/services, and any
  approved restrictions on running a separate temporary publisher.
- Qt version/modules, available Qt Bluetooth peripheral backend, Qt D-Bus,
  cross-compiler/sysroot and approved deployment/debug workflow.
- Phone/browser versions and an HTTPS route to load the page. Machine Wi-Fi is
  not required; phone network access is needed for initial page delivery.
  Offline startup remains unimplemented.

Example read-only Linux commands, **on the target**, only if already available:

```sh
uname -srmo
bluetoothctl --version
bluetoothctl show
btmgmt info
busctl --system introspect org.bluez /org/bluez/hci0 org.bluez.GattManager1
busctl --system introspect org.bluez /org/bluez/hci0 org.bluez.LEAdvertisingManager1
```

Replace `hci0` with the actual adapter path. These are inspection commands,
not instructions to change power, pairing, advertising or configuration.
Missing commands/access-denied responses are evidence to record, not a reason
to install tools or elevate automatically. Use an existing D-Bus inspection
tool if `busctl` is unavailable. `btmgmt info` capability flags and advertised
D-Bus interfaces are prerequisites, not the acceptance test.

Inspect diagnostics locally. Outputs can contain device addresses, names and
internal details: retain only a redacted capability summary in this public
repository, not screenshots or raw target logs.

**Exit:** actual DI5 facts known, both manager APIs present, and an approved
way to build/run a temporary application. If a prerequisite is missing, isolate
that layer and stop before proposing firmware, policy or hardware changes.

## Gate 2: standalone simulated DI5 peripheral

Choose the smallest supported implementation after inventory: Qt Bluetooth
peripheral API if the installed version/backend supports it, or direct BlueZ
D-Bus from a standalone C++/Qt application. Do not select a Qt API based only
on its existence in newer desktop Qt documentation. The Windows C#/WinRT
publisher is a behavioral reference, not a Linux binary to copy.

Keep the existing contract:

- Service UUID: `7f4a0001-6f9c-47ac-9f65-1c4db9870021`.
- Characteristic UUID: `7f4a0002-6f9c-47ac-9f65-1c4db9870021`.
- Read/notify only; plain decimal UTF-8/ASCII increasing counter, once per
  second, matching [the Windows publisher](ble/Program.cs).
- Connectable LE advertisement; request `Type = "peripheral"` with the
  service UUID when using BlueZ directly. Keep advertising data within limits.
- With direct D-Bus, export the GATT hierarchy and ObjectManager, register it
  with GattManager1, implement reads and notification subscription lifecycle,
  and emit value updates only while subscribed.
- Explicit error/status reporting, bounded startup and runtime, unregister
  advertisement/application on exit. Verify restart and reconnect.
- No writes to application values, commands, real telemetry or Qt application
  integration. Normal BLE subscription configuration is not machine control.

Do not alter daemon flags, D-Bus policy, permissions, drivers or existing
services silently. If access requires a change, report the exact blocker for
the responsible team. Use an approved bench setup with no machine actuation.

**Exit:** successful registration and actual advertising. This alone is not a
phone transport pass.

## Gate 3: phone acceptance on DI5

1. In nRF Connect, discover the DI5, connect, find the UUIDs, read decimal text,
   subscribe and observe advancing notifications for at least 30 seconds.
2. Disconnect/reconnect twice, confirming reads and notifications each time.
3. Disconnect the utility. Load the published dashboard in Bluefy, select DI5
   and confirm advancing instruments for at least 30 seconds. The existing
   page scans without a service filter because Windows sometimes omits UUIDs.
4. Freeze the simulated counter: after three seconds verify STALE, dimmed
   readings and a visible warning; resume and verify recovery.
5. Stop the publisher and verify disconnection/non-live indication; restart
   and manually reconnect. Record any GATT stage/error from the page.
6. Confirm cleanup: no advertisement or temporary publisher left behind.

Keep the page foregrounded. Record date, redacted platform/runtime versions,
exact commands, error/status text, timed observations and pending items.
Do not equate screenshots, successful builds or API enumeration with passing
these checks. Android is a separate subsequent acceptance run.

## Gate 4: separately scoped application integration

Only after Gate 3, agree on the real telemetry contract and integration
location in the appropriate authorized work repository, not this public one.
Expose selected **already decoded** Qt application state read-only; do not
reimplement machine protocol decoding or add control paths.

The current page derives all three instruments from one simulated counter.
Real RPM/temperature/pressure require a new versioned contract and corresponding
phone changes, including units, validity, sequence/source freshness and
disconnect behavior. Define security/access requirements and review,
reconnection, lifecycle/resource impact, background behavior, offline delivery
and deployment before calling anything production-ready. Never label frozen
source data fresh merely because Bluetooth notifications continue.

## Handoff status

- Personal-laptop demo and preferred visual design: recorded in
  [README](README.md); current dashboard published on GitHub Pages.
- Work-laptop setup and Gates 1-4: pending.
- No DI4/DI5 source, internal diagnostics, credentials or real machine data
  are needed in this repository.
