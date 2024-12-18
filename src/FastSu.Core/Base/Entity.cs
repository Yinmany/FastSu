namespace FastSu.Core;

/// <summary>
/// 只用实现此接口的Entity才能作为Root
/// </summary>
public interface IEntityRoot
{
}

[Flags]
public enum EntityRemoveFlag
{
    Id = 1,
    Type = 2,
    Both = Id | Type
}

public abstract class Entity
{
    public long Id { get; private set; }

    /// <summary>
    /// 版本号(实体释放时+1)
    /// </summary>
    public uint Version { get; private set; } = 1;

    private Entity? _root = null;
    private Entity? _parent = null;
    private Dictionary<long, Entity>? _children;
    private Dictionary<int, Entity>? _childrenByTypeId;

    // 用于移除所有时的link(防止迭代时访问集合)
    private Entity? _tail = null;

    /// <summary>
    /// 获取Root(实现IEntityRoot的实体，此值就是自己)
    /// </summary>
    /// <exception cref="Exception">没有Root</exception>
    public Entity Root
    {
        get
        {
            if (_root != null)
                return _root;

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is IEntityRoot)
                return this;

            throw new Exception("当前实体没有Root");
        }
    }

    /// <summary>
    /// 获取Parent
    /// </summary>
    /// <exception cref="Exception">没有Parent</exception>
    public Entity Parent => _parent ?? throw new Exception("当前实体没有Parent");

    protected Entity()
    {
    }

    protected Entity(long id)
    {
        this.Id = id;
    }

    public void Add(Entity entity)
    {
        if (entity == this)
            throw new Exception("不能添加自己.");

        if (entity.Id == 0) // 自动分配一个id
            entity.Id = Did.Next();

        // 先调用一次,一定得有root才能进行Add操作
        var checkRoot = this.Root;

        _children ??= new Dictionary<long, Entity>();
        _children.Add(entity.Id, entity);
        entity._parent = this;
        entity._root = checkRoot;
    }

    public Entity? Get(long id)
    {
        Entity? result = null;
        _children?.TryGetValue(id, out result);
        return result;
    }

    public T? Get<T>(long id) where T : Entity
    {
        Entity? result = null;
        _children?.TryGetValue(id, out result);
        return result as T;
    }

    public bool Remove(long id)
    {
        if (_children != null && _children.Remove(id, out var entity))
        {
            entity.Destroy();
            return true;
        }

        return false;
    }

    public void RemoveAll(EntityRemoveFlag flag = EntityRemoveFlag.Both)
    {
        Entity tail = this; // 使用链表形式，把所有要移除的entity连接起来
        if (flag.HasFlag(EntityRemoveFlag.Id) && _children != null)
        {
            foreach (var e in _children.Values)
            {
                tail = tail._tail = e;
            }

            _children.Clear();
            _children = null;
        }

        if (flag.HasFlag(EntityRemoveFlag.Type) && _childrenByTypeId != null)
        {
            foreach (var e in _childrenByTypeId.Values)
            {
                tail = tail._tail = e;
            }

            _childrenByTypeId.Clear();
            _childrenByTypeId = null;
        }

        while (tail._tail != null)
        {
            var cur = tail._tail;
            tail._tail = null;
            tail = cur;
            cur.Destroy();
        }
    }

    public void AddBy<T>(T entity) where T : Entity
    {
        if (entity == this)
            throw new Exception("不能添加自己.");

        if (entity.Id == 0) // 自动分配一个id
            entity.Id = Did.Next();

        // 先调用一次,一定得有root才能进行Add操作
        var checkRoot = this.Root;

        _childrenByTypeId ??= new Dictionary<int, Entity>();
        _childrenByTypeId.Add(TypeId.Cache<T>.Value, entity);
        entity._parent = this;
        entity._root = checkRoot;
    }

    public T? GetBy<T>() where T : Entity
    {
        Entity? result = null;
        _children?.TryGetValue(TypeId.Cache<T>.Value, out result);
        return result as T;
    }

    public bool RemoveBy<T>() where T : Entity
    {
        if (_childrenByTypeId != null && _childrenByTypeId.Remove(TypeId.Cache<T>.Value, out var entity))
        {
            entity.Destroy();
            return true;
        }

        return false;
    }

    private void Destroy()
    {
        OnDestroy();
        this._parent = null;
        this._root = null;
        ++this.Version;
    }

    protected virtual void OnDestroy()
    {
    }
}