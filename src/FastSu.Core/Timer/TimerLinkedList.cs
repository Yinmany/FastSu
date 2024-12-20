namespace FastSu;

internal class TimerLinkedList
{
    public TimerNode? First { get; private set; }
    public TimerNode? Last { get; private set; }

    public void AddLast(TimerNode timer)
    {
        timer.List = this;
        timer.Prev = Last;
        timer.Next = null;
        if (Last != null)
            Last.Next = timer;
        Last = timer;
        First ??= timer;
    }

    public void Remove(TimerNode timer)
    {
        if (timer.Prev != null)
            timer.Prev.Next = timer.Next;

        Last = timer.Prev;
        First = timer.Next;

        timer.List = null;
    }

    /// <summary>
    /// 移动链表数据到另一个链表
    /// </summary>
    /// <param name="list"></param>
    public void MoveTo(TimerLinkedList list)
    {
        if (First != null)
            list.AddLast(First);
        First = null;
        Last = null;
    }

    public void Clear()
    {
        First = null;
        Last = null;
    }
}