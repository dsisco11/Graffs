using System;
using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Graffs.Results;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    public class AdvancedConnectivityTests
    {
        [Fact]
        public void FindStronglyConnectedComponents_EmptyGraph_ReturnsEmpty()
        {
            // Arrange
            var graph = new GraphBuilder<string>().Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Empty(result.Components);
            Assert.Equal(0, result.Statistics.TotalComponents);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(0, result.Statistics.NonTrivialComponents);
            Assert.Equal(0, result.Statistics.LargestComponentSize);
        }

        [Fact]
        public void FindStronglyConnectedComponents_SingleNode_ReturnsTrivialComponent()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddNode("A")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Single(result.Components);
            var component = result.Components.First();
            Assert.True(component.IsTrivial);
            Assert.Single(component.Nodes);
            Assert.Equal("A", component.Nodes.First());
            Assert.Equal(1, result.Statistics.TrivialComponents);
            Assert.Equal(0, result.Statistics.NonTrivialComponents);
        }

        [Fact]
        public void FindStronglyConnectedComponents_SimpleChain_ReturnsTrivialComponents()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(3, result.Components.Length);
            Assert.All(result.Components, c => Assert.True(c.IsTrivial));
            Assert.Equal(3, result.Statistics.TrivialComponents);
            Assert.Equal(0, result.Statistics.NonTrivialComponents);
        }

        [Fact]
        public void FindStronglyConnectedComponents_SimpleCycle_ReturnsNonTrivialComponent()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Single(result.Components);
            var component = result.Components.First();
            Assert.False(component.IsTrivial);
            Assert.Equal(3, component.Size);
            Assert.Contains("A", component.Nodes);
            Assert.Contains("B", component.Nodes);
            Assert.Contains("C", component.Nodes);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(1, result.Statistics.NonTrivialComponents);
            Assert.Equal(3, result.Statistics.LargestComponentSize);
        }

        [Fact]
        public void FindStronglyConnectedComponents_SelfLoop_ReturnsNonTrivialComponent()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "A")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Single(result.Components);
            var component = result.Components.First();
            Assert.False(component.IsTrivial);
            Assert.Single(component.Nodes);
            Assert.Equal("A", component.Nodes.First());
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(1, result.Statistics.NonTrivialComponents);
        }

        [Fact]
        public void FindStronglyConnectedComponents_MixedComponents_ReturnsCorrectClassification()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                // First SCC: A <-> B
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                // Second SCC: C -> D -> E -> C
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                // Connection between SCCs
                .AddEdge("B", "C")
                // Isolated node
                .AddEdge("F", "G")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(4, result.Components.Length);
            
            var nonTrivialComponents = result.Components.Where(c => !c.IsTrivial).ToList();
            var trivialComponents = result.Components.Where(c => c.IsTrivial).ToList();
            
            Assert.Equal(2, nonTrivialComponents.Count);
            Assert.Equal(2, trivialComponents.Count);
            
            Assert.Equal(2, result.Statistics.TrivialComponents);
            Assert.Equal(2, result.Statistics.NonTrivialComponents);
            Assert.Equal(3, result.Statistics.LargestComponentSize);
        }

        [Fact]
        public void FindStronglyConnectedComponents_ComplexGraph_HandlesCorrectly()
        {
            // Arrange - Classic example graph
            var graph = new GraphBuilder<int>()
                .AddEdge(1, 2)
                .AddEdge(2, 3)
                .AddEdge(3, 1)
                .AddEdge(2, 4)
                .AddEdge(4, 5)
                .AddEdge(5, 6)
                .AddEdge(6, 4)
                .AddEdge(6, 7)
                .AddEdge(7, 8)
                .AddEdge(8, 7)
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(3, result.Components.Length);
            Assert.All(result.Components, c => Assert.False(c.IsTrivial));
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(3, result.Statistics.NonTrivialComponents);
            
            // Should have components of sizes 3, 3, and 2
            var sizes = result.Components.Select(c => c.Size).OrderBy(s => s).ToArray();
            Assert.Equal(new[] { 2, 3, 3 }, sizes);
        }

        [Fact]
        public void FindStronglyConnectedComponents_DisconnectedGraph_HandlesCorrectly()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                // First component
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                // Second component (isolated)
                .AddEdge("C", "D")
                .AddEdge("D", "C")
                // Third component (single node)
                .AddNode("E")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(3, result.Components.Length);
            
            var nonTrivialComponents = result.Components.Where(c => !c.IsTrivial).ToList();
            var trivialComponents = result.Components.Where(c => c.IsTrivial).ToList();
            
            Assert.Equal(2, nonTrivialComponents.Count);
            Assert.Single(trivialComponents);
            
            Assert.Equal(1, result.Statistics.TrivialComponents);
            Assert.Equal(2, result.Statistics.NonTrivialComponents);
        }

        [Fact]
        public void FindStronglyConnectedComponents_PerformanceTest_HandlesLargeGraphs()
        {
            // Arrange - Create a larger graph with multiple SCCs
            var builder = new GraphBuilder<int>();
            
            // Create several strongly connected components
            for (int component = 0; component < 10; component++)
            {
                int baseNode = component * 10;
                // Create a cycle within each component
                for (int i = 0; i < 10; i++)
                {
                    builder.AddEdge(baseNode + i, baseNode + ((i + 1) % 10));
                }
            }
            
            // Add some connections between components
            for (int i = 0; i < 9; i++)
            {
                builder.AddEdge((i * 10) + 5, ((i + 1) * 10) + 2);
            }
            
            var graph = builder.Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(10, result.Components.Length);
            Assert.All(result.Components, c => Assert.False(c.IsTrivial));
            Assert.All(result.Components, c => Assert.Equal(10, c.Size));
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(10, result.Statistics.NonTrivialComponents);
            Assert.Equal(10, result.Statistics.LargestComponentSize);
            Assert.True(result.ProcessingTime.TotalSeconds < 1.0); // Should complete quickly
        }

        [Fact]
        public void HasPath_ConnectedNodes_ReturnsTrue()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .Build();

            // Act & Assert
            Assert.True(ConnectivityAnalysis.HasPath(graph, "A", "B"));
            Assert.True(ConnectivityAnalysis.HasPath(graph, "A", "C"));
            Assert.True(ConnectivityAnalysis.HasPath(graph, "B", "C"));
        }

        [Fact]
        public void HasPath_DisconnectedNodes_ReturnsFalse()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("C", "D")
                .Build();

            // Act & Assert
            Assert.False(ConnectivityAnalysis.HasPath(graph, "A", "C"));
            Assert.False(ConnectivityAnalysis.HasPath(graph, "B", "D"));
            Assert.False(ConnectivityAnalysis.HasPath(graph, "D", "A"));
        }

        [Fact]
        public void HasPath_SameNode_ReturnsTrue()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddNode("A")
                .Build();

            // Act & Assert
            Assert.True(ConnectivityAnalysis.HasPath(graph, "A", "A"));
        }

        [Fact]
        public void FindNodesReaching_TargetNode_ReturnsCorrectNodes()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "C")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .AddEdge("E", "F")
                .Build();

            // Act
            var reachingC = ConnectivityAnalysis.FindNodesReaching(graph, "C");
            var reachingD = ConnectivityAnalysis.FindNodesReaching(graph, "D");

            // Assert
            Assert.Contains("A", reachingC);
            Assert.Contains("B", reachingC);
            Assert.Contains("C", reachingC); // Target includes itself
            Assert.DoesNotContain("D", reachingC);
            Assert.DoesNotContain("E", reachingC);
            Assert.DoesNotContain("F", reachingC);

            Assert.Contains("A", reachingD);
            Assert.Contains("B", reachingD);
            Assert.Contains("C", reachingD);
            Assert.Contains("D", reachingD); // Target includes itself
            Assert.DoesNotContain("E", reachingD);
            Assert.DoesNotContain("F", reachingD);
        }

        [Fact]
        public void ConnectivityAnalysis_NullArguments_ThrowsArgumentNullException()
        {
            // Arrange
            var graph = new GraphBuilder<string>().Build();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ConnectivityAnalysis.FindStronglyConnectedComponents<string>(null!));
            Assert.Throws<ArgumentNullException>(() => ConnectivityAnalysis.HasPath<string>(null!, "A", "B"));
            Assert.Throws<ArgumentNullException>(() => ConnectivityAnalysis.HasPath(graph, null!, "B"));
            Assert.Throws<ArgumentNullException>(() => ConnectivityAnalysis.HasPath(graph, "A", null!));
            Assert.Throws<ArgumentNullException>(() => ConnectivityAnalysis.FindNodesReaching<string>(null!, "A"));
            Assert.Throws<ArgumentNullException>(() => ConnectivityAnalysis.FindNodesReaching(graph, null!));
        }

        [Fact]
        public void FindStronglyConnectedComponents_ResultProperties_AreCorrect()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.NotNull(result.Algorithm);
            Assert.True(result.ProcessingTime >= TimeSpan.Zero);
            Assert.True(result.CompletedAt <= DateTime.UtcNow);
            Assert.NotNull(result.Statistics);
        }
    }
}
