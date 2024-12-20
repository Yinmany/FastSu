using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using Cysharp.Threading.Tasks;
using FastSu;
using Microsoft.AspNetCore.Connections;

namespace FastSu.Server.Rpc;

public partial class InternalNetwork : IRpc
{
    private readonly SLogger _serverLogger = new SLogger("RpcServer");
    private readonly SLogger _clientLogger = new SLogger("RpcClient");

    private readonly IConnectionFactory _connectionFactory;
    private readonly IRpcSerializer _serializer;
    private readonly ConcurrentDictionary<ushort, IPEndPoint> _endPoints = new();
    private readonly ConcurrentDictionary<ushort, OneSender> _senders = new();
    private readonly ConcurrentDictionary<int, ResponseTcs> _rpcCallbacks = new();
    private int _rpcId;

    public InternalNetwork(IConnectionFactory connectionFactory, IConnectionListenerFactory connectionListenerFactory,
        IRpcSerializer serializer)
    {
        this._connectionFactory = connectionFactory;
        this._connectionListenerFactory = connectionListenerFactory;
        _serializer = serializer;
    }

    private OneSender GetSender(ushort pid, IPEndPoint ipEndPoint)
    {
        return _senders.GetOrAdd(pid, _ => new OneSender(pid, ipEndPoint, this));
    }

    /// <summary>
    /// 注册进程对应的连接地址
    /// </summary>
    /// <param name="pid"></param>
    /// <param name="ipEndPoint"></param>
    public void Register(ushort pid, IPEndPoint ipEndPoint)
    {
        _endPoints.AddOrUpdate(pid, ipEndPoint, (_, _) => ipEndPoint);
        _clientLogger.Info($"Register {pid} {ipEndPoint}");
    }

    /// <summary>
    /// 注销进程对应的连接地址
    ///     如果已经连接了，会把队列里面的消息发送完成，然后断开连接；
    /// </summary>
    /// <param name="pid"></param>
    public void Unregister(ushort pid)
    {
        if (_endPoints.TryRemove(pid, out IPEndPoint? ipEndPoint) && _senders.TryRemove(pid, out OneSender? sender))
        {
            // 释放连接，会把队列里面的消息发送完成
            sender.DisposeAsync().AsUniTask().Forget();
        }

        _clientLogger.Info($"Unregister {pid} {ipEndPoint}");
    }

    /// <summary>
    /// 向指定服务发送一个消息
    /// </summary>
    /// <param name="serviceId"></param>
    /// <param name="msg"></param>
    /// <param name="subId"></param>
    public void Send(long serviceId, IMessage msg, long subId = 0)
    {
        ushort pid = Did.GetPid(serviceId);
        if (!_endPoints.TryGetValue(pid, out IPEndPoint? ipEndPoint)) // 
        {
            _clientLogger.Warn($"Send pid {pid} not found. {msg.GetType().Name}");
            return;
        }

        // if (pid == Did.Pid) // 本进程
        // {
        // 
        //     return;
        // }

        // 获取sender
        OneSender sender = GetSender(pid, ipEndPoint);
        if (!sender.Send(new NetMsg(serviceId, msg, subId))) // 已经断开了
        {
            _clientLogger.Warn($"Send pid {pid} disconnected. {msg.GetType().Name}");
        }
    }

    /// <summary>
    /// 向指定进程发送请求，并等待响应
    /// </summary>
    /// <param name="serviceId"></param>
    /// <param name="request"></param>
    /// <param name="subId"></param>
    /// <returns></returns>
    public ValueTask<IResponse> Call(long serviceId, IRequest request, long subId = 0)
    {
        ushort pid = Did.GetPid(serviceId);
        if (!_endPoints.TryGetValue(pid, out IPEndPoint? ipEndPoint)) // 
        {
            throw RpcException.NotFoundPid;
        }

        int rpcId = Interlocked.Increment(ref _rpcId);
        request.RpcId = rpcId;

        var tcs = ResponseTcs.Create();
        if (!_rpcCallbacks.TryAdd(rpcId, tcs))
        {
            throw RpcException.DuplicateRpcId;
        }

        // if (pid == Did.Pid) // 本进程
        // {
        // }
        // else
        {
            // 获取sender
            OneSender sender = GetSender(pid, ipEndPoint);
            if (!sender.Send(new NetMsg(serviceId, request, subId))) // 已经断开了
            {
                _rpcCallbacks.TryRemove(rpcId, out _);
                throw RpcException.Disconnect;
            }
        }

        return tcs.Task;
    }

    private void OnDisconnect(ushort pid, Channel<NetMsg> channel)
    {
        // 已经断开了连接，丢弃消息；
        channel.Writer.TryComplete();
        var reader = channel.Reader;
        int msgCount = 0;
        while (reader.TryRead(out NetMsg item))
        {
            if (item.Body is IRequest request) // 触发回调，连接已经断开的异常
            {
                if (_rpcCallbacks.TryRemove(request.RpcId, out ResponseTcs? tcs))
                {
                    tcs.SetException(RpcException.Disconnect);
                }
            }
            else
            {
                ++msgCount;
            }
        }

        if (msgCount > 0)
            _clientLogger.Info($"Send to pid {pid} error, drop {msgCount} messages.");

        _senders.TryRemove(pid, out _);
    }

    public async ValueTask StopAsync()
    {
        // 打印日志rpc客户端关闭中
        _clientLogger.Info("Closing...");
        _endPoints.Clear();

        List<Task> tasks = new List<Task>();
        foreach (var kv in _senders)
        {
            tasks.Add(kv.Value.DisposeAsync().AsTask());
        }

        _senders.Clear();
        await Task.WhenAll(tasks);
        _clientLogger.Info("Closed.");

        if (_stoppingCts != null)
            await _stoppingCts.CancelAsync();
    }

    /// <summary>
    /// 一个指定Pid的Sender
    /// </summary>
    private class OneSender : IAsyncDisposable
    {
        private readonly ushort _pid;
        private readonly IPEndPoint _ipEndPoint;
        private readonly InternalNetwork _network;
        private readonly Channel<NetMsg> _channel;
        private readonly ChannelWriter<NetMsg> _channelWriter;
        private readonly Task _sendTask;
        private readonly SLogger _logger;

        /// <summary>
        /// 一个指定Pid的Sender
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="ipEndPoint"></param>
        /// <param name="network"></param>
        public OneSender(ushort pid, IPEndPoint ipEndPoint, InternalNetwork network)
        {
            _pid = pid;
            _ipEndPoint = ipEndPoint;
            _network = network;
            _logger = network._clientLogger;

            _channel = Channel.CreateSingleConsumerUnbounded<NetMsg>();
            _channelWriter = _channel.Writer;
            _sendTask = StartAsync(network._connectionFactory);
        }

        public bool Send(in NetMsg msg)
        {
            return _channelWriter.TryWrite(msg);
        }

        private async Task StartAsync(IConnectionFactory connectionFactory)
        {
            ConnectionContext? connection = null;
            try
            {
                // 正在连接
                _logger.Info($"Connecting to pid {_pid} {_ipEndPoint}...");
                connection = await connectionFactory.ConnectAsync(_ipEndPoint);

                // 连接成功
                _logger.Info($"Connected to pid {_pid} {_ipEndPoint}.");

                await ProcessOutgoing(connection);
            }
            catch (ConnectionAbortedException) // 忽略异常
            {
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Send to pid {_pid} error: {ex}");
            }
            finally
            {
                // 连接已经断开了,让所有消息,都马上丢弃掉;
                if (connection != null)
                    await connection.DisposeAsync();
            }

            // 连接断开
            _logger.Info($"Disconnected to pid {_pid} {_ipEndPoint}.");
            _network.OnDisconnect(_pid, _channel);
        }

        private async Task ProcessOutgoing(ConnectionContext connection)
        {
            // 连接成功后，处理发送消息
            var reader = _channel.Reader;
            var output = connection.Transport.Output;
            var s = _network._serializer;
            while (true)
            {
                bool more = await reader.WaitToReadAsync();
                if (!more)
                    break;

                while (reader.TryRead(out NetMsg item))
                {
                    // 预留长度字段
                    Span<byte> lengthField = output.GetSpan(4);
                    output.Advance(4);

                    // 写入消息头
                    Span<byte> head = output.GetSpan(20);
                    BinaryPrimitives.WriteInt64LittleEndian(head, item.ServiceId);
                    BinaryPrimitives.WriteInt64LittleEndian(head[8..], item.SubId);
                    BinaryPrimitives.WriteInt32LittleEndian(head[16..], item.Body.MsgId);
                    output.Advance(20);

                    // 序列化消息体
                    long size = output.UnflushedBytes;
                    s.Serialize(item.Body, output);
                    long bodySize = output.UnflushedBytes - size;
                    BinaryPrimitives.WriteInt32LittleEndian(lengthField, (int)bodySize);
                }

                var flushResult = await output.FlushAsync();
                if (flushResult.IsCompleted || flushResult.IsCanceled)
                {
                    break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_channelWriter.TryComplete())
            {
                return;
            }

            await _sendTask;
        }
    }
}