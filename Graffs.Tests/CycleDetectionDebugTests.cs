using System;
using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    public class CycleDetectionDebugTests
    {
        [Fact]
        public void DebugCycleLength_SimpleCycle()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A") // Simple 2-node cycle
                .Build();

            // Act - Test both algorithms
            var regularResult = CycleDetection.FindAllCycles(graph);

            // Debug output - Check what the regular algorithm produces
            Assert.True(regularResult.HasCycles);
            var cycle = regularResult.Cycles.First();
            
            // What does the DFS algorithm produce for A→B→A?
            // This cycle has 2 nodes and 2 edges
            Assert.Equal(2, cycle.Nodes.Count); // Should be A, B
            // What is cycle.Length? Let's see what the test expects...
        }

        [Fact]
        public void DebugCycleLength_SelfLoop()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "A")
                .Build();

            // Act
            var regularResult = CycleDetection.FindAllCycles(graph);
            var johnsonResult = CycleDetection.FindAllCyclesJohnson(graph);

            // Debug - Check what both produce for self-loops
            Assert.True(regularResult.HasCycles || johnsonResult.HasCycles);
        }
    }
}
