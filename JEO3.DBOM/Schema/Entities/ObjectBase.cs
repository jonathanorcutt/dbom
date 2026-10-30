using System.Text.Json.Serialization;

namespace JEO3.Schema
{
    public abstract class ObjectBase : IObject
    {
        public abstract DatabaseObjectType ObjectType { get; }
        [JsonInclude]
        public int? ObjectId { get; internal init; }
        [JsonInclude]
        public string? Identifier { get; internal init; }
        [JsonInclude]
        public string? DatabaseName { get; internal init; }
        [JsonInclude]
        public string? SchemaName { get; internal init; }
        [JsonInclude]
        public string Name { get; internal init; } = string.Empty;
        [JsonInclude]
        public string? FullName { get; internal init; }
    }
}