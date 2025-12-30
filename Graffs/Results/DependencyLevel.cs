using System.Collections.Immutable;
using System.Linq;

namespace Graffs.Results;

/// <summary>
/// Represents a single dependency level in graph slicing analysis.
/// A dependency level contains all nodes at the same distance from root nodes.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph</typeparam>
public sealed record DependencyLevel<TNode> where TNode : notnull
{
    /// <summary>
    /// The 0-based depth from root nodes (level 0 = roots, level 1 = direct dependents of roots, etc.).
    /// </summary>
    public int LevelIndex { get; init; }

    /// <summary>
    /// All nodes at this dependency level, in deterministic order.
    /// </summary>
    public ImmutableArray<TNode> Nodes { get; init; } = ImmutableArray<TNode>.Empty;

    /// <summary>
    /// All nodes from previous levels that this level depends on.
    /// </summary>
    public ImmutableHashSet<TNode> LevelDependencies { get; init; } = ImmutableHashSet<TNode>.Empty;

    /// <summary>
    /// All nodes in future levels that depend on this level.
    /// </summary>
    public ImmutableHashSet<TNode> LevelDependents { get; init; } = ImmutableHashSet<TNode>.Empty;

    /// <summary>
    /// True if this is the root level (level 0, no dependencies).
    /// </summary>
    public bool IsRootLevel => LevelIndex == 0;

    /// <summary>
    /// True if this is a leaf level (no dependents in future levels).
    /// </summary>
    public bool IsLeafLevel => LevelDependents.Count == 0;

    /// <summary>
    /// Number of nodes at this level.
    /// </summary>
    public int NodeCount => Nodes.Length;

    /// <summary>
    /// Creates a new dependency level.
    /// </summary>
    /// <param name="levelIndex">The 0-based level index</param>
    /// <param name="nodes">Nodes at this level</param>
    /// <param name="levelDependencies">Dependencies from previous levels</param>
    /// <param name="levelDependents">Dependents in future levels</param>
    /// <returns>A new DependencyLevel instance</returns>
    public static DependencyLevel<TNode> Create(
        int levelIndex,
        ImmutableArray<TNode> nodes,
        ImmutableHashSet<TNode> levelDependencies,
        ImmutableHashSet<TNode> levelDependents)
    {
        return new DependencyLevel<TNode>
        {
            LevelIndex = levelIndex,
            Nodes = nodes,
            LevelDependencies = levelDependencies,
            LevelDependents = levelDependents
        };
    }

    /// <summary>
    /// Returns a string representation of this dependency level.
    /// </summary>
    public override string ToString()
    {
        var nodeList = string.Join(", ", Nodes.Take(3));
        var suffix = Nodes.Length > 3 ? $" (+{Nodes.Length - 3} more)" : "";
        return $"Level {LevelIndex}: [{nodeList}{suffix}] ({Nodes.Length} nodes)";
    }
}
