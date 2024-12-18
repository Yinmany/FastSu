using FastSu.Core;

namespace FastSu.Tests;

class TestMsg : IMessage
{
    public int MsgId => 1;
}

class TestReq : IRequest
{
    public int MsgId => 2;
    public int RpcId { get; set; }
}

class TestAck : IResponse
{
    public int MsgId => 3;
    public int RpcId { get; set; }
    public int ErrCode { get; set; }
    public string ErrMsg { get; set; }
}