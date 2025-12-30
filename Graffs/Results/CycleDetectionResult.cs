using System;
using System.Collections.Generic;
using System.Linq;

namespace Graffs.Results;

/// <summary>
/// Represents a single cycle found in a directed graph.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the cycle</typeparam>
public sealed record GraphCycle<TNode>(
    IReadOnlyList<TNode> Nodes,
    int Length,
    DateTime DiscoveredAt
) where TNode : notnull
{
    /// <summary>
    /// Gets whether this cycle is a self-loop (single node pointing to itself).
    /// </summary>
    public bool IsSelfLoop => Length == 1;

    /// <summary>
    /// Gets whether this cycle involves multiple nodes.
    /// </summary>
    public bool IsComplexCycle => Length > 1;

    /// <summary>
    /// Gets the starting node of the cycle.
    /// </summary>
    public TNode StartNode => Nodes.Count > 0 ? Nodes[0] : throw new InvalidOperationException("Empty cycle");

    /// <summary>
    /// Checks if the cycle contains the specified node.
    /// </summary>
    /// <param name="node">The node to check for</param>
    /// <returns>True if the cycle contains the node, false otherwise</returns>
    public bool ContainsNode(TNode node) => Nodes.Contains(node);

    /// <summary>
    /// Gets a string representation of the cycle path.
    /// </summary>
    /// <returns>A string showing the cycle path</returns>
    public string ToPathString() => string.Join(" → ", Nodes) + $" → {StartNode}";
}

/// <summary>
/// Contains statistical information about cycles found in a graph.
/// </summary>
public sealed record CycleStatistics(
    int TotalCycles,
    int SelfLoops,
    int ComplexCycles,
    int ShortestCycleLength,
    int LongestCycleLength,
    double AverageCycleLength,
    IReadOnlyList<int> CycleLengthDistribution
)
{
    /// <summary>
    /// Gets whether any cycles were found.
    /// </summary>
    public bool HasCycles => TotalCycles > 0;

    /// <summary>
    /// Gets the percentage of cycles that are self-loops.
    /// </summary>
    public double SelfLoopPercentage => TotalCycles > 0 ? (double)SelfLoops / TotalCycles * 100 : 0;

    /// <summary>
    /// Gets the percentage of cycles that involve multiple nodes.
    /// </summary>
    public double ComplexCyclePercentage => TotalCycles > 0 ? (double)ComplexCycles / TotalCycles * 100 : 0;

    /// <summary>
    /// Creates empty cycle statistics for graphs with no cycles.
    /// </summary>
    /// <returns>Empty cycle statistics</returns>
    public static CycleStatistics Empty => new(
        TotalCycles: 0,
        SelfLoops: 0,
        ComplexCycles: 0,
        ShortestCycleLength: 0,
        LongestCycleLength: 0,
        AverageCycleLength: 0,
        CycleLengthDistribution: Array.Empty<int>()
    );

    /// <summary>
    /// Creates cycle statistics from a collection of cycles.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the cycles</typeparam>
    /// <param name="cycles">The cycles to analyze</param>
    /// <returns>Statistics about the cycles</returns>
    public static CycleStatistics FromCycles<TNode>(IEnumerable<GraphCycle<TNode>> cycles) where TNode : notnull
    {
        var cycleList = cycles.ToList();
        if (cycleList.Count == 0)
            return Empty;

        var lengths = cycleList.Select(c => c.Length).ToList();
        var selfLoops = cycleList.Count(c => c.IsSelfLoop);
        var complexCycles = cycleList.Count(c => c.IsComplexCycle);

        // Create length distribution (index = length - 1, value = count)
        var maxLength = lengths.Max();
        var distribution = new int[maxLength];
        foreach (var length in lengths)
        {
            distribution[length - 1]++;
        }

        return new CycleStatistics(
            TotalCycles: cycleList.Count,
            SelfLoops: selfLoops,
            ComplexCycles: complexCycles,
            ShortestCycleLength: lengths.Min(),
            LongestCycleLength: lengths.Max(),
            AverageCycleLength: lengths.Average(),
            CycleLengthDistribution: distribution
        );
    }
}

/// <summary>
/// Represents the result of cycle detection analysis on a directed graph.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph</typeparam>
public sealed record CycleDetectionResult<TNode>(
    bool HasCycles,
    IReadOnlyList<GraphCycle<TNode>> Cycles,
    CycleStatistics Statistics,
    TimeSpan ProcessingTime,
    string Algorithm,
    DateTime CompletedAt
) where TNode : notnull
{
    /// <summary>
    /// Gets whether the graph is acyclic (contains no cycles).
    /// </summary>
    public bool IsAcyclic => !HasCycles;

    /// <summary>
    /// Gets the number of cycles found.
    /// </summary>
    public int CycleCount => Cycles.Count;

    /// <summary>
    /// Gets the processing time in milliseconds.
    /// </summary>
    public double ProcessingTimeMs => ProcessingTime.TotalMilliseconds;

    /// <summary>
    /// Gets whether the analysis found only self-loops.
    /// </summary>
    public bool HasOnlySelfLoops => HasCycles && Statistics.ComplexCycles == 0;

    /// <summary>
    /// Gets whether the analysis found complex cycles (involving multiple nodes).
    /// </summary>
    public bool HasComplexCycles => Statistics.ComplexCycles > 0;

    /// <summary>
    /// Gets all cycles that contain the specified node.
    /// </summary>
    /// <param name="node">The node to search for</param>
    /// <returns>All cycles containing the node</returns>
    public IEnumerable<GraphCycle<TNode>> GetCyclesContaining(TNode node) =>
        Cycles.Where(cycle => cycle.ContainsNode(node));

    /// <summary>
    /// Gets all self-loop cycles.
    /// </summary>
    /// <returns>All self-loop cycles</returns>
    public IEnumerable<GraphCycle<TNode>> GetSelfLoops() =>
        Cycles.Where(cycle => cycle.IsSelfLoop);

    /// <summary>
    /// Gets all complex cycles (involving multiple nodes).
    /// </summary>
    /// <returns>All complex cycles</returns>
    public IEnumerable<GraphCycle<TNode>> GetComplexCycles() =>
        Cycles.Where(cycle => cycle.IsComplexCycle);

    /// <summary>
    /// Creates a result indicating no cycles were found.
    /// </summary>
    /// <param name="processingTime">Time taken for the analysis</param>
    /// <param name="algorithm">Algorithm used for detection</param>
    /// <returns>Result indicating no cycles</returns>
    public static CycleDetectionResult<TNode> NoCycles(TimeSpan processingTime, string algorithm) =>
        new(
            HasCycles: false,
            Cycles: Array.Empty<GraphCycle<TNode>>(),
            Statistics: CycleStatistics.Empty,
            ProcessingTime: processingTime,
            Algorithm: algorithm,
            CompletedAt: DateTime.UtcNow
        );

    /// <summary>
    /// Creates a result with detected cycles.
    /// </summary>
    /// <param name="cycles">The cycles that were found</param>
    /// <param name="processingTime">Time taken for the analysis</param>
    /// <param name="algorithm">Algorithm used for detection</param>
    /// <returns>Result containing the detected cycles</returns>
    public static CycleDetectionResult<TNode> WithCycles(
        IEnumerable<GraphCycle<TNode>> cycles, 
        TimeSpan processingTime, 
        string algorithm)
    {
        var cycleList = cycles.ToList();
        return new(
            HasCycles: cycleList.Count > 0,
            Cycles: cycleList,
            Statistics: CycleStatistics.FromCycles(cycleList),
            ProcessingTime: processingTime,
            Algorithm: algorithm,
            CompletedAt: DateTime.UtcNow
        );
    }
}
