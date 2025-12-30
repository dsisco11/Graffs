using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Graffs;
using Graffs.Results;

namespace Graffs.Algorithms;

/// <summary>
/// Provides static methods for topological sorting of directed graphs.
/// </summary>
public static class TopologicalSort
{
    /// <summary>
    /// Performs topological sorting using Kahn's algorithm.
    /// This algorithm works by repeatedly removing nodes with no incoming edges.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph.</typeparam>
    /// <param name="graph">The directed graph to sort.</param>
    /// <returns>A TopologicalSortResult containing the sorted nodes and cycle information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null.</exception>
    public static TopologicalSortResult<TNode> KahnSort<TNode>(IDirectedGraph<TNode> graph)
        where TNode : notnull
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));
        
        var stopwatch = Stopwatch.StartNew();
        
        // Handle empty graph
        if (graph.Count == 0)
        {
            stopwatch.Stop();
            return TopologicalSortResult<TNode>.Empty(stopwatch.Elapsed);
        }
        
        // Calculate in-degrees for all nodes
        var inDegrees = new Dictionary<TNode, int>();
        foreach (var node in graph.Nodes)
        {
            inDegrees[node] = graph.GetDependencies(node).Count;
        }
        
        // Initialize queue with nodes that have no dependencies (in-degree = 0)
        var queue = new Queue<TNode>();
        foreach (var kvp in inDegrees)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }
        
        var result = new List<TNode>();
        var processedCount = 0;
        
        // Process nodes in topological order
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);
            processedCount++;
            
            // Update in-degrees of dependent nodes
            foreach (var dependent in graph.GetDependents(current))
            {
                inDegrees[dependent]--;
                if (inDegrees[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }
        
        stopwatch.Stop();
        
        // Check for cycles
        if (processedCount < graph.Count)
        {
            // Nodes with non-zero in-degree are part of cycles
            var cyclicNodes = inDegrees
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => kvp.Key)
                .ToImmutableArray();
            
            return TopologicalSortResult<TNode>.WithCycles(
                result.ToImmutableArray(),
                cyclicNodes,
                stopwatch.Elapsed);
        }
        
        return TopologicalSortResult<TNode>.Success(
            result.ToImmutableArray(),
            stopwatch.Elapsed);
    }
    
    /// <summary>
    /// Performs topological sorting using depth-first search (DFS).
    /// This algorithm uses DFS to detect cycles and produce topological ordering.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph.</typeparam>
    /// <param name="graph">The directed graph to sort.</param>
    /// <returns>A TopologicalSortResult containing the sorted nodes and cycle information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null.</exception>
    public static TopologicalSortResult<TNode> DfsSort<TNode>(IDirectedGraph<TNode> graph)
        where TNode : notnull
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));
        
        var stopwatch = Stopwatch.StartNew();
        
        // Handle empty graph
        if (graph.Count == 0)
        {
            stopwatch.Stop();
            return TopologicalSortResult<TNode>.Empty(stopwatch.Elapsed);
        }
        
        // Track visit states: 0 = unvisited, 1 = visiting (in current path), 2 = visited
        var visitStates = new Dictionary<TNode, int>();
        var result = new List<TNode>();
        var cyclicNodes = new HashSet<TNode>();
        
        // Initialize all nodes as unvisited
        foreach (var node in graph.Nodes)
        {
            visitStates[node] = 0;
        }
        
        // Use explicit stack to avoid stack overflow on large graphs
        var stack = new Stack<(TNode Node, bool IsReturning)>();
        
        // Process each unvisited node
        foreach (var startNode in graph.Nodes)
        {
            if (visitStates[startNode] == 0)
            {
                stack.Push((startNode, false));
                
                while (stack.Count > 0)
                {
                    var (current, isReturning) = stack.Pop();
                    
                    if (isReturning)
                    {
                        // Returning from recursion - add to result in reverse finish order
                        if (visitStates[current] == 1)
                        {
                            visitStates[current] = 2; // Mark as visited
                            result.Add(current);
                        }
                    }
                    else
                    {
                        // First visit to this node
                        if (visitStates[current] == 1)
                        {
                            // Back edge detected - cycle found
                            cyclicNodes.Add(current);
                            continue;
                        }
                        
                        if (visitStates[current] == 2)
                        {
                            // Already processed
                            continue;
                        }
                        
                        // Mark as visiting
                        visitStates[current] = 1;
                        
                        // Add return visit
                        stack.Push((current, true));
                        
                        // Add dependents to stack (they will be processed first)
                        foreach (var dependent in graph.GetDependents(current))
                        {
                            if (visitStates[dependent] != 2)
                            {
                                stack.Push((dependent, false));
                            }
                        }
                    }
                }
            }
        }
        
        stopwatch.Stop();
        
        // Reverse the result to get topological order
        result.Reverse();
        
        if (cyclicNodes.Count > 0)
        {
            return TopologicalSortResult<TNode>.WithCycles(
                result.ToImmutableArray(),
                cyclicNodes.ToImmutableArray(),
                stopwatch.Elapsed);
        }
        
        return TopologicalSortResult<TNode>.Success(
            result.ToImmutableArray(),
            stopwatch.Elapsed);
    }
    
    /// <summary>
    /// Performs stable topological sorting using Kahn's algorithm with custom ordering.
    /// Provides deterministic ordering by applying custom comparison within dependency levels.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph.</typeparam>
    /// <param name="graph">The directed graph to sort.</param>
    /// <param name="stableComparer">Optional comparer for deterministic ordering within dependency levels.</param>
    /// <returns>A TopologicalSortResult containing the stable-sorted nodes and cycle information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null.</exception>
    public static TopologicalSortResult<TNode> StableSort<TNode>(
        IDirectedGraph<TNode> graph,
        IComparer<TNode>? stableComparer = null)
        where TNode : notnull
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));
        
        var stopwatch = Stopwatch.StartNew();
        
        // Handle empty graph
        if (graph.Count == 0)
        {
            stopwatch.Stop();
            return TopologicalSortResult<TNode>.Empty(stopwatch.Elapsed);
        }
        
        // Use default stable comparer if none provided (deterministic by hash code)
        stableComparer ??= Comparer<TNode>.Create((a, b) => 
            a.GetHashCode().CompareTo(b.GetHashCode()));
        
        // Calculate in-degrees for all nodes
        var inDegrees = new Dictionary<TNode, int>();
        foreach (var node in graph.Nodes)
        {
            inDegrees[node] = graph.GetDependencies(node).Count;
        }
        
        var result = new List<TNode>();
        var processedCount = 0;
        var processed = new HashSet<TNode>();
        
        // Process nodes level by level for stable ordering
        while (processedCount < graph.Count)
        {
            // Find all nodes ready for processing (in-degree = 0)
            var readyNodes = new List<TNode>();
            foreach (var kvp in inDegrees)
            {
                if (kvp.Value == 0 && !processed.Contains(kvp.Key))
                {
                    readyNodes.Add(kvp.Key);
                }
            }
            
            // Check for cycles (no ready nodes but unprocessed nodes remain)
            if (readyNodes.Count == 0)
            {
                break;
            }
            
            // Sort ready nodes using stable comparer for deterministic ordering
            readyNodes.Sort(stableComparer);
            
            // Process all ready nodes in sorted order
            foreach (var current in readyNodes)
            {
                result.Add(current);
                processed.Add(current);
                processedCount++;
                
                // Update in-degrees of dependent nodes
                foreach (var dependent in graph.GetDependents(current))
                {
                    inDegrees[dependent]--;
                }
            }
        }
        
        stopwatch.Stop();
        
        // Check for cycles
        if (processedCount < graph.Count)
        {
            // Nodes with non-zero in-degree are part of cycles
            var cyclicNodes = inDegrees
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => kvp.Key)
                .ToImmutableArray();
            
            return TopologicalSortResult<TNode>.WithCycles(
                result.ToImmutableArray(),
                cyclicNodes,
                stopwatch.Elapsed);
        }
        
        return TopologicalSortResult<TNode>.Success(
            result.ToImmutableArray(),
            stopwatch.Elapsed);
    }
    
    /// <summary>
    /// Performs stable topological sorting using Kahn's algorithm with custom comparison function.
    /// Provides deterministic ordering by applying custom comparison within dependency levels.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph.</typeparam>
    /// <param name="graph">The directed graph to sort.</param>
    /// <param name="comparison">Comparison function for deterministic ordering within dependency levels.</param>
    /// <returns>A TopologicalSortResult containing the stable-sorted nodes and cycle information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph or comparison is null.</exception>
    public static TopologicalSortResult<TNode> StableSort<TNode>(
        IDirectedGraph<TNode> graph,
        Comparison<TNode> comparison)
        where TNode : notnull
    {
        if (comparison == null)
            throw new ArgumentNullException(nameof(comparison));
            
        var comparer = Comparer<TNode>.Create(comparison);
        return StableSort(graph, comparer);
    }
}
