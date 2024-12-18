using System.Text;
using System.Xml;
using FastSu.GenTools.Base;
using Scriban;

namespace FastSu.GenTools.Enum;

public static class EnumGen
{
    private static Template _tpl;

    public static void Gen(EnumConfig config)
    {
        SLog.Info("==== 开始生成Enum ====");

        _tpl = TplHelper.Load(config.Tpl);

        if (!File.Exists(config.In))
            throw new Exception($"xml文件不存在: {config.In}");

        XmlDocument doc = new();
        doc.Load(config.In);

        var root = doc.DocumentElement;
        if (root == null)
            throw new Exception($"xml格式错误: {config.In}");

        string typeName = root.GetAttribute("name");
        string ns = root.GetAttribute("namespace");
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(ns))
        {
            throw new Exception($"name 与 namespace 必须填写: name={typeName} namespace={ns}");
        }

        Dictionary<int, EnumItem> items = new Dictionary<int, EnumItem>();
        int startIndex = 0;
        foreach (XmlNode xmlNode in root.ChildNodes)
        {
            if (xmlNode.Name is not "var")
                continue;

            string name = xmlNode.Attributes["name"].Value;
            string comment = xmlNode.Attributes["comment"]?.Value;
            string value = xmlNode.Attributes["value"]?.Value;

            if (value != null && int.TryParse(value, out startIndex))
            {
                SLog.Info($"设置 index = {startIndex}");
            }

            if (items.ContainsKey(startIndex))
                throw new Exception($"value重复: {name} = {value} // {comment}");

            items.Add(startIndex, new EnumItem(name, startIndex, comment));
            ++startIndex;
        }

        string str = _tpl.Render(new EnumData { Namespace = ns, Name = typeName, Items = items.Values.ToArray() });

        string fileName = Path.GetFileNameWithoutExtension(config.In);
        string fileExt = Path.GetFileNameWithoutExtension(config.Tpl).Split('_')[^1];

        string outPath = Path.Combine(config.Out, $"{fileName}.{fileExt}");
        File.WriteAllText(outPath, str);
        Console.WriteLine($"out => {outPath}");

        SLog.Info("==== 生成Enum成功 ====");
    }
}