using System.Xml.Serialization;
using CommandLine;
using FastSu.GenTools.Base;
using FastSu.GenTools.Enum;

namespace FastSu.GenTools;

public class Program
{
    public class Options
    {
        // 输入目录
        [Option("conf", Required = false, Default = "conf.xml")]
        public string Conf { get; set; }
    }

    public static void Main(string[] args)
    {
        ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);
        Options options = result.Value;

        if (!File.Exists(options.Conf))
        {
            SLog.Error($"配置文件不存在: {options.Conf}");
            return;
        }
        SLog.Info($"conf: {options.Conf}");

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GenConfig));
            GenConfig genConfig = (GenConfig)serializer.Deserialize(File.OpenRead(options.Conf));
            if (genConfig is null)
            {
                SLog.Error($"配置文件解析失败: {options.Conf}");
                return;
            }

            if (genConfig.Proto is { Length: > 0 })
            {
                foreach (var proto in genConfig.Proto)
                {
                    ProtoGen.Gen(proto);
                }
            }

            if (genConfig.Enum is { Length: > 0 })
            {
                foreach (var item in genConfig.Enum)
                {
                    EnumGen.Gen(item);
                }
            }
        }
        catch (Exception e)
        {
            SLog.Error(e);
        }
    }
}