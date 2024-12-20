using ProtoBuf;
using FastSu;
using System;

namespace HelloServer
{
    
    [ProtoContract]
    public partial class PingMsg : IMessage
    {
        public const int _MsgId_ = 1;
        public int MsgId => _MsgId_;
    
    }
    
    [ProtoContract]
    public partial class LoginReq : IRequest
    {
        public const int _MsgId_ = 2;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        /// <summary> 账号 </summary>
        [ProtoMember(2)] public string Account { get; set; }
        /// <summary> 密码 </summary>
        [ProtoMember(3)] public string Pwd { get; set; }
    }
    
    [ProtoContract]
    public partial class LoginAck : IResponse
    {
        public const int _MsgId_ = 3;
        public int MsgId => _MsgId_;
        [ProtoMember(1)] public int RpcId { get; set; }
        [ProtoMember(2)] public int ErrCode { get; set; }
        [ProtoMember(3)] public string ErrMsg { get; set; }
        [ProtoMember(4)] public string Token { get; set; }
        [ProtoMember(5)] public string ServerAddr { get; set; }
        public enum Result
        {
            Success = 0,
        }
    
    }
    
}