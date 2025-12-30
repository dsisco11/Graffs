using System;
using System.Collections.Generic;
using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Graffs;
using Graffs.Results;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    public class PathFindingTests
    {
        [Fact]
        public void FindShortestPath_EmptyGraph_ReturnsNoPath()
        {
            // Arrange
            var graph = new GraphBuilder<string>().Build();

            // Act
            var result = PathFinding.FindShortestPath(graph, "A", "B");

            // Assert
            Assert.False(result.PathExists);
            Assert.Equal("A", result.Source);
            Assert.Equal("B", result.Target);
            Assert.Equal(-1, result.Distance);
            Assert.Empty(result.Path);
        }

        [Fact]
        public void FindShortestPath_SameNode_ReturnsSelfPath()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddNode("A")
                .Build();

            // Act
            var result = PathFinding.FindShortestPath(graph, "A", "A");

            // Assert
            Assert.True(result.PathExists);
            Assert.Equal("A", result.Source);
            Assert.Equal("A", result.Target);
            Assert.Equal(0, result.Distance);
            Assert.Single(result.Path);
            Assert.Equal("A", result.Path[0]);
        }

        [Fact]
        public void FindShortestPath_DirectConnection_ReturnsDirectPath()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .Build();

            // Act
            var result = PathFinding.FindShortestPath(graph, "A", "B");

            // Assert
            Assert.True(result.PathExists);
            Assert.Equal("A", result.Source);
            Assert.Equal("B", result.Target);
            Assert.Equal(1, result.Distance);
            Assert.Equal(2, result.Path.Length);
            Assert.Equal("A", result.Path[0]);
            Assert.Equal("B", result.Path[1]);
        }

        [Fact]
        public void FindShortestPath_MultipleHops_ReturnsShortestPath()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("A", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                .Build();

            // Act
            var result = PathFinding.FindShortestPath(graph, "A", "C");

            // Assert
            Assert.True(result.PathExists);
            Assert.Equal("A", result.Source);
            Assert.Equal("C", result.Target);
            Assert.Equal(2, result.Distance); // A -> B -> C is shorter than A -> D -> E -> C
            Assert.Equal(3, result.Path.Length);
            Assert.Equal("A", result.Path[0]);
            Assert.Equal("B", result.Path[1]);
            Assert.Equal("C", result.Path[2]);
        }

        [Fact]
        public void FindShortestPath_NoPath_ReturnsNoPath()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = PathFinding.FindShortestPath(graph, "A", "D");

            // Assert
            Assert.False(result.PathExists);
            Assert.Equal("A", result.Source);
            Assert.Equal("D", result.Target);
            Assert.Equal(-1, result.Distance);
            Assert.Empty(result.Path);
        }

        [Fact]
        public void FindShortestPath_WithCycle_FindsPath()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "B") // Cycle
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = PathFinding.FindShortestPath(graph, "A", "D");

            // Assert
            Assert.True(result.PathExists);
            Assert.Equal("A", result.Source);
            Assert.Equal("D", result.Target);
            Assert.Equal(3, result.Distance);
            Assert.Equal(4, result.Path.Length);
            Assert.Equal("A", result.Path[0]);
            Assert.Equal("B", result.Path[1]);
            Assert.Equal("C", result.Path[2]);
            Assert.Equal("D", result.Path[3]);
        }

        [Fact]
        public void FindAllSimplePaths_EmptyGraph_ReturnsNoPaths()
        {
            // Arrange
            var graph = new GraphBuilder<string>().Build();

            // Act
            var result = PathFinding.FindAllSimplePaths(graph, "A", "B");

            // Assert
            Assert.Empty(result.Paths);
            Assert.Equal(-1, result.ShortestDistance);
            Assert.Equal(-1, result.LongestDistance);
        }

        [Fact]
        public void FindAllSimplePaths_SameNode_ReturnsSinglePath()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddNode("A")
                .Build();

            // Act
            var result = PathFinding.FindAllSimplePaths(graph, "A", "A");

            // Assert
            Assert.Single(result.Paths);
            Assert.Single(result.Paths[0]);
            Assert.Equal("A", result.Paths[0][0]);
            Assert.Equal(0, result.ShortestDistance);
            Assert.Equal(0, result.LongestDistance);
        }

        [Fact]
        public void FindAllSimplePaths_MultiplePaths_ReturnsAllPaths()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("B", "D")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = PathFinding.FindAllSimplePaths(graph, "A", "D");

            // Assert
            Assert.Equal(2, result.Paths.Length);
            Assert.Equal(2, result.ShortestDistance);
            Assert.Equal(2, result.LongestDistance);

            // Both paths should have same length (A->B->D and A->C->D)
            Assert.All(result.Paths, path => Assert.Equal(3, path.Length));
            Assert.All(result.Paths, path => Assert.Equal("A", path[0]));
            Assert.All(result.Paths, path => Assert.Equal("D", path[2]));
        }

        [Fact]
        public void FindAllSimplePaths_WithMaxLimit_RespectsLimit()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("A", "D")
                .AddEdge("B", "E")
                .AddEdge("C", "E")
                .AddEdge("D", "E")
                .Build();

            // Act
            var result = PathFinding.FindAllSimplePaths(graph, "A", "E", maxPaths: 2);

            // Assert
            Assert.True(result.Paths.Length <= 2);
        }

        [Fact]
        public void FindPath_WithDFS_FindsValidPath()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("A", "D")
                .AddEdge("D", "C")
                .Build();

            // Act
            var result = PathFinding.FindPath(graph, "A", "C");

            // Assert
            Assert.True(result.PathExists);
            Assert.Equal("A", result.Source);
            Assert.Equal("C", result.Target);
            Assert.True(result.Distance > 0);
            Assert.Equal("A", result.Path[0]);
            Assert.Equal("C", result.Path[result.Path.Length - 1]);
        }

        [Fact]
        public void CheckConnectivity_MultipleNodes_ReturnsCorrectMatrix()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("D", "E")
                .Build();

            var nodes = new[] { "A", "B", "C", "D", "E" };

            // Act
            var result = PathFinding.CheckConnectivity(graph, nodes);

            // Assert
            Assert.True(result[("A", "B")]);
            Assert.True(result[("A", "C")]);
            Assert.True(result[("B", "C")]);
            Assert.True(result[("D", "E")]);
            Assert.False(result[("A", "D")]);
            Assert.False(result[("C", "D")]);
            Assert.False(result[("E", "A")]);
        }

        [Fact]
        public void FindShortestPathsFromSource_SingleSource_ReturnsAllReachablePaths()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("B", "D")
                .AddEdge("C", "D")
                .AddEdge("E", "F") // Disconnected
                .Build();

            // Act
            var result = PathFinding.FindShortestPathsFromSource(graph, "A");

            // Assert
            Assert.Contains("B", result.Keys);
            Assert.Contains("C", result.Keys);
            Assert.Contains("D", result.Keys);
            Assert.DoesNotContain("E", result.Keys);
            Assert.DoesNotContain("F", result.Keys);

            Assert.True(result["B"].PathExists);
            Assert.True(result["C"].PathExists);
            Assert.True(result["D"].PathExists);
            Assert.Equal(1, result["B"].Distance);
            Assert.Equal(1, result["C"].Distance);
            Assert.Equal(2, result["D"].Distance);
        }

        [Fact]
        public void PathFinding_NullArguments_ThrowsArgumentNullException()
        {
            // Arrange
            var graph = new GraphBuilder<string>().Build();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => PathFinding.FindShortestPath<string>(null!, "A", "B"));
            Assert.Throws<ArgumentNullException>(() => PathFinding.FindShortestPath(graph, null!, "B"));
            Assert.Throws<ArgumentNullException>(() => PathFinding.FindShortestPath(graph, "A", null!));

            Assert.Throws<ArgumentNullException>(() => PathFinding.FindAllSimplePaths<string>(null!, "A", "B"));
            Assert.Throws<ArgumentNullException>(() => PathFinding.FindAllSimplePaths(graph, null!, "B"));
            Assert.Throws<ArgumentNullException>(() => PathFinding.FindAllSimplePaths(graph, "A", null!));

            Assert.Throws<ArgumentNullException>(() => PathFinding.FindPath<string>(null!, "A", "B"));
            Assert.Throws<ArgumentNullException>(() => PathFinding.FindPath(graph, null!, "B"));
            Assert.Throws<ArgumentNullException>(() => PathFinding.FindPath(graph, "A", null!));
        }

        [Fact]
        public void FindAllSimplePaths_InvalidMaxPaths_ThrowsArgumentException()
        {
            // Arrange
            var graph = new GraphBuilder<string>().Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => PathFinding.FindAllSimplePaths(graph, "A", "B", maxPaths: 0));
            Assert.Throws<ArgumentException>(() => PathFinding.FindAllSimplePaths(graph, "A", "B", maxPaths: -1));
        }

        [Fact]
        public void PathResult_Statistics_AreCorrect()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .Build();

            // Act
            var result = PathFinding.FindShortestPath(graph, "A", "C");

            // Assert
            Assert.True(result.Statistics.NodesExplored > 0);
            Assert.True(result.Statistics.IsShortestPath);
            Assert.False(result.Statistics.HasCycles);
            Assert.True(result.ProcessingTime >= TimeSpan.Zero);
            Assert.NotNull(result.Algorithm);
            Assert.True(result.CompletedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void PathFinding_PerformanceTest_HandlesLargeGraphs()
        {
            // Arrange - Create a larger graph
            var builder = new GraphBuilder<int>();
            for (int i = 0; i < 100; i++)
            {
                for (int j = i + 1; j < Math.Min(i + 5, 100); j++)
                {
                    builder.AddEdge(i, j);
                }
            }
            var graph = builder.Build();

            // Act
            var result = PathFinding.FindShortestPath(graph, 0, 99);

            // Assert
            Assert.True(result.PathExists);
            Assert.True(result.ProcessingTime.TotalSeconds < 1.0); // Should complete quickly
            Assert.True(result.Distance > 0);
        }
    }
}
