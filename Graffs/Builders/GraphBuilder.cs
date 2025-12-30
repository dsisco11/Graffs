using System;
using System.Collections.Generic;
using Graffs;
using Graffs.Implementations;

namespace Graffs.Builders;

/// <summary>
/// Generic fluent API for building any directed graph.
/// Provides a convenient way to construct graphs using method chaining.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph. Must be non-null.</typeparam>
public sealed class GraphBuilder<TNode> where TNode : notnull
{
    private readonly HashSet<TNode> _nodes = new();
    private readonly HashSet<IGraphEdge<TNode>> _edges = new();
    
    /// <summary>
    /// Adds a single node to the graph.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    public GraphBuilder<TNode> AddNode(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        _nodes.Add(node);
        return this;
    }
    
    /// <summary>
    /// Adds multiple nodes to the graph.
    /// </summary>
    /// <param name="nodes">The nodes to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> is null.</exception>
    public GraphBuilder<TNode> AddNodes(IEnumerable<TNode> nodes)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        
        foreach (var node in nodes)
        {
            if (node != null)
            {
                _nodes.Add(node);
            }
        }
        return this;
    }
    
    /// <summary>
    /// Adds a directed edge between two nodes.
    /// </summary>
    /// <param name="from">The source node of the edge.</param>
    /// <param name="to">The target node of the edge.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="to"/> is null.</exception>
    public GraphBuilder<TNode> AddEdge(TNode from, TNode to)
    {
        if (from == null)
            throw new ArgumentNullException(nameof(from));
        if (to == null)
            throw new ArgumentNullException(nameof(to));
        
        _nodes.Add(from);
        _nodes.Add(to);
        _edges.Add(new GraphEdge<TNode>(from, to));
        return this;
    }
    
    /// <summary>
    /// Adds a directed edge to the graph.
    /// </summary>
    /// <param name="edge">The edge to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="edge"/> is null.</exception>
    public GraphBuilder<TNode> AddEdge(IGraphEdge<TNode> edge)
    {
        if (edge == null)
            throw new ArgumentNullException(nameof(edge));
        
        _nodes.Add(edge.Source);
        _nodes.Add(edge.Target);
        _edges.Add(edge);
        return this;
    }
    
    /// <summary>
    /// Adds multiple edges to the graph.
    /// </summary>
    /// <param name="edges">The edges to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="edges"/> is null.</exception>
    public GraphBuilder<TNode> AddEdges(IEnumerable<IGraphEdge<TNode>> edges)
    {
        if (edges == null)
            throw new ArgumentNullException(nameof(edges));
        
        foreach (var edge in edges)
        {
            if (edge != null)
            {
                AddEdge(edge);
            }
        }
        return this;
    }
    
    /// <summary>
    /// Builds the graph from the added nodes and edges.
    /// </summary>
    /// <returns>An immutable directed graph containing the specified nodes and edges.</returns>
    public IDirectedGraph<TNode> Build()
    {
        return new AdjacencyListGraph<TNode>(_nodes, _edges);
    }
    
    /// <summary>
    /// Gets the current number of nodes that will be in the built graph.
    /// </summary>
    public int NodeCount => _nodes.Count;
    
    /// <summary>
    /// Gets the current number of edges that will be in the built graph.
    /// </summary>
    public int EdgeCount => _edges.Count;
    
    /// <summary>
    /// Clears all nodes and edges from the builder.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public GraphBuilder<TNode> Clear()
    {
        _nodes.Clear();
        _edges.Clear();
        return this;
    }
}
