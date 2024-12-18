using System.Xml.Serialization;

namespace FastSu.GenTools;

[XmlRoot("conf")]
public class GenConfig
{
    [XmlElement("proto")] public ProtoConfig[] Proto { get; set; }

    [XmlElement("enum")] public EnumConfig[] Enum { get; set; }
}

public class ProtoConfig
{
    [XmlAttribute("in")] public string In { get; set; }

    [XmlAttribute("out")] public string Out { get; set; }

    [XmlAttribute("tpl")] public string Tpl { get; set; }

    [XmlAttribute("msg_id")] public string MsgId { get; set; }
}

public class EnumConfig
{
    [XmlAttribute("in")] public string In { get; set; }

    [XmlAttribute("out")] public string Out { get; set; }

    [XmlAttribute("tpl")] public string Tpl { get; set; }
}