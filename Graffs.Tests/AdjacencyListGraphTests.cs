using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Graffs;
using Graffs.Implementations;

namespace Graffs.Tests;

public class AdjacencyListGraphTests
{
    [Fact]
    public void Constructor_WithEmptyEdges_CreatesEmptyGraph()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>>();
        
        // Act
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Assert
        Assert.Empty(graph); // Count property represents number of nodes
        Assert.Empty(graph.Nodes);
        Assert.Empty(graph.Edges);
    }
    
    [Fact]
    public void Constructor_WithSingleEdge_CreatesGraphWithTwoNodes()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B") 
        };
        
        // Act
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Assert
        Assert.Equal(2, graph.Count);
        Assert.Contains("A", graph.Nodes);
        Assert.Contains("B", graph.Nodes);
        Assert.Single(graph.Edges);
    }
    
    [Fact]
    public void Constructor_WithNullEdges_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<IGraphEdge<string>>? edges = null;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AdjacencyListGraph<string>(edges!));
    }
    
    [Fact]
    public void Constructor_WithDependencies_CreatesGraphCorrectly()
    {
        // Arrange
        var dependencies = new Dictionary<string, IEnumerable<string>>
        {
            ["A"] = new[] { "B", "C" },
            ["B"] = new[] { "C" },
            ["C"] = Array.Empty<string>()
        };
        
        // Act
        var graph = new AdjacencyListGraph<string>(dependencies);
        
        // Assert
        Assert.Equal(3, graph.Count);
        Assert.Contains("A", graph.Nodes);
        Assert.Contains("B", graph.Nodes);
        Assert.Contains("C", graph.Nodes);
        
        // A depends on B and C, so B and C should have A as dependent
        Assert.Contains("A", graph.GetDependents("B"));
        Assert.Contains("A", graph.GetDependents("C"));
        Assert.Contains("B", graph.GetDependents("C"));
        
        // A depends on B and C
        Assert.Contains("B", graph.GetDependencies("A"));
        Assert.Contains("C", graph.GetDependencies("A"));
        Assert.Contains("C", graph.GetDependencies("B"));
    }
    
    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        // Arrange
        IDictionary<string, IEnumerable<string>>? dependencies = null;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AdjacencyListGraph<string>(dependencies!));
    }
    
    [Fact]
    public void GetDependencies_WithValidNode_ReturnsDependencies()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("B", "A"),  // B → A (A depends on B)
            new GraphEdge<string>("C", "A")   // C → A (A depends on C)
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act
        var dependencies = graph.GetDependencies("A");
        
        // Assert
        Assert.Equal(2, dependencies.Count);
        Assert.Contains("B", dependencies);
        Assert.Contains("C", dependencies);
    }
    
    [Fact]
    public void GetDependencies_WithNullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = new AdjacencyListGraph<string>(new List<IGraphEdge<string>>());
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => graph.GetDependencies(null!));
    }
    
    [Fact]
    public void GetDependents_WithValidNode_ReturnsDependents()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B"),  // A → B (B depends on A)
            new GraphEdge<string>("A", "C")   // A → C (C depends on A)
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act
        var dependents = graph.GetDependents("A");
        
        // Assert
        Assert.Equal(2, dependents.Count);
        Assert.Contains("B", dependents);
        Assert.Contains("C", dependents);
    }
    
    [Fact]
    public void HasEdge_WithExistingEdge_ReturnsTrue()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act & Assert
        Assert.True(graph.HasEdge("A", "B"));
        Assert.False(graph.HasEdge("B", "A"));
    }
    
    [Fact]
    public void HasEdge_WithNonExistentEdge_ReturnsFalse()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act & Assert
        Assert.False(graph.HasEdge("B", "C"));
        Assert.False(graph.HasEdge("C", "A"));
    }
    
    [Fact]
    public void HasPath_WithDirectEdge_ReturnsTrue()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act & Assert
        Assert.True(graph.HasPath("A", "B"));
    }
    
    [Fact]
    public void HasPath_WithIndirectPath_ReturnsTrue()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B"),
            new GraphEdge<string>("B", "C")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act & Assert
        Assert.True(graph.HasPath("A", "C"));
        Assert.False(graph.HasPath("C", "A"));
    }
    
    [Fact]
    public void HasPath_WithSameNode_ReturnsTrue()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act & Assert
        Assert.True(graph.HasPath("A", "A"));
        Assert.True(graph.HasPath("B", "B"));
    }
    
    [Fact]
    public void HasPath_WithNoPath_ReturnsFalse()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B"),
            new GraphEdge<string>("C", "D")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act & Assert
        Assert.False(graph.HasPath("A", "D"));
        Assert.False(graph.HasPath("B", "C"));
    }
    
    [Fact]
    public void GetOutgoingEdges_WithValidNode_ReturnsCorrectEdges()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B"),
            new GraphEdge<string>("A", "C"),
            new GraphEdge<string>("B", "C")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act
        var outgoingEdges = graph.GetOutgoingEdges("A").ToList();
        
        // Assert
        Assert.Equal(2, outgoingEdges.Count);
        Assert.Contains(outgoingEdges, e => e.Source.Equals("A") && e.Target.Equals("B"));
        Assert.Contains(outgoingEdges, e => e.Source.Equals("A") && e.Target.Equals("C"));
    }
    
    [Fact]
    public void GetIncomingEdges_WithValidNode_ReturnsCorrectEdges()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "C"),
            new GraphEdge<string>("B", "C"),
            new GraphEdge<string>("B", "D")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act
        var incomingEdges = graph.GetIncomingEdges("C").ToList();
        
        // Assert
        Assert.Equal(2, incomingEdges.Count);
        Assert.Contains(incomingEdges, e => e.Source.Equals("A") && e.Target.Equals("C"));
        Assert.Contains(incomingEdges, e => e.Source.Equals("B") && e.Target.Equals("C"));
    }
    
    [Fact]
    public void GetOutgoingEdges_WithNodeWithNoOutgoingEdges_ReturnsEmptyCollection()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act
        var outgoingEdges = graph.GetOutgoingEdges("B");
        
        // Assert
        Assert.Empty(outgoingEdges);
    }
    
    [Fact]
    public void Enumeration_IteratesOverAllNodes()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B"),
            new GraphEdge<string>("B", "C")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act
        var nodeList = graph.ToList();
        
        // Assert
        Assert.Equal(3, nodeList.Count);
        Assert.Contains("A", nodeList);
        Assert.Contains("B", nodeList);
        Assert.Contains("C", nodeList);
    }
    
    [Fact]
    public void MultipleEdgesBetweenSameNodes_AreDeduplicatedInGraph()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B"),
            new GraphEdge<string>("A", "B"),  // Duplicate
            new GraphEdge<string>("A", "B")   // Another duplicate
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act
        var dependents = graph.GetDependents("A");
        
        // Assert
        Assert.Single(dependents);
        Assert.Contains("B", dependents);
        Assert.True(graph.HasEdge("A", "B"));
    }
    
    [Fact]
    public void DisconnectedComponents_AreHandledCorrectly()
    {
        // Arrange
        var edges = new List<IGraphEdge<string>> 
        { 
            new GraphEdge<string>("A", "B"),
            new GraphEdge<string>("C", "D")
        };
        var graph = new AdjacencyListGraph<string>(edges);
        
        // Act & Assert
        Assert.Equal(4, graph.Count);
        Assert.True(graph.HasPath("A", "B"));
        Assert.True(graph.HasPath("C", "D"));
        Assert.False(graph.HasPath("A", "C"));
        Assert.False(graph.HasPath("B", "D"));
    }
}
