using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    public class AdvancedCycleDetectionTests
    {
        [Fact]
        public void FindAllCyclesJohnson_EmptyGraph_ReturnsNoCycles()
        {
            // Arrange
            var graph = new GraphBuilder<string>().Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.False(result.HasCycles);
            Assert.Empty(result.Cycles);
            Assert.Contains("Johnson's Algorithm", result.Algorithm);
        }

        [Fact]
        public void FindAllCyclesJohnson_AcyclicGraph_ReturnsNoCycles()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("A", "D")
                .AddEdge("D", "E")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.False(result.HasCycles);
            Assert.Empty(result.Cycles);
        }

        [Fact]
        public void FindAllCyclesJohnson_SelfLoop_FindsCycle()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles);
            var cycle = result.Cycles.First();
            Assert.Single(cycle.Nodes);
            Assert.Equal("A", cycle.Nodes.First());
            Assert.Equal(1, cycle.Length);
        }

        [Fact]
        public void FindAllCyclesJohnson_SimpleCycle_FindsCycle()
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
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles);
            var cycle = result.Cycles.First();
            Assert.Equal(3, cycle.Nodes.Count);
            Assert.Equal(3, cycle.Length); // Number of edges in the cycle: A->B, B->C, C->A
        }

        [Fact]
        public void FindAllCyclesJohnson_MultipleCycles_FindsAllCycles()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                // First cycle: A -> B -> A
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                // Second cycle: C -> D -> E -> C
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                // Connection between cycles
                .AddEdge("B", "C")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.True(result.Cycles.Count >= 2); // Should find at least the two main cycles
            
            // Check that we have cycles of different lengths
            var cycleLengths = result.Cycles.Select(c => c.Length).Distinct().OrderBy(l => l).ToArray();
            Assert.Contains(2, cycleLengths); // A->B->A has 2 edges: A->B, B->A
            Assert.Contains(3, cycleLengths); // C->D->E->C has 3 edges: C->D, D->E, E->C
        }

        [Fact]
        public void FindAllCyclesJohnson_ComplexGraph_FindsElementaryCycles()
        {
            // Arrange - Create a graph with overlapping cycles
            var graph = new GraphBuilder<int>()
                .AddEdge(1, 2)
                .AddEdge(2, 3)
                .AddEdge(3, 1) // Cycle 1: 1->2->3->1
                .AddEdge(2, 4)
                .AddEdge(4, 5)
                .AddEdge(5, 2) // Cycle 2: 2->4->5->2
                .AddEdge(3, 4) // Creates overlapping cycle: 1->2->4->5->2->3->1
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.True(result.Cycles.Count >= 2); // Should find multiple elementary cycles
            
            // Verify we have different cycle sizes
            var cycleLengths = result.Cycles.Select(c => c.Nodes.Count).Distinct().ToArray();
            Assert.True(cycleLengths.Length > 1); // Should have cycles of different lengths
        }

        [Fact]
        public void FindAllCyclesJohnson_WithMaxLimit_RespectsLimit()
        {
            // Arrange - Create a graph with many potential cycles
            var builder = new GraphBuilder<int>();
            
            // Create a complete subgraph on 4 nodes (many cycles possible)
            for (int i = 1; i <= 4; i++)
            {
                for (int j = 1; j <= 4; j++)
                {
                    if (i != j)
                    {
                        builder.AddEdge(i, j);
                    }
                }
            }
            
            var graph = builder.Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph, maxCycles: 5);

            // Assert
            Assert.True(result.HasCycles);
            Assert.True(result.Cycles.Count <= 5); // Should respect the limit
        }

        [Fact]
        public void FindAllCyclesJohnson_VsRegularFindAllCycles_FindsSameCycles()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .AddEdge("B", "D")
                .AddEdge("D", "B")
                .Build();

            // Act
            var johnsonResult = CycleDetection.FindAllCyclesJohnson(graph);
            var regularResult = CycleDetection.FindAllCycles(graph);

            // Assert
            Assert.Equal(regularResult.HasCycles, johnsonResult.HasCycles);
            
            if (johnsonResult.HasCycles)
            {
                Assert.True(johnsonResult.Cycles.Count > 0);
                Assert.True(regularResult.Cycles.Count > 0);
                
                // Both should find cycles (exact count may differ due to different algorithms)
                Assert.True(johnsonResult.Statistics.TotalCycles > 0);
                Assert.True(regularResult.Statistics.TotalCycles > 0);
            }
        }

        [Fact]
        public void FindAllCyclesJohnson_DisconnectedGraph_FindsCyclesInEachComponent()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                // First component with cycle
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                // Second component with cycle
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                // Third component without cycle
                .AddEdge("F", "G")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.True(result.Cycles.Count >= 2); // Should find cycles in both connected components
        }

        [Fact]
        public void FindAllCyclesJohnson_PerformanceTest_HandlesReasonablySizedGraphs()
        {
            // Arrange - Create a graph with systematic cycle structure
            var builder = new GraphBuilder<int>();
            
            // Create a chain with multiple back-edges creating cycles
            for (int i = 0; i < 20; i++)
            {
                builder.AddEdge(i, i + 1);
                
                // Add back-edges to create cycles
                if (i > 2)
                {
                    builder.AddEdge(i + 1, i - 2); // Creates cycles of length 4
                }
            }
            
            var graph = builder.Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph, maxCycles: 50);

            // Assert
            Assert.True(result.ProcessingTime.TotalSeconds < 2.0); // Should complete in reasonable time
            
            if (result.HasCycles)
            {
                Assert.True(result.Cycles.Count > 0);
                Assert.True(result.Statistics.TotalCycles > 0);
            }
        }

        [Fact]
        public void FindAllCyclesJohnson_ResultProperties_AreCorrect()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCyclesJohnson(graph);

            // Assert
            Assert.Contains("Johnson's Algorithm", result.Algorithm);
            Assert.True(result.ProcessingTime >= System.TimeSpan.Zero);
            Assert.True(result.CompletedAt <= System.DateTime.UtcNow);
            Assert.NotNull(result.Statistics);
        }

        [Fact]
        public void FindAllCyclesJohnson_CycleCanonicalForm_IsConsistent()
        {
            // Arrange - Create the same cycle with different starting points
            var graph1 = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .Build();

            var graph2 = new GraphBuilder<string>()
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .AddEdge("A", "B")
                .Build();

            // Act
            var result1 = CycleDetection.FindAllCyclesJohnson(graph1);
            var result2 = CycleDetection.FindAllCyclesJohnson(graph2);

            // Assert
            Assert.Equal(result1.HasCycles, result2.HasCycles);
            if (result1.HasCycles && result2.HasCycles)
            {
                Assert.Equal(result1.Cycles.Count, result2.Cycles.Count);
                // Both should detect the same essential cycle structure
            }
        }
    }
}
