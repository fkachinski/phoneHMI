# Bluefy connection diagnostics

The dashboard's provisional name is RexLink. Its red/blue styling is a concept,
not a claim of an approved corporate interface. The actual logo image is served
locally as `rexroth-logo.gif`, downloaded unchanged on 2026-11-16 from the
Bosch Rexroth website's own asset:
https://dc-mkt-prod.cloud.bosch.tech/xrm/media/tech/img/rexroth_logo_animated_300.gif
The source was referenced by https://www.boschrexroth.com/en/us/ .
It is a 300 x 122 GIF, not HTML text or a recreated logo. No runtime request to
the corporate website is needed to display it.

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
