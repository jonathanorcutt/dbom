using System.Text.Json.Serialization;

namespace JEO3.Schema
{
    public sealed class ForeignKey : Relation, IForeignKey
    {
        public DatabaseObjectType ObjectType => DatabaseObjectType.ForeignKey;
        [JsonInclude]
        public string? DeleteAction { get; internal init; }
        [JsonInclude]
        public string? UpdateAction { get; internal init; }
        [JsonInclude]
        public bool IsSelfReferencing { get; internal init; }
        [JsonInclude]
        public bool IsComposite { get; internal init; }
    }
}