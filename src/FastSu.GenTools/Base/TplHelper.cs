using FastSu.GenTools.Base;
using Scriban;

namespace FastSu.GenTools;

public static class TplHelper
{
    /// <summary>
    /// 加载模板
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Template Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new Exception($"模板文件不存在: {path}");
        }

        string tpl = File.ReadAllText(path);
        var template = Template.Parse(tpl);

        if (template.HasErrors)
        {
            foreach (var msg in template.Messages)
            {
                SLog.Error($"模板错误: {msg} {path}");
            }

            throw new Exception($"模板错误!");
        }

        return template;
    }
}