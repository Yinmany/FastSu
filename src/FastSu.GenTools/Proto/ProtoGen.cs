using System.Text;
using FastSu.GenTools.Base;
using Scriban;

namespace FastSu.GenTools;

public static class ProtoGen
{
    private static Template _headTpl;
    private static Template _enumTpl;
    private static Template _msgTpl;
    private static ProtoConfig _config;

    public static void Gen(ProtoConfig config)
    {
        SLog.Info("==== 开始生成Proto ====");

        _config = config;
        _headTpl = TplHelper.Load(Path.Combine(_config.Tpl, "head.tpl"));
        _enumTpl = TplHelper.Load(Path.Combine(_config.Tpl, "enum.tpl"));
        _msgTpl = TplHelper.Load(Path.Combine(_config.Tpl, "message.tpl"));

        // 载入消息id
        MsgIdFile.Load(Path.Combine(config.In, config.MsgId));
        SLog.Info($"加载MsgId成功: {MsgIdFile.Count}");

        // 解析proto文件
        if (!Directory.Exists(config.Out))
            Directory.CreateDirectory(config.Out);
        string[] files = Directory.GetFiles(config.In, "*.proto");
        foreach (var file in files)
        {
            if (!GenProto(file))
            {
                return;
            }
        }

        SLog.Info("==== 生成Proto结束 ====");
    }


    static bool GenProto(string file)
    {
        StringBuilder stringBuilder = new StringBuilder();

        string protoTxt = File.ReadAllText(file);
        var data = ProtoFile.Parse(protoTxt);

        foreach (var e in data.Elems)
        {
            Template curTpl = null;
            if (e is ProtoEnum)
            {
                curTpl = _enumTpl;
            }
            else if (e is ProtoMessage protoMessage)
            {
                curTpl = _msgTpl;

                ProtoElement msgId = protoMessage.Elems.FirstOrDefault(f => f.GetType() == typeof(ProtoMessageOption));
                if (msgId is ProtoMessageOption option)
                {
                    var value = MsgIdFile.Get(option.DefineStr);
                    if (value == null)
                    {
                        SLog.Error($"未找到MsgId: {protoMessage.Name} {option.DefineStr}");
                        return false;
                    }

                    protoMessage.Opcode = value.Index;
                }

                // 嵌套枚举
                StringBuilder temp = new StringBuilder();
                foreach (var inner in protoMessage.Elems)
                {
                    if (inner is ProtoEnum)
                    {
                        temp.AppendLine(_enumTpl.Render(inner));
                    }
                }

                protoMessage.EnumCodes = temp.ToString();
            }

            if (curTpl != null)
            {
                try
                {
                    stringBuilder.AppendLine(curTpl.Render(e));
                }
                catch (Exception exception)
                {
                    SLog.Error(exception);
                    return false;
                }
            }
        }

        data.Codes = stringBuilder.ToString();
        string finalCodes = _headTpl.Render(data);

        string fileName = Path.GetFileNameWithoutExtension(file);
        string fileExt = _config.Tpl.Split('_')[^1];
        string outputFileName = Path.Combine(_config.Out, $"{fileName}.{fileExt}");
        File.WriteAllText(outputFileName, finalCodes);
        SLog.Info($"生成成功: {outputFileName}");
        return true;
    }
}