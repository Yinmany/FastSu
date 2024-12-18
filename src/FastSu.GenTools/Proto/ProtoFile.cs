namespace FastSu.GenTools;

/// <summary>
/// Proto文件解析
/// </summary>
public static class ProtoFile
{
    internal static readonly char[] splitChars = { ' ', '\t' };

    public static ProtoGenData Parse(string protoText)
    {
        ProtoGenData genData = new ProtoGenData();
        foreach (string line in protoText.Split('\n'))
        {
            string newline = line.Trim();

            if (newline == "")
                continue;

            // 命名空间引用
            if (newline.StartsWith("//@using"))
            {
                string tmp = newline.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
                genData.UsingList.Add(tmp);
                continue;
            }

            // proto的选项
            if (newline.StartsWith("option csharp_namespace"))
            {
                // cs命名空间
                if ("csharp_namespace".IndexOf(newline, StringComparison.Ordinal) != 0)
                {
                    // "Namespace";
                    string ns = newline.Split('=')[1];
                    ns = ns.Remove(ns.Length - 1).Replace("\"", "");
                    genData.Namespace = ns.Trim();
                    continue;
                }
            }

            // 开始解析消息
            if (newline.StartsWith("message"))
            {
                genData.Push(new ProtoMessage(line));
                continue;
            }

            // 进入消息解析
            if (genData.TryPeek() is ProtoMessage messageDefine)
            {
                if (newline == "{")
                    continue;

                if (newline == "}")
                {
                    genData.Pop(); // 弹出自己

                    if (genData.TryPeek() is ProtoElement elementDefine)
                    {
                        elementDefine.AddChild(messageDefine);
                    }
                    else
                    {
                        genData.Elems.Add(messageDefine);
                    }

                    continue;
                }

                string tmpLine = newline.Trim();

                if (tmpLine.StartsWith("option")) // 扩展选项
                {
                    messageDefine.AddChild(new ProtoMessageOption(tmpLine));
                    continue;
                }

                if (tmpLine.StartsWith("//"))
                {
                    messageDefine.AddChild(new ProtoComment { Comment = newline, TabCount = 2 });
                    continue;
                }

                if (TryParseEnum())
                    continue;

                messageDefine.AddChild(new ProtoMessageField(newline));
                continue;
            }

            // 注释
            if (newline.StartsWith("//"))
            {
                genData.Elems.Add(new ProtoComment { Comment = newline, TabCount = 1 });
                continue;
            }

            if (TryParseEnum())
                continue;

            bool TryParseEnum()
            {
                // 开始解析枚举
                if (newline.StartsWith("enum"))
                {
                    genData.Push(new ProtoEnum(line));
                    return true;
                }

                if (genData.TryPeek() is ProtoEnum protoElementDefine)
                {
                    if (newline == "{")
                        return true;

                    if (newline == "}")
                    {
                        genData.Pop(); // 弹出自己

                        if (genData.TryPeek() is ProtoElement elementDefine)
                        {
                            elementDefine.AddChild(protoElementDefine);
                        }
                        else
                        {
                            genData.Elems.Add(protoElementDefine);
                        }

                        return true;
                    }

                    protoElementDefine.AddChild(new ProtoEnumField(newline));
                    return true;
                }

                return false;
            }
        }

        return genData;
    }
}