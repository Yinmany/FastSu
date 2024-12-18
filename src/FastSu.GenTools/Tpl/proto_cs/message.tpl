{{
    is_msg = true
    is_req = false
    is_resp = false
    startOrder = 0
    
    if string.ends_with name 'Msg'
        _t = ': IMessage'
    else if string.ends_with name 'Req'
        _t = ': IRequest'
        is_req = true
        startOrder = 1
    else if string.ends_with name 'Ack'
        _t = ': IResponse'
        is_resp = true
        startOrder = 3
    else 
        is_msg = false
    end
}}
[ProtoContract]
public partial class {{ name }} {{_t}}
{
    {{- if is_msg }}
    public const int _MsgId_ = {{ opcode }};
    public int MsgId => _MsgId_;
    {{- end }}

    {{- if is_req }}
    [ProtoMember(1)] public int RpcId { get; set; }
    {{- end }}

    {{- if is_resp ~}}
    [ProtoMember(1)] public int RpcId { get; set; }
    [ProtoMember(2)] public int ErrCode { get; set; }
    [ProtoMember(3)] public string ErrMsg { get; set; }
    {{- end }}

{{- for e in elems; order = e.index + startOrder }}
    {{- if e.type == "ProtoMessageField" }}
        {{- if !(string.empty e.tail_comment) }}
    /// <summary> {{ e.tail_comment }} </summary>
        {{- end }}
        {{- if e.is_repeated }}
    [ProtoMember({{order}})] public List<{{e.field_type}}> {{e.name}} { get; set; } 
        {{ else }}
    [ProtoMember({{order}})] public {{e.field_type}} {{e.name}} { get; set; }
        {{- end }}
    {{- end }}
{{- end }}
    {{ enum_codes ~}}
    {{~ msg_codes -}}
}