using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Graffs;
using Graffs.Results;

namespace Graffs.Algorithms;

/// <summary>
/// Provides static methods for cycle detection in directed graphs.
/// </summary>
public static class CycleDetection
{
    /// <summary>
    /// Quickly checks if the graph contains any cycles using DFS.
    /// This is optimized for early termination and does not enumerate cycles.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The graph to analyze</param>
    /// <returns>True if the graph contains cycles, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null</exception>
    public static bool HasCycles<TNode>(IDirectedGraph<TNode> graph) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (graph.Count == 0) return false;

        // DFS with three states: White (unvisited), Gray (visiting), Black (visited)
        var nodeStates = new Dictionary<TNode, VisitState>();
        
        // Initialize all nodes as unvisited
        foreach (var node in graph.Nodes)
        {
            nodeStates[node] = VisitState.White;
        }

        // Check each unvisited node for cycles
        foreach (var node in graph.Nodes)
        {
            if (nodeStates[node] == VisitState.White)
            {
                if (HasCyclesDfs(graph, node, nodeStates))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds all elementary cycles in the graph using Johnson's algorithm approach.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The graph to analyze</param>
    /// <returns>Complete cycle detection result with all cycles and statistics</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null</exception>
    public static CycleDetectionResult<TNode> FindAllCycles<TNode>(IDirectedGraph<TNode> graph) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);

        var stopwatch = Stopwatch.StartNew();
        const string algorithmName = "Johnson's Algorithm (Elementary Cycles)";

        try
        {
            if (graph.Count == 0)
                return CycleDetectionResult<TNode>.NoCycles(stopwatch.Elapsed, algorithmName);

            // Use Johnson's algorithm for all elementary cycles (including self-loops)
            var allCycles = FindElementaryCyclesJohnson(graph);

            // Remove duplicate cycles 
            var uniqueCycles = RemoveDuplicateCycles(allCycles);

            return CycleDetectionResult<TNode>.WithCycles(uniqueCycles, stopwatch.Elapsed, algorithmName);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Finds all elementary cycles using Johnson's algorithm.
    /// This is more efficient than simple DFS for finding all cycles.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The graph to analyze</param>
    /// <param name="maxCycles">Maximum number of cycles to find (default: 1000)</param>
    /// <returns>All elementary cycles found using Johnson's algorithm</returns>
    public static CycleDetectionResult<TNode> FindAllCyclesJohnson<TNode>(
        IDirectedGraph<TNode> graph, 
        int maxCycles = 1000) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);

        var stopwatch = Stopwatch.StartNew();
        const string algorithmName = "Johnson's Algorithm (Complete)";

        try
        {
            if (graph.Count == 0)
                return CycleDetectionResult<TNode>.NoCycles(stopwatch.Elapsed, algorithmName);

            var cycles = FindElementaryCyclesJohnson(graph, maxCycles);
            return CycleDetectionResult<TNode>.WithCycles(cycles, stopwatch.Elapsed, algorithmName);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Performs a fast cycle check optimized for early termination.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The graph to analyze</param>
    /// <returns>Basic cycle detection result</returns>
    public static CycleDetectionResult<TNode> QuickCycleCheck<TNode>(IDirectedGraph<TNode> graph) where TNode : notnull
    {
        var stopwatch = Stopwatch.StartNew();
        const string algorithmName = "Quick DFS Cycle Check";

        try
        {
            bool hasCycles = HasCycles(graph);
            if (hasCycles)
            {
                return new CycleDetectionResult<TNode>(
                    HasCycles: true,
                    Cycles: Array.Empty<GraphCycle<TNode>>(),
                    Statistics: CycleStatistics.Empty,
                    ProcessingTime: stopwatch.Elapsed,
                    Algorithm: algorithmName,
                    CompletedAt: DateTime.UtcNow
                );
            }
            else
            {
                return CycleDetectionResult<TNode>.NoCycles(stopwatch.Elapsed, algorithmName);
            }
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Implements Johnson's algorithm for finding all elementary cycles.
    /// This is a more efficient approach than simple DFS for enumerating all cycles.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The graph to analyze</param>
    /// <param name="maxCycles">Maximum number of cycles to find</param>
    /// <returns>List of all elementary cycles found</returns>
    private static List<GraphCycle<TNode>> FindElementaryCyclesJohnson<TNode>(
        IDirectedGraph<TNode> graph, 
        int maxCycles = 1000) where TNode : notnull
    {
        var cycles = new List<GraphCycle<TNode>>();
        var nodeList = graph.Nodes.ToList();
        
        if (nodeList.Count == 0)
            return cycles;

        // Create a mapping from nodes to indices for easier processing
        var nodeToIndex = new Dictionary<TNode, int>();
        for (int i = 0; i < nodeList.Count; i++)
        {
            nodeToIndex[nodeList[i]] = i;
        }

        // Johnson's algorithm implementation
        var blocked = new bool[nodeList.Count];
        var blockedSet = new HashSet<TNode>[nodeList.Count];
        var stack = new List<TNode>();

        for (int i = 0; i < nodeList.Count; i++)
        {
            blockedSet[i] = new HashSet<TNode>();
        }

        for (int startIndex = 0; startIndex < nodeList.Count && cycles.Count < maxCycles; startIndex++)
        {
            var startNode = nodeList[startIndex];
            
            // Reset blocked arrays for this iteration
            Array.Fill(blocked, false);
            for (int i = 0; i < blockedSet.Length; i++)
            {
                blockedSet[i].Clear();
            }

            // Find cycles starting from this node
            JohnsonCircuit(graph, startNode, startNode, nodeToIndex, blocked, blockedSet, stack, cycles, maxCycles);
        }

        return cycles;
    }

    /// <summary>
    /// Recursive helper for Johnson's algorithm.
    /// </summary>
    private static bool JohnsonCircuit<TNode>(
        IDirectedGraph<TNode> graph,
        TNode startNode,
        TNode currentNode,
        Dictionary<TNode, int> nodeToIndex,
        bool[] blocked,
        HashSet<TNode>[] blockedSet,
        List<TNode> stack,
        List<GraphCycle<TNode>> cycles,
        int maxCycles) where TNode : notnull
    {
        if (cycles.Count >= maxCycles)
            return false;

        bool foundCycle = false;
        
        if (!nodeToIndex.TryGetValue(currentNode, out int currentIndex))
            return false;

        stack.Add(currentNode);
        blocked[currentIndex] = true;

        foreach (var neighbor in graph.GetDependents(currentNode))
        {
            if (neighbor.Equals(startNode))
            {
                // Found a cycle - according to Johnson's paper: "output circuit composed of stack followed by s"
                List<TNode> cycleNodes;
                int cycleLength;
                
                if (currentNode.Equals(startNode) && stack.Count == 1)
                {
                    // This is a self-loop: A -> A
                    cycleNodes = new List<TNode> { startNode };
                    cycleLength = 1; // Self-loop has length 1
                }
                else
                {
                    // Regular cycle: store unique nodes without duplicating the start node
                    cycleNodes = new List<TNode>(stack);
                    cycleLength = stack.Count; // Length is number of edges in the cycle
                }
                
                cycles.Add(new GraphCycle<TNode>(
                    cycleNodes,
                    cycleLength,
                    DateTime.UtcNow
                ));
                foundCycle = true;
            }
            else if (nodeToIndex.TryGetValue(neighbor, out int neighborIndex) && 
                     !blocked[neighborIndex] &&
                     nodeToIndex[neighbor] >= nodeToIndex[startNode]) // Only consider nodes with higher indices
            {
                if (JohnsonCircuit(graph, startNode, neighbor, nodeToIndex, blocked, blockedSet, stack, cycles, maxCycles))
                {
                    foundCycle = true;
                }
            }
        }

        if (foundCycle)
        {
            JohnsonUnblock(currentNode, nodeToIndex, blocked, blockedSet);
        }
        else
        {
            foreach (var neighbor in graph.GetDependents(currentNode))
            {
                if (nodeToIndex.TryGetValue(neighbor, out int neighborIndex))
                {
                    blockedSet[neighborIndex].Add(currentNode);
                }
            }
        }

        stack.RemoveAt(stack.Count - 1);
        return foundCycle;
    }

    /// <summary>
    /// Unblocks nodes in Johnson's algorithm.
    /// </summary>
    private static void JohnsonUnblock<TNode>(
        TNode node,
        Dictionary<TNode, int> nodeToIndex,
        bool[] blocked,
        HashSet<TNode>[] blockedSet) where TNode : notnull
    {
        if (!nodeToIndex.TryGetValue(node, out int nodeIndex))
            return;

        blocked[nodeIndex] = false;
        
        var toUnblock = new List<TNode>(blockedSet[nodeIndex]);
        blockedSet[nodeIndex].Clear();
        
        foreach (var blockedNode in toUnblock)
        {
            if (nodeToIndex.TryGetValue(blockedNode, out int blockedIndex) && blocked[blockedIndex])
            {
                JohnsonUnblock(blockedNode, nodeToIndex, blocked, blockedSet);
            }
        }
    }

    private static bool HasCyclesDfs<TNode>(
        IDirectedGraph<TNode> graph, 
        TNode node, 
        Dictionary<TNode, VisitState> nodeStates) where TNode : notnull
    {
        nodeStates[node] = VisitState.Gray;

        foreach (var neighbor in graph.GetDependents(node))
        {
            var neighborState = nodeStates[neighbor];
            
            // If we encounter a gray node, we found a back edge (cycle)
            if (neighborState == VisitState.Gray)
                return true;

            // If neighbor is white and has cycles in its subtree
            if (neighborState == VisitState.White && HasCyclesDfs(graph, neighbor, nodeStates))
                return true;
        }

        nodeStates[node] = VisitState.Black;
        return false;
    }

    private static void FindCyclesDfs<TNode>(
        IDirectedGraph<TNode> graph,
        TNode startNode,
        Dictionary<TNode, VisitState> nodeStates,
        List<TNode> currentPath,
        List<GraphCycle<TNode>> cycles) where TNode : notnull
    {
        if (nodeStates[startNode] == VisitState.Gray)
        {
            // Found a cycle - extract it from current path
            var cycleStartIndex = currentPath.IndexOf(startNode);
            if (cycleStartIndex >= 0)
            {
                var cycleNodes = currentPath.Skip(cycleStartIndex).ToList();
                if (cycleNodes.Count > 0) // Ignore self-loops here as they're handled separately
                {
                    cycles.Add(new GraphCycle<TNode>(
                        cycleNodes,
                        cycleNodes.Count, // Length is number of nodes in the cycle
                        DateTime.UtcNow
                    ));
                }
            }
            return;
        }

        if (nodeStates[startNode] == VisitState.Black)
            return;

        nodeStates[startNode] = VisitState.Gray;
        currentPath.Add(startNode);

        foreach (var neighbor in graph.GetDependents(startNode))
        {
            // Skip self-loops in complex cycle detection
            if (!neighbor.Equals(startNode))
            {
                FindCyclesDfs(graph, neighbor, nodeStates, currentPath, cycles);
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        nodeStates[startNode] = VisitState.Black;
    }

    private static List<GraphCycle<TNode>> RemoveDuplicateCycles<TNode>(List<GraphCycle<TNode>> cycles) where TNode : notnull
    {
        var uniqueCycles = new List<GraphCycle<TNode>>();
        var seenCycles = new HashSet<string>();

        foreach (var cycle in cycles)
        {
            // Create a canonical representation of the cycle
            var canonical = GetCanonicalCycleRepresentation(cycle.Nodes);
            if (seenCycles.Add(canonical))
            {
                uniqueCycles.Add(cycle);
            }
        }

        return uniqueCycles;
    }

    private static string GetCanonicalCycleRepresentation<TNode>(IReadOnlyList<TNode> nodes) where TNode : notnull
    {
        if (nodes.Count == 0) return string.Empty;
        if (nodes.Count == 1) return nodes[0].ToString() ?? string.Empty;

        // Find the lexicographically smallest starting point
        var minIndex = 0;
        var minNode = nodes[0].ToString();
        
        for (int i = 1; i < nodes.Count; i++)
        {
            var nodeStr = nodes[i].ToString();
            if (string.Compare(nodeStr, minNode, StringComparison.Ordinal) < 0)
            {
                minIndex = i;
                minNode = nodeStr;
            }
        }

        // Create canonical representation starting from minimum node
        var canonical = new List<string>();
        for (int i = 0; i < nodes.Count; i++)
        {
            canonical.Add(nodes[(minIndex + i) % nodes.Count].ToString() ?? string.Empty);
        }

        return string.Join("→", canonical);
    }

    #endregion

    #region Private Enums

    private enum VisitState
    {
        White, // Unvisited
        Gray,  // Currently being visited (in the recursion stack)
        Black  // Completely visited (finished processing)
    }

    #endregion
}
