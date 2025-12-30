using System;
using System.Collections.Generic;
using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Graffs;
using Xunit;

namespace Graffs.Tests;

public class StableTopologicalSortTests
{
    [Fact]
    public void StableSort_WithEmptyGraph_ReturnsEmptyResult()
    {
        // Arrange
        var graph = new GraphBuilder<string>().Build();

        // Act
        var result = TopologicalSort.StableSort(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.SortedNodes);
        Assert.False(result.HasCycles);
        Assert.Empty(result.CyclicNodes);
    }

    [Fact]
    public void StableSort_WithSingleNode_ReturnsSingleNode()
    {
        // Arrange
        var builder = new GraphBuilder<string>();
        builder.AddNode("A");
        var graph = builder.Build();

        // Act
        var result = TopologicalSort.StableSort(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.SortedNodes);
        Assert.Equal("A", result.SortedNodes[0]);
        Assert.False(result.HasCycles);
    }

    [Fact]
    public void StableSort_WithLinearDependency_ReturnsCorrectOrder()
    {
        // Arrange: A -> B -> C
        var builder = new GraphBuilder<string>();
        builder.AddNode("A");
        builder.AddNode("B");
        builder.AddNode("C");
        builder.AddEdge("A", "B");
        builder.AddEdge("B", "C");
        var graph = builder.Build();

        // Act
        var result = TopologicalSort.StableSort(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.SortedNodes.Length);
        Assert.Equal(new[] { "A", "B", "C" }, result.SortedNodes);
    }

    [Fact]
    public void StableSort_WithCustomComparer_UsesComparerForSameLevel()
    {
        // Arrange: Independent nodes B, A, C (no dependencies)
        var builder = new GraphBuilder<string>();
        builder.AddNode("B");
        builder.AddNode("A");
        builder.AddNode("C");
        var graph = builder.Build();

        // Comparer that sorts alphabetically
        var comparer = Comparer<string>.Create((x, y) => string.Compare(x, y, StringComparison.Ordinal));

        // Act
        var result = TopologicalSort.StableSort(graph, comparer);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.SortedNodes.Length);
        Assert.Equal(new[] { "A", "B", "C" }, result.SortedNodes); // Alphabetical order
    }

    [Fact]
    public void StableSort_WithComparison_UsesComparisonForSameLevel()
    {
        // Arrange: Independent nodes B, A, C (no dependencies)
        var builder = new GraphBuilder<string>();
        builder.AddNode("B");
        builder.AddNode("A");
        builder.AddNode("C");
        var graph = builder.Build();

        // Comparison that sorts in reverse alphabetical order
        Comparison<string> comparison = (x, y) => string.Compare(y, x, StringComparison.Ordinal);

        // Act
        var result = TopologicalSort.StableSort(graph, comparison);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.SortedNodes.Length);
        Assert.Equal(new[] { "C", "B", "A" }, result.SortedNodes); // Reverse alphabetical order
    }

    [Fact]
    public void StableSort_WithMixedLevels_RespectsDepencenciesAndOrdering()
    {
        // Arrange: Complex graph with multiple levels
        // Level 0: A, B (independent)
        // Level 1: C depends on A, D depends on B  
        // Level 2: E depends on C and D
        var builder = new GraphBuilder<string>();
        builder.AddNodes(new[] { "A", "B", "C", "D", "E" });
        builder.AddEdge("A", "C");
        builder.AddEdge("B", "D");
        builder.AddEdge("C", "E");
        builder.AddEdge("D", "E");
        var graph = builder.Build();

        // Comparer that sorts alphabetically
        var comparer = Comparer<string>.Create((x, y) => string.Compare(x, y, StringComparison.Ordinal));

        // Act
        var result = TopologicalSort.StableSort(graph, comparer);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(5, result.SortedNodes.Length);
        
        // Check that dependencies are respected
        var nodeIndices = result.SortedNodes.Select((node, index) => new { Node = node, Index = index })
            .ToDictionary(x => x.Node, x => x.Index);
        
        Assert.True(nodeIndices["A"] < nodeIndices["C"]); // A before C
        Assert.True(nodeIndices["B"] < nodeIndices["D"]); // B before D
        Assert.True(nodeIndices["C"] < nodeIndices["E"]); // C before E
        Assert.True(nodeIndices["D"] < nodeIndices["E"]); // D before E
        
        // Check that within same level, alphabetical order is maintained
        Assert.True(nodeIndices["A"] < nodeIndices["B"]); // A before B (both level 0)
        Assert.True(nodeIndices["C"] < nodeIndices["D"]); // C before D (both level 1)
    }

    [Fact]
    public void StableSort_WithCycles_DetectsCyclesCorrectly()
    {
        // Arrange: A -> B -> C -> A (cycle)
        var builder = new GraphBuilder<string>();
        builder.AddNodes(new[] { "A", "B", "C" });
        builder.AddEdge("A", "B");
        builder.AddEdge("B", "C");
        builder.AddEdge("C", "A");
        var graph = builder.Build();

        // Act
        var result = TopologicalSort.StableSort(graph);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCycles);
        Assert.Equal(3, result.CyclicNodes.Length);
        Assert.Contains("A", result.CyclicNodes);
        Assert.Contains("B", result.CyclicNodes);
        Assert.Contains("C", result.CyclicNodes);
    }

    [Fact]
    public void StableSort_WithPartialCycles_ReturnsBothSortedAndCyclic()
    {
        // Arrange: D -> A -> B -> C -> A (D is independent, A-B-C form cycle)
        var builder = new GraphBuilder<string>();
        builder.AddNodes(new[] { "A", "B", "C", "D" });
        builder.AddEdge("D", "A");
        builder.AddEdge("A", "B");
        builder.AddEdge("B", "C");
        builder.AddEdge("C", "A");
        var graph = builder.Build();

        // Act
        var result = TopologicalSort.StableSort(graph);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCycles);
        
        // D should be in sorted nodes (not part of cycle)
        Assert.Contains("D", result.SortedNodes);
        
        // A, B, C should be in cyclic nodes
        Assert.Contains("A", result.CyclicNodes);
        Assert.Contains("B", result.CyclicNodes);
        Assert.Contains("C", result.CyclicNodes);
    }

    [Fact]
    public void StableSort_WithDefaultComparer_IsDeterministic()
    {
        // Arrange: Multiple independent nodes
        var builder = new GraphBuilder<string>();
        builder.AddNodes(new[] { "Z", "A", "M", "B", "X" });
        var graph = builder.Build();

        // Act - Run multiple times to ensure deterministic results
        var result1 = TopologicalSort.StableSort(graph);
        var result2 = TopologicalSort.StableSort(graph);
        var result3 = TopologicalSort.StableSort(graph);

        // Assert
        Assert.True(result1.IsValid);
        Assert.True(result2.IsValid);
        Assert.True(result3.IsValid);
        
        // Results should be identical (deterministic)
        Assert.Equal(result1.SortedNodes, result2.SortedNodes);
        Assert.Equal(result1.SortedNodes, result3.SortedNodes);
    }

    [Fact]
    public void StableSort_WithComplexGraph_MaintainsBothConstraints()
    {
        // Arrange: Complex graph to test both dependency and priority ordering
        // Priority: High=1, Medium=2, Low=3 (lower number = higher priority)
        var builder = new GraphBuilder<PriorityNode>();
        var nodeA = new PriorityNode("A", 3); // Low priority
        var nodeB = new PriorityNode("B", 1); // High priority
        var nodeC = new PriorityNode("C", 2); // Medium priority
        var nodeD = new PriorityNode("D", 1); // High priority
        var nodeE = new PriorityNode("E", 2); // Medium priority

        builder.AddNodes(new[] { nodeA, nodeB, nodeC, nodeD, nodeE });
        
        // Dependencies: A -> C, B -> E, C -> E, D is independent
        builder.AddEdge(nodeA, nodeC);
        builder.AddEdge(nodeB, nodeE);
        builder.AddEdge(nodeC, nodeE);
        
        var graph = builder.Build();

        // Priority comparer (lower number = higher priority, then alphabetical)
        var comparer = Comparer<PriorityNode>.Create((x, y) =>
        {
            var priorityCompare = x.Priority.CompareTo(y.Priority);
            if (priorityCompare != 0) return priorityCompare;
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        });

        // Act
        var result = TopologicalSort.StableSort(graph, comparer);

        // Assert
        Assert.True(result.IsValid);
        
        var nodeIndices = result.SortedNodes.Select((node, index) => new { Node = node, Index = index })
            .ToDictionary(x => x.Node.Name, x => x.Index);
        
        // Check dependencies are respected
        Assert.True(nodeIndices["A"] < nodeIndices["C"]); // A before C
        Assert.True(nodeIndices["B"] < nodeIndices["E"]); // B before E
        Assert.True(nodeIndices["C"] < nodeIndices["E"]); // C before E
        
        // Check priority ordering within levels
        // Level 0: A(priority=3), B(priority=1), D(priority=1) -> B, D, A (by priority, then name)
        Assert.True(nodeIndices["B"] < nodeIndices["D"]); // B before D (same priority, alphabetical)
        Assert.True(nodeIndices["D"] < nodeIndices["A"]); // D before A (higher priority)
    }

    [Fact]
    public void StableSort_WithNullGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TopologicalSort.StableSort<string>(null!));
        Assert.Throws<ArgumentNullException>(() => TopologicalSort.StableSort<string>(null!, (x, y) => 0));
    }

    [Fact]
    public void StableSort_WithNullComparison_ThrowsArgumentNullException()
    {
        // Arrange
        var graph = new GraphBuilder<string>().Build();
        Comparison<string> nullComparison = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TopologicalSort.StableSort(graph, nullComparison));
    }

    [Fact]
    public void StableSort_WithSelfLoop_DetectsCycleCorrectly()
    {
        // Arrange: A -> A (self-loop)
        var builder = new GraphBuilder<string>();
        builder.AddNode("A");
        builder.AddNode("B"); // Independent node
        builder.AddEdge("A", "A"); // Self-loop
        var graph = builder.Build();

        // Act
        var result = TopologicalSort.StableSort(graph);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCycles);
        Assert.Contains("A", result.CyclicNodes);
        Assert.Contains("B", result.SortedNodes); // B should still be sorted
    }

    [Fact]
    public void StableSort_WithMultipleSeparateCycles_DetectsAllCycles()
    {
        // Arrange: Two separate cycles: A -> B -> A and C -> D -> C, plus independent E
        var builder = new GraphBuilder<string>();
        builder.AddNodes(new[] { "A", "B", "C", "D", "E" });
        
        // First cycle: A -> B -> A
        builder.AddEdge("A", "B");
        builder.AddEdge("B", "A");
        
        // Second cycle: C -> D -> C
        builder.AddEdge("C", "D");
        builder.AddEdge("D", "C");
        
        // Independent node E (no edges)
        
        var graph = builder.Build();

        // Act
        var result = TopologicalSort.StableSort(graph);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCycles);
        Assert.Equal(4, result.CyclicNodes.Length); // A, B, C, D
        Assert.Contains("A", result.CyclicNodes);
        Assert.Contains("B", result.CyclicNodes);
        Assert.Contains("C", result.CyclicNodes);
        Assert.Contains("D", result.CyclicNodes);
        Assert.Contains("E", result.SortedNodes); // E should still be sorted
    }

    [Fact]
    public void StableSort_WithComparerReturningZero_IsDeterministic()
    {
        // Arrange: Multiple nodes where comparer returns 0 (identical comparison)
        var builder = new GraphBuilder<string>();
        builder.AddNodes(new[] { "X1", "X2", "X3", "X4" }); // All start with X
        var graph = builder.Build();

        // Comparer that only looks at first character (all return 0 for X nodes)
        var comparer = Comparer<string>.Create((x, y) => 
            x[0].CompareTo(y[0])); // All X's will compare as equal

        // Act - Run multiple times to ensure deterministic results
        var result1 = TopologicalSort.StableSort(graph, comparer);
        var result2 = TopologicalSort.StableSort(graph, comparer);
        var result3 = TopologicalSort.StableSort(graph, comparer);

        // Assert
        Assert.True(result1.IsValid);
        Assert.True(result2.IsValid);
        Assert.True(result3.IsValid);
        
        // Results should be identical even when comparer returns 0
        Assert.Equal(result1.SortedNodes, result2.SortedNodes);
        Assert.Equal(result1.SortedNodes, result3.SortedNodes);
    }

    [Fact]
    public void StableSort_ProducesValidTopologicalOrdering()
    {
        // Arrange: Complex graph to validate topological ordering property
        var builder = new GraphBuilder<string>();
        builder.AddNodes(new[] { "A", "B", "C", "D", "E", "F" });
        
        // Create dependencies: A->C, B->C, C->D, C->E, D->F, E->F
        builder.AddEdge("A", "C");
        builder.AddEdge("B", "C");
        builder.AddEdge("C", "D");
        builder.AddEdge("C", "E");
        builder.AddEdge("D", "F");
        builder.AddEdge("E", "F");
        
        var graph = builder.Build();

        // Act
        var result = TopologicalSort.StableSort(graph);

        // Assert
        Assert.True(result.IsValid);
        
        // Validate topological ordering property: for every edge u->v, u appears before v
        var nodePositions = result.SortedNodes
            .Select((node, index) => new { Node = node, Position = index })
            .ToDictionary(x => x.Node, x => x.Position);

        // Check all edges respect topological ordering
        foreach (var node in graph.Nodes)
        {
            foreach (var dependent in graph.GetDependents(node))
            {
                Assert.True(nodePositions[node] < nodePositions[dependent],
                    $"Topological ordering violated: {node} (pos {nodePositions[node]}) should come before {dependent} (pos {nodePositions[dependent]})");
            }
        }
    }

    [Fact]
    public void StableSort_WithVeryWideGraph_HandlesManySameLevelNodes()
    {
        // Arrange: Single root with many independent children, then convergence
        var builder = new GraphBuilder<string>();
        var rootNode = "ROOT";
        var leafNode = "LEAF";
        var middleNodes = Enumerable.Range(1, 20).Select(i => $"MIDDLE{i:D2}").ToArray();
        
        builder.AddNode(rootNode);
        builder.AddNode(leafNode);
        builder.AddNodes(middleNodes);
        
        // ROOT -> all MIDDLE nodes
        foreach (var middleNode in middleNodes)
        {
            builder.AddEdge(rootNode, middleNode);
        }
        
        // All MIDDLE nodes -> LEAF
        foreach (var middleNode in middleNodes)
        {
            builder.AddEdge(middleNode, leafNode);
        }
        
        var graph = builder.Build();

        // Comparer that sorts alphabetically
        var comparer = Comparer<string>.Create((x, y) => string.Compare(x, y, StringComparison.Ordinal));

        // Act
        var result = TopologicalSort.StableSort(graph, comparer);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(22, result.SortedNodes.Length); // ROOT + 20 MIDDLE + LEAF
        
        var nodePositions = result.SortedNodes
            .Select((node, index) => new { Node = node, Position = index })
            .ToDictionary(x => x.Node, x => x.Position);

        // ROOT should be first
        Assert.Equal(0, nodePositions[rootNode]);
        
        // LEAF should be last
        Assert.Equal(21, nodePositions[leafNode]);
        
        // All MIDDLE nodes should be between ROOT and LEAF, and in alphabetical order
        var middlePositions = middleNodes.Select(n => nodePositions[n]).OrderBy(p => p).ToArray();
        for (int i = 0; i < middlePositions.Length - 1; i++)
        {
            Assert.True(middlePositions[i] < middlePositions[i + 1]);
        }
        
        // Verify stable ordering within the middle level (alphabetical)
        var sortedMiddleNodes = result.SortedNodes
            .Skip(1) // Skip ROOT
            .Take(20) // Take middle nodes
            .ToArray();
        var expectedOrder = middleNodes.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(expectedOrder, sortedMiddleNodes);
    }

    [Fact]
    public void StableSort_WithDeepDependencyChain_HandlesLongChains()
    {
        // Arrange: Long chain of dependencies A1 -> A2 -> A3 -> ... -> A20
        var builder = new GraphBuilder<string>();
        var chainNodes = Enumerable.Range(1, 20).Select(i => $"A{i:D2}").ToArray();
        
        builder.AddNodes(chainNodes);
        
        // Create chain: A01 -> A02 -> A03 -> ... -> A20
        for (int i = 0; i < chainNodes.Length - 1; i++)
        {
            builder.AddEdge(chainNodes[i], chainNodes[i + 1]);
        }
        
        var graph = builder.Build();

        // Act
        var result = TopologicalSort.StableSort(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(20, result.SortedNodes.Length);
        
        // Verify the exact order matches the chain
        Assert.Equal(chainNodes, result.SortedNodes.ToArray());
        
        // Verify topological ordering
        for (int i = 0; i < chainNodes.Length - 1; i++)
        {
            var currentPos = Array.IndexOf(result.SortedNodes.ToArray(), chainNodes[i]);
            var nextPos = Array.IndexOf(result.SortedNodes.ToArray(), chainNodes[i + 1]);
            Assert.True(currentPos < nextPos, $"{chainNodes[i]} should come before {chainNodes[i + 1]}");
        }
    }

    [Fact]
    public void StableSort_ComparedWithRegularSort_ProducesValidAlternativeOrdering()
    {
        // Arrange: Graph where stable sort might produce different ordering than regular sort
        var builder = new GraphBuilder<string>();
        builder.AddNodes(new[] { "A", "B", "C", "D", "E" });
        
        // Dependencies: A->D, B->D, C->E (A,B,C can execute in parallel, then D,E in parallel)
        builder.AddEdge("A", "D");
        builder.AddEdge("B", "D");
        builder.AddEdge("C", "E");
        
        var graph = builder.Build();

        // Act
        var regularResult = TopologicalSort.KahnSort(graph);
        var stableResult = TopologicalSort.StableSort(graph, 
            Comparer<string>.Create((x, y) => string.Compare(x, y, StringComparison.Ordinal)));

        // Assert
        Assert.True(regularResult.IsValid);
        Assert.True(stableResult.IsValid);
        
        // Both should have same nodes
        Assert.Equal(regularResult.SortedNodes.Length, stableResult.SortedNodes.Length);
        Assert.True(regularResult.SortedNodes.All(n => stableResult.SortedNodes.Contains(n)));
        
        // Both should satisfy topological ordering constraints
        var regularPositions = regularResult.SortedNodes
            .Select((node, index) => new { Node = node, Position = index })
            .ToDictionary(x => x.Node, x => x.Position);
        var stablePositions = stableResult.SortedNodes
            .Select((node, index) => new { Node = node, Position = index })
            .ToDictionary(x => x.Node, x => x.Position);

        // Check dependencies are respected in both
        Assert.True(regularPositions["A"] < regularPositions["D"]);
        Assert.True(regularPositions["B"] < regularPositions["D"]);
        Assert.True(regularPositions["C"] < regularPositions["E"]);
        
        Assert.True(stablePositions["A"] < stablePositions["D"]);
        Assert.True(stablePositions["B"] < stablePositions["D"]);
        Assert.True(stablePositions["C"] < stablePositions["E"]);
        
        // Stable sort should have A,B,C in alphabetical order (if they're at same level)
        if (stablePositions["A"] < stablePositions["D"] && 
            stablePositions["B"] < stablePositions["D"] && 
            stablePositions["C"] < stablePositions["E"])
        {
            // A, B, C should be in alphabetical order
            Assert.True(stablePositions["A"] < stablePositions["B"]);
            Assert.True(stablePositions["B"] < stablePositions["C"]);
        }
    }

    private record PriorityNode(string Name, int Priority);
}
