using FastSu;

namespace FastSu;

/// <summary>
/// 分布式的id生成器(无锁线程安全)
///     实现原理: 让时间与序号组成一个long值，只用原子自增即可
///  1位符号位: 0
/// 31位时间: 68年
/// 12位机器码: 4096个
/// 20位序号: 1,048,575 (1秒千万)
/// </summary>
public readonly struct Did
{
    private static ushort _curPid;

    /// <summary>
    /// 当前的pid值
    /// </summary>
    public static ushort CurPid => _curPid;

    private static ulong _value;

    public static void Init(ushort pid)
    {
        if (CurPid != 0)
            throw new InvalidOperationException("请不要重复初始化.");

        if (pid > 4096)
            throw new ArgumentException($"pid不能超过4096: {pid}");

        _curPid = pid;
        int s = STime.TsSeconds;
        _value = (ulong)s << 20;
    }

    /// <summary>
    /// 唯一,单进程有序(因为时间值并不是取当前的,而是从初始化时，一直往后自增的)
    /// </summary>
    /// <returns></returns>
    public static long Next()
    {
        if (Volatile.Read(ref _curPid) == 0)
            throw new InvalidOperationException("请初始化后使用.");

        ulong val = Interlocked.Increment(ref _value);

        int seq = (int)val & 0xFFFFF;
        int time = (int)(val >> 20) & 0x7FFFFFFF;
        long id = new Did(time, _curPid, seq);

        // 超过当前时间10s: 向未来借用了30s就要输出日志
        int leaseTime = time - STime.TsSeconds;
        if (leaseTime > 0 && id == 0 && leaseTime % 30 == 0)
            SLog.Warn($"DId: 借用时间-{time}");
        return id;
    }

    /// <summary>
    /// 生成一个指定Id的Did
    ///     同Did是一样的, 只是没有了time而且id位只有占16位了
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pid"></param>
    /// <returns></returns>
    public static long Make(ushort id, ushort? pid = null)
    {
        if (Volatile.Read(ref _curPid) == 0)
            throw new InvalidOperationException("请初始化后使用.");

        ushort tmpPid = pid ?? CurPid;
        return new Did(0, tmpPid, id);
    }

    /// <summary>
    /// 获取Did中的pid值
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static ushort GetPid(long id)
    {
        return (ushort)(id >> 20 & 0x00000000FFF);
    }

    /// <summary>
    /// 时间值(秒)
    /// </summary>
    public readonly int Time;

    /// <summary>
    /// pid值
    /// </summary>
    public readonly ushort Pid;

    /// <summary>
    /// 序号值
    /// </summary>
    public readonly int Seq;

    public Did(long id)
    {
        Time = (int)(id >> 32);
        Pid = (ushort)(id >> 20 & 0x00000000FFF);
        Seq = (int)(id & 0x000FFFFF);
    }

    public Did(int time, ushort pid, int seq)
    {
        if (pid > 4096)
            throw new ArgumentException($"pid不能超过4096: {pid}");
        if (seq > 0xFFFFF)
            throw new ArgumentException($"seq不能超过{0xFFFFF}: {seq}");

        this.Time = time;
        this.Pid = pid;
        this.Seq = seq;
    }

    public static implicit operator Did(long id)
    {
        return new Did(id);
    }

    public static implicit operator long(in Did id)
    {
        return (long)((ulong)id.Time << 32 | (ulong)id.Pid << 20 | (uint)id.Seq);
    }
}