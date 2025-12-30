using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Graffs.Builders;
using Graffs;
using Graffs.Implementations;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs
{
    /// <summary>
    /// Comprehensive tests for all graph implementations to ensure behavioral equivalence.
    /// </summary>
    public sealed class GraphImplementationTests
    {
        #region Test Data Generation

        private static IEnumerable<IGraphEdge<string>> CreateSimpleTestEdges()
        {
            return new[]
            {
                new GraphEdge<string>("A", "B"),
                new GraphEdge<string>("B", "C"),
                new GraphEdge<string>("A", "C"),
                new GraphEdge<string>("D", "C")
            };
        }

        private static IEnumerable<IGraphEdge<int>> CreateDenseTestEdges(int nodeCount)
        {
            var edges = new List<IGraphEdge<int>>();
            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = 0; j < nodeCount; j++)
                {
                    if (i != j && (i + j) % 3 == 0) // Create a dense but not complete graph
                    {
                        edges.Add(new GraphEdge<int>(i, j));
                    }
                }
            }
            return edges;
        }

        private static IEnumerable<IGraphEdge<int>> CreateSparseTestEdges(int nodeCount)
        {
            var edges = new List<IGraphEdge<int>>();
            for (int i = 0; i < nodeCount - 1; i++)
            {
                edges.Add(new GraphEdge<int>(i, i + 1)); // Linear chain
                if (i % 5 == 0 && i + 5 < nodeCount) // Some additional sparse connections
                {
                    edges.Add(new GraphEdge<int>(i, i + 5));
                }
            }
            return edges;
        }

        #endregion

        #region Interface Compliance Tests

        [Fact]
        public void AllImplementations_EmptyGraph_BehaveSameWay()
        {
            // Arrange
            var emptyEdges = Array.Empty<IGraphEdge<string>>();
            var adjacencyList = new AdjacencyListGraph<string>(emptyEdges);
            var adjacencyMatrix = new AdjacencyMatrixGraph<string>(emptyEdges);
            var compact = new CompactGraph<string>(emptyEdges);

            var implementations = new IDirectedGraph<string>[] { adjacencyList, adjacencyMatrix, compact };

            // Act & Assert
            foreach (var graph in implementations)
            {
                Assert.Empty(graph.Nodes);
                Assert.Empty(graph.Edges);
            }
        }

        [Fact]
        public void AllImplementations_SimpleGraph_BehaveSameWay()
        {
            // Arrange
            var edges = CreateSimpleTestEdges();
            var adjacencyList = new AdjacencyListGraph<string>(edges);
            var adjacencyMatrix = new AdjacencyMatrixGraph<string>(edges);
            var compact = new CompactGraph<string>(edges);

            var implementations = new IDirectedGraph<string>[] { adjacencyList, adjacencyMatrix, compact };

            // Act & Assert
            foreach (var graph in implementations)
            {
                // Test basic properties
                Assert.Equal(4, graph.Count);
                Assert.Equal(4, graph.Nodes.Count);
                Assert.Equal(4, graph.Edges.Count);

                // Test node presence
                Assert.Contains("A", graph.Nodes);
                Assert.Contains("B", graph.Nodes);
                Assert.Contains("C", graph.Nodes);
                Assert.Contains("D", graph.Nodes);

                // Test edge queries
                Assert.True(graph.HasEdge("A", "B"));
                Assert.True(graph.HasEdge("B", "C"));
                Assert.True(graph.HasEdge("A", "C"));
                Assert.True(graph.HasEdge("D", "C"));
                Assert.False(graph.HasEdge("C", "A"));
                Assert.False(graph.HasEdge("B", "A"));

                // Test path queries
                Assert.True(graph.HasPath("A", "C"));
                Assert.True(graph.HasPath("D", "C"));
                Assert.False(graph.HasPath("C", "A"));
                Assert.False(graph.HasPath("C", "D"));

                // Test dependencies
                var cDependencies = graph.GetDependencies("C");
                Assert.Equal(3, cDependencies.Count);
                Assert.Contains("A", cDependencies);
                Assert.Contains("B", cDependencies);
                Assert.Contains("D", cDependencies);

                // Test dependents
                var aDependents = graph.GetDependents("A");
                Assert.Equal(2, aDependents.Count);
                Assert.Contains("B", aDependents);
                Assert.Contains("C", aDependents);
            }
        }

        [Fact]
        public void AllImplementations_SingleNodeWithSelfLoop_BehaveSameWay()
        {
            // Arrange
            var edges = new[] { new GraphEdge<string>("A", "A") };
            var adjacencyList = new AdjacencyListGraph<string>(edges);
            var adjacencyMatrix = new AdjacencyMatrixGraph<string>(edges);
            var compact = new CompactGraph<string>(edges);

            var implementations = new IDirectedGraph<string>[] { adjacencyList, adjacencyMatrix, compact };

            // Act & Assert
            foreach (var graph in implementations)
            {
                Assert.Single(graph.Nodes);
                Assert.Single(graph.Edges);
                Assert.True(graph.HasEdge("A", "A"));
                Assert.True(graph.HasPath("A", "A"));
                
                var aDependencies = graph.GetDependencies("A");
                Assert.Single(aDependencies);
                Assert.Contains("A", aDependencies);
                
                var aDependents = graph.GetDependents("A");
                Assert.Single(aDependents);
                Assert.Contains("A", aDependents);
            }
        }

        [Fact]
        public void AllImplementations_DisconnectedComponents_BehaveSameWay()
        {
            // Arrange: A→B and C→D (disconnected)
            var edges = new[]
            {
                new GraphEdge<string>("A", "B"),
                new GraphEdge<string>("C", "D")
            };
            
            var adjacencyList = new AdjacencyListGraph<string>(edges);
            var adjacencyMatrix = new AdjacencyMatrixGraph<string>(edges);
            var compact = new CompactGraph<string>(edges);

            var implementations = new IDirectedGraph<string>[] { adjacencyList, adjacencyMatrix, compact };

            // Act & Assert
            foreach (var graph in implementations)
            {
                Assert.Equal(4, graph.Count);
                
                // Test connectivity within components
                Assert.True(graph.HasPath("A", "B"));
                Assert.True(graph.HasPath("C", "D"));
                
                // Test no connectivity between components
                Assert.False(graph.HasPath("A", "C"));
                Assert.False(graph.HasPath("A", "D"));
                Assert.False(graph.HasPath("B", "C"));
                Assert.False(graph.HasPath("B", "D"));
                Assert.False(graph.HasPath("C", "A"));
                Assert.False(graph.HasPath("D", "A"));
            }
        }

        [Fact]
        public void AllImplementations_CyclicGraph_BehaveSameWay()
        {
            // Arrange: A→B→C→A (cycle)
            var edges = new[]
            {
                new GraphEdge<string>("A", "B"),
                new GraphEdge<string>("B", "C"),
                new GraphEdge<string>("C", "A")
            };
            
            var adjacencyList = new AdjacencyListGraph<string>(edges);
            var adjacencyMatrix = new AdjacencyMatrixGraph<string>(edges);
            var compact = new CompactGraph<string>(edges);

            var implementations = new IDirectedGraph<string>[] { adjacencyList, adjacencyMatrix, compact };

            // Act & Assert
            foreach (var graph in implementations)
            {
                Assert.Equal(3, graph.Count);
                
                // All nodes should be reachable from each other in a cycle
                Assert.True(graph.HasPath("A", "B"));
                Assert.True(graph.HasPath("B", "C"));
                Assert.True(graph.HasPath("C", "A"));
                Assert.True(graph.HasPath("A", "C"));
                Assert.True(graph.HasPath("B", "A"));
                Assert.True(graph.HasPath("C", "B"));
            }
        }

        #endregion

        #region Performance Characteristic Tests

        [Fact]
        public void AdjacencyMatrix_DenseGraph_HasO1EdgeQueries()
        {
            // Arrange
            var edges = CreateDenseTestEdges(20);
            var graph = new AdjacencyMatrixGraph<int>(edges);

            // Act - Measure edge query performance
            var stopwatch = Stopwatch.StartNew();
            int queryCount = 0;
            
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    graph.HasEdge(i, j);
                    queryCount++;
                }
            }
            
            stopwatch.Stop();

            // Assert - Should be very fast for dense graphs
            Assert.True(stopwatch.ElapsedMilliseconds < 10, 
                $"Edge queries took {stopwatch.ElapsedMilliseconds}ms for {queryCount} queries");
        }

        [Fact]
        public void CompactGraph_SparseGraph_HasReasonableMemoryUsage()
        {
            // Arrange
            var edges = CreateSparseTestEdges(100);
            var compactGraph = new CompactGraph<int>(edges);

            // Act
            var memoryStats = compactGraph.GetMemoryStats();

            // Assert
            Assert.Equal(100, memoryStats.NodeCount);
            Assert.True(memoryStats.EdgeCount > 0);
            Assert.True(memoryStats.TotalSizeBytes > 0);
            Assert.True(memoryStats.BytesPerNode < 1000); // Should be efficient for sparse graphs
            Assert.True(memoryStats.CompressionRatio > 1.0); // Should be compressed vs naive
        }

        [Fact]
        public void AllImplementations_ModerateGraph_HandleEfficiently()
        {
            // Arrange: Create a moderate-sized graph (30 nodes, ~90 edges)
            var edges = CreateDenseTestEdges(30);
            
            // Act & Assert - All implementations should handle this efficiently
            var adjacencyList = new AdjacencyListGraph<int>(edges);
            var adjacencyMatrix = new AdjacencyMatrixGraph<int>(edges);
            var compact = new CompactGraph<int>(edges);

            var implementations = new IDirectedGraph<int>[] { adjacencyList, adjacencyMatrix, compact };

            foreach (var graph in implementations)
            {
                Assert.Equal(30, graph.Count);
                
                // Test some operations for performance
                var stopwatch = Stopwatch.StartNew();
                
                // Test edge queries
                for (int i = 0; i < 10; i++)
                {
                    graph.HasEdge(i, (i + 5) % 30);
                }
                
                // Test dependency queries
                for (int i = 0; i < 10; i++)
                {
                    var deps = graph.GetDependencies(i);
                    var dependents = graph.GetDependents(i);
                }
                
                stopwatch.Stop();
                
                // Should complete quickly
                Assert.True(stopwatch.ElapsedMilliseconds < 100, 
                    $"{graph.GetType().Name} took {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void AllImplementations_InvalidNodeQueries_ThrowSameExceptions()
        {
            // Arrange
            var edges = CreateSimpleTestEdges();
            var adjacencyList = new AdjacencyListGraph<string>(edges);
            var adjacencyMatrix = new AdjacencyMatrixGraph<string>(edges);
            var compact = new CompactGraph<string>(edges);

            var implementations = new IDirectedGraph<string>[] { adjacencyList, adjacencyMatrix, compact };

            // Act & Assert
            foreach (var graph in implementations)
            {
                // Test queries with nodes not in graph
                Assert.False(graph.HasEdge("X", "Y"));
                Assert.False(graph.HasPath("X", "Y"));
                
                // Test exception for GetDependencies with invalid node
                Assert.Throws<ArgumentException>(() => graph.GetDependencies("X"));
                Assert.Throws<ArgumentException>(() => graph.GetDependents("X"));
                Assert.Throws<ArgumentException>(() => graph.GetOutgoingEdges("X"));
                Assert.Throws<ArgumentException>(() => graph.GetIncomingEdges("X"));
                
                // Test null argument exceptions
                Assert.Throws<ArgumentNullException>(() => graph.HasEdge(null!, "A"));
                Assert.Throws<ArgumentNullException>(() => graph.HasPath("A", null!));
                Assert.Throws<ArgumentNullException>(() => graph.GetDependencies(null!));
            }
        }

        [Fact]
        public void AllImplementations_NullConstructorArguments_ThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AdjacencyListGraph<string>((IEnumerable<IGraphEdge<string>>)null!));
            Assert.Throws<ArgumentNullException>(() => new AdjacencyMatrixGraph<string>(null!));
            Assert.Throws<ArgumentNullException>(() => new CompactGraph<string>(null!));
        }

        #endregion

        #region Behavioral Equivalence Tests

        [Fact]
        public void AllImplementations_ComplexGraph_ProduceSameResults()
        {
            // Arrange: Create a complex graph with various patterns
            var builder = new GraphBuilder<string>();
            var graph = builder
                // Linear chain
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                // Branching
                .AddEdge("A", "E")
                .AddEdge("E", "F")
                // Convergence
                .AddEdge("D", "G")
                .AddEdge("F", "G")
                // Self-loop
                .AddEdge("G", "G")
                // Back edge
                .AddEdge("F", "A")
                // Isolated node
                .AddNode("H")
                .Build();

            var edges = graph.Edges;
            var nodes = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
            
            var adjacencyList = new AdjacencyListGraph<string>(nodes, edges);
            var adjacencyMatrix = new AdjacencyMatrixGraph<string>(nodes, edges);
            var compact = new CompactGraph<string>(nodes, edges);

            var implementations = new IDirectedGraph<string>[] { adjacencyList, adjacencyMatrix, compact };

            // Act & Assert - Test that all implementations produce identical results
            foreach (var impl1 in implementations)
            {
                foreach (var impl2 in implementations)
                {
                    // Basic properties
                    Assert.Equal(impl1.Count, impl2.Count);
                    Assert.Equal(impl1.Nodes.Count, impl2.Nodes.Count);
                    Assert.Equal(impl1.Edges.Count, impl2.Edges.Count);

                    // Test all possible node pairs for edge and path queries
                    foreach (var node1 in nodes)
                    {
                        foreach (var node2 in nodes)
                        {
                            Assert.Equal(impl1.HasEdge(node1, node2), impl2.HasEdge(node1, node2));
                            Assert.Equal(impl1.HasPath(node1, node2), impl2.HasPath(node1, node2));
                        }
                        
                        // Test dependency/dependent queries
                        var deps1 = impl1.GetDependencies(node1).OrderBy(x => x).ToList();
                        var deps2 = impl2.GetDependencies(node1).OrderBy(x => x).ToList();
                        Assert.Equal(deps1, deps2);
                        
                        var dependents1 = impl1.GetDependents(node1).OrderBy(x => x).ToList();
                        var dependents2 = impl2.GetDependents(node1).OrderBy(x => x).ToList();
                        Assert.Equal(dependents1, dependents2);
                        
                        // Test edge collections
                        var outgoing1 = impl1.GetOutgoingEdges(node1).Select(e => $"{e.Source}->{e.Target}").OrderBy(x => x).ToList();
                        var outgoing2 = impl2.GetOutgoingEdges(node1).Select(e => $"{e.Source}->{e.Target}").OrderBy(x => x).ToList();
                        Assert.Equal(outgoing1, outgoing2);
                        
                        var incoming1 = impl1.GetIncomingEdges(node1).Select(e => $"{e.Source}->{e.Target}").OrderBy(x => x).ToList();
                        var incoming2 = impl2.GetIncomingEdges(node1).Select(e => $"{e.Source}->{e.Target}").OrderBy(x => x).ToList();
                        Assert.Equal(incoming1, incoming2);
                    }
                }
            }
        }

        #endregion

        #region Graph Density and Implementation Choice Tests

        [Fact]
        public void ImplementationChoice_SparseVsDense_ShowsExpectedCharacteristics()
        {
            // Test with sparse graph (better for AdjacencyList and Compact)
            var sparseEdges = CreateSparseTestEdges(50);
            var sparseAdjList = new AdjacencyListGraph<int>(sparseEdges);
            var sparseMatrix = new AdjacencyMatrixGraph<int>(sparseEdges);
            var sparseCompact = new CompactGraph<int>(sparseEdges);

            // Test with dense graph (better for AdjacencyMatrix)
            var denseEdges = CreateDenseTestEdges(30);
            var denseAdjList = new AdjacencyListGraph<int>(denseEdges);
            var denseMatrix = new AdjacencyMatrixGraph<int>(denseEdges);
            var denseCompact = new CompactGraph<int>(denseEdges);

            // Verify all work correctly regardless of density
            Assert.True(sparseAdjList.Count > 0);
            Assert.True(sparseMatrix.Count > 0);
            Assert.True(sparseCompact.Count > 0);
            Assert.True(denseAdjList.Count > 0);
            Assert.True(denseMatrix.Count > 0);
            Assert.True(denseCompact.Count > 0);

            // Test matrix-specific features
            Assert.True(denseMatrix.Density > sparseMatrix.Density);
            Assert.True(denseMatrix.Density > 0.1); // Should be reasonably dense

            // Test compact graph memory efficiency
            var sparseStats = sparseCompact.GetMemoryStats();
            var denseStats = denseCompact.GetMemoryStats();
            
            Assert.True(sparseStats.BytesPerEdge > 0);
            Assert.True(denseStats.BytesPerEdge > 0);
            Assert.True(sparseStats.CompressionRatio >= 1.0); // Sparse graphs should be compressed vs naive
            Assert.True(denseStats.CompressionRatio > 0.0); // Dense graphs may not compress well vs boolean matrix
        }

        #endregion

        #region Implementation-Specific Feature Tests

        [Fact]
        public void AdjacencyMatrixGraph_SpecificFeatures_WorkCorrectly()
        {
            // Arrange
            var edges = CreateDenseTestEdges(10);
            var graph = new AdjacencyMatrixGraph<int>(edges);

            // Act & Assert
            Assert.True(graph.Density >= 0.0 && graph.Density <= 1.0);

            // Test matrix access
            var matrix = graph.GetAdjacencyMatrix();
            Assert.Equal(graph.Count, matrix.GetLength(0));
            Assert.Equal(graph.Count, matrix.GetLength(1));

            // Test node indexing
            for (int i = 0; i < graph.Count; i++)
            {
                var node = graph.GetNodeByIndex(i);
                var index = graph.GetNodeIndex(node);
                Assert.Equal(i, index);
            }

            // Test invalid index
            Assert.Throws<ArgumentOutOfRangeException>(() => graph.GetNodeByIndex(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => graph.GetNodeByIndex(graph.Count));
        }

        [Fact]
        public void CompactGraph_SpecificFeatures_WorkCorrectly()
        {
            // Arrange
            var edges = CreateSparseTestEdges(20);
            var graph = new CompactGraph<int>(edges);

            // Act & Assert
            var memoryStats = graph.GetMemoryStats();
            
            Assert.Equal(graph.Count, memoryStats.NodeCount);
            Assert.Equal(graph.Edges.Count, memoryStats.EdgeCount);
            Assert.True(memoryStats.TotalSizeBytes > 0);
            Assert.True(memoryStats.BytesPerNode > 0);
            
            // Test degree methods
            for (int i = 0; i < Math.Min(5, graph.Count); i++)
            {
                var outDegree = graph.GetOutDegree(i);
                var inDegree = graph.GetInDegree(i);
                
                Assert.True(outDegree >= 0);
                Assert.True(inDegree >= 0);
                
                // Verify degree matches actual edge counts
                var actualOutDegree = graph.GetOutgoingEdges(i).Count;
                var actualInDegree = graph.GetIncomingEdges(i).Count;
                
                Assert.Equal(actualOutDegree, outDegree);
                Assert.Equal(actualInDegree, inDegree);
            }
        }

        #endregion
    }
}
