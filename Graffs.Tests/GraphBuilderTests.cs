using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Graffs;
using Graffs.Builders;
using Graffs.Implementations;

namespace Graffs.Tests;

public class GraphBuilderTests
{
    [Fact]
    public void GraphBuilder_AddNode_AddsNodeSuccessfully()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        
        // Act
        var result = builder.AddNode("A");
        var graph = builder.Build();
        
        // Assert
        Assert.Same(builder, result); // Fluent interface
        Assert.Contains("A", graph.Nodes);
        Assert.Single(graph.Nodes);
    }
    
    [Fact]
    public void GraphBuilder_AddNode_WithNullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddNode(null!));
    }
    
    [Fact]
    public void GraphBuilder_AddNodes_AddsMultipleNodesSuccessfully()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        var nodes = new[] { "A", "B", "C" };
        
        // Act
        var result = builder.AddNodes(nodes);
        var graph = builder.Build();
        
        // Assert
        Assert.Same(builder, result); // Fluent interface
        Assert.Equal(3, graph.Count);
        Assert.Contains("A", graph.Nodes);
        Assert.Contains("B", graph.Nodes);
        Assert.Contains("C", graph.Nodes);
    }
    
    [Fact]
    public void GraphBuilder_AddNodes_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddNodes(null!));
    }
    
    [Fact]
    public void GraphBuilder_AddNodes_WithNullItemsInCollection_IgnoresNullItems()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        var nodes = new[] { "A", null, "B", null, "C" };
        
        // Act
        builder.AddNodes(nodes!);
        var graph = builder.Build();
        
        // Assert
        Assert.Equal(3, graph.Count);
        Assert.Contains("A", graph.Nodes);
        Assert.Contains("B", graph.Nodes);
        Assert.Contains("C", graph.Nodes);
    }
    
    [Fact]
    public void GraphBuilder_AddEdge_WithSourceAndTarget_AddsEdgeSuccessfully()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        
        // Act
        var result = builder.AddEdge("A", "B");
        var graph = builder.Build();
        
        // Assert
        Assert.Same(builder, result); // Fluent interface
        Assert.Equal(2, graph.Count);
        Assert.Contains("A", graph.Nodes);
        Assert.Contains("B", graph.Nodes);
        Assert.True(graph.HasEdge("A", "B"));
        Assert.Single(graph.Edges);
    }
    
    [Fact]
    public void GraphBuilder_AddEdge_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddEdge(null!, "B"));
    }
    
    [Fact]
    public void GraphBuilder_AddEdge_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddEdge("A", null!));
    }
    
    [Fact]
    public void GraphBuilder_AddEdge_WithGraphEdge_AddsEdgeSuccessfully()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        var edge = new GraphEdge<string>("A", "B");
        
        // Act
        var result = builder.AddEdge(edge);
        var graph = builder.Build();
        
        // Assert
        Assert.Same(builder, result); // Fluent interface
        Assert.Equal(2, graph.Count);
        Assert.True(graph.HasEdge("A", "B"));
        Assert.Single(graph.Edges);
    }
    
    [Fact]
    public void GraphBuilder_AddEdge_WithNullGraphEdge_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddEdge(null!));
    }
    
    [Fact]
    public void GraphBuilder_FluentChaining_WorksCorrectly()
    {
        // Arrange & Act
        var graph = new GraphBuilder<string>()
            .AddNode("A")
            .AddNode("B")
            .AddNodes(new[] { "C", "D" })
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge(new GraphEdge<string>("C", "D"))
            .Build();
        
        // Assert
        Assert.Equal(4, graph.Count);
        Assert.True(graph.HasEdge("A", "B"));
        Assert.True(graph.HasEdge("B", "C"));
        Assert.True(graph.HasEdge("C", "D"));
        Assert.Equal(3, graph.Edges.Count);
    }
    
    [Fact]
    public void GraphBuilder_NodeDeduplication_WorksCorrectly()
    {
        // Arrange & Act
        var graph = new GraphBuilder<string>()
            .AddNode("A")
            .AddNode("A") // Duplicate
            .AddNodes(new[] { "A", "B", "A" }) // More duplicates
            .Build();
        
        // Assert
        Assert.Equal(2, graph.Count);
        Assert.Contains("A", graph.Nodes);
        Assert.Contains("B", graph.Nodes);
    }
    
    [Fact]
    public void GraphBuilder_EdgeDeduplication_WorksCorrectly()
    {
        // Arrange & Act
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("A", "B") // Duplicate
            .AddEdge(new GraphEdge<string>("A", "B")) // Another duplicate
            .Build();
        
        // Assert
        Assert.Equal(2, graph.Count);
        Assert.Single(graph.Edges);
        Assert.True(graph.HasEdge("A", "B"));
    }
    
    [Fact]
    public void DependencyGraphBuilder_AddNode_AddsNodeSuccessfully()
    {
        // Arrange
        var builder = new DependencyGraphBuilder<string>();
        
        // Act
        var result = builder.AddNode("A");
        var graph = builder.Build();
        
        // Assert
        Assert.Same(builder, result); // Fluent interface
        Assert.Contains("A", graph.Nodes);
        Assert.Single(graph.Nodes);
        Assert.Empty(graph.Edges);
    }
    
    [Fact]
    public void DependencyGraphBuilder_AddNode_WithNullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new DependencyGraphBuilder<string>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddNode(null!));
    }
    
    [Fact]
    public void DependencyGraphBuilder_DependsOn_SingleDependency_CreatesCorrectGraph()
    {
        // Arrange
        var builder = new DependencyGraphBuilder<string>();
        
        // Act
        var result = builder.DependsOn("A", "B");
        var graph = builder.Build();
        
        // Assert
        Assert.Same(builder, result); // Fluent interface
        Assert.Equal(2, graph.Count);
        Assert.Contains("A", graph.Nodes);
        Assert.Contains("B", graph.Nodes);
        
        // In dependency terms: A depends on B
        // In graph terms: B → A (B must come before A)
        Assert.True(graph.HasEdge("B", "A"));
        Assert.Contains("B", graph.GetDependencies("A"));
        Assert.Contains("A", graph.GetDependents("B"));
    }
    
    [Fact]
    public void DependencyGraphBuilder_DependsOn_MultipleDependencies_CreatesCorrectGraph()
    {
        // Arrange
        var builder = new DependencyGraphBuilder<string>();
        var dependencies = new[] { "B", "C", "D" };
        
        // Act
        var result = builder.DependsOn("A", dependencies);
        var graph = builder.Build();
        
        // Assert
        Assert.Same(builder, result); // Fluent interface
        Assert.Equal(4, graph.Count);
        
        // A depends on B, C, D
        Assert.True(graph.HasEdge("B", "A"));
        Assert.True(graph.HasEdge("C", "A"));
        Assert.True(graph.HasEdge("D", "A"));
        
        var aDependencies = graph.GetDependencies("A");
        Assert.Contains("B", aDependencies);
        Assert.Contains("C", aDependencies);
        Assert.Contains("D", aDependencies);
        Assert.Equal(3, aDependencies.Count);
    }
    
    [Fact]
    public void DependencyGraphBuilder_DependsOn_WithNullDependent_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new DependencyGraphBuilder<string>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.DependsOn(null!, "B"));
        Assert.Throws<ArgumentNullException>(() => builder.DependsOn(null!, new[] { "B", "C" }));
    }
    
    [Fact]
    public void DependencyGraphBuilder_DependsOn_WithNullDependency_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new DependencyGraphBuilder<string>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.DependsOn("A", (string)null!));
    }
    
    [Fact]
    public void DependencyGraphBuilder_DependsOn_WithNullDependencies_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new DependencyGraphBuilder<string>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.DependsOn("A", (IEnumerable<string>)null!));
    }
    
    [Fact]
    public void DependencyGraphBuilder_ComplexDependencyScenario_CreatesCorrectGraph()
    {
        // Arrange & Act
        var graph = new DependencyGraphBuilder<string>()
            .AddNode("Root") // No dependencies
            .DependsOn("A", "Root")
            .DependsOn("B", "Root")
            .DependsOn("C", new[] { "A", "B" })
            .DependsOn("D", "C")
            .Build();
        
        // Assert
        Assert.Equal(5, graph.Count);
        
        // Root has no dependencies, but A and B depend on it
        Assert.Empty(graph.GetDependencies("Root"));
        Assert.Contains("A", graph.GetDependents("Root"));
        Assert.Contains("B", graph.GetDependents("Root"));
        
        // C depends on both A and B
        var cDependencies = graph.GetDependencies("C");
        Assert.Contains("A", cDependencies);
        Assert.Contains("B", cDependencies);
        Assert.Equal(2, cDependencies.Count);
        
        // D depends on C
        Assert.Contains("C", graph.GetDependencies("D"));
        Assert.Contains("D", graph.GetDependents("C"));
        
        // Verify path relationships
        Assert.True(graph.HasPath("Root", "D")); // Root → A/B → C → D
        Assert.False(graph.HasPath("D", "Root")); // No reverse path
    }
    
    [Fact]
    public void BothBuilders_ProduceEquivalentResults_ForSameGraph()
    {
        // Arrange - Build same graph with both builders
        var graph1 = new GraphBuilder<string>()
            .AddEdge("B", "A") // B → A (A depends on B)
            .AddEdge("C", "A") // C → A (A depends on C)
            .AddEdge("D", "B") // D → B (B depends on D)
            .Build();
        
        var graph2 = new DependencyGraphBuilder<string>()
            .DependsOn("A", new[] { "B", "C" })
            .DependsOn("B", "D")
            .Build();
        
        // Assert - Both graphs should have same structure
        Assert.Equal(graph1.Count, graph2.Count);
        Assert.Equal(graph1.Edges.Count, graph2.Edges.Count);
        
        // Same nodes
        foreach (var node in graph1.Nodes)
        {
            Assert.Contains(node, graph2.Nodes);
        }
        
        // Same edges
        foreach (var edge in graph1.Edges)
        {
            Assert.True(graph2.HasEdge(edge.Source, edge.Target));
        }
        
        // Same dependency relationships
        foreach (var node in graph1.Nodes)
        {
            var deps1 = graph1.GetDependencies(node);
            var deps2 = graph2.GetDependencies(node);
            Assert.Equal(deps1.Count, deps2.Count);
            foreach (var dep in deps1)
            {
                Assert.Contains(dep, deps2);
            }
        }
    }
    
    [Fact]
    public void GraphBuilder_EmptyBuild_CreatesEmptyGraph()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        
        // Act
        var graph = builder.Build();
        
        // Assert
        Assert.Empty(graph.Nodes);
        Assert.Empty(graph.Edges);
    }
    
    [Fact]
    public void DependencyGraphBuilder_EmptyBuild_CreatesEmptyGraph()
    {
        // Arrange
        var builder = new DependencyGraphBuilder<string>();
        
        // Act
        var graph = builder.Build();
        
        // Assert
        Assert.Empty(graph.Nodes);
        Assert.Empty(graph.Edges);
    }
}
