using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Graffs;
using Graffs.Implementations;
using Graffs.Algorithms;
using Graffs.Builders;

namespace Graffs.Tests;

public class TopologicalSortTests
{
    [Fact]
    public void KahnSort_WithEmptyGraph_ReturnsEmptyResult()
    {
        // Arrange
        var graph = new GraphBuilder<string>().Build();
        
        // Act
        var result = TopologicalSort.KahnSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Empty(result.SortedNodes);
        Assert.Empty(result.CyclicNodes);
        Assert.Equal(0, result.NodeCount);
        Assert.True(result.ProcessingTimeMs >= 0);
    }
    
    [Fact]
    public void DfsSort_WithEmptyGraph_ReturnsEmptyResult()
    {
        // Arrange
        var graph = new GraphBuilder<string>().Build();
        
        // Act
        var result = TopologicalSort.DfsSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Empty(result.SortedNodes);
        Assert.Empty(result.CyclicNodes);
        Assert.Equal(0, result.NodeCount);
        Assert.True(result.ProcessingTimeMs >= 0);
    }
    
    [Fact]
    public void KahnSort_WithSingleNode_ReturnsSingleNodeResult()
    {
        // Arrange
        var graph = new GraphBuilder<string>()
            .AddNode("A")
            .Build();
        
        // Act
        var result = TopologicalSort.KahnSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Single(result.SortedNodes);
        Assert.Contains("A", result.SortedNodes);
        Assert.Empty(result.CyclicNodes);
        Assert.Equal(1, result.NodeCount);
    }
    
    [Fact]
    public void DfsSort_WithSingleNode_ReturnsSingleNodeResult()
    {
        // Arrange
        var graph = new GraphBuilder<string>()
            .AddNode("A")
            .Build();
        
        // Act
        var result = TopologicalSort.DfsSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Single(result.SortedNodes);
        Assert.Contains("A", result.SortedNodes);
        Assert.Empty(result.CyclicNodes);
        Assert.Equal(1, result.NodeCount);
    }
    
    [Fact]
    public void KahnSort_WithSimpleDAG_ReturnsValidOrder()
    {
        // Arrange
        // A → B → C (A depends on B, B depends on C)
        var graph = new DependencyGraphBuilder<string>()
            .DependsOn("A", "B")
            .DependsOn("B", "C")
            .AddNode("C") // No dependencies
            .Build();
        
        // Act
        var result = TopologicalSort.KahnSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Equal(3, result.NodeCount);
        Assert.Empty(result.CyclicNodes);
        
        // Verify topological order: C should come before B, B should come before A
        var sortedList = result.SortedNodes.ToList();
        Assert.True(sortedList.IndexOf("C") < sortedList.IndexOf("B"));
        Assert.True(sortedList.IndexOf("B") < sortedList.IndexOf("A"));
    }
    
    [Fact]
    public void DfsSort_WithSimpleDAG_ReturnsValidOrder()
    {
        // Arrange
        // A → B → C (A depends on B, B depends on C)
        var graph = new DependencyGraphBuilder<string>()
            .DependsOn("A", "B")
            .DependsOn("B", "C")
            .AddNode("C") // No dependencies
            .Build();
        
        // Act
        var result = TopologicalSort.DfsSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Equal(3, result.NodeCount);
        Assert.Empty(result.CyclicNodes);
        
        // Verify topological order: C should come before B, B should come before A
        var sortedList = result.SortedNodes.ToList();
        Assert.True(sortedList.IndexOf("C") < sortedList.IndexOf("B"));
        Assert.True(sortedList.IndexOf("B") < sortedList.IndexOf("A"));
    }
    
    [Fact]
    public void KahnSort_WithComplexDAG_ReturnsValidOrder()
    {
        // Arrange
        // Complex dependency graph:
        //     D
        //    ↙ ↘
        //   A   B
        //    ↘ ↙
        //     C
        var graph = new DependencyGraphBuilder<string>()
            .DependsOn("A", "D")
            .DependsOn("B", "D")
            .DependsOn("C", new[] { "A", "B" })
            .AddNode("D") // No dependencies
            .Build();
        
        // Act
        var result = TopologicalSort.KahnSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Equal(4, result.NodeCount);
        Assert.Empty(result.CyclicNodes);
        
        // Verify ordering constraints
        var sortedList = result.SortedNodes.ToList();
        Assert.True(sortedList.IndexOf("D") < sortedList.IndexOf("A"));
        Assert.True(sortedList.IndexOf("D") < sortedList.IndexOf("B"));
        Assert.True(sortedList.IndexOf("A") < sortedList.IndexOf("C"));
        Assert.True(sortedList.IndexOf("B") < sortedList.IndexOf("C"));
    }
    
    [Fact]
    public void DfsSort_WithComplexDAG_ReturnsValidOrder()
    {
        // Arrange
        // Complex dependency graph:
        //     D
        //    ↙ ↘
        //   A   B
        //    ↘ ↙
        //     C
        var graph = new DependencyGraphBuilder<string>()
            .DependsOn("A", "D")
            .DependsOn("B", "D")
            .DependsOn("C", new[] { "A", "B" })
            .AddNode("D") // No dependencies
            .Build();
        
        // Act
        var result = TopologicalSort.DfsSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Equal(4, result.NodeCount);
        Assert.Empty(result.CyclicNodes);
        
        // Verify ordering constraints
        var sortedList = result.SortedNodes.ToList();
        Assert.True(sortedList.IndexOf("D") < sortedList.IndexOf("A"));
        Assert.True(sortedList.IndexOf("D") < sortedList.IndexOf("B"));
        Assert.True(sortedList.IndexOf("A") < sortedList.IndexOf("C"));
        Assert.True(sortedList.IndexOf("B") < sortedList.IndexOf("C"));
    }
    
    [Fact]
    public void KahnSort_WithSimpleCycle_DetectsCycle()
    {
        // Arrange
        // A → B → A (cycle)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "A")
            .Build();
        
        // Act
        var result = TopologicalSort.KahnSort(graph);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCycles);
        Assert.Equal(2, result.CyclicNodes.Length);
        Assert.Contains("A", result.CyclicNodes);
        Assert.Contains("B", result.CyclicNodes);
        Assert.Empty(result.SortedNodes); // No nodes can be sorted due to cycle
    }
    
    [Fact]
    public void DfsSort_WithSimpleCycle_DetectsCycle()
    {
        // Arrange
        // A → B → A (cycle)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "A")
            .Build();
        
        // Act
        var result = TopologicalSort.DfsSort(graph);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCycles);
        Assert.NotEmpty(result.CyclicNodes);
        // DFS may detect cycles differently than Kahn's, but should detect them
    }
    
    [Fact]
    public void KahnSort_WithSelfLoop_DetectsCycle()
    {
        // Arrange
        // A → A (self-loop)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "A")
            .Build();
        
        // Act
        var result = TopologicalSort.KahnSort(graph);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCycles);
        Assert.Contains("A", result.CyclicNodes);
    }
    
    [Fact]
    public void DfsSort_WithSelfLoop_DetectsCycle()
    {
        // Arrange
        // A → A (self-loop)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "A")
            .Build();
        
        // Act
        var result = TopologicalSort.DfsSort(graph);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCycles);
        Assert.Contains("A", result.CyclicNodes);
    }
    
    [Fact]
    public void KahnSort_WithPartialCycle_SortsNonCyclicNodes()
    {
        // Arrange
        // D → A → B → C → A (D is not in cycle, A,B,C form cycle)
        var graph = new GraphBuilder<string>()
            .AddEdge("D", "A")
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("C", "A")
            .Build();
        
        // Act
        var result = TopologicalSort.KahnSort(graph);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCycles);
        Assert.Contains("D", result.SortedNodes); // D can be sorted
        Assert.Contains("A", result.CyclicNodes);
        Assert.Contains("B", result.CyclicNodes);
        Assert.Contains("C", result.CyclicNodes);
    }
    
    [Fact]
    public void KahnSort_WithDisconnectedComponents_SortsAllComponents()
    {
        // Arrange
        // Two disconnected components: A → B and C → D
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("C", "D")
            .Build();
        
        // Act
        var result = TopologicalSort.KahnSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Equal(4, result.NodeCount);
        Assert.Empty(result.CyclicNodes);
        
        // Verify ordering within components
        var sortedList = result.SortedNodes.ToList();
        Assert.True(sortedList.IndexOf("A") < sortedList.IndexOf("B"));
        Assert.True(sortedList.IndexOf("C") < sortedList.IndexOf("D"));
    }
    
    [Fact]
    public void DfsSort_WithDisconnectedComponents_SortsAllComponents()
    {
        // Arrange
        // Two disconnected components: A → B and C → D
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("C", "D")
            .Build();
        
        // Act
        var result = TopologicalSort.DfsSort(graph);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCycles);
        Assert.Equal(4, result.NodeCount);
        Assert.Empty(result.CyclicNodes);
        
        // Verify ordering within components
        var sortedList = result.SortedNodes.ToList();
        Assert.True(sortedList.IndexOf("A") < sortedList.IndexOf("B"));
        Assert.True(sortedList.IndexOf("C") < sortedList.IndexOf("D"));
    }
    
    [Fact]
    public void TopologicalSort_WithNullGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TopologicalSort.KahnSort<string>(null!));
        Assert.Throws<ArgumentNullException>(() => TopologicalSort.DfsSort<string>(null!));
    }
    
    [Fact]
    public void BothAlgorithms_WithSameDAG_ProduceSameValidOrdering()
    {
        // Arrange
        var graph = new DependencyGraphBuilder<string>()
            .DependsOn("A", new[] { "B", "C" })
            .DependsOn("B", "D")
            .DependsOn("C", "D")
            .AddNode("D")
            .Build();
        
        // Act
        var kahnResult = TopologicalSort.KahnSort(graph);
        var dfsResult = TopologicalSort.DfsSort(graph);
        
        // Assert
        Assert.True(kahnResult.IsValid);
        Assert.True(dfsResult.IsValid);
        Assert.Equal(kahnResult.NodeCount, dfsResult.NodeCount);
        
        // Both should respect the same ordering constraints
        var kahnList = kahnResult.SortedNodes.ToList();
        var dfsList = dfsResult.SortedNodes.ToList();
        
        // D should come before B and C in both
        Assert.True(kahnList.IndexOf("D") < kahnList.IndexOf("B"));
        Assert.True(kahnList.IndexOf("D") < kahnList.IndexOf("C"));
        Assert.True(dfsList.IndexOf("D") < dfsList.IndexOf("B"));
        Assert.True(dfsList.IndexOf("D") < dfsList.IndexOf("C"));
        
        // B and C should come before A in both
        Assert.True(kahnList.IndexOf("B") < kahnList.IndexOf("A"));
        Assert.True(kahnList.IndexOf("C") < kahnList.IndexOf("A"));
        Assert.True(dfsList.IndexOf("B") < dfsList.IndexOf("A"));
        Assert.True(dfsList.IndexOf("C") < dfsList.IndexOf("A"));
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void TopologicalSort_WithDifferentGraphSizes_CompletesInReasonableTime(int nodeCount)
    {
        // Arrange - Create a linear dependency chain
        var builder = new DependencyGraphBuilder<int>();
        for (int i = 1; i < nodeCount; i++)
        {
            builder.DependsOn(i, i - 1);
        }
        if (nodeCount > 0)
        {
            builder.AddNode(0); // Root node
        }
        var graph = builder.Build();
        
        // Act
        var kahnResult = TopologicalSort.KahnSort(graph);
        var dfsResult = TopologicalSort.DfsSort(graph);
        
        // Assert
        Assert.True(kahnResult.IsValid);
        Assert.True(dfsResult.IsValid);
        Assert.Equal(nodeCount, kahnResult.NodeCount);
        Assert.Equal(nodeCount, dfsResult.NodeCount);
        
        // Performance should be reasonable (< 100ms for these sizes)
        Assert.True(kahnResult.ProcessingTimeMs < 100);
        Assert.True(dfsResult.ProcessingTimeMs < 100);
    }
}
