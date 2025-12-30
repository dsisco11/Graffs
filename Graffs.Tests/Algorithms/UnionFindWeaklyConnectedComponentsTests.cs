using Graffs.Algorithms;
using Graffs.Builders;

namespace Graffs.Tests.Algorithms;

/// <summary>
/// Comprehensive test suite for UnionFindWeaklyConnectedComponents algorithm.
/// Tests the Union-Find implementation for detecting weakly connected components in directed graphs
/// through the public UnionFindWeaklyConnectedComponents.Execute API.
/// </summary>
public sealed class UnionFindWeaklyConnectedComponentsTests
{
    #region Basic Graph Structure Tests

    [Fact]
    public void FindWeaklyConnectedComponents_EmptyGraph_ReturnsEmptyResult()
    {
        // Arrange
        var graph = new GraphBuilder<string>().Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Empty(result.Components);
        Assert.Equal(0, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(0, result.Statistics.NonTrivialComponents);
        Assert.Equal(0, result.Statistics.LargestComponentSize);
        Assert.Equal("Union-Find Weakly Connected Components", result.Algorithm);
        Assert.True(result.ProcessingTime.TotalMilliseconds >= 0);
    }

    [Fact]
    public void FindWeaklyConnectedComponents_SingleNode_ReturnsSingleTrivialComponent()
    {
        // Arrange
        var graph = new GraphBuilder<string>()
            .AddNode("A")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components.First();
        Assert.True(component.IsTrivial);
        Assert.Single(component.Nodes);
        Assert.Equal("A", component.Nodes.Single());
        Assert.Equal(1, result.Statistics.TotalComponents);
        Assert.Equal(1, result.Statistics.TrivialComponents);
        Assert.Equal(0, result.Statistics.NonTrivialComponents);
        Assert.Equal(1, result.Statistics.LargestComponentSize);
    }

    [Fact]
    public void FindWeaklyConnectedComponents_TwoIsolatedNodes_ReturnsTwoTrivialComponents()
    {
        // Arrange
        var graph = new GraphBuilder<string>()
            .AddNode("A")
            .AddNode("B")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Equal(2, result.Components.Length);
        Assert.All(result.Components, c => Assert.True(c.IsTrivial));
        Assert.All(result.Components, c => Assert.Single(c.Nodes));
        Assert.Equal(2, result.Statistics.TotalComponents);
        Assert.Equal(2, result.Statistics.TrivialComponents);
        Assert.Equal(0, result.Statistics.NonTrivialComponents);
        Assert.Equal(1, result.Statistics.LargestComponentSize);
        
        var nodeNames = result.Components.SelectMany(c => c.Nodes).ToHashSet();
        Assert.Contains("A", nodeNames);
        Assert.Contains("B", nodeNames);
    }

    #endregion

    #region Directed Graph Tests (Treated as Undirected)

    [Fact]
    public void FindWeaklyConnectedComponents_SimpleDirectedEdge_ReturnsSingleNonTrivialComponent()
    {
        // Arrange: A → B (treated as A ↔ B for weak connectivity)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components.First();
        Assert.False(component.IsTrivial);
        Assert.Equal(2, component.Size);
        Assert.Contains("A", component.Nodes);
        Assert.Contains("B", component.Nodes);
        Assert.Equal(1, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(1, result.Statistics.NonTrivialComponents);
        Assert.Equal(2, result.Statistics.LargestComponentSize);
    }

    [Fact]
    public void FindWeaklyConnectedComponents_DirectedChain_ReturnsSingleNonTrivialComponent()
    {
        // Arrange: A → B → C → D (all weakly connected)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("C", "D")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components.First();
        Assert.False(component.IsTrivial);
        Assert.Equal(4, component.Size);
        Assert.Contains("A", component.Nodes);
        Assert.Contains("B", component.Nodes);
        Assert.Contains("C", component.Nodes);
        Assert.Contains("D", component.Nodes);
        Assert.Equal(1, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(1, result.Statistics.NonTrivialComponents);
        Assert.Equal(4, result.Statistics.LargestComponentSize);
    }

    [Fact]
    public void FindWeaklyConnectedComponents_DirectedTree_ReturnsSingleNonTrivialComponent()
    {
        // Arrange: Tree structure with directed edges
        //     A
        //   ↙   ↘
        //  B     C
        // ↙ ↘   ↙
        // D  E  F
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("A", "C")
            .AddEdge("B", "D")
            .AddEdge("B", "E")
            .AddEdge("C", "F")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components.First();
        Assert.False(component.IsTrivial);
        Assert.Equal(6, component.Size);
        Assert.Contains("A", component.Nodes);
        Assert.Contains("B", component.Nodes);
        Assert.Contains("C", component.Nodes);
        Assert.Contains("D", component.Nodes);
        Assert.Contains("E", component.Nodes);
        Assert.Contains("F", component.Nodes);
        Assert.Equal(1, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(1, result.Statistics.NonTrivialComponents);
        Assert.Equal(6, result.Statistics.LargestComponentSize);
    }

    #endregion

    #region Multiple Component Tests

    [Fact]
    public void FindWeaklyConnectedComponents_TwoSeparateChains_ReturnsTwoNonTrivialComponents()
    {
        // Arrange: A → B → C and D → E → F (two separate chains)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("D", "E")
            .AddEdge("E", "F")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Equal(2, result.Components.Length);
        Assert.All(result.Components, c => Assert.False(c.IsTrivial));
        Assert.All(result.Components, c => Assert.Equal(3, c.Size));
        Assert.Equal(2, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(2, result.Statistics.NonTrivialComponents);
        Assert.Equal(3, result.Statistics.LargestComponentSize);

        var allNodes = result.Components.SelectMany(c => c.Nodes).ToHashSet();
        Assert.Equal(6, allNodes.Count);
        Assert.Contains("A", allNodes);
        Assert.Contains("B", allNodes);
        Assert.Contains("C", allNodes);
        Assert.Contains("D", allNodes);
        Assert.Contains("E", allNodes);
        Assert.Contains("F", allNodes);
    }

    [Fact]
    public void FindWeaklyConnectedComponents_MixedComponentSizes_ReturnsCorrectComponents()
    {
        // Arrange: Single node, pair, and chain
        // G (isolated)
        // H → I (pair)
        // A → B → C → D (chain)
        var graph = new GraphBuilder<string>()
            .AddNode("G")
            .AddEdge("H", "I")
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("C", "D")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Equal(3, result.Components.Length);
        Assert.Equal(3, result.Statistics.TotalComponents);
        Assert.Equal(1, result.Statistics.TrivialComponents);
        Assert.Equal(2, result.Statistics.NonTrivialComponents);
        Assert.Equal(4, result.Statistics.LargestComponentSize);

        var componentSizes = result.Components.Select(c => c.Size).OrderBy(s => s).ToArray();
        Assert.Equal(new[] { 1, 2, 4 }, componentSizes);

        var trivialComponent = result.Components.Single(c => c.IsTrivial);
        Assert.Single(trivialComponent.Nodes);
        Assert.Equal("G", trivialComponent.Nodes.Single());
    }

    #endregion

    #region Cycle and Self-Loop Tests

    [Fact]
    public void FindWeaklyConnectedComponents_SimpleCycle_ReturnsSingleNonTrivialComponent()
    {
        // Arrange: A → B → C → A (strongly connected cycle)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("C", "A")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components.First();
        Assert.False(component.IsTrivial);
        Assert.Equal(3, component.Size);
        Assert.Contains("A", component.Nodes);
        Assert.Contains("B", component.Nodes);
        Assert.Contains("C", component.Nodes);
        Assert.Equal(1, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(1, result.Statistics.NonTrivialComponents);
        Assert.Equal(3, result.Statistics.LargestComponentSize);
    }

    [Fact]
    public void FindWeaklyConnectedComponents_SelfLoop_ReturnsSingleNonTrivialComponent()
    {
        // Arrange: A → A (self-loop)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "A")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components.First();
        Assert.False(component.IsTrivial);
        Assert.Single(component.Nodes);
        Assert.Equal("A", component.Nodes.Single());
        Assert.Equal(1, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(1, result.Statistics.NonTrivialComponents);
        Assert.Equal(1, result.Statistics.LargestComponentSize);
    }

    [Fact]
    public void FindWeaklyConnectedComponents_NodeWithSelfLoopAndOtherConnections_ReturnsSingleNonTrivialComponent()
    {
        // Arrange: A → A, A → B, B → C (self-loop plus other connections)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "A")
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components.First();
        Assert.False(component.IsTrivial);
        Assert.Equal(3, component.Size);
        Assert.Contains("A", component.Nodes);
        Assert.Contains("B", component.Nodes);
        Assert.Contains("C", component.Nodes);
        Assert.Equal(1, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(1, result.Statistics.NonTrivialComponents);
        Assert.Equal(3, result.Statistics.LargestComponentSize);
    }

    #endregion

    #region Complex Graph Tests

    [Fact]
    public void FindWeaklyConnectedComponents_DiamondStructure_ReturnsSingleNonTrivialComponent()
    {
        // Arrange: Diamond/rhombus structure
        //   A
        //  ↙ ↘
        // B   C
        //  ↘ ↙
        //   D
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("A", "C")
            .AddEdge("B", "D")
            .AddEdge("C", "D")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components.First();
        Assert.False(component.IsTrivial);
        Assert.Equal(4, component.Size);
        Assert.Contains("A", component.Nodes);
        Assert.Contains("B", component.Nodes);
        Assert.Contains("C", component.Nodes);
        Assert.Contains("D", component.Nodes);
        Assert.Equal(1, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(1, result.Statistics.NonTrivialComponents);
        Assert.Equal(4, result.Statistics.LargestComponentSize);
    }

    [Fact]
    public void FindWeaklyConnectedComponents_ComplexMixedGraph_ReturnsCorrectComponents()
    {
        // Arrange: Complex graph with multiple patterns
        // Strongly connected cycle: A → B → C → A
        // Tree branch from B: B → D → E
        // Separate chain: F → G → H
        // Isolated nodes: I, J
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("C", "A")
            .AddEdge("B", "D")
            .AddEdge("D", "E")
            .AddEdge("F", "G")
            .AddEdge("G", "H")
            .AddNode("I")
            .AddNode("J")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Equal(4, result.Components.Length);
        Assert.Equal(4, result.Statistics.TotalComponents);
        Assert.Equal(2, result.Statistics.TrivialComponents);
        Assert.Equal(2, result.Statistics.NonTrivialComponents);
        Assert.Equal(5, result.Statistics.LargestComponentSize);

        // Find largest component (should contain A, B, C, D, E)
        var largestComponent = result.Components.OrderByDescending(c => c.Size).First();
        Assert.Equal(5, largestComponent.Size);
        Assert.False(largestComponent.IsTrivial);
        Assert.Contains("A", largestComponent.Nodes);
        Assert.Contains("B", largestComponent.Nodes);
        Assert.Contains("C", largestComponent.Nodes);
        Assert.Contains("D", largestComponent.Nodes);
        Assert.Contains("E", largestComponent.Nodes);

        // Find chain component (should contain F, G, H)
        var chainComponent = result.Components.Single(c => c.Size == 3);
        Assert.False(chainComponent.IsTrivial);
        Assert.Contains("F", chainComponent.Nodes);
        Assert.Contains("G", chainComponent.Nodes);
        Assert.Contains("H", chainComponent.Nodes);

        // Find isolated components (should contain I and J separately)
        var isolatedComponents = result.Components.Where(c => c.IsTrivial).ToArray();
        Assert.Equal(2, isolatedComponents.Length);
        var isolatedNodes = isolatedComponents.SelectMany(c => c.Nodes).ToHashSet();
        Assert.Contains("I", isolatedNodes);
        Assert.Contains("J", isolatedNodes);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void FindWeaklyConnectedComponents_NullGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => UnionFindWeaklyConnectedComponents.Execute<string>(null!));
    }

    [Fact]
    public void FindWeaklyConnectedComponents_LargeLinearChain_HandlesEfficientlyAndCorrectly()
    {
        // Arrange: Create a large linear chain to test performance and correctness
        const int chainLength = 1000;
        var builder = new GraphBuilder<int>();
        
        for (int i = 0; i < chainLength - 1; i++)
        {
            builder.AddEdge(i, i + 1);
        }
        var graph = builder.Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components.First();
        Assert.False(component.IsTrivial);
        Assert.Equal(chainLength, component.Size);
        Assert.Equal(1, result.Statistics.TotalComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(1, result.Statistics.NonTrivialComponents);
        Assert.Equal(chainLength, result.Statistics.LargestComponentSize);
        
        // Verify all nodes are present
        var nodeSet = component.Nodes.ToHashSet();
        for (int i = 0; i < chainLength; i++)
        {
            Assert.Contains(i, nodeSet);
        }
        
        // Verify performance is reasonable (should be very fast with Union-Find)
        Assert.True(result.ProcessingTime.TotalMilliseconds < 100, 
            $"Processing took {result.ProcessingTime.TotalMilliseconds}ms, expected < 100ms");
    }

    #endregion

    #region Algorithm Properties Tests

    [Fact]
    public void FindWeaklyConnectedComponents_VerifyAlgorithmMetadata()
    {
        // Arrange
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Equal("Union-Find Weakly Connected Components", result.Algorithm);
        Assert.True(result.ProcessingTime.TotalMilliseconds >= 0);
        Assert.True(result.CompletedAt <= DateTime.UtcNow);
        Assert.True(result.CompletedAt >= DateTime.UtcNow.AddSeconds(-1)); // Should complete within 1 second
    }

    [Fact] 
    public void FindWeaklyConnectedComponents_DifferentNodeTypes_WorksCorrectly()
    {
        // Arrange: Test with integer nodes
        var graph = new GraphBuilder<int>()
            .AddEdge(1, 2)
            .AddEdge(2, 3)
            .AddNode(4)
            .Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Equal(2, result.Components.Length);
        
        var chainComponent = result.Components.Single(c => c.Size == 3);
        Assert.Contains(1, chainComponent.Nodes);
        Assert.Contains(2, chainComponent.Nodes);
        Assert.Contains(3, chainComponent.Nodes);
        
        var isolatedComponent = result.Components.Single(c => c.Size == 1);
        Assert.Contains(4, isolatedComponent.Nodes);
    }

    #endregion

    #region Weak vs Strong Connectivity Comparison Tests

    [Fact]
    public void FindWeaklyConnectedComponents_VsStronglyConnected_DifferentResults()
    {
        // Arrange: Graph where weak and strong connectivity differ
        // A → B → C ← D (weak: 1 component, strong: 4 components)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("D", "C")
            .Build();

        // Act
        var weakResult = UnionFindWeaklyConnectedComponents.Execute(graph);
        var strongResult = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

        // Assert
        // Weakly connected: all nodes are connected
        Assert.Single(weakResult.Components);
        Assert.Equal(4, weakResult.Components.First().Size);
        Assert.False(weakResult.Components.First().IsTrivial);

        // Strongly connected: each node is its own component
        Assert.Equal(4, strongResult.Components.Length);
        Assert.All(strongResult.Components, c => Assert.True(c.IsTrivial));
    }

    [Fact]
    public void FindWeaklyConnectedComponents_VsStronglyConnected_SameForBidirectional()
    {
        // Arrange: Bidirectional edges create same weak and strong components
        // A ↔ B ↔ C (bidirectional)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "A")
            .AddEdge("B", "C")
            .AddEdge("C", "B")
            .Build();

        // Act
        var weakResult = UnionFindWeaklyConnectedComponents.Execute(graph);
        var strongResult = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

        // Assert
        // Both should find the same single component
        Assert.Single(weakResult.Components);
        Assert.Single(strongResult.Components);
        Assert.Equal(3, weakResult.Components.First().Size);
        Assert.Equal(3, strongResult.Components.First().Size);
        Assert.False(weakResult.Components.First().IsTrivial);
        Assert.False(strongResult.Components.First().IsTrivial);
    }

    #endregion

    #region Performance and Scalability Tests

    [Fact]
    public void FindWeaklyConnectedComponents_DenseGraph_HandlesEfficiently()
    {
        // Arrange: Create a dense graph (nearly complete)
        const int nodeCount = 100;
        var builder = new GraphBuilder<int>();
        
        // Add many edges to create a dense graph
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = i + 1; j < Math.Min(i + 10, nodeCount); j++) // Each node connects to next 10
            {
                builder.AddEdge(i, j);
            }
        }
        var graph = builder.Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Single(result.Components); // Should be one large component
        var component = result.Components.First();
        Assert.Equal(nodeCount, component.Size);
        Assert.False(component.IsTrivial);
        
        // Verify performance is good with Union-Find
        Assert.True(result.ProcessingTime.TotalMilliseconds < 50, 
            $"Dense graph processing took {result.ProcessingTime.TotalMilliseconds}ms, expected < 50ms");
    }

    [Fact]
    public void FindWeaklyConnectedComponents_ManySparseComponents_HandlesCorrectly()
    {
        // Arrange: Many small disconnected components
        const int componentCount = 50;
        var builder = new GraphBuilder<int>();
        
        for (int c = 0; c < componentCount; c++)
        {
            int baseNode = c * 3;
            builder.AddEdge(baseNode, baseNode + 1);
            builder.AddEdge(baseNode + 1, baseNode + 2);
        }
        var graph = builder.Build();

        // Act
        var result = UnionFindWeaklyConnectedComponents.Execute(graph);

        // Assert
        Assert.Equal(componentCount, result.Components.Length);
        Assert.All(result.Components, c => Assert.Equal(3, c.Size));
        Assert.All(result.Components, c => Assert.False(c.IsTrivial));
        Assert.Equal(componentCount, result.Statistics.NonTrivialComponents);
        Assert.Equal(0, result.Statistics.TrivialComponents);
        Assert.Equal(3, result.Statistics.LargestComponentSize);
        
        // Verify good performance
        Assert.True(result.ProcessingTime.TotalMilliseconds < 100,
            $"Many components processing took {result.ProcessingTime.TotalMilliseconds}ms, expected < 100ms");
    }

    #endregion
}
