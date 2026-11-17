using System.Buffers.Binary;

namespace PhoneHmi.RemoteXY;

// Protocol reference: RemoteXY Arduino library (MIT); see THIRD-PARTY-NOTICES.txt.
public sealed class Protocol(Func<long> seconds, Action<string> log)
{
    private readonly List<byte> pending = [];
    private bool connected;

    public void Reset()
    {
        pending.Clear();
        connected = false;
    }

    public IEnumerable<byte[]> Receive(byte[] bytes)
    {
        var responses = new List<byte[]>();
        foreach (byte value in bytes)
        {
            if (pending.Count == 0 && value != 0x55)
                throw new InvalidDataException("RemoteXY frame must start with 0x55.");
            pending.Add(value);
            if (pending.Count < 3) continue;
            int length = pending[1] | pending[2] << 8;
            if (length is < 6 or > 256)
                throw new InvalidDataException($"Unsupported RemoteXY frame length: {length}.");
            if (pending.Count < length) continue;
            byte[] frame = pending.ToArray();
            pending.Clear();
            if (Crc(frame) != 0)
                throw new InvalidDataException("RemoteXY CRC mismatch.");
            byte command = (byte)(frame[3] & 0xF1);
            if ((command & 1) != 0)
                throw new InvalidDataException("Fragmented RemoteXY requests are not supported.");
            int payloadLength = frame.Length - 6;
            log($"Request 0x{command:X2}, payload {payloadLength} bytes");
            if (command != 0 && !connected)
                throw new InvalidDataException("Request received before layout handshake.");
            long elapsed = seconds();
            byte[] output = Dashboard.Snapshot(elapsed);
            byte[] payload;
            switch (command)
            {
                case 0x00 when payloadLength == 0:
                    connected = true;
                    payload = [20, .. Dashboard.Configuration[7..]];
                    break;
                case 0x40 when payloadLength == 0:
                    payload = output;
                    break;
                case 0xC0 when payloadLength == 0:
                    payload = [0, .. output];
                    break;
                case 0x20 when payloadLength == 0:
                    payload = new byte[8];
                    BinaryPrimitives.WriteInt64LittleEndian(payload, checked(elapsed * 1000));
                    break;
                case 0x80 when payloadLength == 0:
                    // There are zero input variables; only an empty acknowledgement is allowed.
                    payload = [];
                    break;
                case 0x30 when payloadLength == 0:
                case 0xA0 when payloadLength == 0:
                    payload = [];
                    break;
                case 0xE0 when payloadLength == 0:
                    connected = false;
                    payload = [];
                    break;
                default:
                    throw new InvalidDataException($"Unsupported RemoteXY request 0x{command:X2} or payload. No controls are exposed.");
            }
            responses.Add(Frame((byte)(command | (frame[3] & 0x0E)), payload));
        }
        return responses;
    }

    public static byte[] Frame(byte command, byte[] payload)
    {
        byte[] frame = new byte[payload.Length + 6];
        frame[0] = 0x55;
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(1), checked((ushort)frame.Length));
        frame[3] = command;
        payload.CopyTo(frame, 4);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(frame.Length - 2), Crc(frame.AsSpan(0, frame.Length - 2)));
        return frame;
    }

    public static ushort Crc(ReadOnlySpan<byte> bytes)
    {
        ushort crc = 0xFFFF;
        foreach (byte value in bytes)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
                crc = (ushort)((crc >> 1) ^ ((crc & 1) != 0 ? 0xA001 : 0));
        }
        return crc;
    }
}
