using System;
using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Serialization;

namespace TheOtherRoles.Networking;

[MessageConverter]
public class ByteArrayConverter : MessageConverter<byte[]>
{
    public override void Write(MessageWriter writer, byte[] value)
    {
        writer.WriteBytesAndSize(value);
    }

    public override byte[] Read(MessageReader reader, Type objectType)
    {
        return reader.ReadBytesAndSize();
    }
}
