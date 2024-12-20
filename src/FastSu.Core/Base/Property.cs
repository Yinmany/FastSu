using System.Runtime.InteropServices;

namespace FastSu;

/// <summary>
/// 表示一个属性(8byte)
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 4)]
public struct Property
{
    /// <summary>
    /// 属性数值类型的最大值
    /// </summary>
    [FieldOffset(0)] private object obj; // 引用类型数据，低32位

    [FieldOffset(0)] private Number number;
    [FieldOffset(7)] private byte type; // 数据类型
}

[StructLayout(LayoutKind.Explicit, Pack = 8)]
public struct Number
{
    public const ulong MaxValue = 0xFFFFFFFFFFFFFFFF;

    [FieldOffset(0)] public ulong v1;
    [FieldOffset(0)] public double v2;
}