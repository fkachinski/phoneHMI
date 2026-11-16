using System.Globalization;
using System.Diagnostics;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

int? seconds = null;
if (args.Length != 0)
{
    if (args.Length != 2 || args[0] != "--seconds"
        || !int.TryParse(args[1], out int value) || value <= 0 || value > 86400)
    {
        Console.Error.WriteLine("Usage: dotnet run --project ble/Counter.csproj -- [--seconds 1..86400]");
        return 2;
    }
    seconds = value;
}

try
{
    return await RunAsync(seconds);
}
catch (Exception error)
{
    Console.Error.WriteLine($"BLE proof failed: {error}");
    return 1;
}

static async Task<int> RunAsync(int? seconds)
{
    const string serviceId = "7f4a0001-6f9c-47ac-9f65-1c4db9870021";
    const string characteristicId = "7f4a0002-6f9c-47ac-9f65-1c4db9870021";
    using var stop = new CancellationTokenSource();
    ConsoleCancelEventHandler cancel = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        stop.Cancel();
    };
    Console.CancelKeyPress += cancel;
    try
    {
        var adapter = await BluetoothAdapter.GetDefaultAsync();
        if (adapter is null || !adapter.IsLowEnergySupported || !adapter.IsPeripheralRoleSupported)
        {
            Console.Error.WriteLine("No BLE adapter reporting peripheral-role support.");
            return 1;
        }
        Console.WriteLine("Adapter reports peripheral support; a phone connection is still unproven.");
        var serviceResult = await GattServiceProvider.CreateAsync(Guid.Parse(serviceId));
        if (serviceResult.Error != BluetoothError.Success)
        {
            Console.Error.WriteLine($"Service creation failed: {serviceResult.Error}");
            return 1;
        }
        var provider = serviceResult.ServiceProvider;
        var characteristicResult = await provider.Service.CreateCharacteristicAsync(
            Guid.Parse(characteristicId), new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
                ReadProtectionLevel = GattProtectionLevel.Plain
            });
        if (characteristicResult.Error != BluetoothError.Success)
        {
            Console.Error.WriteLine($"Characteristic creation failed: {characteristicResult.Error}");
            return 1;
        }

        var characteristic = characteristicResult.Characteristic;
        long counter = 0;
        int failed = 0;
        IBuffer CounterBuffer()
        {
            using var writer = new DataWriter();
            writer.WriteBytes(Encoding.UTF8.GetBytes(
                Interlocked.Read(ref counter).ToString(CultureInfo.InvariantCulture)));
            return writer.DetachBuffer();
        }

        characteristic.ReadRequested += async (_, eventArgs) =>
        {
            var deferral = eventArgs.GetDeferral();
            try
            {
                var request = await eventArgs.GetRequestAsync();
                if (request is null)
                {
                    Console.Error.WriteLine("Read request expired before a response could be sent.");
                    return;
                }
                request.RespondWithValue(CounterBuffer());
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"Read failed: {error}");
                Interlocked.Exchange(ref failed, 1);
                stop.Cancel();
            }
            finally
            {
                deferral.Complete();
            }
        };
        characteristic.SubscribedClientsChanged += (sender, _) =>
            Console.WriteLine($"Subscribers: {sender.SubscribedClients.Count}");

        provider.AdvertisementStatusChanged += (sender, eventArgs) =>
        {
            Console.WriteLine($"Advertisement event: {eventArgs.Status}; error: {eventArgs.Error}; current: {sender.AdvertisementStatus}");
        };

        Console.WriteLine("SIMULATED, public, unencrypted, read/notify only. Ctrl+C to stop.");
        Console.WriteLine($"Service:        {serviceId}");
        Console.WriteLine($"Characteristic: {characteristicId}");
        try
        {
            provider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsConnectable = true,
                IsDiscoverable = true
            });
            // Event callbacks can arrive after the provider has already changed state.
            var startup = Stopwatch.StartNew();
            var status = provider.AdvertisementStatus;
            while (status is not (GattServiceProviderAdvertisementStatus.Started
                or GattServiceProviderAdvertisementStatus.StartedWithoutAllAdvertisementData))
            {
                if (startup.Elapsed >= TimeSpan.FromSeconds(10))
                    throw new TimeoutException();
                await Task.Delay(TimeSpan.FromMilliseconds(100), stop.Token);
                status = provider.AdvertisementStatus;
            }
            Console.WriteLine($"Advertising confirmed: {status}. A phone connection is still unproven.");
            if (status == GattServiceProviderAdvertisementStatus.StartedWithoutAllAdvertisementData)
                Console.WriteLine("Some advertising data omitted. Scan without a UUID filter.");
            if (seconds.HasValue)
                stop.CancelAfter(TimeSpan.FromSeconds(seconds.Value));
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stop.Token);
                status = provider.AdvertisementStatus;
                if (status is not (GattServiceProviderAdvertisementStatus.Started
                    or GattServiceProviderAdvertisementStatus.StartedWithoutAllAdvertisementData))
                {
                    Console.Error.WriteLine($"Advertising is no longer active: {status}");
                    return 1;
                }
                Interlocked.Increment(ref counter);
                if (characteristic.SubscribedClients.Count > 0)
                {
                    foreach (var result in await characteristic.NotifyValueAsync(CounterBuffer()))
                    {
                        if (result.Status != GattCommunicationStatus.Success)
                            Console.Error.WriteLine($"Notification delivery failed: {result.Status}");
                    }
                }
                Console.WriteLine($"Counter: {Interlocked.Read(ref counter)}");
            }
        }
        catch (TimeoutException)
        {
            Console.Error.WriteLine($"Advertising did not start within 10 seconds: {provider.AdvertisementStatus}");
            return 1;
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            return Volatile.Read(ref failed) == 0 ? 0 : 1;
        }
        finally
        {
            provider.StopAdvertising();
            Console.WriteLine("Advertising stopped. A phone read/notification test is required to prove BLE.");
        }
    }
    finally
    {
        Console.CancelKeyPress -= cancel;
    }
}
