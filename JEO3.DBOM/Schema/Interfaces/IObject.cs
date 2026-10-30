namespace JEO3.Schema
{
    /// <summary>
    /// Provider-agnostic representation of a database object.
    /// </summary>
    public interface IObject
    {
        /// <summary>
        /// Provider-specific object identifier, when available.
        /// </summary>
        int? ObjectId { get; }
        /// <summary>
        /// Provider-independent stable identifier, when available.
        /// </summary>
        string? Identifier { get; }
        /// <summary>
        /// Database containing the object.
        /// </summary>
        string? DatabaseName { get; }
        /// <summary>
        /// Schema containing the object.
        /// </summary>
        string? SchemaName { get; }
        /// <summary>
        /// Object name.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Fully qualified object name.
        /// </summary>
        string? FullName { get; }
        /// <summary>
        /// Type of database object.
        /// </summary>
        DatabaseObjectType ObjectType { get; }
    }
}