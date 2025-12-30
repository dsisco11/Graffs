using System;
using System.Collections.Immutable;

namespace Graffs.Results;

/// <summary>
/// Represents the result of a topological sorting operation on a directed graph.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph.</typeparam>
public sealed record TopologicalSortResult<TNode>
{
    /// <summary>
    /// Gets the nodes sorted in topological order.
    /// If HasCycles is true, this contains only the nodes that could be sorted.
    /// </summary>
    public ImmutableArray<TNode> SortedNodes { get; init; } = ImmutableArray<TNode>.Empty;
    
    /// <summary>
    /// Gets a value indicating whether the graph contains cycles.
    /// </summary>
    public bool HasCycles { get; init; }
    
    /// <summary>
    /// Gets the nodes that are part of cycles and could not be topologically sorted.
    /// Empty if HasCycles is false.
    /// </summary>
    public ImmutableArray<TNode> CyclicNodes { get; init; } = ImmutableArray<TNode>.Empty;
    
    /// <summary>
    /// Gets the time taken to perform the topological sort operation.
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the topological sort is valid (no cycles detected).
    /// </summary>
    public bool IsValid => !HasCycles;
    
    /// <summary>
    /// Gets the number of nodes in the sorted result.
    /// </summary>
    public int NodeCount => SortedNodes.Length;
    
    /// <summary>
    /// Gets the processing time in milliseconds.
    /// </summary>
    public double ProcessingTimeMs => ProcessingTime.TotalMilliseconds;
    
    /// <summary>
    /// Creates a successful topological sort result.
    /// </summary>
    /// <param name="sortedNodes">The nodes in topological order.</param>
    /// <param name="processingTime">The time taken to perform the sort.</param>
    /// <returns>A TopologicalSortResult indicating successful sorting.</returns>
    public static TopologicalSortResult<TNode> Success(
        ImmutableArray<TNode> sortedNodes, 
        TimeSpan processingTime)
    {
        return new TopologicalSortResult<TNode>
        {
            SortedNodes = sortedNodes,
            HasCycles = false,
            CyclicNodes = ImmutableArray<TNode>.Empty,
            ProcessingTime = processingTime
        };
    }
    
    /// <summary>
    /// Creates a topological sort result with cycle detection.
    /// </summary>
    /// <param name="sortedNodes">The nodes that could be sorted (partial ordering).</param>
    /// <param name="cyclicNodes">The nodes that are part of cycles.</param>
    /// <param name="processingTime">The time taken to perform the sort.</param>
    /// <returns>A TopologicalSortResult indicating cycles were detected.</returns>
    public static TopologicalSortResult<TNode> WithCycles(
        ImmutableArray<TNode> sortedNodes,
        ImmutableArray<TNode> cyclicNodes,
        TimeSpan processingTime)
    {
        return new TopologicalSortResult<TNode>
        {
            SortedNodes = sortedNodes,
            HasCycles = true,
            CyclicNodes = cyclicNodes,
            ProcessingTime = processingTime
        };
    }
    
    /// <summary>
    /// Creates an empty topological sort result.
    /// </summary>
    /// <param name="processingTime">The time taken to perform the sort.</param>
    /// <returns>A TopologicalSortResult for an empty graph.</returns>
    public static TopologicalSortResult<TNode> Empty(TimeSpan processingTime)
    {
        return new TopologicalSortResult<TNode>
        {
            SortedNodes = ImmutableArray<TNode>.Empty,
            HasCycles = false,
            CyclicNodes = ImmutableArray<TNode>.Empty,
            ProcessingTime = processingTime
        };
    }
}
