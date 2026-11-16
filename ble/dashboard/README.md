# Bluefy connection diagnostics

After selecting a device, the page displays the current GATT connection step:
connection, service discovery, characteristic discovery, notification setup,
or initial read. Each step times out after 12 seconds and enables retry.
Disconnect can cancel setup after device selection. A late connection result
from a cancelled attempt is disconnected instead of used.

If setup fails, record the displayed step and error. Verify the original
`ble/Counter.csproj` publisher is running, not the independent RemoteXY
publisher. Disconnect other phone BLE clients before retrying. Do not change
firewall or pairing settings to troubleshoot a GATT discovery failure.

Local mocked-BLE regression checks covered a never-resolving connection,
timeout messaging, retry, instrument rendering, and manual disconnect.
These checks do not establish the cause of a physical-phone connection hang.
