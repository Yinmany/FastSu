using System.Collections.Concurrent;
using System.Diagnostics;

namespace FastSu;

/// <summary>
/// 时间轮(以10ms为一格;一轮最大可表示497天)
/// </summary>
public partial class TimerWheel : ITimerService, IThreadPoolWorkItem
{
    private const uint UnitMs = 10; // 第一层: 255 * 10 = 2550/1000 = 2.55秒
    private const long TickIntervalMs = UnitMs * TimeSpan.TicksPerMillisecond;

    private const uint TimeNear = 256;
    private const uint TimeNearMask = 0xFF;
    private const int TimeNearShift = 8;

    private const int TimeLevelMask = 0x3F;
    private const int TimeLevelShift = 6;

    /// <summary>
    /// 线程同步使用
    /// </summary>
    struct Cmd
    {
        public bool IsAdd;
        public TimerNode Node;
    }

    // 5层(刚好占满一个uint最大值的时间值)
    private readonly TimerLinkedList[] _nearSlot;
    private readonly TimerLinkedList[][] _levelSlot;
    private readonly TimerLinkedList _freeTimers;

    // 当前时间(执行到的格子)
    private uint _curTime = 0;
    private readonly Timer _timer;
    private readonly ConcurrentQueue<Cmd> _cmdQueue = new();
    private readonly Action<TimerNode> _disposableAction;
    private int _doingWork;

    private long _lastTime;
    private long _tickTimeNum;

    /// <summary>
    /// 时间轮的循环次数(整个时间轮的格子都执行完后，开始下一轮即可视为一次)
    /// </summary>
    public int CycleCount { get; private set; } = 1;

    public TimerWheel()
    {
        _disposableAction = Remove;
        _nearSlot = new TimerLinkedList[256];
        for (int i = 0; i < 256; i++)
            _nearSlot[i] = new TimerLinkedList();

        _levelSlot = new TimerLinkedList[4][];
        for (int i = 0; i < 4; i++)
        {
            _levelSlot[i] = new TimerLinkedList[64];
            for (int j = 0; j < 64; j++)
            {
                _levelSlot[i][j] = new TimerLinkedList();
            }
        }

        _freeTimers = new TimerLinkedList();
        _timer = new Timer(this.OnTimeout, this, 0, UnitMs);
        _lastTime = Stopwatch.GetTimestamp();
    }

    public ITimerNode AddTimeout(uint dueTime, TimerCallback callback, int type = 0, object? state = null) =>
        InternalAdd(dueTime, callback, state, type);

    public ITimerNode AddInterval(uint dueTime, uint period, TimerCallback callback, int type = 0, object? state = null) =>
        InternalAdd(dueTime, callback, state, type, period);

    private ITimerNode InternalAdd(uint timeout, TimerCallback callback, object? state, int type,
        uint interval = 0)
    {
        timeout /= UnitMs;
        interval /= UnitMs;
        TimerNode timer = new TimerNode(timeout, state, _disposableAction, type, interval, callback);
        _cmdQueue.Enqueue(new Cmd { IsAdd = true, Node = timer });
        return timer;
    }

    private void InternalAdd(TimerNode t)
    {
        // 绝对超时时间
        t.Expires = _curTime + t.Expires;
        if (t.Expires > uint.MaxValue) // 超过最大值的属于下一轮的，等到下一轮开始时，在重新放入即可；
        {
            _freeTimers.AddLast(t);
            return;
        }

        AddNode(t);
    }

    private void AddNode(TimerNode t)
    {
        uint exp = (uint)t.Expires;
        if ((exp | TimeNearMask) == (_curTime | TimeNearMask))
        {
            _nearSlot[exp & TimeNearMask].AddLast(t);
        }
        else
        {
            uint mask = TimeNear << TimeLevelShift;
            int i = 0;
            for (; i < 3; ++i)
            {
                if ((exp | (mask - 1)) == (_curTime | (mask - 1)))
                    break;
                mask <<= TimeLevelShift;
            }

            int idx = (int)((exp >> (TimeNearShift + i * TimeLevelShift)) & TimeLevelMask);
            _levelSlot[i][idx].AddLast(t);
        }
    }

    private void Remove(TimerNode t) => _cmdQueue.Enqueue(new Cmd { IsAdd = false, Node = t });

    private void OnTimeout(object? state)
    {
        if (Interlocked.CompareExchange(ref _doingWork, 1, 0) == 0)
            ThreadPool.UnsafeQueueUserWorkItem(this, true);
    }

    /// <summary>
    /// 使用 System.Threading.Timer 进行超时,每次超时后马上使用线程池调用一次Execute以执行TimerWheel的逻辑。
    /// </summary>
    public void Execute()
    {
        long curTime = Stopwatch.GetTimestamp();
        long delta = curTime - _lastTime;
        _lastTime = curTime;

        _tickTimeNum += delta;
        while (_tickTimeNum >= TickIntervalMs)
        {
            Tick();

            // 只要附件的调试器，将不会补偿时间
            if (Debugger.IsAttached)
                _tickTimeNum = 0;
            else
                _tickTimeNum -= TickIntervalMs;
        }

        Interlocked.Exchange(ref _doingWork, 0);
    }

    private void MoveShift()
    {
        if (_curTime == 0) // 溢出归零后，超过最大时间的定时器全部重新放入一遍。
        {
            TimerNode? node = _freeTimers.First;
            while (node != null)
            {
                node.Expires -= uint.MaxValue;
                node.List!.Remove(node);
                InternalAdd(node); // 重新放入
                node = node.Next;
            }

            this.CycleCount += 1; // 新的一轮
            return;
        }

        uint t = _curTime;
        uint mask = TimeNear;
        uint time = _curTime >> TimeNearShift;

        int level = 0;
        while ((t & (mask - 1)) == 0) // 进位时才进行移动(底一级是0时)
        {
            uint idx = time & TimeLevelMask;
            if (idx != 0)
            {
                var list = _levelSlot[level][idx];
                TimerNode? node = list.First;
                while (node != null)
                {
                    node.List!.Remove(node);
                    AddNode(node); // 重新放入
                    node = node.Next;
                }

                break;
            }

            mask <<= TimeLevelShift;
            time >>= TimeLevelShift;
            ++level;
        }
    }

    private void Tick()
    {
        // 处理计时器操作
        while (_cmdQueue.TryDequeue(out Cmd cmd))
        {
            if (cmd.IsAdd)
                InternalAdd(cmd.Node);
            else
                cmd.Node.List!.Remove(cmd.Node);
        }


        Trigger((byte)_curTime);
        unchecked
        {
            ++_curTime;
        }

        // 降级: 以tv2->tv1举例,每次curTime进位时就会把高一级的格子移动到下一级
        //  比如: 256是处于v2格子1,curTime执行到256时,会把v2格子1->移动到v1格子1；然后curTime强转转到byte=0 执行v1的格子1
        MoveShift();
    }

    private void Trigger(int index)
    {
        TimerLinkedList list = _nearSlot[index];
        while (list.First != null)
        {
            TimerNode node = list.First;
            node.Invoke();
            list.Remove(node);

            if (node.Interval > 0)
            {
                // 重新添加
                node.Expires = node.Interval;
                InternalAdd(node);
            }
        }
    }
}