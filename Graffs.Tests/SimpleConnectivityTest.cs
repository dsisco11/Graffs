using System;
using Graffs.Algorithms;
using Graffs.Builders;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    public class SimpleConnectivityTest
    {
        [Fact]
        public void SimpleReachabilityTest()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindReachableNodes(graph, "A");

            // Assert
            Assert.Equal(3, result.ReachableCount);
            Assert.Contains("A", result.ReachableNodes);
            Assert.Contains("B", result.ReachableNodes);
            Assert.Contains("C", result.ReachableNodes);
        }

        [Fact]
        public void SimpleSCCTest()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(1, result.ComponentCount);
            Assert.True(result.IsStronglyConnected);
        }
    }
}
