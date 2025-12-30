using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Graffs.Results;

/// <summary>
/// Represents a strongly connected component in a directed graph.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the component</typeparam>
public sealed record StronglyConnectedComponent<TNode>(
    ImmutableHashSet<TNode> Nodes,
    int Size,
    bool IsTrivial,
    DateTime DiscoveredAt
) where TNode : notnull
{
    /// <summary>
    /// Gets whether this component contains only a single node with no self-loop.
    /// </summary>
    public bool IsSingleNode => Size == 1;

    /// <summary>
    /// Gets whether this component contains cycles (non-trivial component).
    /// </summary>
    public bool HasCycles => !IsTrivial;

    /// <summary>
    /// Gets the nodes in the component as a sorted list for consistent ordering.
    /// </summary>
    public IEnumerable<TNode> SortedNodes => Nodes.OrderBy(n => n.ToString());

    /// <summary>
    /// Checks if the component contains the specified node.
    /// </summary>
    /// <param name="node">The node to check for</param>
    /// <returns>True if the component contains the node, false otherwise</returns>
    public bool ContainsNode(TNode node) => Nodes.Contains(node);

    /// <summary>
    /// Creates a trivial strongly connected component (single node, no cycles).
    /// </summary>
    /// <param name="node">The single node in the component</param>
    /// <returns>A trivial strongly connected component</returns>
    public static StronglyConnectedComponent<TNode> Trivial(TNode node)
    {
        return new StronglyConnectedComponent<TNode>(
            Nodes: ImmutableHashSet.Create(node),
            Size: 1,
            IsTrivial: true,
            DiscoveredAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Creates a non-trivial strongly connected component (multiple nodes or cycles).
    /// </summary>
    /// <param name="nodes">The nodes in the component</param>
    /// <returns>A non-trivial strongly connected component</returns>
    public static StronglyConnectedComponent<TNode> NonTrivial(IEnumerable<TNode> nodes)
    {
        var nodeSet = nodes.ToImmutableHashSet();
        return new StronglyConnectedComponent<TNode>(
            Nodes: nodeSet,
            Size: nodeSet.Count,
            IsTrivial: false,
            DiscoveredAt: DateTime.UtcNow
        );
    }
}

/// <summary>
/// Contains statistics about connectivity analysis results.
/// </summary>
public sealed record ConnectivityStatistics(
    int TotalComponents,
    int TrivialComponents,
    int NonTrivialComponents,
    int LargestComponentSize,
    int SmallestComponentSize,
    double AverageComponentSize,
    IReadOnlyList<int> ComponentSizeDistribution
)
{
    /// <summary>
    /// Gets whether the graph has multiple strongly connected components.
    /// </summary>
    public bool HasMultipleComponents => TotalComponents > 1;

    /// <summary>
    /// Gets whether the graph is strongly connected (single component).
    /// </summary>
    public bool IsStronglyConnected => TotalComponents == 1;

    /// <summary>
    /// Gets the percentage of components that are trivial.
    /// </summary>
    public double TrivialComponentPercentage => TotalComponents > 0 ? (double)TrivialComponents / TotalComponents * 100 : 0;

    /// <summary>
    /// Gets the percentage of components that are non-trivial.
    /// </summary>
    public double NonTrivialComponentPercentage => TotalComponents > 0 ? (double)NonTrivialComponents / TotalComponents * 100 : 0;

    /// <summary>
    /// Creates empty connectivity statistics for graphs with no components.
    /// </summary>
    /// <returns>Empty connectivity statistics</returns>
    public static ConnectivityStatistics Empty => new(
        TotalComponents: 0,
        TrivialComponents: 0,
        NonTrivialComponents: 0,
        LargestComponentSize: 0,
        SmallestComponentSize: 0,
        AverageComponentSize: 0,
        ComponentSizeDistribution: Array.Empty<int>()
    );

    /// <summary>
    /// Creates connectivity statistics from a collection of components.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the components</typeparam>
    /// <param name="components">The components to analyze</param>
    /// <returns>Statistics about the components</returns>
    public static ConnectivityStatistics FromComponents<TNode>(IEnumerable<StronglyConnectedComponent<TNode>> components) where TNode : notnull
    {
        var componentList = components.ToList();
        if (componentList.Count == 0)
            return Empty;

        var sizes = componentList.Select(c => c.Size).ToList();
        var trivialCount = componentList.Count(c => c.IsTrivial);
        var nonTrivialCount = componentList.Count(c => !c.IsTrivial);

        // Create size distribution (index = size - 1, value = count)
        var maxSize = sizes.Max();
        var distribution = new int[maxSize];
        foreach (var size in sizes)
        {
            distribution[size - 1]++;
        }

        return new ConnectivityStatistics(
            TotalComponents: componentList.Count,
            TrivialComponents: trivialCount,
            NonTrivialComponents: nonTrivialCount,
            LargestComponentSize: sizes.Max(),
            SmallestComponentSize: sizes.Min(),
            AverageComponentSize: sizes.Average(),
            ComponentSizeDistribution: distribution
        );
    }
}

/// <summary>
/// Represents the result of reachability analysis from a starting node.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph</typeparam>
public sealed record ReachabilityResult<TNode>(
    TNode StartNode,
    ImmutableHashSet<TNode> ReachableNodes,
    ImmutableDictionary<TNode, int> DistanceMap,
    TimeSpan ProcessingTime,
    string Algorithm,
    DateTime CompletedAt
) where TNode : notnull
{
    /// <summary>
    /// Gets the number of reachable nodes (including the start node).
    /// </summary>
    public int ReachableCount => ReachableNodes.Count;

    /// <summary>
    /// Gets the processing time in milliseconds.
    /// </summary>
    public double ProcessingTimeMs => ProcessingTime.TotalMilliseconds;

    /// <summary>
    /// Gets the maximum distance from the start node to any reachable node.
    /// </summary>
    public int MaxDistance => DistanceMap.Count > 0 ? DistanceMap.Values.Max() : 0;

    /// <summary>
    /// Gets the average distance from the start node to all reachable nodes.
    /// </summary>
    public double AverageDistance => DistanceMap.Count > 0 ? DistanceMap.Values.Average() : 0;

    /// <summary>
    /// Checks if a specific node is reachable from the start node.
    /// </summary>
    /// <param name="node">The node to check for reachability</param>
    /// <returns>True if the node is reachable, false otherwise</returns>
    public bool IsReachable(TNode node) => ReachableNodes.Contains(node);

    /// <summary>
    /// Gets the distance from the start node to the specified node.
    /// </summary>
    /// <param name="node">The target node</param>
    /// <returns>The distance to the node, or -1 if not reachable</returns>
    public int GetDistance(TNode node) => DistanceMap.TryGetValue(node, out var distance) ? distance : -1;

    /// <summary>
    /// Gets all nodes at a specific distance from the start node.
    /// </summary>
    /// <param name="distance">The distance to filter by</param>
    /// <returns>All nodes at the specified distance</returns>
    public IEnumerable<TNode> GetNodesAtDistance(int distance) =>
        DistanceMap.Where(kvp => kvp.Value == distance).Select(kvp => kvp.Key);
}

/// <summary>
/// Represents the result of connectivity analysis on a directed graph.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph</typeparam>
public sealed record ConnectivityResult<TNode>(
    ImmutableArray<StronglyConnectedComponent<TNode>> Components,
    ConnectivityStatistics Statistics,
    TimeSpan ProcessingTime,
    string Algorithm,
    DateTime CompletedAt
) where TNode : notnull
{
    /// <summary>
    /// Gets the number of strongly connected components.
    /// </summary>
    public int ComponentCount => Components.Length;

    /// <summary>
    /// Gets the processing time in milliseconds.
    /// </summary>
    public double ProcessingTimeMs => ProcessingTime.TotalMilliseconds;

    /// <summary>
    /// Gets whether the graph is strongly connected (single component).
    /// </summary>
    public bool IsStronglyConnected => ComponentCount == 1;

    /// <summary>
    /// Gets whether the graph has multiple components.
    /// </summary>
    public bool HasMultipleComponents => ComponentCount > 1;

    /// <summary>
    /// Gets all trivial components (single nodes with no self-loops).
    /// </summary>
    public IEnumerable<StronglyConnectedComponent<TNode>> TrivialComponents =>
        Components.Where(c => c.IsTrivial);

    /// <summary>
    /// Gets all non-trivial components (multiple nodes or cycles).
    /// </summary>
    public IEnumerable<StronglyConnectedComponent<TNode>> NonTrivialComponents =>
        Components.Where(c => !c.IsTrivial);

    /// <summary>
    /// Gets the largest strongly connected component.
    /// </summary>
    public StronglyConnectedComponent<TNode>? LargestComponent =>
        Components.Length > 0 ? Components.MaxBy(c => c.Size) : null;

    /// <summary>
    /// Finds the component that contains the specified node.
    /// </summary>
    /// <param name="node">The node to find the component for</param>
    /// <returns>The component containing the node, or null if not found</returns>
    public StronglyConnectedComponent<TNode>? FindComponentContaining(TNode node) =>
        Components.FirstOrDefault(c => c.ContainsNode(node));

    /// <summary>
    /// Checks if two nodes are in the same strongly connected component.
    /// </summary>
    /// <param name="node1">The first node</param>
    /// <param name="node2">The second node</param>
    /// <returns>True if both nodes are in the same component, false otherwise</returns>
    public bool AreInSameComponent(TNode node1, TNode node2)
    {
        var component = FindComponentContaining(node1);
        return component != null && component.ContainsNode(node2);
    }

    /// <summary>
    /// Creates a connectivity result for an empty graph.
    /// </summary>
    /// <param name="processingTime">Time taken for the analysis</param>
    /// <param name="algorithm">Algorithm used for analysis</param>
    /// <returns>Result for an empty graph</returns>
    public static ConnectivityResult<TNode> Empty(TimeSpan processingTime, string algorithm) =>
        new(
            Components: ImmutableArray<StronglyConnectedComponent<TNode>>.Empty,
            Statistics: ConnectivityStatistics.Empty,
            ProcessingTime: processingTime,
            Algorithm: algorithm,
            CompletedAt: DateTime.UtcNow
        );

    /// <summary>
    /// Creates a connectivity result with the specified components.
    /// </summary>
    /// <param name="components">The strongly connected components</param>
    /// <param name="processingTime">Time taken for the analysis</param>
    /// <param name="algorithm">Algorithm used for analysis</param>
    /// <returns>Result containing the components and statistics</returns>
    public static ConnectivityResult<TNode> WithComponents(
        IEnumerable<StronglyConnectedComponent<TNode>> components,
        TimeSpan processingTime,
        string algorithm)
    {
        var componentArray = components.ToImmutableArray();
        return new ConnectivityResult<TNode>(
            Components: componentArray,
            Statistics: ConnectivityStatistics.FromComponents(componentArray),
            ProcessingTime: processingTime,
            Algorithm: algorithm,
            CompletedAt: DateTime.UtcNow
        );
    }
}
