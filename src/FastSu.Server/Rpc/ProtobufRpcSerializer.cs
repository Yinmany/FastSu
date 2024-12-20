using System.Buffers;
using System.IO.Pipelines;
using FastSu;

namespace FastSu.Server.Rpc;

public class ProtobufRpcSerializer : IRpcSerializer
{
    public void Serialize(IMessageBase msg, PipeWriter writer)
    {
        ProtoBuf.Serializer.Serialize(writer, msg);
    }

    public IMessageBase Deserialize(Type type, ReadOnlySequence<byte> data)
    {
        return (IMessageBase)ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(type, data);
    }
}