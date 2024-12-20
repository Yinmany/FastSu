namespace FastSu;

public readonly record struct ErrorOr<T>
{
    public readonly int ErrorCode;
    public readonly T Value;

    public ErrorOr(int value)
    {
        this.Value = default!;
        this.ErrorCode = value;
    }

    public ErrorOr(T data)
    {
        this.Value = data;
        this.ErrorCode = 0;
    }

    public bool IsError => ErrorCode != 0;

    public void Deconstruct(out int err, out T value)
    {
        err = ErrorCode;
        value = Value;
    }

    public static implicit operator ErrorOr<T>(T value)
    {
        return new ErrorOr<T>(value);
    }

    public static implicit operator ErrorOr<T>(int errorCode)
    {
        return new ErrorOr<T>(errorCode);
    }
}