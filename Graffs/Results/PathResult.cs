using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Graffs.Results;

/// <summary>
/// Represents the result of a path finding operation in a directed graph.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the path</typeparam>
public sealed record PathResult<TNode> where TNode : notnull
{
    /// <summary>
    /// Gets the source node of the path.
    /// </summary>
    public TNode Source { get; init; }

    /// <summary>
    /// Gets the target node of the path.
    /// </summary>
    public TNode Target { get; init; }

    /// <summary>
    /// Gets a value indicating whether a path was found.
    /// </summary>
    public bool PathExists { get; init; }

    /// <summary>
    /// Gets the path as a sequence of nodes from source to target.
    /// Empty if no path exists.
    /// </summary>
    public ImmutableArray<TNode> Path { get; init; }

    /// <summary>
    /// Gets the distance (number of edges) in the path.
    /// -1 if no path exists.
    /// </summary>
    public int Distance { get; init; }

    /// <summary>
    /// Gets additional path statistics.
    /// </summary>
    public PathStatistics Statistics { get; init; }

    /// <summary>
    /// Gets the time taken to compute the path.
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Gets the algorithm used to find the path.
    /// </summary>
    public string Algorithm { get; init; }

    /// <summary>
    /// Gets the timestamp when the path finding was completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PathResult{TNode}"/> record.
    /// </summary>
    /// <param name="Source">The source node</param>
    /// <param name="Target">The target node</param>
    /// <param name="PathExists">Whether a path was found</param>
    /// <param name="Path">The path nodes</param>
    /// <param name="Distance">The path distance</param>
    /// <param name="Statistics">Path statistics</param>
    /// <param name="ProcessingTime">Processing time</param>
    /// <param name="Algorithm">Algorithm used</param>
    /// <param name="CompletedAt">Completion timestamp</param>
    public PathResult(
        TNode Source,
        TNode Target,
        bool PathExists,
        ImmutableArray<TNode> Path,
        int Distance,
        PathStatistics Statistics,
        TimeSpan ProcessingTime,
        string Algorithm,
        DateTime CompletedAt)
    {
        this.Source = Source;
        this.Target = Target;
        this.PathExists = PathExists;
        this.Path = Path;
        this.Distance = Distance;
        this.Statistics = Statistics;
        this.ProcessingTime = ProcessingTime;
        this.Algorithm = Algorithm;
        this.CompletedAt = CompletedAt;
    }

    /// <summary>
    /// Creates a path result indicating no path was found.
    /// </summary>
    /// <param name="source">The source node</param>
    /// <param name="target">The target node</param>
    /// <param name="processingTime">The time taken to determine no path exists</param>
    /// <param name="algorithm">The algorithm used</param>
    /// <returns>A path result indicating no path</returns>
    public static PathResult<TNode> NoPath(TNode source, TNode target, TimeSpan processingTime, string algorithm)
    {
        return new PathResult<TNode>(
            Source: source,
            Target: target,
            PathExists: false,
            Path: ImmutableArray<TNode>.Empty,
            Distance: -1,
            Statistics: PathStatistics.Empty,
            ProcessingTime: processingTime,
            Algorithm: algorithm,
            CompletedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Creates a path result for a successful path finding operation.
    /// </summary>
    /// <param name="source">The source node</param>
    /// <param name="target">The target node</param>
    /// <param name="path">The found path</param>
    /// <param name="processingTime">The time taken to find the path</param>
    /// <param name="algorithm">The algorithm used</param>
    /// <param name="nodesExplored">Number of nodes explored during search</param>
    /// <returns>A path result indicating a successful path finding</returns>
    public static PathResult<TNode> WithPath(
        TNode source, 
        TNode target, 
        IEnumerable<TNode> path, 
        TimeSpan processingTime, 
        string algorithm,
        int nodesExplored = 0)
    {
        var pathArray = path.ToImmutableArray();
        var distance = Math.Max(0, pathArray.Length - 1);

        var statistics = new PathStatistics(
            NodesExplored: nodesExplored,
            IsShortestPath: true, // Assume shortest path for now
            HasCycles: false // Single path cannot have cycles
        );

        return new PathResult<TNode>(
            Source: source,
            Target: target,
            PathExists: true,
            Path: pathArray,
            Distance: distance,
            Statistics: statistics,
            ProcessingTime: processingTime,
            Algorithm: algorithm,
            CompletedAt: DateTime.UtcNow
        );
    }
}

/// <summary>
/// Represents statistics about a path finding operation.
/// </summary>
/// <param name="NodesExplored">The number of nodes explored during the search</param>
/// <param name="IsShortestPath">Whether the found path is guaranteed to be shortest</param>
/// <param name="HasCycles">Whether the path contains cycles</param>
public sealed record PathStatistics(
    int NodesExplored,
    bool IsShortestPath,
    bool HasCycles)
{
    /// <summary>
    /// Gets empty path statistics.
    /// </summary>
    public static PathStatistics Empty => new(0, false, false);
}

/// <summary>
/// Represents the result of finding multiple paths between nodes.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the paths</typeparam>
public sealed record MultiPathResult<TNode> where TNode : notnull
{
    /// <summary>
    /// Gets the source node.
    /// </summary>
    public TNode Source { get; init; }

    /// <summary>
    /// Gets the target node.
    /// </summary>
    public TNode Target { get; init; }

    /// <summary>
    /// Gets all found paths from source to target.
    /// </summary>
    public ImmutableArray<ImmutableArray<TNode>> Paths { get; init; }

    /// <summary>
    /// Gets the shortest path distance.
    /// -1 if no paths exist.
    /// </summary>
    public int ShortestDistance { get; init; }

    /// <summary>
    /// Gets the longest path distance.
    /// -1 if no paths exist.
    /// </summary>
    public int LongestDistance { get; init; }

    /// <summary>
    /// Gets additional statistics about the path finding operation.
    /// </summary>
    public PathStatistics Statistics { get; init; }

    /// <summary>
    /// Gets the time taken to find all paths.
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Gets the algorithm used to find the paths.
    /// </summary>
    public string Algorithm { get; init; }

    /// <summary>
    /// Gets the timestamp when the path finding was completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiPathResult{TNode}"/> record.
    /// </summary>
    public MultiPathResult(
        TNode Source,
        TNode Target,
        ImmutableArray<ImmutableArray<TNode>> Paths,
        int ShortestDistance,
        int LongestDistance,
        PathStatistics Statistics,
        TimeSpan ProcessingTime,
        string Algorithm,
        DateTime CompletedAt)
    {
        this.Source = Source;
        this.Target = Target;
        this.Paths = Paths;
        this.ShortestDistance = ShortestDistance;
        this.LongestDistance = LongestDistance;
        this.Statistics = Statistics;
        this.ProcessingTime = ProcessingTime;
        this.Algorithm = Algorithm;
        this.CompletedAt = CompletedAt;
    }

    /// <summary>
    /// Creates a multi-path result indicating no paths were found.
    /// </summary>
    /// <param name="source">The source node</param>
    /// <param name="target">The target node</param>
    /// <param name="processingTime">The time taken to determine no paths exist</param>
    /// <param name="algorithm">The algorithm used</param>
    /// <returns>A multi-path result indicating no paths</returns>
    public static MultiPathResult<TNode> NoPaths(TNode source, TNode target, TimeSpan processingTime, string algorithm)
    {
        return new MultiPathResult<TNode>(
            Source: source,
            Target: target,
            Paths: ImmutableArray<ImmutableArray<TNode>>.Empty,
            ShortestDistance: -1,
            LongestDistance: -1,
            Statistics: PathStatistics.Empty,
            ProcessingTime: processingTime,
            Algorithm: algorithm,
            CompletedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Creates a multi-path result for successful path finding.
    /// </summary>
    /// <param name="source">The source node</param>
    /// <param name="target">The target node</param>
    /// <param name="paths">The found paths</param>
    /// <param name="processingTime">The time taken to find paths</param>
    /// <param name="algorithm">The algorithm used</param>
    /// <param name="nodesExplored">Number of nodes explored during search</param>
    /// <returns>A multi-path result with the found paths</returns>
    public static MultiPathResult<TNode> WithPaths(
        TNode source,
        TNode target,
        IEnumerable<IEnumerable<TNode>> paths,
        TimeSpan processingTime,
        string algorithm,
        int nodesExplored = 0)
    {
        var pathArrays = paths.Select(p => p.ToImmutableArray()).ToImmutableArray();
        
        var shortestDistance = pathArrays.Length > 0 
            ? pathArrays.Min(p => Math.Max(0, p.Length - 1))
            : -1;
        
        var longestDistance = pathArrays.Length > 0
            ? pathArrays.Max(p => Math.Max(0, p.Length - 1))
            : -1;

        var statistics = new PathStatistics(
            NodesExplored: nodesExplored,
            IsShortestPath: true,
            HasCycles: false
        );

        return new MultiPathResult<TNode>(
            Source: source,
            Target: target,
            Paths: pathArrays,
            ShortestDistance: shortestDistance,
            LongestDistance: longestDistance,
            Statistics: statistics,
            ProcessingTime: processingTime,
            Algorithm: algorithm,
            CompletedAt: DateTime.UtcNow
        );
    }
}
