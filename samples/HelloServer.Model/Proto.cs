using FastSu.Core;
using ProtoBuf;

namespace HelloServer;

[ProtoContract]
public class PingMsg : IMessage
{
    public const int _MsgId_ = 1;
    public int MsgId => _MsgId_;
}