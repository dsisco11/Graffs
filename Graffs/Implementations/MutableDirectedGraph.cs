using System.Collections.Immutable;

namespace Graffs.Implementations;

/// <summary>A mutable directed graph with indexed incoming and outgoing relationships.</summary>
/// <typeparam name="TNode">The non-null node type.</typeparam>
/// <remarks>
/// Edges point from dependency to dependent. Cycles and self-edges are allowed.
/// The comparer defines identity; the first registered instance is retained in all neighbor results
/// until removal. Node equality and hash codes must remain stable while registered.
/// Operations are not thread safe. Neighbor queries on this concrete type return live read-only views;
/// queries through <see cref="IDirectedGraph{TNode}"/> return immutable neighborhood snapshots.
/// </remarks>
public sealed partial class MutableDirectedGraph<TNode> : IDirectedGraph<TNode> where TNode : notnull
{
    private readonly Dictionary<TNode, NodeEntry> _nodes;
    private readonly IReadOnlySet<TNode> _emptyNeighbors;
    private IReadOnlySet<TNode>? _nodesSnapshot;
    private IReadOnlyCollection<IGraphEdge<TNode>>? _edgesSnapshot;

    #region Public API

    #region Construction

    /// <summary>Creates an empty graph using the supplied comparer, or the default comparer.</summary>
    /// <param name="comparer">The comparer used for node identity in every index.</param>
    public MutableDirectedGraph(IEqualityComparer<TNode>? comparer = null)
    {
        _nodes = new Dictionary<TNode, NodeEntry>(comparer);
        _emptyNeighbors = ImmutableHashSet.Create<TNode>(_nodes.Comparer);
    }

    #endregion

    #region Mutation

    /// <summary>Registers a node in O(1) expected time; returns false if already registered.</summary>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public bool AddNode(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_nodes.ContainsKey(node))
            return false;

        _nodes.Add(node, new NodeEntry(node, _nodes.Comparer));
        _nodesSnapshot = null;
        return true;
    }

    /// <summary>Adds a dependency-to-dependent edge and any missing endpoints in O(1) expected time.</summary>
    /// <returns>True if the edge was added; false if it already existed.</returns>
    /// <exception cref="ArgumentNullException">Either endpoint is null; no nodes are added.</exception>
    public bool AddEdge(TNode dependency, TNode dependent)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(dependent);

        // Resolve canonical registrations before storing either end of the relationship.
        var source = GetOrAddEntry(dependency);
        var target = GetOrAddEntry(dependent);
        if (!source.Dependents.Add(target.Node))
            return false;

        target.Dependencies.Add(source.Node);
        _edgesSnapshot = null;
        return true;
    }

    /// <summary>Removes an edge in O(1) expected time, retaining both endpoint registrations.</summary>
    /// <returns>True if the edge existed.</returns>
    /// <exception cref="ArgumentNullException">Either endpoint is null.</exception>
    public bool RemoveEdge(TNode dependency, TNode dependent)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(dependent);
        if (!_nodes.TryGetValue(dependency, out var source) || !source.Dependents.Remove(dependent))
            return false;

        // Every forward edge has a corresponding reverse entry.
        _nodes[dependent].Dependencies.Remove(source.Node);
        _edgesSnapshot = null;
        return true;
    }

    /// <summary>Removes a node and all incident edges in O(1 + incoming + outgoing) expected time.</summary>
    /// <returns>True if the node was registered.</returns>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public bool RemoveNode(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_nodes.TryGetValue(node, out var entry))
            return false;

        // An isolated registration changes the node set but leaves the edge snapshot valid.
        if (entry.Dependencies.View.Count != 0 || entry.Dependents.View.Count != 0)
            _edgesSnapshot = null;

        // Visit only incident neighbors. Defer self-edge cleanup to avoid changing an iterated set.
        foreach (var dependency in entry.Dependencies.View)
        {
            var neighbor = _nodes[dependency];
            if (!ReferenceEquals(neighbor, entry))
                neighbor.Dependents.Remove(entry.Node);
        }
        foreach (var dependent in entry.Dependents.View)
        {
            var neighbor = _nodes[dependent];
            if (!ReferenceEquals(neighbor, entry))
                neighbor.Dependencies.Remove(entry.Node);
        }

        // Previously obtained views become empty and stay detached after re-registration.
        entry.Dependencies.Clear();
        entry.Dependents.Clear();
        _nodes.Remove(entry.Node);
        _nodesSnapshot = null;
        return true;
    }

    /// <summary>Removes all nodes and edges and empties existing live views.</summary>
    public void Clear()
    {
        if (_nodes.Count == 0)
            return;

        // Empty edge snapshots stay valid when only isolated registrations are cleared.
        _nodesSnapshot = null;
        if (_edgesSnapshot is { Count: > 0 })
            _edgesSnapshot = null;

        // Clear the owned sets as well as the index so retained views release their neighbors.
        foreach (var entry in _nodes.Values)
        {
            entry.Dependencies.Clear();
            entry.Dependents.Clear();
        }
        _nodes.Clear();
    }

    #endregion

    #region Queries

    /// <summary>Checks node registration in O(1) expected time.</summary>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public bool ContainsNode(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _nodes.ContainsKey(node);
    }

    /// <summary>Checks for a direct dependency-to-dependent edge in O(1) expected time.</summary>
    /// <exception cref="ArgumentNullException">Either endpoint is null.</exception>
    public bool HasEdge(TNode dependency, TNode dependent)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(dependent);
        return _nodes.TryGetValue(dependency, out var entry) && entry.Dependents.Contains(dependent);
    }

    /// <summary>Returns a live read-only view of direct dependencies in O(1) expected time.</summary>
    /// <remarks>
    /// Missing nodes return an empty collection that stays empty. Existing views reflect edge changes
    /// until node removal or Clear, when they become permanently empty. Enumeration order is unspecified.
    /// Do not mutate this neighborhood while enumerating; use GetDependenciesSnapshot instead.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public IReadOnlyCollection<TNode> GetDependencies(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _nodes.TryGetValue(node, out var entry) ? entry.Dependencies.View : Array.Empty<TNode>();
    }

    /// <summary>Returns a live read-only view of direct dependents in O(1) expected time.</summary>
    /// <remarks>
    /// Missing nodes return an empty collection that stays empty. Existing views reflect edge changes
    /// until node removal or Clear, when they become permanently empty. Enumeration order is unspecified.
    /// Do not mutate this neighborhood while enumerating; use GetDependentsSnapshot instead.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public IReadOnlyCollection<TNode> GetDependents(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _nodes.TryGetValue(node, out var entry) ? entry.Dependents.View : Array.Empty<TNode>();
    }

    /// <summary>Copies only the direct dependencies into a stable read-only snapshot in O(1 + incoming) time.</summary>
    /// <remarks>Missing nodes return empty. Node objects themselves are not cloned. Order is unspecified.</remarks>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public IReadOnlyCollection<TNode> GetDependenciesSnapshot(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _nodes.TryGetValue(node, out var entry) ? entry.Dependencies.Snapshot() : Array.Empty<TNode>();
    }

    /// <summary>Copies only the direct dependents into a stable read-only snapshot in O(1 + outgoing) time.</summary>
    /// <remarks>Missing nodes return empty. Node objects themselves are not cloned. Order is unspecified.</remarks>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public IReadOnlyCollection<TNode> GetDependentsSnapshot(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _nodes.TryGetValue(node, out var entry) ? entry.Dependents.Snapshot() : Array.Empty<TNode>();
    }

    #endregion

    #endregion

    #region Private

    /// <summary>Resolves the original registration or creates its paired adjacency sets.</summary>
    private NodeEntry GetOrAddEntry(TNode node)
    {
        if (_nodes.TryGetValue(node, out var entry))
            return entry;

        entry = new NodeEntry(node, _nodes.Comparer);
        _nodes.Add(node, entry);
        _nodesSnapshot = null;
        return entry;
    }

    /// <summary>Owns a canonical node and its two adjacency indexes and cached views.</summary>
    private sealed class NodeEntry
    {
        public readonly TNode Node;
        public readonly DirectedGraphNeighbors<TNode> Dependencies;
        public readonly DirectedGraphNeighbors<TNode> Dependents;

        #region Public API

        /// <summary>Creates both indexes with the graph's identity comparer.</summary>
        public NodeEntry(TNode node, IEqualityComparer<TNode> comparer)
        {
            Node = node;
            Dependencies = new DirectedGraphNeighbors<TNode>(comparer);
            Dependents = new DirectedGraphNeighbors<TNode>(comparer);
        }

        #endregion
    }

    #endregion
}
