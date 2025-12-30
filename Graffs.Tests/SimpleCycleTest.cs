using System;
using Graffs.Algorithms;
using Graffs.Builders;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    public class SimpleCycleTest
    {
        [Fact]
        public void SimpleHasCyclesTest()
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
    }
}
