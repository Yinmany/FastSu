﻿using System.Buffers;
using System.IO.Pipelines;
using FastSu;

namespace FastSu.Server.Rpc;

public interface IRpcSerializer
{
    void Serialize(IMessageBase msg, PipeWriter writer);

    IMessageBase Deserialize(Type type, ReadOnlySequence<byte> data);
}