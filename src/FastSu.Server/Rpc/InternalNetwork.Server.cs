using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using Cysharp.Threading.Tasks;
using FastSu.Core;
using Microsoft.AspNetCore.Connections;

namespace FastSu.Server.Rpc;

partial class InternalNetwork
{
    // lengthField + serviceId + subId + msgId
    private const int MIN_SIZE = 4 + 8 + 8 + 4;

    private readonly IConnectionListenerFactory _connectionListenerFactory;
    private CancellationTokenSource? _stoppingCts;

    public async Task StartAsync(IPEndPoint bindIp, CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stoppingToken = _stoppingCts.Token;

        IConnectionListener listener = await _connectionListenerFactory.BindAsync(bindIp, stoppingToken);
        _serverLogger.Info($"Listen on {bindIp}");
        _ = RunAsync(listener, stoppingToken);
    }

    private async Task RunAsync(IConnectionListener listener, CancellationToken stoppingToken)
    {
        while (true)
        {
            ConnectionContext? connection = await listener.AcceptAsync(stoppingToken);
            if (connection is null)
                break;
            _ = ProcessIncoming(connection, stoppingToken);
        }

        _serverLogger.Info("Listen closed.");
    }

    private async Task ProcessIncoming(ConnectionContext connection, CancellationToken stoppingToken)
    {
        try
        {
            _serverLogger.Info($"Connected from {connection.RemoteEndPoint}");

            var input = connection.Transport.Input;
            while (true)
            {
                ReadResult readResult = await input.ReadAsync(stoppingToken);
                if (readResult.IsCanceled || readResult.IsCompleted)
                    break;

                var buffer = readResult.Buffer;
                while (TryParse(ref buffer, out NetMsg msg))
                {
                    try
                    {
                        OnMessage(in msg);
                    }
                    catch (Exception e)
                    {
                        _serverLogger.Error(e, "数据到达处理异常:");
                    }
                }

                input.AdvanceTo(buffer.Start, buffer.End);
            }

            _serverLogger.Info($"Disconnected from {connection.RemoteEndPoint}");
        }
        catch (Exception e)
        {
            _serverLogger.Error(e, "连接异常:");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private bool TryParse(ref ReadOnlySequence<byte> buffer, out NetMsg msg)
    {
        msg = default;
        if (buffer.Length < MIN_SIZE)
            return false;

        Span<byte> tmpSpan = stackalloc byte[MIN_SIZE];
        buffer.CopyTo(tmpSpan);

        // 消息体长度
        uint bodySize = BinaryPrimitives.ReadUInt32LittleEndian(tmpSpan);
        if (buffer.Length - MIN_SIZE < bodySize) // 消息体数据还不够
            return false;

        // 读取消息头
        long serviceId = BinaryPrimitives.ReadInt64LittleEndian(tmpSpan[4..]);
        long subId = BinaryPrimitives.ReadInt64LittleEndian(tmpSpan[12..]);
        int msgId = BinaryPrimitives.ReadInt32LittleEndian(tmpSpan[20..]);

        // 消息体
        var bodyBuffer = buffer.Slice(MIN_SIZE, bodySize);
        buffer = buffer.Slice(bodySize + MIN_SIZE);

        // 反序列化
        Type? type = MessageTypes.Ins.GetById(msgId);
        if (type is null)
        {
            _serverLogger.Error($"无法解析消息,MsgId对应类型不存在: {msgId}");
            return false;
        }

        IMessageBase body = _serializer.Deserialize(type, bodyBuffer);
        msg = new NetMsg(serviceId, body, subId);
        return true;
    }

    private void OnMessage(in NetMsg msg)
    {
        // 响应消息的处理
        if (msg.Body is IResponse response)
        {
            if (_rpcCallbacks.TryRemove(response.RpcId, out ResponseTcs? tcs))
            {
                tcs.SetResult(response);
            }

            return;
        }

        Did idInfo = new Did(msg.ServiceId);
        Did dstId = new Did(idInfo.Time, Did.CurPid, idInfo.Seq);
        ServiceId serviceId = new ServiceId(dstId);
        if (msg.Body is IRequest request)
        {
            HandlerRequest(idInfo.Pid, serviceId, request, msg.SubId).Forget();
            return;
        }

        serviceId.Send((IMessage)msg.Body, msg.SubId);
    }

    private async UniTask HandlerRequest(ushort srcPid, ServiceId serviceId, IRequest request, long subId)
    {
        // 异步捕获srcPid与rpcId用于响应
        int rpcId = request.RpcId;
        IResponse response = await serviceId.Call(request, subId);
        response.RpcId = rpcId;
        Send(srcPid, response);
    }
}