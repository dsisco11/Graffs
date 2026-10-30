using System.Collections;
using System.Collections.Immutable;

namespace Graffs.Implementations;

/// <summary>Provides immutable collection snapshots and traversal for the directed graph contract.</summary>
public sealed partial class MutableDirectedGraph<TNode>
{
    #region Public API

    #region Graph Collections

    /// <summary>Gets an immutable node snapshot, reused until node registrations change.</summary>
    /// <remarks>
    /// The first read after a registration change constructs a graph-wide snapshot. Subsequent reads
    /// take O(1) time. The snapshot uses the graph's comparer and retains original registration references.
    /// </remarks>
    public IReadOnlySet<TNode> Nodes => _nodesSnapshot ??= _nodes.Keys.ToImmutableHashSet(_nodes.Comparer);

    /// <summary>Gets an immutable edge snapshot, reused until relationships change.</summary>
    /// <remarks>The first read after a relationship change visits the whole graph; subsequent reads take O(1) time.</remarks>
    public IReadOnlyCollection<IGraphEdge<TNode>> Edges
    {
        get
        {
            if (_edgesSnapshot is not null)
                return _edgesSnapshot;

            // Enumerate each forward relationship once, retaining both canonical endpoint instances.
            var edges = ImmutableArray.CreateBuilder<IGraphEdge<TNode>>();
            foreach (var entry in _nodes.Values)
                foreach (var dependent in entry.Dependents.View)
                    edges.Add(new GraphEdge<TNode>(entry.Node, dependent));
            return _edgesSnapshot = edges.ToImmutable();
        }
    }

    /// <summary>Gets the current number of registered nodes in O(1) time.</summary>
    public int Count => _nodes.Count;

    /// <summary>Enumerates registered nodes without copying them.</summary>
    /// <remarks>Do not add or remove nodes during enumeration; enumerate Nodes for a stable snapshot.</remarks>
    public IEnumerator<TNode> GetEnumerator() => _nodes.Keys.GetEnumerator();

    /// <summary>Enumerates registered nodes through the non-generic collection contract.</summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Immutable Neighborhoods

    /// <summary>Returns an immutable snapshot of direct dependencies using the graph's comparer.</summary>
    /// <remarks>Copies only this neighborhood on its first read after a change, then reuses the snapshot. Missing nodes return empty.</remarks>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    IReadOnlySet<TNode> IDirectedGraph<TNode>.GetDependencies(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _nodes.TryGetValue(node, out var entry)
            ? entry.Dependencies.SetSnapshot
            : _emptyNeighbors;
    }

    /// <summary>Returns an immutable snapshot of direct dependents using the graph's comparer.</summary>
    /// <remarks>Copies only this neighborhood on its first read after a change, then reuses the snapshot. Missing nodes return empty.</remarks>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    IReadOnlySet<TNode> IDirectedGraph<TNode>.GetDependents(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _nodes.TryGetValue(node, out var entry)
            ? entry.Dependents.SetSnapshot
            : _emptyNeighbors;
    }

    /// <summary>Copies only outgoing edges into an immutable snapshot in O(1 + outgoing) time.</summary>
    /// <remarks>Missing nodes return empty; endpoints retain their original registration references.</remarks>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public IReadOnlyCollection<IGraphEdge<TNode>> GetOutgoingEdges(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_nodes.TryGetValue(node, out var entry))
            return ImmutableArray<IGraphEdge<TNode>>.Empty;

        // Materialize only this node's forward relationships with canonical endpoints.
        var edges = ImmutableArray.CreateBuilder<IGraphEdge<TNode>>(entry.Dependents.View.Count);
        foreach (var dependent in entry.Dependents.View)
            edges.Add(new GraphEdge<TNode>(entry.Node, dependent));
        return edges.MoveToImmutable();
    }

    /// <summary>Copies only incoming edges into an immutable snapshot in O(1 + incoming) time.</summary>
    /// <remarks>Missing nodes return empty; endpoints retain their original registration references.</remarks>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public IReadOnlyCollection<IGraphEdge<TNode>> GetIncomingEdges(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_nodes.TryGetValue(node, out var entry))
            return ImmutableArray<IGraphEdge<TNode>>.Empty;

        // Materialize only this node's reverse relationships with canonical endpoints.
        var edges = ImmutableArray.CreateBuilder<IGraphEdge<TNode>>(entry.Dependencies.View.Count);
        foreach (var dependency in entry.Dependencies.View)
            edges.Add(new GraphEdge<TNode>(dependency, entry.Node));
        return edges.MoveToImmutable();
    }

    #endregion

    #region Reachability

    /// <summary>Checks directed reachability by visiting only the source's reachable relationships.</summary>
    /// <remarks>
    /// A registered node reaches itself by a zero-length path. Missing endpoints return false.
    /// Uses the graph's comparer and O(V + E) expected time in the worst case, without caching.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Either endpoint is null.</exception>
    public bool HasPath(TNode from, TNode to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (!_nodes.TryGetValue(from, out var source) || !_nodes.TryGetValue(to, out var target))
            return false;
        if (ReferenceEquals(source, target))
            return true;

        // Track comparer-defined identity so cycles terminate without merging distinct registrations.
        var visited = new HashSet<TNode>(_nodes.Comparer) { source.Node };
        var pending = new Queue<NodeEntry>();
        pending.Enqueue(source);
        while (pending.TryDequeue(out var current))
        {
            foreach (var dependent in current.Dependents.View)
            {
                if (_nodes.Comparer.Equals(dependent, target.Node))
                    return true;
                if (visited.Add(dependent))
                    pending.Enqueue(_nodes[dependent]);
            }
        }
        return false;
    }

    #endregion

    #endregion
}
