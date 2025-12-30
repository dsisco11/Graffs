using System;
using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Graffs;
using Graffs.Implementations;
using Graffs.Results;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    /// <summary>
    /// Comprehensive tests for cycle detection algorithms in directed graphs.
    /// </summary>
    public sealed class CycleDetectionTests
    {
        #region Basic Cycle Detection Tests

        [Fact]
        public void HasCycles_EmptyGraph_ReturnsFalse()
        {
            // Arrange
            var graph = new AdjacencyListGraph<string>(Array.Empty<IGraphEdge<string>>());

            // Act
            var hasCycles = CycleDetection.HasCycles(graph);

            // Assert
            Assert.False(hasCycles);
        }

        [Fact]
        public void HasCycles_SingleNodeNoSelfLoop_ReturnsFalse()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddNode("A")
                .Build();

            // Act
            var hasCycles = CycleDetection.HasCycles(graph);

            // Assert
            Assert.False(hasCycles);
        }

        [Fact]
        public void HasCycles_SingleNodeWithSelfLoop_ReturnsTrue()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddNode("A")
                .AddEdge("A", "A")
                .Build();

            // Act
            var hasCycles = CycleDetection.HasCycles(graph);

            // Assert
            Assert.True(hasCycles);
        }

        [Fact]
        public void HasCycles_SimpleTwoNodeCycle_ReturnsTrue()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .Build();

            // Act
            var hasCycles = CycleDetection.HasCycles(graph);

            // Assert
            Assert.True(hasCycles);
        }

        [Fact]
        public void HasCycles_SimpleThreeNodeCycle_ReturnsTrue()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .Build();

            // Act
            var hasCycles = CycleDetection.HasCycles(graph);

            // Assert
            Assert.True(hasCycles);
        }

        [Fact]
        public void HasCycles_AcyclicDAG_ReturnsFalse()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("B", "D")
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .Build();

            // Act
            var hasCycles = CycleDetection.HasCycles(graph);

            // Assert
            Assert.False(hasCycles);
        }

        [Fact]
        public void HasCycles_DisconnectedGraphWithCycle_ReturnsTrue()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                // First component: acyclic
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                // Second component: has cycle
                .AddEdge("X", "Y")
                .AddEdge("Y", "Z")
                .AddEdge("Z", "X")
                // Third component: isolated
                .AddNode("Isolated")
                .Build();

            // Act
            var hasCycles = CycleDetection.HasCycles(graph);

            // Assert
            Assert.True(hasCycles);
        }

        [Fact]
        public void HasCycles_DisconnectedGraphNoCycles_ReturnsFalse()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                // First component: acyclic
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                // Second component: acyclic
                .AddEdge("X", "Y")
                .AddEdge("Y", "Z")
                // Third component: isolated
                .AddNode("Isolated")
                .Build();

            // Act
            var hasCycles = CycleDetection.HasCycles(graph);

            // Assert
            Assert.False(hasCycles);
        }

        [Fact]
        public void HasCycles_NullGraph_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CycleDetection.HasCycles<string>(null!));
        }

        #endregion

        #region Advanced Cycle Detection Tests

        [Fact]
        public void FindAllCycles_EmptyGraph_ReturnsNoCycles()
        {
            // Arrange
            var graph = new AdjacencyListGraph<string>(Array.Empty<IGraphEdge<string>>());

            // Act
            var result = CycleDetection.FindAllCycles(graph);

            // Assert
            Assert.False(result.HasCycles);
            Assert.True(result.IsAcyclic);
            Assert.Empty(result.Cycles);
            Assert.Equal(0, result.CycleCount);
            Assert.Equal(0, result.Statistics.TotalCycles);
        }

        [Fact]
        public void FindAllCycles_SelfLoop_FindsCycle()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddNode("A")
                .AddEdge("A", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCycles(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.False(result.IsAcyclic);
            Assert.Single(result.Cycles);
            Assert.Equal(1, result.CycleCount);
            
            var cycle = result.Cycles.First();
            Assert.True(cycle.IsSelfLoop);
            Assert.False(cycle.IsComplexCycle);
            Assert.Equal(1, cycle.Length);
            Assert.Equal("A", cycle.StartNode);
            Assert.Equal("A → A", cycle.ToPathString());
            
            Assert.Equal(1, result.Statistics.TotalCycles);
            Assert.Equal(1, result.Statistics.SelfLoops);
            Assert.Equal(0, result.Statistics.ComplexCycles);
            Assert.True(result.HasOnlySelfLoops);
            Assert.False(result.HasComplexCycles);
        }

        [Fact]
        public void FindAllCycles_SimpleTwoNodeCycle_FindsCycle()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .Build();

            // Act
            var result = CycleDetection.FindAllCycles(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Single(result.Cycles);
            
            var cycle = result.Cycles.First();
            Assert.False(cycle.IsSelfLoop);
            Assert.True(cycle.IsComplexCycle);
            Assert.Equal(2, cycle.Length);
            Assert.True(cycle.ContainsNode("A"));
            Assert.True(cycle.ContainsNode("B"));
            
            Assert.Equal(1, result.Statistics.TotalCycles);
            Assert.Equal(0, result.Statistics.SelfLoops);
            Assert.Equal(1, result.Statistics.ComplexCycles);
            Assert.False(result.HasOnlySelfLoops);
            Assert.True(result.HasComplexCycles);
        }

        [Fact]
        public void FindAllCycles_MixedCycleTypes_FindsBothTypes()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                // Self-loop
                .AddEdge("A", "A")
                // Two-node cycle
                .AddEdge("B", "C")
                .AddEdge("C", "B")
                // Three-node cycle
                .AddEdge("X", "Y")
                .AddEdge("Y", "Z")
                .AddEdge("Z", "X")
                .Build();

            // Act
            var result = CycleDetection.FindAllCycles(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.Equal(3, result.CycleCount);
            
            var selfLoops = result.GetSelfLoops().ToList();
            var complexCycles = result.GetComplexCycles().ToList();
            
            Assert.Single(selfLoops);
            Assert.Equal(2, complexCycles.Count);
            
            Assert.Equal(3, result.Statistics.TotalCycles);
            Assert.Equal(1, result.Statistics.SelfLoops);
            Assert.Equal(2, result.Statistics.ComplexCycles);
            
            Assert.False(result.HasOnlySelfLoops);
            Assert.True(result.HasComplexCycles);
            
            // Check statistics
            Assert.Equal(1, result.Statistics.ShortestCycleLength);
            Assert.Equal(3, result.Statistics.LongestCycleLength);
            Assert.Equal(2.0, result.Statistics.AverageCycleLength, 1);
        }

        [Fact]
        public void FindAllCycles_AcyclicGraph_ReturnsNoCycles()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("B", "D")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = CycleDetection.FindAllCycles(graph);

            // Assert
            Assert.False(result.HasCycles);
            Assert.True(result.IsAcyclic);
            Assert.Empty(result.Cycles);
            Assert.Equal(0, result.CycleCount);
            Assert.False(result.HasOnlySelfLoops);
            Assert.False(result.HasComplexCycles);
        }

        [Fact]
        public void FindAllCycles_ComplexGraphWithMultipleCycles_FindsAllCycles()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                // First cycle: A → B → C → A
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                // Second cycle: C → D → E → C
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                // Self-loop on F
                .AddEdge("F", "F")
                // Acyclic part
                .AddEdge("G", "H")
                .Build();

            // Act
            var result = CycleDetection.FindAllCycles(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.True(result.CycleCount >= 3); // At least the three obvious cycles
            
            var selfLoops = result.GetSelfLoops().ToList();
            Assert.Single(selfLoops);
            Assert.Equal("F", selfLoops.First().StartNode);
            
            var complexCycles = result.GetComplexCycles().ToList();
            Assert.True(complexCycles.Count >= 2);
            
            // Check if we can find cycles containing specific nodes
            var cyclesWithA = result.GetCyclesContaining("A").ToList();
            Assert.NotEmpty(cyclesWithA);
            
            var cyclesWithC = result.GetCyclesContaining("C").ToList();
            Assert.NotEmpty(cyclesWithC);
            
            // Node G should not be in any cycle
            var cyclesWithG = result.GetCyclesContaining("G").ToList();
            Assert.Empty(cyclesWithG);
        }

        [Fact]
        public void FindAllCycles_NullGraph_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CycleDetection.FindAllCycles<string>(null!));
        }

        #endregion

        #region Quick Cycle Check Tests

        [Fact]
        public void QuickCycleCheck_AcyclicGraph_ReturnsCorrectResult()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .Build();

            // Act
            var result = CycleDetection.QuickCycleCheck(graph);

            // Assert
            Assert.False(result.HasCycles);
            Assert.True(result.IsAcyclic);
            Assert.Empty(result.Cycles);
            Assert.Equal("Quick DFS Cycle Check", result.Algorithm);
            Assert.True(result.ProcessingTimeMs >= 0);
        }

        [Fact]
        public void QuickCycleCheck_CyclicGraph_ReturnsCorrectResult()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .Build();

            // Act
            var result = CycleDetection.QuickCycleCheck(graph);

            // Assert
            Assert.True(result.HasCycles);
            Assert.False(result.IsAcyclic);
            Assert.Empty(result.Cycles); // Quick check doesn't enumerate cycles
            Assert.Equal("Quick DFS Cycle Check", result.Algorithm);
            Assert.True(result.ProcessingTimeMs >= 0);
        }

        #endregion

        #region Edge Cases and Stress Tests

        [Fact]
        public void CycleDetection_LargeAcyclicGraph_PerformanceTest()
        {
            // Arrange: Create a large DAG
            var builder = new GraphBuilder<int>();
            
            // Create a layered DAG: 100 layers with 10 nodes each
            for (int layer = 0; layer < 100; layer++)
            {
                for (int node = 0; node < 10; node++)
                {
                    int currentNode = layer * 10 + node;
                    builder.AddNode(currentNode);
                    
                    // Connect to next layer if not last layer
                    if (layer < 99)
                    {
                        for (int nextNode = 0; nextNode < 10; nextNode++)
                        {
                            int targetNode = (layer + 1) * 10 + nextNode;
                            builder.AddEdge(currentNode, targetNode);
                        }
                    }
                }
            }
            
            var graph = builder.Build();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var hasCycles = CycleDetection.HasCycles(graph);
            stopwatch.Stop();

            // Assert
            Assert.False(hasCycles);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should complete within 1 second
        }

        [Fact]
        public void CycleDetection_DifferentNodeTypes_WorksCorrectly()
        {
            // Test with integer nodes
            var intBuilder = new GraphBuilder<int>();
            var intGraph = intBuilder
                .AddEdge(1, 2)
                .AddEdge(2, 3)
                .AddEdge(3, 1)
                .Build();

            Assert.True(CycleDetection.HasCycles(intGraph));

            // Test with custom object nodes
            var customBuilder = new GraphBuilder<TestNode>();
            var node1 = new TestNode("Node1");
            var node2 = new TestNode("Node2");
            var node3 = new TestNode("Node3");
            
            var customGraph = customBuilder
                .AddEdge(node1, node2)
                .AddEdge(node2, node3)
                .AddEdge(node3, node1)
                .Build();

            Assert.True(CycleDetection.HasCycles(customGraph));
        }

        #endregion

        #region Result Type Tests

        [Fact]
        public void CycleStatistics_Empty_HasCorrectProperties()
        {
            // Arrange & Act
            var stats = CycleStatistics.Empty;

            // Assert
            Assert.Equal(0, stats.TotalCycles);
            Assert.Equal(0, stats.SelfLoops);
            Assert.Equal(0, stats.ComplexCycles);
            Assert.Equal(0, stats.ShortestCycleLength);
            Assert.Equal(0, stats.LongestCycleLength);
            Assert.Equal(0, stats.AverageCycleLength);
            Assert.False(stats.HasCycles);
            Assert.Equal(0, stats.SelfLoopPercentage);
            Assert.Equal(0, stats.ComplexCyclePercentage);
            Assert.Empty(stats.CycleLengthDistribution);
        }

        [Fact]
        public void GraphCycle_Properties_WorkCorrectly()
        {
            // Arrange
            var nodes = new[] { "A", "B", "C" };
            var cycle = new GraphCycle<string>(nodes, 3, DateTime.UtcNow);

            // Assert
            Assert.False(cycle.IsSelfLoop);
            Assert.True(cycle.IsComplexCycle);
            Assert.Equal(3, cycle.Length);
            Assert.Equal("A", cycle.StartNode);
            Assert.True(cycle.ContainsNode("B"));
            Assert.False(cycle.ContainsNode("D"));
            Assert.Equal("A → B → C → A", cycle.ToPathString());
        }

        #endregion

        #region Helper Classes

        private sealed record TestNode(string Name)
        {
            public override string ToString() => Name;
        }

        #endregion
    }
}
