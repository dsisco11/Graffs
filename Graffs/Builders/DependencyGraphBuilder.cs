using System;
using System.Collections.Generic;
using System.Linq;
using Graffs;
using Graffs.Implementations;

namespace Graffs.Builders;

/// <summary>
/// Specialized fluent API for building dependency graphs.
/// This builder provides a more intuitive interface for expressing dependency relationships
/// where dependencies are explicit rather than working with raw edges.
/// </summary>
/// <typeparam name="TNode">The type of nodes in the graph. Must be non-null.</typeparam>
public sealed class DependencyGraphBuilder<TNode> where TNode : notnull
{
    private readonly Dictionary<TNode, HashSet<TNode>> _dependencies = new();
    
    /// <summary>
    /// Adds a node to the graph without any dependencies.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    public DependencyGraphBuilder<TNode> AddNode(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        _dependencies.TryAdd(node, new HashSet<TNode>());
        return this;
    }
    
    /// <summary>
    /// Adds multiple nodes to the graph without any dependencies.
    /// </summary>
    /// <param name="nodes">The nodes to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> is null.</exception>
    public DependencyGraphBuilder<TNode> AddNodes(IEnumerable<TNode> nodes)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        
        foreach (var node in nodes)
        {
            if (node != null)
            {
                AddNode(node);
            }
        }
        return this;
    }
    
    /// <summary>
    /// Specifies that a node depends on another node.
    /// </summary>
    /// <param name="dependent">The node that has the dependency.</param>
    /// <param name="dependency">The node that is depended upon.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dependent"/> or <paramref name="dependency"/> is null.</exception>
    /// <remarks>
    /// This creates a directed edge from the dependency to the dependent in the resulting graph.
    /// In dependency terms: dependent depends on dependency.
    /// In graph terms: edge from dependency → dependent.
    /// </remarks>
    public DependencyGraphBuilder<TNode> DependsOn(TNode dependent, TNode dependency)
    {
        if (dependent == null)
            throw new ArgumentNullException(nameof(dependent));
        if (dependency == null)
            throw new ArgumentNullException(nameof(dependency));
        
        // Ensure both nodes exist
        AddNode(dependent);
        AddNode(dependency);
        
        // Add the dependency relationship
        _dependencies[dependent].Add(dependency);
        
        return this;
    }
    
    /// <summary>
    /// Specifies that a node depends on multiple other nodes.
    /// </summary>
    /// <param name="dependent">The node that has the dependencies.</param>
    /// <param name="dependencies">The nodes that are depended upon.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dependent"/> or <paramref name="dependencies"/> is null.</exception>
    public DependencyGraphBuilder<TNode> DependsOn(TNode dependent, IEnumerable<TNode> dependencies)
    {
        if (dependent == null)
            throw new ArgumentNullException(nameof(dependent));
        if (dependencies == null)
            throw new ArgumentNullException(nameof(dependencies));
        
        foreach (var dependency in dependencies)
        {
            if (dependency != null)
            {
                DependsOn(dependent, dependency);
            }
        }
        return this;
    }
    
    /// <summary>
    /// Specifies that a node depends on multiple other nodes using params syntax.
    /// </summary>
    /// <param name="dependent">The node that has the dependencies.</param>
    /// <param name="dependencies">The nodes that are depended upon.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dependent"/> is null.</exception>
    public DependencyGraphBuilder<TNode> DependsOn(TNode dependent, params TNode[] dependencies)
    {
        return DependsOn(dependent, (IEnumerable<TNode>)dependencies);
    }
    
    /// <summary>
    /// Adds a dependency relationship using a more explicit method name.
    /// </summary>
    /// <param name="dependency">The node that is depended upon.</param>
    /// <param name="dependent">The node that has the dependency.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dependency"/> or <paramref name="dependent"/> is null.</exception>
    /// <remarks>
    /// This is equivalent to calling DependsOn(dependent, dependency) but with reversed parameter order
    /// for cases where this reading feels more natural.
    /// </remarks>
    public DependencyGraphBuilder<TNode> AddDependency(TNode dependency, TNode dependent)
    {
        return DependsOn(dependent, dependency);
    }
    
    /// <summary>
    /// Removes a dependency relationship between two nodes.
    /// </summary>
    /// <param name="dependent">The node that currently has the dependency.</param>
    /// <param name="dependency">The node that is currently depended upon.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dependent"/> or <paramref name="dependency"/> is null.</exception>
    public DependencyGraphBuilder<TNode> RemoveDependency(TNode dependent, TNode dependency)
    {
        if (dependent == null)
            throw new ArgumentNullException(nameof(dependent));
        if (dependency == null)
            throw new ArgumentNullException(nameof(dependency));
        
        if (_dependencies.TryGetValue(dependent, out var dependencySet))
        {
            dependencySet.Remove(dependency);
        }
        
        return this;
    }
    
    /// <summary>
    /// Builds the dependency graph from the specified relationships.
    /// </summary>
    /// <returns>An immutable directed graph representing the dependency relationships.</returns>
    /// <remarks>
    /// The resulting graph will have edges from dependencies to dependents.
    /// For example, if A depends on B, there will be an edge B → A in the graph.
    /// </remarks>
    public IDirectedGraph<TNode> Build()
    {
        // Convert dependency relationships to edge-based dictionary for AdjacencyListGraph
        var edgeBasedDependencies = new Dictionary<TNode, IEnumerable<TNode>>();
        
        foreach (var kvp in _dependencies)
        {
            edgeBasedDependencies[kvp.Key] = kvp.Value.ToList();
        }
        
        return new AdjacencyListGraph<TNode>(edgeBasedDependencies);
    }
    
    /// <summary>
    /// Gets the current number of nodes that will be in the built graph.
    /// </summary>
    public int NodeCount => _dependencies.Count;
    
    /// <summary>
    /// Gets the current number of dependency relationships that will be in the built graph.
    /// </summary>
    public int DependencyCount => _dependencies.Values.Sum(deps => deps.Count);
    
    /// <summary>
    /// Gets the dependencies for a specific node.
    /// </summary>
    /// <param name="node">The node to get dependencies for.</param>
    /// <returns>The dependencies of the specified node, or an empty set if the node doesn't exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    public IReadOnlySet<TNode> GetDependencies(TNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        return _dependencies.TryGetValue(node, out var deps) 
            ? deps.ToHashSet() 
            : new HashSet<TNode>();
    }
    
    /// <summary>
    /// Checks if a dependency relationship exists.
    /// </summary>
    /// <param name="dependent">The node that might have the dependency.</param>
    /// <param name="dependency">The node that might be depended upon.</param>
    /// <returns><c>true</c> if the dependency relationship exists; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dependent"/> or <paramref name="dependency"/> is null.</exception>
    public bool HasDependency(TNode dependent, TNode dependency)
    {
        if (dependent == null)
            throw new ArgumentNullException(nameof(dependent));
        if (dependency == null)
            throw new ArgumentNullException(nameof(dependency));
        
        return _dependencies.TryGetValue(dependent, out var deps) && deps.Contains(dependency);
    }
    
    /// <summary>
    /// Clears all nodes and dependencies from the builder.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public DependencyGraphBuilder<TNode> Clear()
    {
        _dependencies.Clear();
        return this;
    }
}
