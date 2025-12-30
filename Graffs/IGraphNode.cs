namespace Graffs;

/// <summary>
/// Optional interface for nodes that want to provide additional graph information.
/// This interface is not required for graph operations but provides standardized
/// access to node identification and display information.
/// </summary>
/// <typeparam name="TKey">The type of the node identifier. Must be non-null.</typeparam>
/// <remarks>
/// This interface is covariant in TKey, allowing for flexible type relationships.
/// Implementing this interface allows nodes to participate in enhanced graph
/// operations that benefit from structured identification and display capabilities.
/// </remarks>
public interface IGraphNode<out TKey> where TKey : notnull
{
    /// <summary>
    /// Gets the unique identifier for this node.
    /// </summary>
    /// <remarks>
    /// This identifier should be unique within the context of the graph
    /// and should remain stable throughout the node's lifetime.
    /// It is used for node comparison, hashing, and lookup operations.
    /// </remarks>
    TKey Id { get; }
    
    /// <summary>
    /// Gets an optional display name for this node.
    /// </summary>
    /// <remarks>
    /// This property provides a human-readable representation of the node
    /// for debugging, logging, and user interface purposes.
    /// Returns <c>null</c> if no display name is available.
    /// If null, graph operations may use the Id or ToString() for display.
    /// </remarks>
    string? DisplayName { get; }
}
