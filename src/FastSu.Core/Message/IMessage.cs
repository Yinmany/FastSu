namespace FastSu.Core;

public interface IMessageBase
{
    public int MsgId { get; }
}

public interface IMessage : IMessageBase
{
}

public interface IRequest : IMessage
{
    public int RpcId { get; set; }
}

public interface IResponse : IMessage
{
    public int RpcId { get; set; }
    public int ErrCode { get; set; }
    public string ErrMsg { get; set; }
}