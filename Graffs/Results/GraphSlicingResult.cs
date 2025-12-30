using System;
using System.Collections.Immutable;
using System.Linq;

namespace Graffs.Results;

/// <summary>
/// Results of graph slicing analysis for dependency chain identification.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph</typeparam>
public sealed record GraphSlicingResult<TNode> where TNode : notnull
{
    /// <summary>
    /// All dependency levels in the graph, ordered by level index (distance from root nodes).
    /// </summary>
    public ImmutableArray<DependencyLevel<TNode>> Levels { get; init; } = ImmutableArray<DependencyLevel<TNode>>.Empty;

    /// <summary>
    /// Independent dependency chains identified in the graph.
    /// </summary>
    public ImmutableArray<IndependentChain<TNode>> IndependentChains { get; init; } = ImmutableArray<IndependentChain<TNode>>.Empty;

    /// <summary>
    /// Maximum dependency depth (total number of levels) in the graph.
    /// This represents the total count of dependency levels, not the highest level index.
    /// For example, if levels are [0, 1, 2, 3], then MaxDependencyDepth = 4.
    /// This corresponds to the length of the longest dependency chain in the graph.
    /// </summary>
    public int MaxDependencyDepth { get; init; }

    /// <summary>
    /// Whether the slicing analysis is valid (graph has no cycles).
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error message if validation failed, null if valid.
    /// </summary>
    public string? ValidationError { get; init; }

    /// <summary>
    /// Total number of nodes analyzed.
    /// </summary>
    public int TotalNodes { get; init; }

    /// <summary>
    /// Total number of dependency levels.
    /// </summary>
    public int TotalLevels => Levels.Length;

    /// <summary>
    /// Average number of nodes per level.
    /// </summary>
    public double AverageNodesPerLevel => TotalLevels > 0 ? (double)TotalNodes / TotalLevels : 0;

    /// <summary>
    /// Time taken to perform the slicing analysis.
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Creates a valid graph slicing result.
    /// </summary>
    /// <param name="levels">The dependency levels</param>
    /// <param name="independentChains">The independent chains</param>
    /// <param name="totalNodes">Total number of nodes</param>
    /// <param name="processingTime">Time taken for analysis</param>
    /// <returns>A valid GraphSlicingResult</returns>
    public static GraphSlicingResult<TNode> Success(
        ImmutableArray<DependencyLevel<TNode>> levels,
        ImmutableArray<IndependentChain<TNode>> independentChains,
        int totalNodes,
        TimeSpan processingTime)
    {
        return new GraphSlicingResult<TNode>
        {
            Levels = levels,
            IndependentChains = independentChains,
            MaxDependencyDepth = levels.Length,
            IsValid = true,
            ValidationError = null,
            TotalNodes = totalNodes,
            ProcessingTime = processingTime
        };
    }

    /// <summary>
    /// Creates an invalid graph slicing result for graphs with cycles.
    /// </summary>
    /// <param name="validationError">The validation error message</param>
    /// <param name="processingTime">Time taken for analysis</param>
    /// <returns>An invalid GraphSlicingResult</returns>
    public static GraphSlicingResult<TNode> Invalid(string validationError, TimeSpan processingTime)
    {
        return new GraphSlicingResult<TNode>
        {
            Levels = ImmutableArray<DependencyLevel<TNode>>.Empty,
            IndependentChains = ImmutableArray<IndependentChain<TNode>>.Empty,
            MaxDependencyDepth = 0,
            IsValid = false,
            ValidationError = validationError,
            TotalNodes = 0,
            ProcessingTime = processingTime
        };
    }
}
