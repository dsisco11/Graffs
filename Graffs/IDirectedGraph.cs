using System.Collections.Generic;

namespace Graffs;

/// <summary>
/// Generic interface for directed graphs with typed nodes.
/// Provides efficient operations for dependency analysis, topological sorting, and connectivity analysis.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph. Must be non-null.</typeparam>
public interface IDirectedGraph<TNode> : IReadOnlyCollection<TNode>
    where TNode : notnull
{
    /// <summary>
    /// Gets all nodes in the graph as an immutable set.
    /// </summary>
    /// <remarks>
    /// This property provides O(1) access to the complete set of nodes.
    /// The returned set is immutable and reflects the current state of the graph.
    /// </remarks>
    IReadOnlySet<TNode> Nodes { get; }
    
    /// <summary>
    /// Gets all edges in the graph as an immutable collection.
    /// </summary>
    /// <remarks>
    /// This property provides access to all edges in the graph.
    /// The returned collection is immutable and reflects the current state of the graph.
    /// </remarks>
    IReadOnlyCollection<IGraphEdge<TNode>> Edges { get; }
    
    /// <summary>
    /// Gets the nodes that the specified node depends on (incoming edges).
    /// </summary>
    /// <param name="node">The node to get dependencies for.</param>
    /// <returns>An immutable set of nodes that the specified node depends on.</returns>
    /// <remarks>
    /// This method returns the nodes that have outgoing edges to the specified node.
    /// In dependency terms, these are the dependencies of the specified node.
    /// Time complexity: O(1) average case for most implementations.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    IReadOnlySet<TNode> GetDependencies(TNode node);
    
    /// <summary>
    /// Gets the nodes that depend on the specified node (outgoing edges).
    /// </summary>
    /// <param name="node">The node to get dependents for.</param>
    /// <returns>An immutable set of nodes that depend on the specified node.</returns>
    /// <remarks>
    /// This method returns the nodes that the specified node has outgoing edges to.
    /// In dependency terms, these are the nodes that depend on the specified node.
    /// Time complexity: O(1) average case for most implementations.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    IReadOnlySet<TNode> GetDependents(TNode node);
    
    /// <summary>
    /// Checks if there is a direct edge from one node to another.
    /// </summary>
    /// <param name="from">The source node of the edge.</param>
    /// <param name="to">The target node of the edge.</param>
    /// <returns><c>true</c> if there is a direct edge from <paramref name="from"/> to <paramref name="to"/>; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method only checks for direct edges, not transitive paths.
    /// Time complexity: O(1) average case for most implementations.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="to"/> is null.</exception>
    bool HasEdge(TNode from, TNode to);
    
    /// <summary>
    /// Checks if there is any path (direct or indirect) from one node to another.
    /// </summary>
    /// <param name="from">The source node of the path.</param>
    /// <param name="to">The target node of the path.</param>
    /// <returns><c>true</c> if there is any path from <paramref name="from"/> to <paramref name="to"/>; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method checks for transitive reachability, not just direct edges.
    /// Time complexity: O(V + E) in the worst case, where V is vertices and E is edges.
    /// Results may be cached by implementations for better performance.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="to"/> is null.</exception>
    bool HasPath(TNode from, TNode to);
    
    /// <summary>
    /// Gets all outgoing edges from the specified node.
    /// </summary>
    /// <param name="node">The node to get outgoing edges for.</param>
    /// <returns>An immutable collection of edges originating from the specified node.</returns>
    /// <remarks>
    /// This method returns all edges where the specified node is the source.
    /// Time complexity: O(k) where k is the number of outgoing edges.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    IReadOnlyCollection<IGraphEdge<TNode>> GetOutgoingEdges(TNode node);
    
    /// <summary>
    /// Gets all incoming edges to the specified node.
    /// </summary>
    /// <param name="node">The node to get incoming edges for.</param>
    /// <returns>An immutable collection of edges targeting the specified node.</returns>
    /// <remarks>
    /// This method returns all edges where the specified node is the target.
    /// Time complexity: O(k) where k is the number of incoming edges.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    IReadOnlyCollection<IGraphEdge<TNode>> GetIncomingEdges(TNode node);
}
