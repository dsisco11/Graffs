using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Graffs.Analysis;
using Graffs.Builders;
using Graffs;

namespace Graffs.Tests;

/// <summary>
/// Comprehensive tests for graph slicing functionality.
/// </summary>
public sealed class GraphSlicingTests
{
    #region Basic Functionality Tests

    [Fact]
    public void AnalyzeDependencyStructure_EmptyGraph_ReturnsValidEmptyResult()
    {
        // Arrange
        var graph = new GraphBuilder<string>().Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ValidationError);
        Assert.Equal(0, result.TotalNodes);
        Assert.Equal(0, result.TotalLevels);
        Assert.Equal(0, result.MaxDependencyDepth);
        Assert.Empty(result.Levels);
        Assert.Empty(result.IndependentChains);
        Assert.Equal(0, result.AverageNodesPerLevel);
        Assert.True(result.ProcessingTime >= TimeSpan.Zero);
    }

    [Fact]
    public void AnalyzeDependencyStructure_SingleNode_ReturnsCorrectStructure()
    {
        // Arrange
        var graph = new GraphBuilder<string>()
            .AddNode("A")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.TotalNodes);
        Assert.Equal(1, result.TotalLevels);
        Assert.Equal(1, result.MaxDependencyDepth);
        Assert.Single(result.Levels);
        Assert.Single(result.IndependentChains);

        // Check level structure
        var level0 = result.Levels[0];
        Assert.Equal(0, level0.LevelIndex);
        Assert.True(level0.IsRootLevel);
        Assert.True(level0.IsLeafLevel);
        Assert.Single(level0.Nodes);
        Assert.Contains("A", level0.Nodes);
        Assert.Empty(level0.LevelDependencies);
        Assert.Empty(level0.LevelDependents);

        // Check chain structure
        var chain = result.IndependentChains[0];
        Assert.Single(chain.Nodes);
        Assert.Contains("A", chain.Nodes);
        Assert.Equal("A", chain.RootNode);
        Assert.Contains("A", chain.LeafNodes);
        Assert.Equal(1, chain.ChainDepth);
        Assert.Equal(1, chain.NodeCount);
    }

    [Fact]
    public void AnalyzeDependencyStructure_LinearChain_ReturnsCorrectLevels()
    {
        // Arrange: A → B → C → D
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("C", "D")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(4, result.TotalNodes);
        Assert.Equal(4, result.TotalLevels);
        Assert.Equal(4, result.MaxDependencyDepth);
        Assert.Equal(1.0, result.AverageNodesPerLevel);
        Assert.Single(result.IndependentChains);

        // Verify level structure
        Assert.Equal(4, result.Levels.Length);
        
        // Level 0: A (root)
        var level0 = result.Levels[0];
        Assert.Equal(0, level0.LevelIndex);
        Assert.True(level0.IsRootLevel);
        Assert.False(level0.IsLeafLevel);
        Assert.Single(level0.Nodes);
        Assert.Contains("A", level0.Nodes);
        
        // Level 1: B
        var level1 = result.Levels[1];
        Assert.Equal(1, level1.LevelIndex);
        Assert.False(level1.IsRootLevel);
        Assert.False(level1.IsLeafLevel);
        Assert.Single(level1.Nodes);
        Assert.Contains("B", level1.Nodes);
        Assert.Contains("A", level1.LevelDependencies);
        
        // Level 2: C
        var level2 = result.Levels[2];
        Assert.Equal(2, level2.LevelIndex);
        Assert.Single(level2.Nodes);
        Assert.Contains("C", level2.Nodes);
        
        // Level 3: D (leaf)
        var level3 = result.Levels[3];
        Assert.Equal(3, level3.LevelIndex);
        Assert.False(level3.IsRootLevel);
        Assert.True(level3.IsLeafLevel);
        Assert.Single(level3.Nodes);
        Assert.Contains("D", level3.Nodes);
        Assert.Empty(level3.LevelDependents);

        // Verify chain structure
        var chain = result.IndependentChains[0];
        Assert.Equal(4, chain.NodeCount);
        Assert.Equal(4, chain.ChainDepth);
        Assert.Equal("A", chain.RootNode);
        Assert.Contains("D", chain.LeafNodes);
        Assert.Single(chain.LeafNodes);
    }

    #endregion

    #region Diamond Pattern Tests

    [Fact]
    public void AnalyzeDependencyStructure_DiamondPattern_ReturnsCorrectStructure()
    {
        // Arrange: A → B, A → C, B → D, C → D
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("A", "C")
            .AddEdge("B", "D")
            .AddEdge("C", "D")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(4, result.TotalNodes);
        Assert.Equal(3, result.TotalLevels);
        Assert.Equal(3, result.MaxDependencyDepth);
        Assert.Single(result.IndependentChains);

        // Level 0: A (root)
        var level0 = result.Levels[0];
        Assert.Equal(0, level0.LevelIndex);
        Assert.Single(level0.Nodes);
        Assert.Contains("A", level0.Nodes);

        // Level 1: B, C (parallel dependencies)
        var level1 = result.Levels[1];
        Assert.Equal(1, level1.LevelIndex);
        Assert.Equal(2, level1.NodeCount);
        Assert.Contains("B", level1.Nodes);
        Assert.Contains("C", level1.Nodes);
        Assert.Contains("A", level1.LevelDependencies);

        // Level 2: D (converges from B and C)
        var level2 = result.Levels[2];
        Assert.Equal(2, level2.LevelIndex);
        Assert.Single(level2.Nodes);
        Assert.Contains("D", level2.Nodes);
        Assert.Contains("B", level2.LevelDependencies);
        Assert.Contains("C", level2.LevelDependencies);
        Assert.True(level2.IsLeafLevel);

        // Verify chain structure
        var chain = result.IndependentChains[0];
        Assert.Equal(4, chain.NodeCount);
        Assert.Equal("A", chain.RootNode);
        Assert.Contains("D", chain.LeafNodes);
        Assert.Single(chain.LeafNodes);
    }

    #endregion

    #region Independent Chains Tests

    [Fact]
    public void AnalyzeDependencyStructure_TwoIndependentChains_ReturnsCorrectChains()
    {
        // Arrange: A → B → C and D → E
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("D", "E")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(5, result.TotalNodes);
        Assert.Equal(3, result.TotalLevels); // Max depth is 3 (A→B→C)
        Assert.Equal(2, result.IndependentChains.Length);

        // Find chains by root node
        var chainA = result.IndependentChains.First(c => c.RootNode.Equals("A"));
        var chainD = result.IndependentChains.First(c => c.RootNode.Equals("D"));

        // Verify chain A → B → C
        Assert.Equal(3, chainA.NodeCount);
        Assert.Equal(3, chainA.ChainDepth);
        Assert.Equal("A", chainA.RootNode);
        Assert.Contains("C", chainA.LeafNodes);

        // Verify chain D → E
        Assert.Equal(2, chainD.NodeCount);
        Assert.Equal(2, chainD.ChainDepth);
        Assert.Equal("D", chainD.RootNode);
        Assert.Contains("E", chainD.LeafNodes);

        // Verify no overlap between chains
        var chainANodes = chainA.Nodes.ToHashSet();
        var chainDNodes = chainD.Nodes.ToHashSet();
        Assert.Empty(chainANodes.Intersect(chainDNodes));
    }

    [Fact]
    public void AnalyzeDependencyStructure_ComplexIndependentChains_ReturnsCorrectStructure()
    {
        // Arrange: Complex independent structures
        // Chain 1: A → B → D, A → C → D (diamond)
        // Chain 2: E → F
        // Chain 3: G (isolated)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("A", "C")
            .AddEdge("B", "D")
            .AddEdge("C", "D")
            .AddEdge("E", "F")
            .AddNode("G")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(7, result.TotalNodes);
        Assert.Equal(3, result.IndependentChains.Length);

        // Verify all chains have unique root nodes
        var rootNodes = result.IndependentChains.Select(c => c.RootNode).ToHashSet();
        Assert.Equal(3, rootNodes.Count);
        Assert.Contains("A", rootNodes);
        Assert.Contains("E", rootNodes);
        Assert.Contains("G", rootNodes);

        // Verify chain complexity
        var chainA = result.IndependentChains.First(c => c.RootNode.Equals("A"));
        var chainE = result.IndependentChains.First(c => c.RootNode.Equals("E"));
        var chainG = result.IndependentChains.First(c => c.RootNode.Equals("G"));

        Assert.Equal(4, chainA.NodeCount); // A, B, C, D
        Assert.Equal(2, chainE.NodeCount); // E, F
        Assert.Equal(1, chainG.NodeCount); // G
    }

    #endregion

    #region Cycle Detection Tests

    [Fact]
    public void AnalyzeDependencyStructure_WithCycles_ReturnsInvalidResult()
    {
        // Arrange: A → B → A (simple cycle)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "A")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("cycles", result.ValidationError);
        Assert.Equal(0, result.TotalNodes);
        Assert.Equal(0, result.TotalLevels);
        Assert.Empty(result.Levels);
        Assert.Empty(result.IndependentChains);
    }

    [Fact]
    public void AnalyzeDependencyStructure_WithComplexCycles_ReturnsInvalidResult()
    {
        // Arrange: A → B → C → A (complex cycle)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .AddEdge("C", "A")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ValidationError);
    }

    #endregion

    #region Custom Comparer Tests

    [Fact]
    public void AnalyzeDependencyStructure_WithCustomComparer_UsesDeterministicOrdering()
    {
        // Arrange: Diamond pattern with specific ordering
        var graph = new GraphBuilder<string>()
            .AddEdge("Root", "B_Second")
            .AddEdge("Root", "A_First")
            .AddEdge("B_Second", "Leaf")
            .AddEdge("A_First", "Leaf")
            .Build();

        var comparer = StringComparer.Ordinal; // Alphabetical ordering

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph, comparer);

        // Assert
        Assert.True(result.IsValid);
        
        // Level 1 should have nodes in alphabetical order
        var level1 = result.Levels[1];
        Assert.Equal(2, level1.NodeCount);
        Assert.Equal("A_First", level1.Nodes[0]);
        Assert.Equal("B_Second", level1.Nodes[1]);
    }

    #endregion

    #region Null Arguments Tests

    [Fact]
    public void AnalyzeDependencyStructure_NullGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            GraphSlicer.AnalyzeDependencyStructure<string>(null!));
    }

    [Fact]
    public void AnalyzeDependencyStructure_NullGraphWithComparer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            GraphSlicer.AnalyzeDependencyStructure<string>(null!, StringComparer.Ordinal));
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void AnalyzeDependencyStructure_LargerGraph_CompletesInReasonableTime()
    {
        // Arrange: Create a layered graph with multiple chains
        var builder = new GraphBuilder<int>();
        
        // Create 5 independent linear chains of length 10 each
        for (int chain = 0; chain < 5; chain++)
        {
            int baseNode = chain * 100;
            for (int level = 0; level < 9; level++)
            {
                builder.AddEdge(baseNode + level, baseNode + level + 1);
            }
        }

        var graph = builder.Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(50, result.TotalNodes); // 5 chains * 10 nodes each
        Assert.Equal(10, result.MaxDependencyDepth); // Linear chains of length 10
        Assert.Equal(5, result.IndependentChains.Length);
        Assert.True(result.ProcessingTime.TotalMilliseconds < 1000); // Should complete quickly
    }

    #endregion

    #region Level Relationship Tests

    [Fact]
    public void AnalyzeDependencyStructure_ComplexGraph_CalculatesLevelRelationshipsCorrectly()
    {
        // Arrange: Multi-level graph with various dependencies
        // Level 0: A, B (roots)
        // Level 1: C (depends on A), D (depends on B)
        // Level 2: E (depends on C and D)
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "C")
            .AddEdge("B", "D")
            .AddEdge("C", "E")
            .AddEdge("D", "E")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.TotalLevels);

        // Level 0: A, B
        var level0 = result.Levels[0];
        Assert.Equal(2, level0.NodeCount);
        Assert.Contains("A", level0.Nodes);
        Assert.Contains("B", level0.Nodes);
        Assert.Empty(level0.LevelDependencies);
        Assert.Contains("C", level0.LevelDependents);
        Assert.Contains("D", level0.LevelDependents);

        // Level 1: C, D
        var level1 = result.Levels[1];
        Assert.Equal(2, level1.NodeCount);
        Assert.Contains("C", level1.Nodes);
        Assert.Contains("D", level1.Nodes);
        Assert.Contains("A", level1.LevelDependencies);
        Assert.Contains("B", level1.LevelDependencies);
        Assert.Contains("E", level1.LevelDependents);

        // Level 2: E
        var level2 = result.Levels[2];
        Assert.Single(level2.Nodes);
        Assert.Contains("E", level2.Nodes);
        Assert.Contains("C", level2.LevelDependencies);
        Assert.Contains("D", level2.LevelDependencies);
        Assert.Empty(level2.LevelDependents);
        Assert.True(level2.IsLeafLevel);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AnalyzeDependencyStructure_OnlyRootNodes_ReturnsCorrectStructure()
    {
        // Arrange: Multiple disconnected root nodes
        var graph = new GraphBuilder<string>()
            .AddNode("A")
            .AddNode("B")
            .AddNode("C")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.TotalNodes);
        Assert.Equal(1, result.TotalLevels);
        Assert.Equal(3, result.IndependentChains.Length);

        // All nodes should be at level 0
        var level0 = result.Levels[0];
        Assert.Equal(3, level0.NodeCount);
        Assert.True(level0.IsRootLevel);
        Assert.True(level0.IsLeafLevel);

        // Each node should form its own chain
        foreach (var chain in result.IndependentChains)
        {
            Assert.Equal(1, chain.NodeCount);
            Assert.Equal(1, chain.ChainDepth);
            Assert.Equal(chain.RootNode, chain.Nodes[0]);
            Assert.Contains(chain.RootNode, chain.LeafNodes);
        }
    }

    #endregion
}
