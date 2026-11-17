using System.Text;
using System.Buffers.Binary;
using PhoneHmi.RemoteXY;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAIL: {name}");
    checks++;
    Console.WriteLine($"PASS: {name}");
}
void Reject(byte[] request, string name)
{
    try
    {
        new Protocol(() => 175, Console.WriteLine).Receive(request).ToArray();
    }
    catch (InvalidDataException) { Check(true, name); return; }
    throw new Exception($"FAIL: {name}");
}
Check(Protocol.Crc(Encoding.ASCII.GetBytes("123456789")) == 0x4B37, "standard Modbus CRC vector");
var protocol = new Protocol(() => 175, Console.WriteLine);
byte[] hello = Protocol.Frame(0, []);
Check(protocol.Receive(hello[..2]).Count() == 0, "partial frame buffered");
byte[] layout = protocol.Receive(hello[2..]).Single();
Check(Protocol.Crc(layout) == 0 && layout[4] == 20 && layout[5] == 19, "editor v19 layout and response CRC");
Check(Dashboard.Configuration.Length == 194
    && BinaryPrimitives.ReadUInt16LittleEndian(Dashboard.Configuration[5..]) == 187,
    "editor-generated configuration length");
Check(BinaryPrimitives.ReadUInt16LittleEndian(Dashboard.Configuration[1..]) == 0
    && BinaryPrimitives.ReadUInt16LittleEndian(Dashboard.Configuration[3..]) == Dashboard.OutputLength,
    "zero inputs and four float32 output variables");
Check(Dashboard.Configuration[18] == 5, "five editor-generated elements within free personal-test limit");
Check(layout.AsSpan(5, layout.Length - 7).SequenceEqual(Dashboard.Configuration[7..]),
    "send exact editor-generated GUI bytes, excluding library storage header");
byte[] values = protocol.Receive(Protocol.Frame(0x40, [])).Single();
Check(values.AsSpan(4, Dashboard.OutputLength).SequenceEqual(Dashboard.Snapshot(175)),
    "dashboard snapshot matches protocol values");
Check(Encoding.ASCII.GetString(layout).Contains("SIMULATED / READ ONLY"),
    "simulation label always visible");
Check(BinaryPrimitives.ReadSingleLittleEndian(values.AsSpan(4)) ==
    BinaryPrimitives.ReadSingleLittleEndian(values.AsSpan(16)), "digital RPM matches dial");
for (long second = 0; second <= 86400; second++)
{
    byte[] snapshot = Dashboard.Snapshot(second);
    float rpm = BinaryPrimitives.ReadSingleLittleEndian(snapshot);
    float temperature = BinaryPrimitives.ReadSingleLittleEndian(snapshot.AsSpan(4));
    float pressure = BinaryPrimitives.ReadSingleLittleEndian(snapshot.AsSpan(8));
    if (snapshot.Length != Dashboard.OutputLength
        || !float.IsFinite(rpm) || rpm is < 1450 or > 2150
        || !float.IsFinite(temperature) || temperature is < 66 or > 78
        || !float.IsFinite(pressure) || pressure is < 165 or > 205
        || BinaryPrimitives.ReadSingleLittleEndian(snapshot.AsSpan(12)) != rpm)
        throw new Exception($"Invalid dashboard snapshot at {second} seconds");
}
Check(true, "all 86401 runtime snapshots contain finite, in-range little-endian floats");
Check(!Dashboard.Snapshot(0).SequenceEqual(Dashboard.Snapshot(30)), "simulated instruments advance");
byte[] update = protocol.Receive(Protocol.Frame(0xC0, [])).Single();
Check(update.Length == Dashboard.OutputLength + 7 && update[4] == 0 && Protocol.Crc(update) == 0,
    "output flags and numeric dashboard frame size");
byte[] time = protocol.Receive(Protocol.Frame(0x20, [])).Single();
Check(time.Length == 14 && System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(time.AsSpan(4, 8)) == 175000
    && Protocol.Crc(time) == 0, "device time is signed 64-bit little-endian uptime milliseconds");
Check(protocol.Receive(Protocol.Frame(0xC0, [])).Count() == 1, "output polling continues after device time request");
Check(protocol.Receive([.. Protocol.Frame(0x40, []), .. Protocol.Frame(0xC0, [])]).Count() == 2,
    "coalesced frames");
byte[] client = protocol.Receive(Protocol.Frame(0x46, [])).Single();
Check(client[3] == 0x46, "client identifier preserved");
protocol.Receive(Protocol.Frame(0xE0, []));
Check(protocol.Receive(hello).Count() == 1, "handshake after disconnect");
protocol.Reset();
Check(protocol.Receive(hello).Count() == 1, "reset and reconnect");
protocol.Receive(hello[..3]);
protocol.Reset();
Check(protocol.Receive(hello).Count() == 1, "partial request discarded on session reset");
Check(protocol.Receive(Protocol.Frame(0x80, [])).Single().Length == 6, "empty input sync acknowledged");
foreach (byte command in new byte[] { 0x30, 0xA0 })
    Check(protocol.Receive(Protocol.Frame(command, [])).Single().Length == 6, $"empty metadata 0x{command:X2}");
byte[] broken = hello.ToArray();
broken[^1] ^= 1;
Reject(broken, "bad CRC rejected");
Reject([0x55, 0xFF, 0xFF], "oversized frame rejected");
Reject([0x55, 0x01, 0], "undersized frame rejected");
Reject([0x20], "non-frame bytes rejected");
Reject(Protocol.Frame(0x40, []), "values before handshake rejected");
Reject(Protocol.Frame(0, [1]), "password/control payload rejected");
Reject(Protocol.Frame(1, []), "fragmented request rejected");
try
{
    protocol.Receive(Protocol.Frame(0x80, [1]));
    throw new Exception("FAIL: control write accepted");
}
catch (InvalidDataException) { Check(true, "nonempty input writes rejected"); }
Console.WriteLine($"{checks} protocol checks passed. This does not verify RemoteXY phone rendering.");
