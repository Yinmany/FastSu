using System.Diagnostics;

namespace FastSu.Utils;

/// <summary>
/// 时间相关操作
/// </summary>
public struct STime
{
    public static readonly DateTime Epoch = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 时间戳(相对Epoch的毫秒数)
    /// </summary>
    public static long Timestamp => (DateTime.Now - Epoch).Ticks / TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// 时间戳(相对Epoch的秒数)
    /// <returns>使用int最多可表示68年</returns>
    /// </summary>
    public static int TsSeconds => (int)((DateTime.Now - Epoch).Ticks / TimeSpan.TicksPerSecond);

    /// <summary>
    /// 转换到DateTime从相对的秒数
    /// </summary>
    /// <param name="totalSeconds"></param>
    /// <returns></returns>
    public static DateTime ToDateTimeWithSeconds(long totalSeconds)
    {
        long ticks = Epoch.Ticks + totalSeconds * TimeSpan.TicksPerSecond;
        return new DateTime(ticks);
    }

    /// <summary>
    /// 转换到DateTime从相对的毫秒数
    /// </summary>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    public static DateTime ToDateTime(long timestamp)
    {
        long ticks = Epoch.Ticks + timestamp * TimeSpan.TicksPerMillisecond;
        return new DateTime(ticks);
    }

    /// <summary>
    /// 从DateTime中获取时间戳(相对Epoch)
    /// </summary>
    /// <param name="dt"></param>
    public static long GetTimestamp(DateTime dt)
    {
        return (dt - Epoch).Ticks / TimeSpan.TicksPerMillisecond;
    }

    #region 计时器

    /// <summary>
    /// 开始计时
    /// </summary>
    /// <returns></returns>
    public static STime Start() => new STime();

    private long _t;

    public STime()
    {
        _t = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// 结束
    /// </summary>
    /// <returns>返回从start到stop所经过的毫秒数</returns>
    public long Stop() => (Stopwatch.GetTimestamp() - _t) / TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// 重新开始计时
    /// </summary>
    public void Restart()
    {
        _t = Stopwatch.GetTimestamp();
    }

    #endregion
}