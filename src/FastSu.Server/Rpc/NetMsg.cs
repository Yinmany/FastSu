﻿using FastSu;

namespace FastSu.Server.Rpc;

readonly record struct NetMsg(long ServiceId, IMessageBase Body, long SubId);