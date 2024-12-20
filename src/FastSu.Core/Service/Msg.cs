using System.Runtime.InteropServices;

namespace FastSu;

/// <summary>
/// 消息(总共16个字节)
/// </summary>
[StructLayout(LayoutKind.Auto, Pack = 4)]
public readonly struct Msg(uint id, object body, long subId = 0)
{
    public readonly uint Id = id;
    public readonly object Body = body;
    public readonly long SubId = subId;

    public override string ToString()
    {
        return $"{Id} {Body} {SubId}";
    }
}