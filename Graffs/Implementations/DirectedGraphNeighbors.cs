using System.Collections.Immutable;

namespace Graffs.Implementations;

/// <summary>Stores unique neighbors densely so traversal visits only current relationships.</summary>
/// <typeparam name="TNode">The non-null node type.</typeparam>
internal sealed class DirectedGraphNeighbors<TNode> where TNode : notnull
{
    private Dictionary<TNode, int> _positions;
    private readonly List<TNode> _neighbors = new();
    private IReadOnlySet<TNode>? _setSnapshot;

    #region Public API

    #region Construction and Access

    /// <summary>Creates a neighborhood with a cached read-only view and the graph's comparer.</summary>
    public DirectedGraphNeighbors(IEqualityComparer<TNode> comparer)
    {
        _positions = new Dictionary<TNode, int>(comparer);
        View = _neighbors.AsReadOnly();
    }

    /// <summary>Gets a live wrapper that prevents mutations through collection interfaces.</summary>
    public IReadOnlyCollection<TNode> View { get; }

    /// <summary>Gets an immutable comparer-aware snapshot, reused until this neighborhood changes.</summary>
    public IReadOnlySet<TNode> SetSnapshot =>
        _setSnapshot ??= _neighbors.ToImmutableHashSet(_positions.Comparer);

    /// <summary>Checks direct membership using the supplied identity comparer.</summary>
    public bool Contains(TNode node) => _positions.ContainsKey(node);

    /// <summary>Copies only current neighbors into an independently owned read-only collection.</summary>
    public IReadOnlyCollection<TNode> Snapshot() => Array.AsReadOnly(_neighbors.ToArray());

    #endregion

    #region Mutation

    /// <summary>Adds a neighbor if absent, preserving the instance already stored on duplicates.</summary>
    public bool Add(TNode node)
    {
        if (!_positions.TryAdd(node, _neighbors.Count))
            return false;

        _neighbors.Add(node);
        _setSnapshot = null;
        return true;
    }

    /// <summary>Removes a neighbor in expected constant time without leaving traversal holes.</summary>
    public bool Remove(TNode node)
    {
        if (!_positions.Remove(node, out var position))
            return false;

        // Fill the gap with the final neighbor and repair only that neighbor's index.
        var lastPosition = _neighbors.Count - 1;
        if (position != lastPosition)
        {
            var lastNode = _neighbors[lastPosition];
            _neighbors[position] = lastNode;
            _positions[lastNode] = position;
        }
        _neighbors.RemoveAt(lastPosition);
        _setSnapshot = null;
        return true;
    }

    /// <summary>Empties the neighborhood and its live view.</summary>
    public void Clear()
    {
        if (_neighbors.Count != 0)
            _setSnapshot = null;
        // Discard historical hash capacity rather than scanning slots for formerly removed neighbors.
        _positions = new Dictionary<TNode, int>(_positions.Comparer);
        _neighbors.Clear();
    }

    #endregion

    #endregion
}
