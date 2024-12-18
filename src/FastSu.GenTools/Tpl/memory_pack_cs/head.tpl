using MemoryPack;
using Chuan.Server.BaseLayer;
{{ for item in using_list }}
using {{ item }};
{{- end }}

namespace {{namespace}}
{
    {{codes}}
}