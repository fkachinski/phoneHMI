using System.Diagnostics;
using System.Threading.Channels;
using PhoneHmi.RemoteXY;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

int seconds = 900;
if (args.Length != 0 && (args.Length != 2 || args[0] != "--seconds"
    || !int.TryParse(args[1], out seconds) || seconds is < 1 or > 86400))
{
    Console.Error.WriteLine("Usage: dotnet run --project remotexy\\RemoteXY.csproj -- [--seconds 1..86400]");
    return 2;
}

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
try
{
    return await RunAsync(seconds, stop);
}
catch (Exception error)
{
    Console.Error.WriteLine($"RemoteXY experiment failed: {error}");
    return 1;
}

static async Task<int> RunAsync(int seconds, CancellationTokenSource stop)
{
    var adapter = await BluetoothAdapter.GetDefaultAsync();
    if (adapter is null || !adapter.IsLowEnergySupported || !adapter.IsPeripheralRoleSupported)
        throw new InvalidOperationException("No BLE adapter reporting peripheral-role support.");
    var result = await GattServiceProvider.CreateAsync(Guid.Parse("0000ffe0-0000-1000-8000-00805f9b34fb"));
    if (result.Error != BluetoothError.Success)
        throw new InvalidOperationException($"Service creation failed: {result.Error}");
    var provider = result.ServiceProvider;
    var created = await provider.Service.CreateCharacteristicAsync(
        Guid.Parse("0000ffe1-0000-1000-8000-00805f9b34fb"),
        new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify
                | GattCharacteristicProperties.WriteWithoutResponse | GattCharacteristicProperties.Write,
            ReadProtectionLevel = GattProtectionLevel.Plain,
            WriteProtectionLevel = GattProtectionLevel.Plain
        });
    if (created.Error != BluetoothError.Success)
        throw new InvalidOperationException($"Characteristic creation failed: {created.Error}");
    var characteristic = created.Characteristic;
    var incoming = Channel.CreateBounded<(int Session, byte[]? Bytes)>(32);
    int session = 0;
    var clock = Stopwatch.StartNew();
    int failed = 0;
    void Fail(Exception error)
    {
        Console.Error.WriteLine($"RemoteXY BLE failure: {error}");
        Interlocked.Exchange(ref failed, 1);
        stop.Cancel();
    }
    characteristic.WriteRequested += async (_, e) =>
    {
        int requestSession = Volatile.Read(ref session);
        var deferral = e.GetDeferral();
        try
        {
            var request = await e.GetRequestAsync();
            if (request is null) throw new IOException("Write request expired.");
            if (request.Offset != 0) throw new InvalidDataException("Offset writes are unsupported.");
            var clients = characteristic.SubscribedClients;
            if (clients.Count != 1 || clients[0].Session.DeviceId.Id != e.Session.DeviceId.Id)
                throw new IOException("Only the subscribed phone may send protocol requests.");
            using var reader = DataReader.FromBuffer(request.Value);
            byte[] bytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(bytes);
            if (!incoming.Writer.TryWrite((requestSession, bytes)))
                throw new IOException("RemoteXY request queue overflow.");
            if (request.Option == GattWriteOption.WriteWithResponse) request.Respond();
        }
        catch (Exception error) when (requestSession != Volatile.Read(ref session)
            || characteristic.SubscribedClients.Count == 0)
        {
            Console.WriteLine($"Discarding request from disconnected phone: {error.Message}");
        }
        catch (Exception error) { Fail(error); }
        finally { deferral.Complete(); }
    };
    characteristic.ReadRequested += async (_, e) =>
    {
        var deferral = e.GetDeferral();
        try
        {
            var request = await e.GetRequestAsync();
            if (request is null) throw new IOException("Read request expired.");
            using var writer = new DataWriter();
            request.RespondWithValue(writer.DetachBuffer());
        }
        catch (Exception error) { Fail(error); }
        finally { deferral.Complete(); }
    };
    characteristic.SubscribedClientsChanged += (sender, _) =>
    {
        int nextSession = Interlocked.Increment(ref session);
        Console.WriteLine($"RemoteXY subscribers: {sender.SubscribedClients.Count}");
        if (sender.SubscribedClients.Count > 1)
            Fail(new InvalidOperationException("This experiment supports one phone at a time."));
        if (!incoming.Writer.TryWrite((nextSession, null)))
            Fail(new IOException("Unable to queue RemoteXY session reset."));
    };
    provider.AdvertisementStatusChanged += (sender, e) =>
        Console.WriteLine($"Advertisement event: {e.Status}; error: {e.Error}; current: {sender.AdvertisementStatus}");
    Console.WriteLine("SIMULATED RemoteXY compatibility experiment. No machine connection or controls.");
    Console.WriteLine("Public, unencrypted BLE. Protocol requests use writes; no input variables exist.");
    Console.WriteLine($"Stops after {seconds} seconds of advertising. Ctrl+C to stop.");
    try
    {
        provider.StartAdvertising(new GattServiceProviderAdvertisingParameters
        {
            IsConnectable = true, IsDiscoverable = true
        });
        var startup = Stopwatch.StartNew();
        while (!Advertising(provider))
        {
            if (startup.Elapsed >= TimeSpan.FromSeconds(10))
                throw new TimeoutException($"Advertising startup timed out: {provider.AdvertisementStatus}");
            await Task.Delay(100, stop.Token);
        }
        Console.WriteLine("RemoteXY advertising confirmed. Use the RemoteXY app, not nRF Connect or Bluefy.");
        stop.CancelAfter(TimeSpan.FromSeconds(seconds));
        var protocol = new Protocol(() => (long)clock.Elapsed.TotalSeconds, Console.WriteLine);
        int protocolSession = -1;
        while (true)
        {
            stop.Token.ThrowIfCancellationRequested();
            if (!Advertising(provider))
                throw new IOException($"Advertising is no longer active: {provider.AdvertisementStatus}");
            if (incoming.Reader.TryRead(out var item))
            {
                int currentSession = Volatile.Read(ref session);
                if (protocolSession != currentSession)
                {
                    protocol.Reset();
                    protocolSession = currentSession;
                    Console.WriteLine("RemoteXY protocol session reset.");
                }
                if (item.Bytes is null || item.Session != currentSession
                    || characteristic.SubscribedClients.Count == 0)
                    continue;
                var clients = characteristic.SubscribedClients;
                if (clients.Count != 1)
                    throw new IOException("RemoteXY supports only one subscribed phone.");
                var client = clients[0];
                bool SessionEnded() => currentSession != Volatile.Read(ref session)
                    || characteristic.SubscribedClients.Count == 0;
                foreach (var response in protocol.Receive(item.Bytes))
                {
                    for (int offset = 0; offset < response.Length; offset += 20)
                    {
                        if (SessionEnded())
                        {
                            Console.WriteLine("Phone disconnected; discarding pending reply.");
                            break;
                        }
                        using var writer = new DataWriter();
                        writer.WriteBytes(response.AsSpan(offset, Math.Min(20, response.Length - offset)).ToArray());
                        GattClientNotificationResult delivery;
                        try
                        {
                            delivery = await characteristic.NotifyValueAsync(writer.DetachBuffer(), client);
                        }
                        catch (Exception error) when (SessionEnded())
                        {
                            Console.WriteLine($"Phone disconnected during reply: {error.Message}");
                            break;
                        }
                        if (delivery.Status != GattCommunicationStatus.Success)
                        {
                            if (SessionEnded() || delivery.Status == GattCommunicationStatus.Unreachable)
                            {
                                Console.WriteLine($"Phone unavailable during reply ({delivery.Status}); waiting for reconnect.");
                                protocol.Reset();
                                break;
                            }
                            throw new IOException($"RemoteXY notification delivery failed: {delivery.Status}");
                        }
                        await Task.Delay(10, stop.Token);
                    }
                    if (SessionEnded()) break;
                }
            }
            else await Task.Delay(20, stop.Token);
        }
    }
    catch (OperationCanceledException) when (stop.IsCancellationRequested)
    {
        return Volatile.Read(ref failed) == 0 ? 0 : 1;
    }
    finally
    {
        provider.StopAdvertising();
        Console.WriteLine("RemoteXY advertising stopped. Phone rendering and reconnect still require observation.");
    }
}

static bool Advertising(GattServiceProvider provider) =>
    provider.AdvertisementStatus is GattServiceProviderAdvertisementStatus.Started
        or GattServiceProviderAdvertisementStatus.StartedWithoutAllAdvertisementData;
