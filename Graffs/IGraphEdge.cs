using System.Collections.Generic;

namespace Graffs;

/// <summary>
/// Generic interface for graph edges connecting two nodes.
/// Represents a directed edge in a graph with optional metadata support.
/// </summary>
/// <typeparam name="TNode">The type of nodes that this edge connects. Must be non-null.</typeparam>
public interface IGraphEdge<TNode> where TNode : notnull
{
    /// <summary>
    /// Gets the source node of this edge.
    /// </summary>
    /// <remarks>
    /// In a directed graph, this represents the node where the edge originates.
    /// In dependency terms, this is often the dependent node.
    /// </remarks>
    TNode Source { get; }
    
    /// <summary>
    /// Gets the target node of this edge.
    /// </summary>
    /// <remarks>
    /// In a directed graph, this represents the node where the edge terminates.
    /// In dependency terms, this is often the dependency node.
    /// </remarks>
    TNode Target { get; }
    
    /// <summary>
    /// Gets optional metadata associated with this edge.
    /// </summary>
    /// <remarks>
    /// This property allows attaching arbitrary data to edges, such as:
    /// - Edge weights for weighted graphs
    /// - Edge types or categories
    /// - Import information for dependency graphs
    /// - Custom application-specific data
    /// 
    /// Returns <c>null</c> if no metadata is associated with this edge.
    /// The returned dictionary is immutable.
    /// </remarks>
    IReadOnlyDictionary<string, object>? Metadata { get; }
}
