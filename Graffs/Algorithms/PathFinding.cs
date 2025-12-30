using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Graffs;
using Graffs.Results;

namespace Graffs.Algorithms;

/// <summary>
/// Provides algorithms for path finding in directed graphs.
/// </summary>
public static class PathFinding
{
    /// <summary>
    /// Finds the shortest path between two nodes using breadth-first search.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to search</param>
    /// <param name="source">The source node</param>
    /// <param name="target">The target node</param>
    /// <returns>Path result containing the shortest path if found</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph, source, or target is null</exception>
    public static PathResult<TNode> FindShortestPath<TNode>(IDirectedGraph<TNode> graph, TNode source, TNode target) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var stopwatch = Stopwatch.StartNew();
        const string algorithmName = "BFS Shortest Path";

        try
        {
            if (!graph.Nodes.Contains(source) || !graph.Nodes.Contains(target))
                return PathResult<TNode>.NoPath(source, target, stopwatch.Elapsed, algorithmName);

            if (source.Equals(target))
                return PathResult<TNode>.WithPath(source, target, new[] { source }, stopwatch.Elapsed, algorithmName, 1);

            var visited = new HashSet<TNode>();
            var queue = new Queue<TNode>();
            var parent = new Dictionary<TNode, TNode>();
            var nodesExplored = 0;

            queue.Enqueue(source);
            visited.Add(source);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                nodesExplored++;

                foreach (var neighbor in graph.GetDependents(current))
                {
                    if (neighbor.Equals(target))
                    {
                        // Found target, reconstruct path
                        var path = ReconstructPath(parent, source, current).Append(target);
                        return PathResult<TNode>.WithPath(source, target, path, stopwatch.Elapsed, algorithmName, nodesExplored);
                    }

                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return PathResult<TNode>.NoPath(source, target, stopwatch.Elapsed, algorithmName);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Finds all simple paths between two nodes using depth-first search.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to search</param>
    /// <param name="source">The source node</param>
    /// <param name="target">The target node</param>
    /// <param name="maxPaths">Maximum number of paths to find (default: 100)</param>
    /// <returns>Multi-path result containing all found simple paths</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph, source, or target is null</exception>
    public static MultiPathResult<TNode> FindAllSimplePaths<TNode>(
        IDirectedGraph<TNode> graph, 
        TNode source, 
        TNode target, 
        int maxPaths = 100) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        if (maxPaths <= 0)
            throw new ArgumentException("Maximum paths must be positive", nameof(maxPaths));

        var stopwatch = Stopwatch.StartNew();
        const string algorithmName = "DFS All Simple Paths";

        try
        {
            if (!graph.Nodes.Contains(source) || !graph.Nodes.Contains(target))
                return MultiPathResult<TNode>.NoPaths(source, target, stopwatch.Elapsed, algorithmName);

            if (source.Equals(target))
            {
                var singlePath = new[] { new[] { source }.ToImmutableArray() };
                return MultiPathResult<TNode>.WithPaths(source, target, singlePath.Select(p => p.AsEnumerable()), stopwatch.Elapsed, algorithmName, 1);
            }

            var allPaths = new List<ImmutableArray<TNode>>();
            var currentPath = new List<TNode>();
            var visited = new HashSet<TNode>();
            var nodesExplored = 0;

            FindAllSimplePathsDfs(graph, source, target, currentPath, visited, allPaths, maxPaths, ref nodesExplored);

            if (allPaths.Count == 0)
                return MultiPathResult<TNode>.NoPaths(source, target, stopwatch.Elapsed, algorithmName);

            return MultiPathResult<TNode>.WithPaths(
                source, 
                target, 
                allPaths.Select(p => p.AsEnumerable()), 
                stopwatch.Elapsed, 
                algorithmName, 
                nodesExplored);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Finds a path between two nodes using depth-first search.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to search</param>
    /// <param name="source">The source node</param>
    /// <param name="target">The target node</param>
    /// <returns>Path result containing a path if found</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph, source, or target is null</exception>
    public static PathResult<TNode> FindPath<TNode>(IDirectedGraph<TNode> graph, TNode source, TNode target) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var stopwatch = Stopwatch.StartNew();
        const string algorithmName = "DFS Path Finding";

        try
        {
            if (!graph.Nodes.Contains(source) || !graph.Nodes.Contains(target))
                return PathResult<TNode>.NoPath(source, target, stopwatch.Elapsed, algorithmName);

            if (source.Equals(target))
                return PathResult<TNode>.WithPath(source, target, new[] { source }, stopwatch.Elapsed, algorithmName, 1);

            var visited = new HashSet<TNode>();
            var path = new List<TNode>();
            var nodesExplored = 0;

            if (FindPathDfs(graph, source, target, visited, path, ref nodesExplored))
            {
                return PathResult<TNode>.WithPath(source, target, path, stopwatch.Elapsed, algorithmName, nodesExplored);
            }

            return PathResult<TNode>.NoPath(source, target, stopwatch.Elapsed, algorithmName);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Checks if there are any paths between all pairs of specified nodes.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <param name="nodes">The nodes to check connectivity for</param>
    /// <returns>Dictionary mapping node pairs to path existence</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph or nodes is null</exception>
    public static ImmutableDictionary<(TNode Source, TNode Target), bool> CheckConnectivity<TNode>(
        IDirectedGraph<TNode> graph, 
        IEnumerable<TNode> nodes) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(nodes);

        var nodeList = nodes.ToList();
        var result = new Dictionary<(TNode Source, TNode Target), bool>();

        foreach (var source in nodeList)
        {
            foreach (var target in nodeList)
            {
                if (!source.Equals(target))
                {
                    result[(source, target)] = ConnectivityAnalysis.HasPath(graph, source, target);
                }
            }
        }

        return result.ToImmutableDictionary();
    }

    /// <summary>
    /// Finds the shortest paths from a source node to all reachable nodes.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to search</param>
    /// <param name="source">The source node</param>
    /// <returns>Dictionary mapping target nodes to their shortest paths from source</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph or source is null</exception>
    public static ImmutableDictionary<TNode, PathResult<TNode>> FindShortestPathsFromSource<TNode>(
        IDirectedGraph<TNode> graph, 
        TNode source) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(source);

        if (!graph.Nodes.Contains(source))
            return ImmutableDictionary<TNode, PathResult<TNode>>.Empty;

        var reachabilityResult = ConnectivityAnalysis.FindReachableNodes(graph, source);
        var result = new Dictionary<TNode, PathResult<TNode>>();

        foreach (var target in reachabilityResult.ReachableNodes)
        {
            if (!source.Equals(target))
            {
                result[target] = FindShortestPath(graph, source, target);
            }
        }

        return result.ToImmutableDictionary();
    }

    #region Private Helper Methods

    private static IEnumerable<TNode> ReconstructPath<TNode>(
        Dictionary<TNode, TNode> parent, 
        TNode source, 
        TNode target) where TNode : notnull
    {
        var path = new List<TNode>();
        var current = target;

        while (!current.Equals(source))
        {
            path.Add(current);
            if (!parent.TryGetValue(current, out current!))
                return Enumerable.Empty<TNode>(); // Path reconstruction failed
        }

        path.Add(source);
        path.Reverse();
        return path;
    }

    private static bool FindPathDfs<TNode>(
        IDirectedGraph<TNode> graph,
        TNode current,
        TNode target,
        HashSet<TNode> visited,
        List<TNode> path,
        ref int nodesExplored) where TNode : notnull
    {
        visited.Add(current);
        path.Add(current);
        nodesExplored++;

        if (current.Equals(target))
            return true;

        foreach (var neighbor in graph.GetDependents(current))
        {
            if (!visited.Contains(neighbor))
            {
                if (FindPathDfs(graph, neighbor, target, visited, path, ref nodesExplored))
                    return true;
            }
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }

    private static void FindAllSimplePathsDfs<TNode>(
        IDirectedGraph<TNode> graph,
        TNode current,
        TNode target,
        List<TNode> currentPath,
        HashSet<TNode> visited,
        List<ImmutableArray<TNode>> allPaths,
        int maxPaths,
        ref int nodesExplored) where TNode : notnull
    {
        if (allPaths.Count >= maxPaths)
            return;

        visited.Add(current);
        currentPath.Add(current);
        nodesExplored++;

        if (current.Equals(target))
        {
            allPaths.Add(currentPath.ToImmutableArray());
        }
        else
        {
            foreach (var neighbor in graph.GetDependents(current))
            {
                if (!visited.Contains(neighbor) && allPaths.Count < maxPaths)
                {
                    FindAllSimplePathsDfs(graph, neighbor, target, currentPath, visited, allPaths, maxPaths, ref nodesExplored);
                }
            }
        }

        visited.Remove(current);
        currentPath.RemoveAt(currentPath.Count - 1);
    }

    #endregion
}
