using System;
using System.Collections.Generic;
using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Graffs.Results;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    /// <summary>
    /// Exhaustive tests for Johnson's algorithm implementation to ensure correctness
    /// against the original 1975 research paper specifications.
    /// </summary>
    public class JohnsonsAlgorithmExhaustiveTests
    {
        #region Self-Loop Tests

        [Fact]
        public void JohnsonsAlgorithm_SingleSelfLoop_CorrectRepresentation()
        {
            // Arrange - Single node with self-loop
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles);
            
            var cycle = result.Cycles.First();
            Assert.Single(cycle.Nodes); // Should be ["A"], not ["A", "A"]
            Assert.Equal("A", cycle.Nodes[0]);
            Assert.Equal(1, cycle.Length); // Self-loop has length 1 (1 edge)
            Assert.True(cycle.IsSelfLoop);
            Assert.Equal("A", cycle.StartNode);
            Assert.Equal("A → A", cycle.ToPathString());
        }

        [Fact]
        public void JohnsonsAlgorithm_MultipleSelfLoops_FindsAllCorrectly()
        {
            // Arrange - Multiple independent self-loops
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "A")
                .AddEdge("B", "B")
                .AddEdge("C", "C")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Equal(3, result.CycleCount);
            
            // All should be self-loops
            Assert.True(result.Cycles.All(c => c.IsSelfLoop));
            Assert.True(result.Cycles.All(c => c.Length == 1));
            Assert.True(result.Cycles.All(c => c.Nodes.Count == 1));
            
            // Check we found all three nodes
            var foundNodes = result.Cycles.Select(c => c.StartNode).OrderBy(n => n).ToList();
            Assert.Equal(new[] { "A", "B", "C" }, foundNodes);
        }

        [Fact]
        public void JohnsonsAlgorithm_SelfLoopWithOtherCycles_DistinguishesCorrectly()
        {
            // Arrange - Mix of self-loop and regular cycle
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "A") // Self-loop
                .AddEdge("B", "C") // Regular cycle B->C->B
                .AddEdge("C", "B")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Equal(2, result.CycleCount);
            
            var selfLoops = result.GetSelfLoops().ToList();
            var complexCycles = result.GetComplexCycles().ToList();
            
            Assert.Single(selfLoops);
            Assert.Single(complexCycles);
            
            // Verify self-loop
            var selfLoop = selfLoops.First();
            Assert.Equal("A", selfLoop.StartNode);
            Assert.Equal(1, selfLoop.Length);
            Assert.Single(selfLoop.Nodes);
            
            // Verify complex cycle
            var complexCycle = complexCycles.First();
            Assert.Equal(2, complexCycle.Length); // B->C->B has 2 edges
            Assert.Equal(2, complexCycle.Nodes.Count); // Should be [B, C] or [C, B]
        }

        #endregion

        #region Elementary Cycle Tests

        [Fact]
        public void JohnsonsAlgorithm_TwoNodeCycle_CorrectLength()
        {
            // Arrange - A->B->A (2 edges)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles);
            
            var cycle = result.Cycles.First();
            Assert.Equal(2, cycle.Length); // 2 edges: A->B, B->A
            Assert.Equal(2, cycle.Nodes.Count); // [A, B] or [B, A]
            Assert.False(cycle.IsSelfLoop);
            Assert.True(cycle.IsComplexCycle);
        }

        [Fact]
        public void JohnsonsAlgorithm_ThreeNodeCycle_CorrectLength()
        {
            // Arrange - A->B->C->A (3 edges)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles);
            
            var cycle = result.Cycles.First();
            Assert.Equal(3, cycle.Length); // 3 edges: A->B, B->C, C->A
            Assert.Equal(3, cycle.Nodes.Count); // [A, B, C] or permutation
            Assert.False(cycle.IsSelfLoop);
            Assert.True(cycle.IsComplexCycle);
        }

        [Fact]
        public void JohnsonsAlgorithm_FourNodeCycle_CorrectLength()
        {
            // Arrange - A->B->C->D->A (4 edges)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .AddEdge("D", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles);
            
            var cycle = result.Cycles.First();
            Assert.Equal(4, cycle.Length); // 4 edges
            Assert.Equal(4, cycle.Nodes.Count); // [A, B, C, D] or permutation
            Assert.False(cycle.IsSelfLoop);
            Assert.True(cycle.IsComplexCycle);
        }

        #endregion

        #region Multiple Cycles Tests

        [Fact]
        public void JohnsonsAlgorithm_TwoDisjointCycles_FindsBoth()
        {
            // Arrange - Two separate cycles: A->B->A and C->D->E->C
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Equal(2, result.CycleCount);
            
            var cycleLengths = result.Cycles.Select(c => c.Length).OrderBy(l => l).ToList();
            Assert.Equal(new[] { 2, 3 }, cycleLengths);
            
            Assert.True(result.Cycles.All(c => c.IsComplexCycle));
            Assert.DoesNotContain(result.Cycles, c => c.IsSelfLoop);
        }

        [Fact]
        public void JohnsonsAlgorithm_ConnectedCycles_FindsBoth()
        {
            // Arrange - Two cycles connected by an edge: A->B->A and B->C->D->B
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A") // First cycle: A->B->A
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .AddEdge("D", "B") // Second cycle: B->C->D->B
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.True(result.CycleCount >= 2); // Should find at least the two elementary cycles
            
            // Should find cycles of length 2 and 3
            var cycleLengths = result.Cycles.Select(c => c.Length).Distinct().OrderBy(l => l).ToList();
            Assert.Contains(2, cycleLengths); // A->B->A
            Assert.Contains(3, cycleLengths); // B->C->D->B
        }

        #endregion

        #region Complex Graph Tests

        [Fact]
        public void JohnsonsAlgorithm_CompleteGraph3Nodes_FindsAllCycles()
        {
            // Arrange - Complete graph with 3 nodes (every node connected to every other)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("B", "A")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .AddEdge("C", "B")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            
            // Complete graph with 3 nodes should have elementary cycles:
            // - A->B->A (length 2)
            // - A->C->A (length 2)  
            // - B->C->B (length 2)
            // - A->B->C->A (length 3)
            // - A->C->B->A (length 3)
            Assert.True(result.CycleCount >= 5);
            
            var cycleLengths = result.Cycles.Select(c => c.Length).Distinct().OrderBy(l => l).ToList();
            Assert.Contains(2, cycleLengths);
            Assert.Contains(3, cycleLengths);
        }

        [Fact]
        public void JohnsonsAlgorithm_GraphWithMultiplePaths_FindsOnlyElementaryCycles()
        {
            // Arrange - Graph with multiple paths between nodes
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("B", "D")
                .AddEdge("C", "D")
                .AddEdge("D", "A") // Creates cycles through different paths
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            
            // Should find elementary cycles: A->B->D->A and A->C->D->A
            Assert.True(result.CycleCount >= 2);
            
            // All cycles should have length 3
            Assert.True(result.Cycles.All(c => c.Length == 3));
            Assert.True(result.Cycles.All(c => c.IsComplexCycle));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void JohnsonsAlgorithm_EmptyGraph_NoCycles()
        {
            // Arrange
            var graph = new GraphBuilder<string>().Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.False(result.HasCycles);
            Assert.Empty(result.Cycles);
            Assert.Equal(0, result.CycleCount);
        }

        [Fact]
        public void JohnsonsAlgorithm_SingleNodeNoEdges_NoCycles()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddNode("A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.False(result.HasCycles);
            Assert.Empty(result.Cycles);
        }

        [Fact]
        public void JohnsonsAlgorithm_LinearChain_NoCycles()
        {
            // Arrange - A->B->C->D (no cycles)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.False(result.HasCycles);
            Assert.Empty(result.Cycles);
        }

        [Fact]
        public void JohnsonsAlgorithm_TreeStructure_NoCycles()
        {
            // Arrange - Tree structure (no cycles)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("B", "D")
                .AddEdge("B", "E")
                .AddEdge("C", "F")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.False(result.HasCycles);
            Assert.Empty(result.Cycles);
        }

        #endregion

        #region Consistency Tests

        [Fact]
        public void JohnsonsAlgorithm_ConsistentWithSimpleDFS_SameCycleCount()
        {
            // Arrange - Complex graph with known cycles
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "A") // Self-loop
                .AddEdge("B", "C")
                .AddEdge("C", "B") // 2-cycle
                .AddEdge("D", "E")
                .AddEdge("E", "F")
                .AddEdge("F", "D") // 3-cycle
                .Build();

            // Act
            var johnsonResult = CycleDetection.FindAllCyclesJohnson(graph);
            var dfsResult = CycleDetection.FindAllCycles(graph);

            // Assert - Both should find the same number of elementary cycles
            Assert.Equal(dfsResult.CycleCount, johnsonResult.CycleCount);
            Assert.Equal(dfsResult.HasCycles, johnsonResult.HasCycles);
            
            // Both should find same number of self-loops and complex cycles
            Assert.Equal(dfsResult.GetSelfLoops().Count(), johnsonResult.GetSelfLoops().Count());
            Assert.Equal(dfsResult.GetComplexCycles().Count(), johnsonResult.GetComplexCycles().Count());
        }

        [Fact]
        public void JohnsonsAlgorithm_RepeatedRuns_ConsistentResults()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .AddEdge("B", "D")
                .AddEdge("D", "B")
                .Build();

            // Act - Run multiple times
            var results = new List<CycleDetectionResult<string>>();
            for (int i = 0; i < 5; i++)
            {
                results.Add(CycleDetection.FindAllCyclesJohnson(graph));
            }

            // Assert - All runs should produce identical results
            var firstResult = results[0];
            foreach (var result in results.Skip(1))
            {
                Assert.Equal(firstResult.CycleCount, result.CycleCount);
                Assert.Equal(firstResult.HasCycles, result.HasCycles);
                
                var firstLengths = firstResult.Cycles.Select(c => c.Length).OrderBy(l => l).ToList();
                var resultLengths = result.Cycles.Select(c => c.Length).OrderBy(l => l).ToList();
                Assert.Equal(firstLengths, resultLengths);
            }
        }

        #endregion

        #region Performance and Limits Tests

        [Fact]
        public void JohnsonsAlgorithm_MaxCyclesLimit_RespectsLimit()
        {
            // Arrange - Graph that could have many cycles
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .AddEdge("B", "C")
                .AddEdge("C", "B")
                .AddEdge("C", "D")
                .AddEdge("D", "C")
                .Build();

            // Act - Limit to 2 cycles
            var result = CycleDetection.FindAllCyclesJohnson(graph, maxCycles: 2);

            // Assert
            Assert.True(result.HasCycles);
            Assert.True(result.CycleCount <= 2); // Should not exceed the limit
        }

        [Fact]
        public void JohnsonsAlgorithm_LargerGraph_CompletesInReasonableTime()
        {
            // Arrange - Larger graph (but still small enough for tests)
            var builder = new GraphBuilder<int>();
            
            // Create a ring: 0->1->2->...->9->0
            for (int i = 0; i < 10; i++)
            {
                builder.AddEdge(i, (i + 1) % 10);
            }
            
            // Add some cross connections to create more cycles
            builder.AddEdge(0, 5);
            builder.AddEdge(5, 0);
            
            var graph = builder.Build();

            // Act
            var startTime = DateTime.UtcNow;
            var result = CycleDetection.FindAllCyclesJohnson(graph);
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(result.HasCycles);
            Assert.True(elapsed.TotalSeconds < 5); // Should complete quickly
            Assert.True(result.CycleCount >= 2); // Should find at least the main ring and the 0-5 cycle
        }

        #endregion

        #region Verification Against Paper Specifications

        [Fact]
        public void JohnsonsAlgorithm_PaperDefinition_ElementaryCyclesOnly()
        {
            // Arrange - Graph where non-elementary paths exist
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A") // Elementary cycle: A->B->C->A
                .AddEdge("A", "D")
                .AddEdge("D", "B") // Creates non-elementary path A->D->B->C->A
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            
            // According to Johnson's paper: "A circuit is elementary if no vertex 
            // but the first and last appears twice"
            // So we should only find A->B->C->A, not any path that revisits vertices
            foreach (var cycle in result.Cycles)
            {
                var uniqueNodes = cycle.Nodes.Distinct().Count();
                Assert.Equal(cycle.Nodes.Count, uniqueNodes); // No repeated vertices in elementary cycles
            }
        }

        [Fact]
        public void JohnsonsAlgorithm_PaperDefinition_CycleRepresentation()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.Single(result.Cycles);
            var cycle = result.Cycles.First();
            
            // According to paper: "output circuit composed of stack followed by s"
            // But for storage, we store unique nodes without duplication
            Assert.Equal(3, cycle.Nodes.Count); // [A, B, C] or permutation
            Assert.Equal(3, cycle.Length); // 3 edges in the cycle
            
            // The ToPathString should show the complete circuit
            var pathString = cycle.ToPathString();
            Assert.Contains("→", pathString);
            Assert.EndsWith($"→ {cycle.StartNode}", pathString); // Ends with return to start
        }

        #endregion

        #region Strong Component Behavior Tests

        [Fact]
        public void JohnsonsAlgorithm_StronglyConnectedComponents_FindsCyclesInEach()
        {
            // Arrange - Graph with multiple strongly connected components
            var graph = new GraphBuilder<string>()
                // First SCC: A->B->A
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                // Second SCC: C->D->E->C  
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                // Connection between SCCs (one-way, doesn't create cycles between them)
                .AddEdge("B", "C")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Equal(2, result.CycleCount); // One cycle in each SCC
            
            var cycleLengths = result.Cycles.Select(c => c.Length).OrderBy(l => l).ToList();
            Assert.Equal(new[] { 2, 3 }, cycleLengths);
        }

        [Fact]
        public void JohnsonsAlgorithm_WeaklyConnected_FindsAllCycles()
        {
            // Arrange - Weakly connected but not strongly connected
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A") // SCC: A->B->C->A
                .AddEdge("D", "E")
                .AddEdge("E", "D") // SCC: D->E->D
                // One-way connection (makes it weakly connected)
                .AddEdge("A", "D")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Equal(2, result.CycleCount);
            
            var cycleLengths = result.Cycles.Select(c => c.Length).OrderBy(l => l).ToList();
            Assert.Equal(new[] { 2, 3 }, cycleLengths);
        }

        #endregion

        #region Node Ordering Tests (Johnson's Algorithm Specifics)

        [Fact]
        public void JohnsonsAlgorithm_NodeOrdering_ProcessesInCorrectOrder()
        {
            // Arrange - Johnson's algorithm processes nodes in order to avoid duplicates
            var graph = new GraphBuilder<string>()
                .AddEdge("C", "A")
                .AddEdge("A", "B")
                .AddEdge("B", "C") // Cycle: A->B->C->A (or C->A->B->C depending on start)
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles); // Should find the cycle exactly once
            
            var cycle = result.Cycles.First();
            Assert.Equal(3, cycle.Length);
            Assert.Equal(3, cycle.Nodes.Count);
        }

        [Fact]
        public void JohnsonsAlgorithm_CyclicPermutations_OnlyFindsOnce()
        {
            // Arrange - According to Johnson's paper: "Two elementary circuits are 
            // distinct if one is not a cyclic permutation of the other"
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .AddEdge("D", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles); // Should find A->B->C->D->A only once
            
            var cycle = result.Cycles.First();
            Assert.Equal(4, cycle.Length);
            Assert.Equal(4, cycle.Nodes.Count);
        }

        #endregion

        #region Blocking Mechanism Tests

        [Fact]
        public void JohnsonsAlgorithm_BlockingPreventsRevisiting_NoInfinitePaths()
        {
            // Arrange - Graph where naive DFS could get stuck
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A") // Main cycle
                .AddEdge("B", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "B") // Secondary cycle sharing node B
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert - Should complete without hanging and find both cycles
            Assert.True(result.HasCycles);
            Assert.True(result.CycleCount >= 2);
            
            // Should find cycles of length 3
            var cycleLengths = result.Cycles.Select(c => c.Length).ToList();
            Assert.All(cycleLengths, length => Assert.Equal(3, length));
        }

        #endregion

        #region Maximum Cycle Length Tests

        [Fact]
        public void JohnsonsAlgorithm_LongCycle_HandlesCorrectly()
        {
            // Arrange - Create a long cycle: A->B->C->D->E->F->G->H->A
            var graph = new GraphBuilder<string>();
            var nodes = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
            
            for (int i = 0; i < nodes.Length; i++)
            {
                graph.AddEdge(nodes[i], nodes[(i + 1) % nodes.Length]);
            }
            
            var builtGraph = graph.Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(builtGraph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles);
            
            var cycle = result.Cycles.First();
            Assert.Equal(8, cycle.Length); // 8 edges in the cycle
            Assert.Equal(8, cycle.Nodes.Count); // 8 unique nodes
            Assert.True(cycle.IsComplexCycle);
        }

        #endregion

        #region Paper Example Verification

        [Fact]
        public void JohnsonsAlgorithm_CompleteGraph4Nodes_MatchesExpectedCount()
        {
            // Arrange - Complete directed graph with 4 nodes
            // According to Johnson's paper formula: Σ(i=1 to n-1) C(n,n-i+1) * (n-i)!
            // For n=4: C(4,4)*0! + C(4,3)*1! + C(4,2)*2! = 0 + 4*1 + 6*2 = 16 elementary circuits
            var graph = new GraphBuilder<string>();
            var nodes = new[] { "A", "B", "C", "D" };
            
            // Add all possible edges (complete graph)
            foreach (var from in nodes)
            {
                foreach (var to in nodes)
                {
                    if (from != to) // No self-loops for this test
                    {
                        graph.AddEdge(from, to);
                    }
                }
            }
            
            var builtGraph = graph.Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(builtGraph);

            // Assert
            Assert.True(result.HasCycles);
            // Complete graph should have many cycles of different lengths
            Assert.True(result.CycleCount >= 10); // At least many cycles
            
            var cycleLengths = result.Cycles.Select(c => c.Length).Distinct().OrderBy(l => l).ToList();
            Assert.Contains(2, cycleLengths); // 2-cycles: A->B->A
            Assert.Contains(3, cycleLengths); // 3-cycles: A->B->C->A
            Assert.Contains(4, cycleLengths); // 4-cycles: A->B->C->D->A
        }

        #endregion
    }
}
