using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Graffs;
using Graffs.Results;

namespace Graffs.Algorithms;

/// <summary>
/// Provides algorithms for connectivity analysis in directed graphs.
/// This class coordinates access to specialized algorithm implementations for optimal performance.
/// 
/// Algorithm Delegation:
/// - Strongly Connected Components: Uses Tarjan's algorithm (O(V + E))
/// - Weakly Connected Components: Uses Union-Find with path compression (O(E * α(V)))
/// - Reachability and Path Finding: Uses breadth-first search (O(V + E))
/// </summary>
public static class ConnectivityAnalysis
{
    /// <summary>
    /// Finds all nodes reachable from a given starting node using breadth-first search.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <param name="startNode">The starting node for reachability analysis</param>
    /// <returns>Reachability result with all reachable nodes and distances</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph or startNode is null</exception>
    /// <exception cref="ArgumentException">Thrown when startNode is not in the graph</exception>
    public static ReachabilityResult<TNode> FindReachableNodes<TNode>(IDirectedGraph<TNode> graph, TNode startNode) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(startNode);

        if (!graph.Nodes.Contains(startNode))
            throw new ArgumentException($"Start node '{startNode}' is not in the graph.", nameof(startNode));

        var stopwatch = Stopwatch.StartNew();
        const string algorithmName = "BFS Reachability Analysis";

        try
        {
            var reachableNodes = new HashSet<TNode> { startNode };
            var distanceMap = new Dictionary<TNode, int> { [startNode] = 0 };
            var queue = new Queue<TNode>();
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDistance = distanceMap[current];

                foreach (var neighbor in graph.GetDependents(current))
                {
                    if (!reachableNodes.Contains(neighbor))
                    {
                        reachableNodes.Add(neighbor);
                        distanceMap[neighbor] = currentDistance + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return new ReachabilityResult<TNode>(
                StartNode: startNode,
                ReachableNodes: reachableNodes.ToImmutableHashSet(),
                DistanceMap: distanceMap.ToImmutableDictionary(),
                ProcessingTime: stopwatch.Elapsed,
                Algorithm: algorithmName,
                CompletedAt: DateTime.UtcNow
            );
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Finds all strongly connected components in the graph using Tarjan's algorithm.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <returns>Connectivity result with all strongly connected components</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null</exception>
    public static ConnectivityResult<TNode> FindStronglyConnectedComponents<TNode>(IDirectedGraph<TNode> graph) where TNode : notnull
    {
        return TarjanStronglyConnectedComponents.Execute(graph);
    }

    /// <summary>
    /// Checks if there is a path from the source node to the target node.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <param name="source">The source node</param>
    /// <param name="target">The target node</param>
    /// <returns>True if a path exists, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph, source, or target is null</exception>
    public static bool HasPath<TNode>(IDirectedGraph<TNode> graph, TNode source, TNode target) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        if (!graph.Nodes.Contains(source) || !graph.Nodes.Contains(target))
            return false;

        if (source.Equals(target))
            return true;

        var visited = new HashSet<TNode>();
        var queue = new Queue<TNode>();
        queue.Enqueue(source);
        visited.Add(source);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in graph.GetDependents(current))
            {
                if (neighbor.Equals(target))
                    return true;

                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Finds all nodes that can reach the specified target node.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <param name="targetNode">The target node</param>
    /// <returns>Set of all nodes that can reach the target</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph or targetNode is null</exception>
    public static ImmutableHashSet<TNode> FindNodesReaching<TNode>(IDirectedGraph<TNode> graph, TNode targetNode) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(targetNode);

        if (!graph.Nodes.Contains(targetNode))
            return ImmutableHashSet<TNode>.Empty;

        var reachingNodes = new HashSet<TNode> { targetNode };
        var queue = new Queue<TNode>();
        queue.Enqueue(targetNode);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var dependency in graph.GetDependencies(current))
            {
                if (!reachingNodes.Contains(dependency))
                {
                    reachingNodes.Add(dependency);
                    queue.Enqueue(dependency);
                }
            }
        }

        return reachingNodes.ToImmutableHashSet();
    }

    /// <summary>
    /// Finds weakly connected components in a directed graph by treating it as undirected.
    /// Uses Union-Find (Disjoint Set Union) algorithm for optimal performance.
    /// Two nodes are in the same weakly connected component if there is an undirected path between them.
    /// 
    /// Algorithm Complexity:
    /// - Time: O(E * α(V)) where E is edges, V is vertices, and α is the inverse Ackermann function
    /// - Space: O(V) for the Union-Find structure
    /// - α(V) is effectively constant for all practical purposes (≤ 5 for any conceivable graph size)
    /// 
    /// This is significantly more efficient than DFS-based approaches for dense graphs.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <returns>Connectivity result with all weakly connected components and performance metrics</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null</exception>
    public static ConnectivityResult<TNode> FindWeaklyConnectedComponents<TNode>(IDirectedGraph<TNode> graph) where TNode : notnull
    {
        return UnionFindWeaklyConnectedComponents.Execute(graph);
    }
}
