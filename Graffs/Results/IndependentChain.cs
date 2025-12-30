using System.Collections.Immutable;
using System.Linq;

namespace Graffs.Results;

/// <summary>
/// Represents an independent dependency chain in graph slicing analysis.
/// An independent chain is a subgraph with no external inter-dependencies with other chains.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph</typeparam>
public sealed record IndependentChain<TNode> where TNode : notnull
{
    /// <summary>
    /// Unique identifier for this chain.
    /// </summary>
    public string ChainId { get; init; } = string.Empty;

    /// <summary>
    /// All nodes in this chain, in topological order.
    /// </summary>
    public ImmutableArray<TNode> Nodes { get; init; } = ImmutableArray<TNode>.Empty;

    /// <summary>
    /// Dependency levels within this chain only.
    /// </summary>
    public ImmutableArray<DependencyLevel<TNode>> ChainLevels { get; init; } = ImmutableArray<DependencyLevel<TNode>>.Empty;

    /// <summary>
    /// Number of dependency levels in this chain.
    /// </summary>
    public int ChainDepth => ChainLevels.Length;

    /// <summary>
    /// The starting node of this chain (root node with no dependencies).
    /// </summary>
    public TNode RootNode { get; init; } = default!;

    /// <summary>
    /// Terminal nodes of this chain (nodes with no dependents within the chain).
    /// </summary>
    public ImmutableHashSet<TNode> LeafNodes { get; init; } = ImmutableHashSet<TNode>.Empty;

    /// <summary>
    /// Number of nodes in this chain.
    /// </summary>
    public int NodeCount => Nodes.Length;

    /// <summary>
    /// True if this chain contains only a single node.
    /// </summary>
    public bool IsSingleNode => Nodes.Length == 1;

    /// <summary>
    /// True if this chain is linear (each level has at most one node).
    /// </summary>
    public bool IsLinear => ChainLevels.All(level => level.NodeCount <= 1);

    /// <summary>
    /// Creates a new independent chain.
    /// </summary>
    /// <param name="chainId">Unique identifier</param>
    /// <param name="rootNode">Starting node</param>
    /// <param name="nodes">All nodes in topological order</param>
    /// <param name="chainLevels">Dependency levels within chain</param>
    /// <param name="leafNodes">Terminal nodes</param>
    /// <returns>A new IndependentChain instance</returns>
    public static IndependentChain<TNode> Create(
        string chainId,
        TNode rootNode,
        ImmutableArray<TNode> nodes,
        ImmutableArray<DependencyLevel<TNode>> chainLevels,
        ImmutableHashSet<TNode> leafNodes)
    {
        return new IndependentChain<TNode>
        {
            ChainId = chainId,
            RootNode = rootNode,
            Nodes = nodes,
            ChainLevels = chainLevels,
            LeafNodes = leafNodes
        };
    }

    /// <summary>
    /// Returns a string representation of this independent chain.
    /// </summary>
    public override string ToString()
    {
        var nodeList = string.Join(" → ", Nodes.Take(3));
        var suffix = Nodes.Length > 3 ? $" → (+{Nodes.Length - 3} more)" : "";
        return $"Chain {ChainId}: {nodeList}{suffix} ({Nodes.Length} nodes, {ChainDepth} levels)";
    }
}
