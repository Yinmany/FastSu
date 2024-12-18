namespace {{namespace}}
{
    public static class {{ name }}
    {
    {{~ for e in items ~}}
        {{~ if !(string.empty e.comment)~}}
        /// <summary> {{ e.comment }} </summary>
        {{~ end ~}}
        public const int {{ e.name }} = {{ e.value }};
    {{~ end }}
    }
}