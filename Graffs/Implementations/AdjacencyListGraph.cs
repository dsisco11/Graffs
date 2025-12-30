using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Graffs;

namespace Graffs.Implementations;

/// <summary>
/// Adjacency list implementation of <see cref="IDirectedGraph{TNode}"/> optimized for sparse graphs.
/// This implementation provides O(1) average case performance for most operations and is suitable
/// for most real-world dependency graph scenarios.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph. Must be non-null.</typeparam>
public sealed class AdjacencyListGraph<TNode> : IDirectedGraph<TNode>
    where TNode : notnull
{
    private readonly ImmutableDictionary<TNode, ImmutableHashSet<TNode>> _adjacencyList;
    private readonly ImmutableDictionary<TNode, ImmutableHashSet<TNode>> _reverseAdjacencyList;
    private readonly ImmutableHashSet<TNode> _nodes;
    private readonly ImmutableArray<IGraphEdge<TNode>> _edges;
    
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
    public int Count => _nodes.Count;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AdjacencyListGraph{TNode}"/> class from a collection of edges.
    /// </summary>
    /// <param name="edges">The edges to include in the graph.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="edges"/> is null.</exception>
    public AdjacencyListGraph(IEnumerable<IGraphEdge<TNode>> edges)
    {
        if (edges == null)
            throw new ArgumentNullException(nameof(edges));
        
        var edgeArray = edges.ToImmutableArray();
        _edges = edgeArray;
        
        // Build node set
        var nodeSet = ImmutableHashSet.CreateBuilder<TNode>();
        foreach (var edge in edgeArray)
        {
            nodeSet.Add(edge.Source);
            nodeSet.Add(edge.Target);
        }
        _nodes = nodeSet.ToImmutable();
        
        // Build adjacency lists
        var adjacencyBuilder = ImmutableDictionary.CreateBuilder<TNode, ImmutableHashSet<TNode>>();
        var reverseAdjacencyBuilder = ImmutableDictionary.CreateBuilder<TNode, ImmutableHashSet<TNode>>();
        
        // Initialize empty sets for all nodes
        foreach (var node in _nodes)
        {
            adjacencyBuilder[node] = ImmutableHashSet<TNode>.Empty;
            reverseAdjacencyBuilder[node] = ImmutableHashSet<TNode>.Empty;
        }
        
        // Populate adjacency lists
        foreach (var edge in edgeArray)
        {
            adjacencyBuilder[edge.Source] = adjacencyBuilder[edge.Source].Add(edge.Target);
            reverseAdjacencyBuilder[edge.Target] = reverseAdjacencyBuilder[edge.Target].Add(edge.Source);
        }
        
        _adjacencyList = adjacencyBuilder.ToImmutable();
        _reverseAdjacencyList = reverseAdjacencyBuilder.ToImmutable();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AdjacencyListGraph{TNode}"/> class from a collection of nodes and edges.
    /// This constructor ensures that all specified nodes are included in the graph, even if they have no edges.
    /// </summary>
    /// <param name="nodes">The nodes to include in the graph.</param>
    /// <param name="edges">The edges to include in the graph.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> or <paramref name="edges"/> is null.</exception>
    public AdjacencyListGraph(IEnumerable<TNode> nodes, IEnumerable<IGraphEdge<TNode>> edges)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        if (edges == null)
            throw new ArgumentNullException(nameof(edges));
        
        var edgeArray = edges.ToImmutableArray();
        _edges = edgeArray;
        
        // Build node set from both explicit nodes and edges
        var nodeSet = ImmutableHashSet.CreateBuilder<TNode>();
        
        // Add explicitly provided nodes
        foreach (var node in nodes)
        {
            nodeSet.Add(node);
        }
        
        // Add nodes from edges
        foreach (var edge in edgeArray)
        {
            nodeSet.Add(edge.Source);
            nodeSet.Add(edge.Target);
        }
        
        _nodes = nodeSet.ToImmutable();
        
        // Build adjacency lists
        var adjacencyBuilder = ImmutableDictionary.CreateBuilder<TNode, ImmutableHashSet<TNode>>();
        var reverseAdjacencyBuilder = ImmutableDictionary.CreateBuilder<TNode, ImmutableHashSet<TNode>>();
        
        // Initialize empty sets for all nodes
        foreach (var node in _nodes)
        {
            adjacencyBuilder[node] = ImmutableHashSet<TNode>.Empty;
            reverseAdjacencyBuilder[node] = ImmutableHashSet<TNode>.Empty;
        }
        
        // Populate adjacency lists
        foreach (var edge in edgeArray)
        {
            adjacencyBuilder[edge.Source] = adjacencyBuilder[edge.Source].Add(edge.Target);
            reverseAdjacencyBuilder[edge.Target] = reverseAdjacencyBuilder[edge.Target].Add(edge.Source);
        }
        
        _adjacencyList = adjacencyBuilder.ToImmutable();
        _reverseAdjacencyList = reverseAdjacencyBuilder.ToImmutable();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AdjacencyListGraph{TNode}"/> class from a dependencies dictionary.
    /// </summary>
    /// <param name="dependencies">A dictionary where keys are dependent nodes and values are their dependencies.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dependencies"/> is null.</exception>
    public AdjacencyListGraph(IDictionary<TNode, IEnumerable<TNode>> dependencies)
    {
        if (dependencies == null)
            throw new ArgumentNullException(nameof(dependencies));
        
        // Convert dependencies to edges
        var edgeList = new List<IGraphEdge<TNode>>();
        foreach (var kvp in dependencies)
        {
            var dependent = kvp.Key;
            foreach (var dependency in kvp.Value ?? Enumerable.Empty<TNode>())
            {
                // Edge from dependency to dependent (dependency → dependent)
                edgeList.Add(new GraphEdge<TNode>(dependency, dependent));
            }
        }
        
        _edges = edgeList.ToImmutableArray();
        
        // Build node set
        var nodeSet = ImmutableHashSet.CreateBuilder<TNode>();
        foreach (var kvp in dependencies)
        {
            nodeSet.Add(kvp.Key);
            if (kvp.Value != null)
            {
                foreach (var dependency in kvp.Value)
                {
                    nodeSet.Add(dependency);
                }
            }
        }
        _nodes = nodeSet.ToImmutable();
        
        // Build adjacency lists
        var adjacencyBuilder = ImmutableDictionary.CreateBuilder<TNode, ImmutableHashSet<TNode>>();
        var reverseAdjacencyBuilder = ImmutableDictionary.CreateBuilder<TNode, ImmutableHashSet<TNode>>();
        
        // Initialize empty sets for all nodes
        foreach (var node in _nodes)
        {
            adjacencyBuilder[node] = ImmutableHashSet<TNode>.Empty;
            reverseAdjacencyBuilder[node] = ImmutableHashSet<TNode>.Empty;
        }
        
        // Populate adjacency lists from dependencies
        foreach (var kvp in dependencies)
        {
            var dependent = kvp.Key;
            if (kvp.Value != null)
            {
                foreach (var dependency in kvp.Value)
                {
                    // Edge from dependency to dependent
                    adjacencyBuilder[dependency] = adjacencyBuilder[dependency].Add(dependent);
                    reverseAdjacencyBuilder[dependent] = reverseAdjacencyBuilder[dependent].Add(dependency);
                }
            }
        }
        
        _adjacencyList = adjacencyBuilder.ToImmutable();
        _reverseAdjacencyList = reverseAdjacencyBuilder.ToImmutable();
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
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        if (!_nodes.Contains(node))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));
        
        return _reverseAdjacencyList.GetValueOrDefault(node, ImmutableHashSet<TNode>.Empty);
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
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        if (!_nodes.Contains(node))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));
        
        return _adjacencyList.GetValueOrDefault(node, ImmutableHashSet<TNode>.Empty);
    }
    
    /// <summary>
    /// Checks if there is a direct edge from one node to another.
    /// </summary>
    /// <param name="from">The source node of the edge.</param>
    /// <param name="to">The target node of the edge.</param>
    /// <returns><c>true</c> if there is a direct edge from <paramref name="from"/> to <paramref name="to"/>; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="to"/> is null.</exception>
    public bool HasEdge(TNode from, TNode to)
    {
        if (from == null)
            throw new ArgumentNullException(nameof(from));
        if (to == null)
            throw new ArgumentNullException(nameof(to));
        
        return _adjacencyList.GetValueOrDefault(from, ImmutableHashSet<TNode>.Empty).Contains(to);
    }
    
    /// <summary>
    /// Checks if there is any path (direct or indirect) from one node to another.
    /// </summary>
    /// <param name="from">The source node of the path.</param>
    /// <param name="to">The target node of the path.</param>
    /// <returns><c>true</c> if there is any path from <paramref name="from"/> to <paramref name="to"/>; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="to"/> is null.</exception>
    public bool HasPath(TNode from, TNode to)
    {
        if (from == null)
            throw new ArgumentNullException(nameof(from));
        if (to == null)
            throw new ArgumentNullException(nameof(to));
        
        if (!_nodes.Contains(from) || !_nodes.Contains(to))
            return false;
        
        if (EqualityComparer<TNode>.Default.Equals(from, to))
            return true;
        
        // BFS to find path
        var visited = new HashSet<TNode>();
        var queue = new Queue<TNode>();
        
        queue.Enqueue(from);
        visited.Add(from);
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var dependents = _adjacencyList.GetValueOrDefault(current, ImmutableHashSet<TNode>.Empty);
            
            foreach (var dependent in dependents)
            {
                if (EqualityComparer<TNode>.Default.Equals(dependent, to))
                    return true;
                
                if (visited.Add(dependent))
                {
                    queue.Enqueue(dependent);
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets all outgoing edges from the specified node.
    /// </summary>
    /// <param name="node">The node to get outgoing edges for.</param>
    /// <returns>An immutable collection of edges originating from the specified node.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> is not in the graph.</exception>
    public IReadOnlyCollection<IGraphEdge<TNode>> GetOutgoingEdges(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        if (!_nodes.Contains(node))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));
        
        var dependents = _adjacencyList.GetValueOrDefault(node, ImmutableHashSet<TNode>.Empty);
        return dependents.Select(target => new GraphEdge<TNode>(node, target)).ToImmutableArray();
    }
    
    /// <summary>
    /// Gets all incoming edges to the specified node.
    /// </summary>
    /// <param name="node">The node to get incoming edges for.</param>
    /// <returns>An immutable collection of edges targeting the specified node.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> is not in the graph.</exception>
    public IReadOnlyCollection<IGraphEdge<TNode>> GetIncomingEdges(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        if (!_nodes.Contains(node))
            throw new ArgumentException($"Node '{node}' is not in the graph.", nameof(node));
        
        var dependencies = _reverseAdjacencyList.GetValueOrDefault(node, ImmutableHashSet<TNode>.Empty);
        return dependencies.Select(source => new GraphEdge<TNode>(source, node)).ToImmutableArray();
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
}
