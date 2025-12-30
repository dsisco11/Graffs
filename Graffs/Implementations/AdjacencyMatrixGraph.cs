using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Graffs;

namespace Graffs.Implementations;

/// <summary>
/// Adjacency matrix implementation of <see cref="IDirectedGraph{TNode}"/> optimized for dense graphs.
/// This implementation provides O(1) edge queries and is suitable for graphs where the number
/// of edges approaches the maximum possible (dense graphs).
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph. Must be non-null.</typeparam>
public sealed class AdjacencyMatrixGraph<TNode> : IDirectedGraph<TNode>
    where TNode : notnull
{
    private readonly ImmutableArray<TNode> _nodeIndex;
    private readonly ImmutableDictionary<TNode, int> _nodeToIndex;
    private readonly bool[,] _adjacencyMatrix;
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
    /// Initializes a new instance of the <see cref="AdjacencyMatrixGraph{TNode}"/> class from a collection of edges.
    /// </summary>
    /// <param name="edges">The edges to include in the graph.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="edges"/> is null.</exception>
    public AdjacencyMatrixGraph(IEnumerable<IGraphEdge<TNode>> edges)
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

        // Create node indexing
        _nodeIndex = allNodes.ToImmutableArray();
        _nodes = allNodes.ToImmutableHashSet();
        _nodeToIndex = _nodeIndex
            .Select((node, index) => new { node, index })
            .ToImmutableDictionary(x => x.node, x => x.index);

        // Initialize adjacency matrix
        var nodeCount = _nodeIndex.Length;
        _adjacencyMatrix = new bool[nodeCount, nodeCount];

        // Populate adjacency matrix
        foreach (var edge in edgeArray)
        {
            var sourceIndex = _nodeToIndex[edge.Source];
            var targetIndex = _nodeToIndex[edge.Target];
            _adjacencyMatrix[sourceIndex, targetIndex] = true;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdjacencyMatrixGraph{TNode}"/> class from nodes and edges.
    /// </summary>
    /// <param name="nodes">The nodes to include in the graph.</param>
    /// <param name="edges">The edges to include in the graph.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> or <paramref name="edges"/> is null.</exception>
    public AdjacencyMatrixGraph(IEnumerable<TNode> nodes, IEnumerable<IGraphEdge<TNode>> edges)
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

        // Create node indexing
        _nodeIndex = nodeArray;
        _nodes = nodeSet;
        _nodeToIndex = _nodeIndex
            .Select((node, index) => new { node, index })
            .ToImmutableDictionary(x => x.node, x => x.index);

        // Initialize adjacency matrix
        var nodeCount = _nodeIndex.Length;
        _adjacencyMatrix = new bool[nodeCount, nodeCount];

        // Populate adjacency matrix
        foreach (var edge in edgeArray)
        {
            var sourceIndex = _nodeToIndex[edge.Source];
            var targetIndex = _nodeToIndex[edge.Target];
            _adjacencyMatrix[sourceIndex, targetIndex] = true;
        }
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
        for (int i = 0; i < _nodeIndex.Length; i++)
        {
            if (_adjacencyMatrix[i, nodeIndex])
            {
                dependencies.Add(_nodeIndex[i]);
            }
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
        for (int i = 0; i < _nodeIndex.Length; i++)
        {
            if (_adjacencyMatrix[nodeIndex, i])
            {
                dependents.Add(_nodeIndex[i]);
            }
        }

        return dependents.ToImmutableHashSet();
    }

    /// <summary>
    /// Determines whether there is a direct edge from the source node to the target node.
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

        return _adjacencyMatrix[sourceIndex, targetIndex];
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
        for (int i = 0; i < _nodeIndex.Length; i++)
        {
            if (_adjacencyMatrix[nodeIndex, i])
            {
                outgoingEdges.Add(new GraphEdge<TNode>(node, _nodeIndex[i]));
            }
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
        for (int i = 0; i < _nodeIndex.Length; i++)
        {
            if (_adjacencyMatrix[i, nodeIndex])
            {
                incomingEdges.Add(new GraphEdge<TNode>(_nodeIndex[i], node));
            }
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
    /// Gets the density of the graph (ratio of actual edges to maximum possible edges).
    /// </summary>
    public double Density
    {
        get
        {
            if (Count <= 1) return 0.0;
            var maxEdges = Count * (Count - 1); // For directed graph
            return (double)_edges.Length / maxEdges;
        }
    }

    /// <summary>
    /// Gets the internal adjacency matrix for advanced matrix operations.
    /// This is a copy to maintain immutability.
    /// </summary>
    /// <returns>A copy of the internal adjacency matrix.</returns>
    public bool[,] GetAdjacencyMatrix()
    {
        var matrix = new bool[Count, Count];
        Array.Copy(_adjacencyMatrix, matrix, _adjacencyMatrix.Length);
        return matrix;
    }

    /// <summary>
    /// Gets the node at the specified index in the internal node ordering.
    /// </summary>
    /// <param name="index">The index of the node.</param>
    /// <returns>The node at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public TNode GetNodeByIndex(int index)
    {
        if (index < 0 || index >= _nodeIndex.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return _nodeIndex[index];
    }

    /// <summary>
    /// Gets the index of the specified node in the internal node ordering.
    /// </summary>
    /// <param name="node">The node to get the index for.</param>
    /// <returns>The index of the node, or -1 if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    public int GetNodeIndex(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _nodeToIndex.TryGetValue(node, out var index) ? index : -1;
    }
}
