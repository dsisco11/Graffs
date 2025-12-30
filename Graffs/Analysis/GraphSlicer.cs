using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Graffs;
using Graffs.Results;

namespace Graffs.Analysis;

/// <summary>
/// Provides graph slicing functionality for dependency chain analysis.
/// Analyzes graph structure to identify independent chains and dependency levels.
/// </summary>
public static class GraphSlicer
{
    /// <summary>
    /// Analyzes the dependency structure of a directed graph, identifying independent chains and dependency levels.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <returns>GraphSlicingResult containing structural analysis</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null</exception>
    public static GraphSlicingResult<TNode> AnalyzeDependencyStructure<TNode>(
        IDirectedGraph<TNode> graph) where TNode : notnull
    {
        return AnalyzeDependencyStructure(graph, null);
    }

    /// <summary>
    /// Analyzes the dependency structure of a directed graph with custom level ordering.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <param name="levelComparer">Comparer for deterministic ordering within levels</param>
    /// <returns>GraphSlicingResult containing structural analysis</returns>
    /// <exception cref="ArgumentNullException">Thrown when graph is null</exception>
    public static GraphSlicingResult<TNode> AnalyzeDependencyStructure<TNode>(
        IDirectedGraph<TNode> graph,
        IComparer<TNode>? levelComparer) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step 1: Get stable topological ordering using existing algorithm
            var sortResult = TopologicalSort.StableSort(graph, levelComparer);
            if (sortResult.HasCycles)
            {
                return GraphSlicingResult<TNode>.Invalid(
                    "Graph contains cycles and cannot be analyzed for dependency structure",
                    stopwatch.Elapsed);
            }

            // Step 2: Calculate dependency levels for each node
            var nodeLevels = CalculateNodeLevels(graph, sortResult.SortedNodes);
            var dependencyLevels = GroupNodesIntoLevels(graph, nodeLevels, levelComparer);

            // Step 3: Identify independent chains using existing connectivity analysis
            var independentChains = IdentifyIndependentChains(graph, dependencyLevels, levelComparer);

            stopwatch.Stop();

            return GraphSlicingResult<TNode>.Success(
                dependencyLevels,
                independentChains,
                graph.Count,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return GraphSlicingResult<TNode>.Invalid(
                $"Error during dependency analysis: {ex.Message}",
                stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Calculates the dependency level (distance from root nodes) for each node.
    /// </summary>
    private static Dictionary<TNode, int> CalculateNodeLevels<TNode>(
        IDirectedGraph<TNode> graph,
        ImmutableArray<TNode> topologicalOrder) where TNode : notnull
    {
        var nodeLevels = new Dictionary<TNode, int>();

        // Process nodes in topological order
        foreach (var node in topologicalOrder)
        {
            var dependencies = graph.GetDependencies(node);
            if (dependencies.Count == 0)
            {
                // Root node - level 0
                nodeLevels[node] = 0;
            }
            else
            {
                // Level is 1 + maximum level of dependencies
                var maxDependencyLevel = dependencies.Max(dep => nodeLevels[dep]);
                nodeLevels[node] = maxDependencyLevel + 1;
            }
        }

        return nodeLevels;
    }

    /// <summary>
    /// Groups nodes into dependency levels and calculates level relationships.
    /// </summary>
    private static ImmutableArray<DependencyLevel<TNode>> GroupNodesIntoLevels<TNode>(
        IDirectedGraph<TNode> graph,
        Dictionary<TNode, int> nodeLevels,
        IComparer<TNode>? levelComparer) where TNode : notnull
    {
        // Group nodes by level
        var levelGroups = nodeLevels
            .GroupBy(kvp => kvp.Value)
            .OrderBy(g => g.Key)
            .ToList();

        var levels = new List<DependencyLevel<TNode>>();

        foreach (var levelGroup in levelGroups)
        {
            var levelIndex = levelGroup.Key;
            var nodesAtLevel = levelGroup.Select(kvp => kvp.Key).ToList();

            // Sort nodes within level for deterministic ordering
            if (levelComparer != null)
            {
                nodesAtLevel.Sort(levelComparer);
            }

            // Calculate level dependencies (all nodes from previous levels that this level depends on)
            var levelDependencies = new HashSet<TNode>();
            foreach (var node in nodesAtLevel)
            {
                foreach (var dependency in graph.GetDependencies(node))
                {
                    if (nodeLevels[dependency] < levelIndex)
                    {
                        levelDependencies.Add(dependency);
                    }
                }
            }

            // Calculate level dependents (all nodes in future levels that depend on this level)
            var levelDependents = new HashSet<TNode>();
            foreach (var node in nodesAtLevel)
            {
                foreach (var dependent in graph.GetDependents(node))
                {
                    if (nodeLevels[dependent] > levelIndex)
                    {
                        levelDependents.Add(dependent);
                    }
                }
            }

            var level = DependencyLevel<TNode>.Create(
                levelIndex,
                nodesAtLevel.ToImmutableArray(),
                levelDependencies.ToImmutableHashSet(),
                levelDependents.ToImmutableHashSet());

            levels.Add(level);
        }

        return levels.ToImmutableArray();
    }

    /// <summary>
    /// Identifies independent dependency chains using existing connectivity analysis.
    /// </summary>
    private static ImmutableArray<IndependentChain<TNode>> IdentifyIndependentChains<TNode>(
        IDirectedGraph<TNode> graph,
        ImmutableArray<DependencyLevel<TNode>> dependencyLevels,
        IComparer<TNode>? levelComparer) where TNode : notnull
    {
        if (dependencyLevels.Length == 0)
        {
            return ImmutableArray<IndependentChain<TNode>>.Empty;
        }

        var independentChains = new List<IndependentChain<TNode>>();
        var globalVisited = new HashSet<TNode>();
        var chainCounter = 0;

        // Start from root nodes (level 0)
        var rootNodes = dependencyLevels[0].Nodes;

        foreach (var rootNode in rootNodes)
        {
            if (globalVisited.Contains(rootNode))
                continue;

            // Use existing reachability analysis to find all nodes reachable from this root
            var reachabilityResult = ConnectivityAnalysis.FindReachableNodes(graph, rootNode);
            var chainNodes = FilterIndependentChainNodes(graph, rootNode, reachabilityResult, globalVisited);

            if (chainNodes.Count > 0)
            {
                // Create subgraph for this chain using existing GraphBuilder
                var subgraphBuilder = new GraphBuilder<TNode>();
                subgraphBuilder.AddNodes(chainNodes);

                // Add edges within the chain
                foreach (var node in chainNodes)
                {
                    foreach (var dependent in graph.GetDependents(node))
                    {
                        if (chainNodes.Contains(dependent))
                        {
                            subgraphBuilder.AddEdge(node, dependent);
                        }
                    }
                }

                var subgraph = subgraphBuilder.Build();

                // Use existing stable sort for chain ordering
                var chainSortResult = TopologicalSort.StableSort(subgraph, levelComparer);

                // Calculate chain levels
                var chainLevels = CalculateChainLevels(subgraph, chainSortResult.SortedNodes, levelComparer);

                // Find leaf nodes (nodes with no dependents within the chain)
                var leafNodes = chainNodes
                    .Where(node => !graph.GetDependents(node).Any(dep => chainNodes.Contains(dep)))
                    .ToImmutableHashSet();

                var chain = IndependentChain<TNode>.Create(
                    $"Chain_{chainCounter++}",
                    rootNode,
                    chainSortResult.SortedNodes,
                    chainLevels,
                    leafNodes);

                independentChains.Add(chain);
                globalVisited.UnionWith(chainNodes);
            }
        }

        return independentChains.ToImmutableArray();
    }

    /// <summary>
    /// Filters nodes to include only those that belong to an independent chain starting from the root.
    /// </summary>
    private static HashSet<TNode> FilterIndependentChainNodes<TNode>(
        IDirectedGraph<TNode> graph,
        TNode rootNode,
        ReachabilityResult<TNode> reachabilityResult,
        HashSet<TNode> globalVisited) where TNode : notnull
    {
        var chainNodes = new HashSet<TNode>();

        // Start with nodes reachable from root that haven't been visited
        var candidateNodes = reachabilityResult.ReachableNodes
            .Where(node => !globalVisited.Contains(node))
            .ToList();

        // Verify that these nodes form an independent chain
        // (no external dependencies outside the chain)
        foreach (var node in candidateNodes)
        {
            var dependencies = graph.GetDependencies(node);
            bool hasExternalDependencies = dependencies.Any(dep => 
                !candidateNodes.Contains(dep) && !globalVisited.Contains(dep));

            if (!hasExternalDependencies)
            {
                chainNodes.Add(node);
            }
        }

        return chainNodes;
    }

    /// <summary>
    /// Calculates dependency levels within a specific chain.
    /// </summary>
    private static ImmutableArray<DependencyLevel<TNode>> CalculateChainLevels<TNode>(
        IDirectedGraph<TNode> chainGraph,
        ImmutableArray<TNode> topologicalOrder,
        IComparer<TNode>? levelComparer) where TNode : notnull
    {
        var nodeLevels = CalculateNodeLevels(chainGraph, topologicalOrder);
        return GroupNodesIntoLevels(chainGraph, nodeLevels, levelComparer);
    }
}
