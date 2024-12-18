namespace FastSu.GenTools.Enum;

public record struct EnumItem(string Name, int Value, string Comment);

public class EnumData
{
    public string Namespace;
    public string Name;
    public EnumItem[] Items;
}