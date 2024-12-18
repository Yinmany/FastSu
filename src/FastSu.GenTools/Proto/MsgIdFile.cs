namespace FastSu.GenTools;

public static class MsgIdFile
{
    private static readonly Dictionary<string, ProtoEnumField> Values = new();

    public static int Count => Values.Count;
    public static ProtoEnumField? Get(string name) => Values.GetValueOrDefault(name);

    public static void Load(string path)
    {
        if(!File.Exists(path))
            throw new Exception($"文件不存在: {path}");
        
        Values.Clear();

        string[] lines = File.ReadAllLines(path);
        bool isStart = false;
        foreach (var line in lines)
        {
            string tmp = line.Trim();
            if (string.IsNullOrEmpty(tmp))
                continue;

            if (tmp.StartsWith("enum"))
            {
                isStart = true;
                continue;
            }

            if (!isStart)
                continue;

            if (tmp.StartsWith('{') || tmp.StartsWith('}'))
                continue;

            if (tmp.StartsWith("//"))
                continue;

            ProtoEnumField field = new ProtoEnumField(tmp);
            Values.Add(field.Name, field);
        }
    }
}