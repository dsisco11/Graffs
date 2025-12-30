using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Graffs;

namespace Graffs.Implementations;

/// <summary>
/// Compact implementation of <see cref="IDirectedGraph{TNode}"/> optimized for memory efficiency.
/// This implementation uses Compressed Sparse Row (CSR) format and is suitable for moderate-sized
/// graphs where memory usage is more important than some performance characteristics.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph. Must be non-null.</typeparam>
public sealed class CompactGraph<TNode> : IDirectedGraph<TNode>
    where TNode : notnull
{
    private readonly ImmutableArray<TNode> _nodeIndex;
    private readonly ImmutableDictionary<TNode, int> _nodeToIndex;
    private readonly ImmutableArray<int> _outgoingOffsets;
    private readonly ImmutableArray<int> _outgoingTargets;
    private readonly ImmutableArray<int> _incomingOffsets;
    private readonly ImmutableArray<int> _incomingTargets;
    private readonly ImmutableArray<IGraphEdge<TNode>> _edges;
    private readonly ImmutableHashSet<TNode> _nodes;

    /// <summary>
    /// Gets all nodes in the graph as an immutable set.
    /// </summary>
    public IReadOnlySet<TNode> Nodes => _nodes;

    /// <summary>
    /// Gets all edges in the graph as an immutable collection.
    /// </summary>
    public IReadOnlyCollection<IGraphEdge<TNode>> Edges => _edges;

    /// <summary>
    /// Gets the number of nodes in the graph.
    /// </summary>
    public int Count => _nodeIndex.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactGraph{TNode}"/> class from a collection of edges.
    /// </summary>
    /// <param name="edges">The edges to include in the graph.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="edges"/> is null.</exception>
    public CompactGraph(IEnumerable<IGraphEdge<TNode>> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);

        var edgeArray = edges.ToImmutableArray();
        _edges = edgeArray;

        // Extract all unique nodes
        var allNodes = new HashSet<TNode>();
        foreach (var edge in edgeArray)
        {
            allNodes.Add(edge.Source);
            allNodes.Add(edge.Target);
        }

        // Create node indexing (sorted for deterministic ordering)
        _nodeIndex = allNodes.OrderBy(n => n.ToString()).ToImmutableArray();
        _nodes = allNodes.ToImmutableHashSet();
        _nodeToIndex = _nodeIndex
            .Select((node, index) => new { node, index })
            .ToImmutableDictionary(x => x.node, x => x.index);

        // Build CSR representation for outgoing edges
        var (outgoingOffsets, outgoingTargets) = BuildOutgoingCSR(edgeArray);
        _outgoingOffsets = outgoingOffsets;
        _outgoingTargets = outgoingTargets;

        // Build CSR representation for incoming edges
        var (incomingOffsets, incomingTargets) = BuildIncomingCSR(edgeArray);
        _incomingOffsets = incomingOffsets;
        _incomingTargets = incomingTargets;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactGraph{TNode}"/> class from nodes and edges.
    /// </summary>
    /// <param name="nodes">The nodes to include in the graph.</param>
    /// <param name="edges">The edges to include in the graph.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> or <paramref name="edges"/> is null.</exception>
    public CompactGraph(IEnumerable<TNode> nodes, IEnumerable<IGraphEdge<TNode>> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        var nodeArray = nodes.ToImmutableArray();
        var edgeArray = edges.ToImmutableArray();
        _edges = edgeArray;

        // Validate that all edge nodes are in the node set
        var nodeSet = nodeArray.ToImmutableHashSet();
        foreach (var edge in edgeArray)
        {
            if (!nodeSet.Contains(edge.Source))
                throw new ArgumentException($"Edge source '{edge.Source}' is not in the node set.");
            if (!nodeSet.Contains(edge.Target))
                throw new ArgumentException($"Edge target '{edge.Target}' is not in the node set.");
        }

        // Create node indexing (preserve input order)
        _nodeIndex = nodeArray;
        _nodes = nodeSet;
        _nodeToIndex = _nodeIndex
            .Select((node, index) => new { node, index })
            .ToImmutableDictionary(x => x.node, x => x.index);

        // Build CSR representations
        var (outgoingOffsets, outgoingTargets) = BuildOutgoingCSR(edgeArray);
        _outgoingOffsets = outgoingOffsets;
        _outgoingTargets = outgoingTargets;

        var (incomingOffsets, incomingTargets) = BuildIncomingCSR(edgeArray);
        _incomingOffsets = incomingOffsets;
        _incomingTargets = incomingTargets;
    }

    private (ImmutableArray<int> offsets, ImmutableArray<int> targets) BuildOutgoingCSR(ImmutableArray<IGraphEdge<TNode>> edgeArray)
    {
        var nodeCount = _nodeIndex.Length;
        var outgoingCounts = new int[nodeCount];
        var outgoingLists = new List<int>[nodeCount];

        // Initialize lists
        for (int i = 0; i < nodeCount; i++)
        {
            outgoingLists[i] = new List<int>();
        }

        // Count outgoing edges and build adjacency lists
        foreach (var edge in edgeArray)
        {
            var sourceIndex = _nodeToIndex[edge.Source];
            var targetIndex = _nodeToIndex[edge.Target];
            outgoingLists[sourceIndex].Add(targetIndex);
            outgoingCounts[sourceIndex]++;
        }

        // Sort adjacency lists for deterministic ordering and potential binary search
        for (int i = 0; i < nodeCount; i++)
        {
            outgoingLists[i].Sort();
        }

        // Build CSR arrays
        var offsets = new int[nodeCount + 1];
        var totalEdges = outgoingCounts.Sum();
        var targets = new int[totalEdges];

        int currentOffset = 0;
        for (int i = 0; i < nodeCount; i++)
        {
            offsets[i] = currentOffset;
            for (int j = 0; j < outgoingLists[i].Count; j++)
            {
                targets[currentOffset + j] = outgoingLists[i][j];
            }
            currentOffset += outgoingLists[i].Count;
        }
        offsets[nodeCount] = currentOffset;

        return (offsets.ToImmutableArray(), targets.ToImmutableArray());
    }

    private (ImmutableArray<int> offsets, ImmutableArray<int> sources) BuildIncomingCSR(ImmutableArray<IGraphEdge<TNode>> edgeArray)
    {
        var nodeCount = _nodeIndex.Length;
        var incomingCounts = new int[nodeCount];
        var incomingLists = new List<int>[nodeCount];

        // Initialize lists
        for (int i = 0; i < nodeCount; i++)
        {
            incomingLists[i] = new List<int>();
        }

        // Count incoming edges and build adjacency lists
        foreach (var edge in edgeArray)
        {
            var sourceIndex = _nodeToIndex[edge.Source];
            var targetIndex = _nodeToIndex[edge.Target];
            incomingLists[targetIndex].Add(sourceIndex);
            incomingCounts[targetIndex]++;
        }

        // Sort adjacency lists for deterministic ordering
        for (int i = 0; i < nodeCount; i++)
        {
            incomingLists[i].Sort();
        }

        // Build CSR arrays
        var offsets = new int[nodeCount + 1];
        var totalEdges = incomingCounts.Sum();
        var sources = new int[totalEdges];

        int currentOffset = 0;
        for (int i = 0; i < nodeCount; i++)
        {
            offsets[i] = currentOffset;
            for (int j = 0; j < incomingLists[i].Count; j++)
            {
                sources[currentOffset + j] = incomingLists[i][j];
            }
            currentOffset += incomingLists[i].Count;
        }
        offsets[nodeCount] = currentOffset;

        return (offsets.ToImmutableArray(), sources.ToImmutableArray());
    }

    /// <summary>
    /// Gets the nodes that the specified node depends on (incoming edges).
    /// </summary>
    /// <param name="node">The node to get dependencies for.</param>
    /// <returns>An immutable set of nodes that the specified node depends on.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> is not in the graph.</exception>
    public IReadOnlySet<TNode> GetDependencies(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodeToIndex.TryGetValue(node, out var nodeIndex))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));

        var dependencies = new HashSet<TNode>();
        var startOffset = _incomingOffsets[nodeIndex];
        var endOffset = _incomingOffsets[nodeIndex + 1];

        for (int i = startOffset; i < endOffset; i++)
        {
            var sourceIndex = _incomingTargets[i];
            dependencies.Add(_nodeIndex[sourceIndex]);
        }

        return dependencies.ToImmutableHashSet();
    }

    /// <summary>
    /// Gets the nodes that depend on the specified node (outgoing edges).
    /// </summary>
    /// <param name="node">The node to get dependents for.</param>
    /// <returns>An immutable set of nodes that depend on the specified node.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> is not in the graph.</exception>
    public IReadOnlySet<TNode> GetDependents(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodeToIndex.TryGetValue(node, out var nodeIndex))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));

        var dependents = new HashSet<TNode>();
        var startOffset = _outgoingOffsets[nodeIndex];
        var endOffset = _outgoingOffsets[nodeIndex + 1];

        for (int i = startOffset; i < endOffset; i++)
        {
            var targetIndex = _outgoingTargets[i];
            dependents.Add(_nodeIndex[targetIndex]);
        }

        return dependents.ToImmutableHashSet();
    }

    /// <summary>
    /// Determines whether there is a direct edge from the source node to the target node.
    /// Uses binary search for efficient lookup since adjacency lists are sorted.
    /// </summary>
    /// <param name="source">The source node.</param>
    /// <param name="target">The target node.</param>
    /// <returns>True if there is a direct edge from source to target; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="target"/> is null.</exception>
    public bool HasEdge(TNode source, TNode target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        if (!_nodeToIndex.TryGetValue(source, out var sourceIndex) ||
            !_nodeToIndex.TryGetValue(target, out var targetIndex))
        {
            return false;
        }

        var startOffset = _outgoingOffsets[sourceIndex];
        var endOffset = _outgoingOffsets[sourceIndex + 1];

        // Use binary search since targets are sorted
        var left = startOffset;
        var right = endOffset - 1;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var midTarget = _outgoingTargets[mid];

            if (midTarget == targetIndex)
                return true;
            
            if (midTarget < targetIndex)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return false;
    }

    /// <summary>
    /// Determines whether there is a path from the source node to the target node.
    /// </summary>
    /// <param name="source">The source node.</param>
    /// <param name="target">The target node.</param>
    /// <returns>True if there is a path from source to target; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="target"/> is null.</exception>
    public bool HasPath(TNode source, TNode target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        if (!_nodeToIndex.ContainsKey(source) || !_nodeToIndex.ContainsKey(target))
            return false;

        if (source.Equals(target))
            return true;

        // Use BFS to find path
        var visited = new HashSet<TNode>();
        var queue = new Queue<TNode>();
        queue.Enqueue(source);
        visited.Add(source);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in GetDependents(current))
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
    /// Gets all outgoing edges from the specified node.
    /// </summary>
    /// <param name="node">The node to get outgoing edges for.</param>
    /// <returns>An immutable collection of outgoing edges.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> is not in the graph.</exception>
    public IReadOnlyCollection<IGraphEdge<TNode>> GetOutgoingEdges(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodeToIndex.TryGetValue(node, out var nodeIndex))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));

        var outgoingEdges = new List<IGraphEdge<TNode>>();
        var startOffset = _outgoingOffsets[nodeIndex];
        var endOffset = _outgoingOffsets[nodeIndex + 1];

        for (int i = startOffset; i < endOffset; i++)
        {
            var targetIndex = _outgoingTargets[i];
            outgoingEdges.Add(new GraphEdge<TNode>(node, _nodeIndex[targetIndex]));
        }

        return outgoingEdges.ToImmutableArray();
    }

    /// <summary>
    /// Gets all incoming edges to the specified node.
    /// </summary>
    /// <param name="node">The node to get incoming edges for.</param>
    /// <returns>An immutable collection of incoming edges.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> is not in the graph.</exception>
    public IReadOnlyCollection<IGraphEdge<TNode>> GetIncomingEdges(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodeToIndex.TryGetValue(node, out var nodeIndex))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));

        var incomingEdges = new List<IGraphEdge<TNode>>();
        var startOffset = _incomingOffsets[nodeIndex];
        var endOffset = _incomingOffsets[nodeIndex + 1];

        for (int i = startOffset; i < endOffset; i++)
        {
            var sourceIndex = _incomingTargets[i];
            incomingEdges.Add(new GraphEdge<TNode>(_nodeIndex[sourceIndex], node));
        }

        return incomingEdges.ToImmutableArray();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the nodes in the graph.
    /// </summary>
    /// <returns>An enumerator for the nodes in the graph.</returns>
    public IEnumerator<TNode> GetEnumerator()
    {
        return _nodes.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the nodes in the graph.
    /// </summary>
    /// <returns>An enumerator for the nodes in the graph.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Gets the memory footprint statistics of this graph implementation.
    /// </summary>
    public CompactGraphMemoryStats GetMemoryStats()
    {
        var nodeIndexSize = _nodeIndex.Length * IntPtr.Size; // Reference size
        var nodeToIndexSize = _nodeToIndex.Count * (IntPtr.Size + sizeof(int)); // Approx dictionary overhead
        var outgoingOffsetsSize = _outgoingOffsets.Length * sizeof(int);
        var outgoingTargetsSize = _outgoingTargets.Length * sizeof(int);
        var incomingOffsetsSize = _incomingOffsets.Length * sizeof(int);
        var incomingTargetsSize = _incomingTargets.Length * sizeof(int);
        var edgesSize = _edges.Length * IntPtr.Size; // Reference size for edge objects

        var totalSize = nodeIndexSize + nodeToIndexSize + outgoingOffsetsSize + 
                      outgoingTargetsSize + incomingOffsetsSize + incomingTargetsSize + edgesSize;

        return new CompactGraphMemoryStats(
            NodeCount: Count,
            EdgeCount: _edges.Length,
            NodeIndexSizeBytes: nodeIndexSize,
            NodeMappingSizeBytes: nodeToIndexSize,
            OutgoingCSRSizeBytes: outgoingOffsetsSize + outgoingTargetsSize,
            IncomingCSRSizeBytes: incomingOffsetsSize + incomingTargetsSize,
            EdgesSizeBytes: edgesSize,
            TotalSizeBytes: totalSize,
            AverageOutDegree: Count > 0 ? (double)_edges.Length / Count : 0.0
        );
    }

    /// <summary>
    /// Gets the out-degree of the specified node.
    /// </summary>
    /// <param name="node">The node to get the out-degree for.</param>
    /// <returns>The number of outgoing edges from the node.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> is not in the graph.</exception>
    public int GetOutDegree(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodeToIndex.TryGetValue(node, out var nodeIndex))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));

        return _outgoingOffsets[nodeIndex + 1] - _outgoingOffsets[nodeIndex];
    }

    /// <summary>
    /// Gets the in-degree of the specified node.
    /// </summary>
    /// <param name="node">The node to get the in-degree for.</param>
    /// <returns>The number of incoming edges to the node.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> is not in the graph.</exception>
    public int GetInDegree(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodeToIndex.TryGetValue(node, out var nodeIndex))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));

        return _incomingOffsets[nodeIndex + 1] - _incomingOffsets[nodeIndex];
    }
}

/// <summary>
/// Memory usage statistics for a CompactGraph instance.
/// </summary>
public sealed record CompactGraphMemoryStats(
    int NodeCount,
    int EdgeCount,
    int NodeIndexSizeBytes,
    int NodeMappingSizeBytes,
    int OutgoingCSRSizeBytes,
    int IncomingCSRSizeBytes,
    int EdgesSizeBytes,
    int TotalSizeBytes,
    double AverageOutDegree
)
{
    /// <summary>
    /// Gets the bytes per node ratio.
    /// </summary>
    public double BytesPerNode => NodeCount > 0 ? (double)TotalSizeBytes / NodeCount : 0.0;

    /// <summary>
    /// Gets the bytes per edge ratio.
    /// </summary>
    public double BytesPerEdge => EdgeCount > 0 ? (double)TotalSizeBytes / EdgeCount : 0.0;

    /// <summary>
    /// Gets the compression ratio compared to a naive representation.
    /// </summary>
    public double CompressionRatio
    {
        get
        {
            // Naive representation: adjacency matrix using bool[,] (1 byte per bool)
            if (NodeCount <= 1) return 1.0;
            var naiveSize = NodeCount * NodeCount * sizeof(bool); // bool matrix
            return naiveSize > 0 ? (double)naiveSize / TotalSizeBytes : 1.0;
        }
    }
}
