using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Graffs;

/// <summary>
/// Basic implementation of <see cref="IGraphEdge{TNode}"/> representing a directed edge between two nodes.
/// This implementation is immutable and provides proper equality and hash code semantics.
/// </summary>
/// <typeparam name="TNode">The type of nodes that this edge connects. Must be non-null.</typeparam>
public sealed class GraphEdge<TNode> : IGraphEdge<TNode>, IEquatable<GraphEdge<TNode>>
    where TNode : notnull
{
    /// <summary>
    /// Gets the source node of this edge.
    /// </summary>
    public TNode Source { get; }
    
    /// <summary>
    /// Gets the target node of this edge.
    /// </summary>
    public TNode Target { get; }
    
    /// <summary>
    /// Gets optional metadata associated with this edge.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphEdge{TNode}"/> class without metadata.
    /// </summary>
    /// <param name="source">The source node of the edge.</param>
    /// <param name="target">The target node of the edge.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="target"/> is null.</exception>
    public GraphEdge(TNode source, TNode target)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Metadata = null;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphEdge{TNode}"/> class with metadata.
    /// </summary>
    /// <param name="source">The source node of the edge.</param>
    /// <param name="target">The target node of the edge.</param>
    /// <param name="metadata">Optional metadata to associate with this edge.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="target"/> is null.</exception>
    public GraphEdge(TNode source, TNode target, IReadOnlyDictionary<string, object>? metadata)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Metadata = metadata;
    }
    
    /// <summary>
    /// Creates a new instance of the <see cref="GraphEdge{TNode}"/> class with metadata from a dictionary.
    /// </summary>
    /// <param name="source">The source node of the edge.</param>
    /// <param name="target">The target node of the edge.</param>
    /// <param name="metadata">Metadata to associate with this edge. Will be converted to an immutable dictionary.</param>
    /// <returns>A new graph edge with the specified metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="target"/> is null.</exception>
    public static GraphEdge<TNode> WithMetadata(TNode source, TNode target, IDictionary<string, object> metadata)
    {
        return new GraphEdge<TNode>(source, target, metadata?.ToImmutableDictionary());
    }
    
    /// <summary>
    /// Determines whether the specified object is equal to the current edge.
    /// Two edges are considered equal if they have the same source and target nodes.
    /// Metadata is not considered in equality comparison.
    /// </summary>
    /// <param name="obj">The object to compare with the current edge.</param>
    /// <returns><c>true</c> if the specified object is equal to the current edge; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is GraphEdge<TNode> other && Equals(other);
    }
    
    /// <summary>
    /// Determines whether the specified edge is equal to the current edge.
    /// Two edges are considered equal if they have the same source and target nodes.
    /// Metadata is not considered in equality comparison.
    /// </summary>
    /// <param name="other">The edge to compare with the current edge.</param>
    /// <returns><c>true</c> if the specified edge is equal to the current edge; otherwise, <c>false</c>.</returns>
    public bool Equals(GraphEdge<TNode>? other)
    {
        if (other is null)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;
        
        return EqualityComparer<TNode>.Default.Equals(Source, other.Source) &&
               EqualityComparer<TNode>.Default.Equals(Target, other.Target);
    }
    
    /// <summary>
    /// Returns a hash code for the current edge based on the source and target nodes.
    /// Metadata is not included in the hash code calculation.
    /// </summary>
    /// <returns>A hash code for the current edge.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Source, Target);
    }
    
    /// <summary>
    /// Returns a string representation of the edge.
    /// </summary>
    /// <returns>A string that represents the current edge in the format "Source → Target".</returns>
    public override string ToString()
    {
        var metadataInfo = Metadata?.Count > 0 ? $" [{Metadata.Count} metadata items]" : "";
        return $"{Source} → {Target}{metadataInfo}";
    }
    
    /// <summary>
    /// Determines whether two edges are equal.
    /// </summary>
    /// <param name="left">The first edge to compare.</param>
    /// <param name="right">The second edge to compare.</param>
    /// <returns><c>true</c> if the edges are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(GraphEdge<TNode>? left, GraphEdge<TNode>? right)
    {
        return EqualityComparer<GraphEdge<TNode>>.Default.Equals(left, right);
    }
    
    /// <summary>
    /// Determines whether two edges are not equal.
    /// </summary>
    /// <param name="left">The first edge to compare.</param>
    /// <param name="right">The second edge to compare.</param>
    /// <returns><c>true</c> if the edges are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(GraphEdge<TNode>? left, GraphEdge<TNode>? right)
    {
        return !(left == right);
    }
}
