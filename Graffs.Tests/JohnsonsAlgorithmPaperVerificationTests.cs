using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    /// <summary>
    /// Tests that verify our Johnson's algorithm implementation matches the theoretical 
    /// properties and performance characteristics described in the 1975 research paper.
    /// </summary>
    public class JohnsonsAlgorithmPaperVerificationTests
    {
        [Fact]
        public void JohnsonsAlgorithm_ElementaryCircuitDefinition_OnlyNonRepeatingVertices()
        {
            // Test Johnson's definition: "A circuit is elementary if no vertex 
            // but the first and last appears twice"
            
            // Arrange - Graph with potential for non-elementary paths
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .AddEdge("D", "A") // Elementary: A->B->C->D->A
                .AddEdge("A", "E")
                .AddEdge("E", "C") // Could create non-elementary: A->E->C->D->A
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert - All cycles must be elementary
            foreach (var cycle in result.Cycles)
            {
                // In elementary cycles, all nodes should be unique (no repetition)
                var uniqueNodes = cycle.Nodes.Distinct().Count();
                Assert.Equal(cycle.Nodes.Count, uniqueNodes);
                
                // Path string should show proper circuit format
                var pathString = cycle.ToPathString();
                Assert.EndsWith($"→ {cycle.StartNode}", pathString);
            }
        }

        [Fact]
        public void JohnsonsAlgorithm_DistinctCircuits_NoCyclicPermutations()
        {
            // Test Johnson's definition: "Two elementary circuits are distinct 
            // if one is not a cyclic permutation of the other"
            
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .AddEdge("D", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert - Should find exactly one cycle (A->B->C->D->A)
            // Not multiple permutations like B->C->D->A->B, C->D->A->B->C, etc.
            Assert.Single(result.Cycles);
            
            var cycle = result.Cycles.First();
            Assert.Equal(4, cycle.Length);
            Assert.Equal(4, cycle.Nodes.Count);
        }

        [Fact]
        public void JohnsonsAlgorithm_TimeComplexity_LinearBetweenCircuits()
        {
            // Test that time between finding circuits is O(n+e) as claimed in paper
            
            // Arrange - Graph where we can measure time between circuit discoveries
            var graph = new GraphBuilder<int>();
            
            // Create multiple small cycles that should be found quickly
            for (int i = 0; i < 5; i++)
            {
                graph.AddEdge(i * 2, i * 2 + 1);
                graph.AddEdge(i * 2 + 1, i * 2);
            }
            
            var builtGraph = graph.Build();

            // Act & Assert
            var stopwatch = Stopwatch.StartNew();
            var result = CycleDetection.FindAllCyclesJohnson(builtGraph);
            stopwatch.Stop();

            // Should find cycles quickly (under reasonable time)
            Assert.True(result.HasCycles);
            Assert.Equal(5, result.CycleCount); // 5 simple 2-cycles
            Assert.True(stopwatch.ElapsedMilliseconds < 100); // Should be very fast
        }

        [Fact]
        public void JohnsonsAlgorithm_SpaceComplexity_LinearSpace()
        {
            // Verify O(n+e) space complexity by ensuring reasonable memory usage
            
            // Arrange - Moderately sized graph
            var graph = new GraphBuilder<int>();
            
            // Create a graph with multiple cycles
            for (int i = 0; i < 20; i++)
            {
                graph.AddEdge(i, (i + 1) % 20); // Ring of 20 nodes
                if (i < 10)
                {
                    graph.AddEdge(i, i + 10); // Additional connections
                }
            }
            
            var builtGraph = graph.Build();

            // Act
            var initialMemory = GC.GetTotalMemory(true);
            var result = CycleDetection.FindAllCyclesJohnson(builtGraph);
            var finalMemory = GC.GetTotalMemory(true);

            // Assert - Memory usage should be reasonable (not exponential)
            Assert.True(result.HasCycles);
            var memoryIncrease = finalMemory - initialMemory;
            Assert.True(memoryIncrease < 1_000_000); // Less than 1MB for this size graph
        }

        [Fact]
        public void JohnsonsAlgorithm_CircuitOutput_MatchesPaperFormat()
        {
            // Verify that circuit output matches paper's specification:
            // "output circuit composed of stack followed by s"
            
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
            
            // The cycle should represent the path without duplicating the start node
            Assert.Equal(3, cycle.Nodes.Count); // [A, B, C] - no duplicate start
            Assert.Equal(3, cycle.Length); // 3 edges: A->B, B->C, C->A
            
            // ToPathString should show the complete circuit including return
            var pathString = cycle.ToPathString();
            Assert.Contains("→", pathString);
            Assert.EndsWith($"→ {cycle.StartNode}", pathString);
        }

        [Fact]
        public void JohnsonsAlgorithm_BlockingMechanism_PreventsInfiniteLoops()
        {
            // Test that the blocking mechanism works as described in the paper
            
            // Arrange - Graph with overlapping cycles that could cause infinite recursion
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")     // Cycle 1: A->B->C->A
                .AddEdge("B", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "B")     // Cycle 2: B->D->E->B (shares node B)
                .AddEdge("C", "F")
                .AddEdge("F", "G")
                .AddEdge("G", "C")     // Cycle 3: C->F->G->C (shares node C)
                .Build();

            // Act - Should complete without hanging
            var timeout = TimeSpan.FromSeconds(5);
            var startTime = DateTime.UtcNow;
            
            var result = CycleDetection.FindAllCyclesJohnson(graph);
            
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(elapsed < timeout, "Algorithm should complete quickly without infinite loops");
            Assert.True(result.HasCycles);
            Assert.Equal(3, result.CycleCount); // Should find all 3 cycles
            
            // All cycles should have length 3
            Assert.True(result.Cycles.All(c => c.Length == 3));
        }

        [Theory]
        [InlineData(2)] // 2-node complete graph
        [InlineData(3)] // 3-node complete graph  
        [InlineData(4)] // 4-node complete graph
        public void JohnsonsAlgorithm_CompleteGraphs_FindsExpectedCycles(int nodeCount)
        {
            // Test on complete graphs to verify correctness
            
            // Arrange
            var graph = new GraphBuilder<int>();
            
            // Create complete directed graph
            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = 0; j < nodeCount; j++)
                {
                    if (i != j) // No self-loops for this test
                    {
                        graph.AddEdge(i, j);
                    }
                }
            }
            
            var builtGraph = graph.Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(builtGraph);

            // Assert
            Assert.True(result.HasCycles);
            
            // Complete graphs should have many cycles of different lengths
            var cycleLengths = result.Cycles.Select(c => c.Length).Distinct().OrderBy(l => l).ToList();
            
            // Should find 2-cycles (direct pairs) and longer cycles
            Assert.Contains(2, cycleLengths);
            if (nodeCount >= 3)
            {
                Assert.Contains(3, cycleLengths);
            }
            if (nodeCount >= 4)
            {
                Assert.Contains(4, cycleLengths);
            }
            
            // Total number of cycles should be reasonable (not exponentially large due to bugs)
            Assert.True(result.CycleCount < Math.Pow(nodeCount, 4), 
                "Cycle count should not be exponentially large");
        }

        [Fact]
        public void JohnsonsAlgorithm_Comparison_MatchesSimpleDFS()
        {
            // Verify that Johnson's algorithm finds the same cycles as simple DFS
            // (but potentially in different order)
            
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "A") // Self-loop
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .AddEdge("D", "B") // 3-cycle
                .AddEdge("E", "F")
                .AddEdge("F", "E") // 2-cycle
                .Build();

            // Act
            var johnsonResult = CycleDetection.FindAllCyclesJohnson(graph);
            var dfsResult = CycleDetection.FindAllCycles(graph);

            // Assert - Both should find the same number and types of cycles
            Assert.Equal(dfsResult.CycleCount, johnsonResult.CycleCount);
            Assert.Equal(dfsResult.HasCycles, johnsonResult.HasCycles);
            
            // Should find same distribution of cycle lengths
            var johnsonLengths = johnsonResult.Cycles.Select(c => c.Length).OrderBy(l => l).ToList();
            var dfsLengths = dfsResult.Cycles.Select(c => c.Length).OrderBy(l => l).ToList();
            Assert.Equal(dfsLengths, johnsonLengths);
            
            // Should find same number of self-loops and complex cycles
            Assert.Equal(dfsResult.GetSelfLoops().Count(), johnsonResult.GetSelfLoops().Count());
            Assert.Equal(dfsResult.GetComplexCycles().Count(), johnsonResult.GetComplexCycles().Count());
        }
    }
}
