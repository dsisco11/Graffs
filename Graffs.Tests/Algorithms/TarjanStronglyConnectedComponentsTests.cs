using System;
using System.Linq;
using Graffs.Algorithms;
using Graffs.Builders;
using Graffs.Results;
using Xunit;

namespace NeuroGraph.Tests.Core.Graphs.Algorithms
{
    /// <summary>
    /// Comprehensive test suite for TarjanStronglyConnectedComponents algorithm.
    /// Tests Tarjan's algorithm for finding strongly connected components in directed graphs
    /// through the public TarjanStronglyConnectedComponents.Execute API.
    /// 
    /// Focuses on algorithm-specific behaviors, edge cases, and performance characteristics
    /// of Tarjan's linear-time O(V + E) SCC detection.
    /// </summary>
    public sealed class TarjanStronglyConnectedComponentsTests
    {
        #region Basic Algorithm Tests

        [Fact]
        public void TarjanSCC_EmptyGraph_ReturnsEmptyResult()
        {
            // Arrange
            var graph = new GraphBuilder<string>().Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Empty(result.Components);
            Assert.Equal(0, result.Statistics.TotalComponents);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(0, result.Statistics.NonTrivialComponents);
            Assert.Equal(0, result.Statistics.LargestComponentSize);
            Assert.Equal("Tarjan's SCC Algorithm", result.Algorithm);
            Assert.True(result.ProcessingTime.TotalMilliseconds >= 0);
        }

        [Fact]
        public void TarjanSCC_SingleNode_ReturnsTrivialComponent()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddNode("A")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Single(result.Components);
            var component = result.Components.First();
            Assert.True(component.IsTrivial);
            Assert.Single(component.Nodes);
            Assert.Equal("A", component.Nodes.Single());
            Assert.Equal(1, result.Statistics.TotalComponents);
            Assert.Equal(1, result.Statistics.TrivialComponents);
            Assert.Equal(0, result.Statistics.NonTrivialComponents);
            Assert.Equal(1, result.Statistics.LargestComponentSize);
        }

        [Fact]
        public void TarjanSCC_SingleNodeWithSelfLoop_ReturnsNonTrivialComponent()
        {
            // Arrange: Self-loop creates a non-trivial SCC
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "A")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Single(result.Components);
            var component = result.Components.First();
            Assert.False(component.IsTrivial);
            Assert.Single(component.Nodes);
            Assert.Equal("A", component.Nodes.Single());
            Assert.Equal(1, result.Statistics.TotalComponents);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(1, result.Statistics.NonTrivialComponents);
            Assert.Equal(1, result.Statistics.LargestComponentSize);
        }

        #endregion

        #region Cycle Detection Tests

        [Fact]
        public void TarjanSCC_SimpleCycle_ReturnsSingleNonTrivialComponent()
        {
            // Arrange: A → B → C → A (simple directed cycle)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Single(result.Components);
            var component = result.Components.First();
            Assert.False(component.IsTrivial);
            Assert.Equal(3, component.Size);
            Assert.Contains("A", component.Nodes);
            Assert.Contains("B", component.Nodes);
            Assert.Contains("C", component.Nodes);
            Assert.Equal(1, result.Statistics.TotalComponents);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(1, result.Statistics.NonTrivialComponents);
            Assert.Equal(3, result.Statistics.LargestComponentSize);
        }

        [Fact]
        public void TarjanSCC_MultipleCycles_ReturnsCorrectComponents()
        {
            // Arrange: Two separate cycles
            // Cycle 1: A → B → A
            // Cycle 2: C → D → E → C
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(2, result.Components.Length);
            Assert.All(result.Components, c => Assert.False(c.IsTrivial));
            Assert.Equal(2, result.Statistics.TotalComponents);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(2, result.Statistics.NonTrivialComponents);
            Assert.Equal(3, result.Statistics.LargestComponentSize);

            var componentSizes = result.Components.Select(c => c.Size).OrderBy(s => s).ToArray();
            Assert.Equal(new[] { 2, 3 }, componentSizes);
        }

        [Fact]
        public void TarjanSCC_NestedCycles_ReturnsCorrectComponents()
        {
            // Arrange: Nested cycle structure
            // A → B → C → A (outer cycle)
            // B → D → B (inner cycle connected to outer)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .AddEdge("B", "D")
                .AddEdge("D", "B")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Single(result.Components);
            var component = result.Components.First();
            Assert.False(component.IsTrivial);
            Assert.Equal(4, component.Size);
            Assert.Contains("A", component.Nodes);
            Assert.Contains("B", component.Nodes);
            Assert.Contains("C", component.Nodes);
            Assert.Contains("D", component.Nodes);
            Assert.Equal(1, result.Statistics.TotalComponents);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(1, result.Statistics.NonTrivialComponents);
            Assert.Equal(4, result.Statistics.LargestComponentSize);
        }

        #endregion

        #region DAG (Directed Acyclic Graph) Tests

        [Fact]
        public void TarjanSCC_SimpleDAG_ReturnsAllTrivialComponents()
        {
            // Arrange: A → B → C → D (no cycles)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(4, result.Components.Length);
            Assert.All(result.Components, c => Assert.True(c.IsTrivial));
            Assert.All(result.Components, c => Assert.Single(c.Nodes));
            Assert.Equal(4, result.Statistics.TotalComponents);
            Assert.Equal(4, result.Statistics.TrivialComponents);
            Assert.Equal(0, result.Statistics.NonTrivialComponents);
            Assert.Equal(1, result.Statistics.LargestComponentSize);

            var allNodes = result.Components.SelectMany(c => c.Nodes).ToHashSet();
            Assert.Contains("A", allNodes);
            Assert.Contains("B", allNodes);
            Assert.Contains("C", allNodes);
            Assert.Contains("D", allNodes);
        }

        [Fact]
        public void TarjanSCC_TreeStructure_ReturnsAllTrivialComponents()
        {
            // Arrange: Tree structure (DAG)
            //     A
            //   ↙   ↘
            //  B     C
            // ↙ ↘   ↙ ↘
            // D  E  F  G
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("B", "D")
                .AddEdge("B", "E")
                .AddEdge("C", "F")
                .AddEdge("C", "G")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(7, result.Components.Length);
            Assert.All(result.Components, c => Assert.True(c.IsTrivial));
            Assert.All(result.Components, c => Assert.Single(c.Nodes));
            Assert.Equal(7, result.Statistics.TotalComponents);
            Assert.Equal(7, result.Statistics.TrivialComponents);
            Assert.Equal(0, result.Statistics.NonTrivialComponents);
            Assert.Equal(1, result.Statistics.LargestComponentSize);
        }

        #endregion

        #region Complex Graph Tests

        [Fact]
        public void TarjanSCC_ClassicExampleGraph_ReturnsCorrectComponents()
        {
            // Arrange: Classic textbook example of SCC
            // SCCs: {1,2,3}, {4,5,6}, {7,8}
            var graph = new GraphBuilder<int>()
                .AddEdge(1, 2)
                .AddEdge(2, 3)
                .AddEdge(3, 1)  // First SCC: {1,2,3}
                .AddEdge(2, 4)
                .AddEdge(4, 5)
                .AddEdge(5, 6)
                .AddEdge(6, 4)  // Second SCC: {4,5,6}
                .AddEdge(6, 7)
                .AddEdge(7, 8)
                .AddEdge(8, 7)  // Third SCC: {7,8}
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(3, result.Components.Length);
            Assert.All(result.Components, c => Assert.False(c.IsTrivial));
            Assert.Equal(3, result.Statistics.TotalComponents);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(3, result.Statistics.NonTrivialComponents);
            Assert.Equal(3, result.Statistics.LargestComponentSize);

            var componentSizes = result.Components.Select(c => c.Size).OrderBy(s => s).ToArray();
            Assert.Equal(new[] { 2, 3, 3 }, componentSizes);

            // Verify specific SCCs
            var scc123 = result.Components.Single(c => c.Nodes.Contains(1));
            Assert.Equal(3, scc123.Size);
            Assert.Contains(1, scc123.Nodes);
            Assert.Contains(2, scc123.Nodes);
            Assert.Contains(3, scc123.Nodes);

            var scc456 = result.Components.Single(c => c.Nodes.Contains(4));
            Assert.Equal(3, scc456.Size);
            Assert.Contains(4, scc456.Nodes);
            Assert.Contains(5, scc456.Nodes);
            Assert.Contains(6, scc456.Nodes);

            var scc78 = result.Components.Single(c => c.Nodes.Contains(7));
            Assert.Equal(2, scc78.Size);
            Assert.Contains(7, scc78.Nodes);
            Assert.Contains(8, scc78.Nodes);
        }

        [Fact]
        public void TarjanSCC_MixedTrivialAndNonTrivial_ReturnsCorrectClassification()
        {
            // Arrange: Mixed graph with both trivial and non-trivial SCCs
            // Non-trivial: A ↔ B (bidirectional)
            // Non-trivial: C → D → E → C (cycle)
            // Trivial: F (isolated)
            // Trivial: G → H (no cycle back)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                .AddNode("F")
                .AddEdge("G", "H")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(5, result.Components.Length);
            Assert.Equal(5, result.Statistics.TotalComponents);
            Assert.Equal(3, result.Statistics.TrivialComponents);
            Assert.Equal(2, result.Statistics.NonTrivialComponents);
            Assert.Equal(3, result.Statistics.LargestComponentSize);

            var trivialComponents = result.Components.Where(c => c.IsTrivial).ToArray();
            var nonTrivialComponents = result.Components.Where(c => !c.IsTrivial).ToArray();

            Assert.Equal(3, trivialComponents.Length);
            Assert.Equal(2, nonTrivialComponents.Length);

            // Check non-trivial components
            var ab_component = nonTrivialComponents.Single(c => c.Nodes.Contains("A"));
            Assert.Equal(2, ab_component.Size);
            Assert.Contains("A", ab_component.Nodes);
            Assert.Contains("B", ab_component.Nodes);

            var cde_component = nonTrivialComponents.Single(c => c.Nodes.Contains("C"));
            Assert.Equal(3, cde_component.Size);
            Assert.Contains("C", cde_component.Nodes);
            Assert.Contains("D", cde_component.Nodes);
            Assert.Contains("E", cde_component.Nodes);
        }

        #endregion

        #region Edge Cases and Special Structures

        [Fact]
        public void TarjanSCC_BidirectionalEdges_ReturnsNonTrivialComponent()
        {
            // Arrange: Simple bidirectional connection
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Single(result.Components);
            var component = result.Components.First();
            Assert.False(component.IsTrivial);
            Assert.Equal(2, component.Size);
            Assert.Contains("A", component.Nodes);
            Assert.Contains("B", component.Nodes);
        }

        [Fact]
        public void TarjanSCC_MultipleDisconnectedComponents_HandlesCorrectly()
        {
            // Arrange: Multiple disconnected SCCs
            // SCC 1: A → B → A
            // SCC 2: C → D → E → C  
            // SCC 3: F (isolated)
            // SCC 4: G → H (chain)
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                .AddNode("F")
                .AddEdge("G", "H")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(5, result.Components.Length);
            Assert.Equal(5, result.Statistics.TotalComponents);
            Assert.Equal(3, result.Statistics.TrivialComponents);
            Assert.Equal(2, result.Statistics.NonTrivialComponents);
            Assert.Equal(3, result.Statistics.LargestComponentSize);

            // All nodes should be accounted for
            var allNodes = result.Components.SelectMany(c => c.Nodes).ToHashSet();
            Assert.Equal(8, allNodes.Count);
            foreach (var node in new[] { "A", "B", "C", "D", "E", "F", "G", "H" })
            {
                Assert.Contains(node, allNodes);
            }
        }

        [Fact]
        public void TarjanSCC_StarPattern_ReturnsCorrectComponents()
        {
            // Arrange: Star pattern (all edges point to center)
            //   A
            //   ↓
            // B→C←D
            //   ↑
            //   E
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "C")
                .AddEdge("B", "C")
                .AddEdge("D", "C")
                .AddEdge("E", "C")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(5, result.Components.Length);
            Assert.All(result.Components, c => Assert.True(c.IsTrivial));
            Assert.Equal(5, result.Statistics.TotalComponents);
            Assert.Equal(5, result.Statistics.TrivialComponents);
            Assert.Equal(0, result.Statistics.NonTrivialComponents);
            Assert.Equal(1, result.Statistics.LargestComponentSize);
        }

        #endregion

        #region Performance and Scalability Tests

        [Fact]
        public void TarjanSCC_LinearChain_HandlesEfficientlyWithCorrectComplexity()
        {
            // Arrange: Long linear chain to test O(V + E) complexity
            const int chainLength = 1000;
            var builder = new GraphBuilder<int>();
            
            for (int i = 0; i < chainLength - 1; i++)
            {
                builder.AddEdge(i, i + 1);
            }
            var graph = builder.Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(chainLength, result.Components.Length);
            Assert.All(result.Components, c => Assert.True(c.IsTrivial));
            Assert.Equal(chainLength, result.Statistics.TotalComponents);
            Assert.Equal(chainLength, result.Statistics.TrivialComponents);
            Assert.Equal(0, result.Statistics.NonTrivialComponents);
            Assert.Equal(1, result.Statistics.LargestComponentSize);
            
            // Verify linear time complexity - should be very fast
            Assert.True(result.ProcessingTime.TotalMilliseconds < 100,
                $"Linear chain processing took {result.ProcessingTime.TotalMilliseconds}ms, expected < 100ms");
        }

        [Fact]
        public void TarjanSCC_LargeCycle_HandlesEfficientlyWithLinearTime()
        {
            // Arrange: Large cycle to test linear time on dense SCC
            const int cycleSize = 1000;
            var builder = new GraphBuilder<int>();
            
            for (int i = 0; i < cycleSize; i++)
            {
                builder.AddEdge(i, (i + 1) % cycleSize);
            }
            var graph = builder.Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Single(result.Components);
            var component = result.Components.First();
            Assert.False(component.IsTrivial);
            Assert.Equal(cycleSize, component.Size);
            Assert.Equal(1, result.Statistics.TotalComponents);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(1, result.Statistics.NonTrivialComponents);
            Assert.Equal(cycleSize, result.Statistics.LargestComponentSize);
            
            // Verify all nodes are in the single SCC
            var nodeSet = component.Nodes.ToHashSet();
            for (int i = 0; i < cycleSize; i++)
            {
                Assert.Contains(i, nodeSet);
            }
            
            // Verify linear time performance
            Assert.True(result.ProcessingTime.TotalMilliseconds < 200,
                $"Large cycle processing took {result.ProcessingTime.TotalMilliseconds}ms, expected < 200ms");
        }

        [Fact]
        public void TarjanSCC_ManySmallComponents_HandlesEfficientlyAndCorrectly()
        {
            // Arrange: Many small SCCs to test scalability
            const int componentCount = 100;
            var builder = new GraphBuilder<int>();
            
            for (int c = 0; c < componentCount; c++)
            {
                int baseNode = c * 3;
                builder.AddEdge(baseNode, baseNode + 1);
                builder.AddEdge(baseNode + 1, baseNode + 2);
                builder.AddEdge(baseNode + 2, baseNode);
            }
            var graph = builder.Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(componentCount, result.Components.Length);
            Assert.All(result.Components, c => Assert.False(c.IsTrivial));
            Assert.All(result.Components, c => Assert.Equal(3, c.Size));
            Assert.Equal(componentCount, result.Statistics.TotalComponents);
            Assert.Equal(0, result.Statistics.TrivialComponents);
            Assert.Equal(componentCount, result.Statistics.NonTrivialComponents);
            Assert.Equal(3, result.Statistics.LargestComponentSize);
            
            // Verify good performance
            Assert.True(result.ProcessingTime.TotalMilliseconds < 200,
                $"Many components processing took {result.ProcessingTime.TotalMilliseconds}ms, expected < 200ms");
        }

        #endregion

        #region Algorithm Properties and Metadata Tests

        [Fact]
        public void TarjanSCC_VerifyAlgorithmMetadata()
        {
            // Arrange
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal("Tarjan's SCC Algorithm", result.Algorithm);
            Assert.True(result.ProcessingTime.TotalMilliseconds >= 0);
            Assert.True(result.CompletedAt <= DateTime.UtcNow);
            Assert.True(result.CompletedAt >= DateTime.UtcNow.AddSeconds(-1));
        }

        [Fact]
        public void TarjanSCC_DifferentNodeTypes_WorksCorrectly()
        {
            // Arrange: Test with different node types
            var intGraph = new GraphBuilder<int>()
                .AddEdge(1, 2)
                .AddEdge(2, 1)
                .AddEdge(3, 4)
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(intGraph);

            // Assert
            Assert.Equal(3, result.Components.Length);
            
            var nonTrivialComponent = result.Components.Single(c => !c.IsTrivial);
            Assert.Equal(2, nonTrivialComponent.Size);
            Assert.Contains(1, nonTrivialComponent.Nodes);
            Assert.Contains(2, nonTrivialComponent.Nodes);
            
            var trivialComponents = result.Components.Where(c => c.IsTrivial).ToArray();
            Assert.Equal(2, trivialComponents.Length);
            Assert.All(trivialComponents, c => Assert.Single(c.Nodes));
            
            // Verify that nodes 3 and 4 are in separate trivial components
            var nodeNumbers = trivialComponents.SelectMany(c => c.Nodes).OrderBy(x => x).ToArray();
            Assert.Equal(new[] { 3, 4 }, nodeNumbers);
        }

        [Fact]
        public void TarjanSCC_NullGraph_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                TarjanStronglyConnectedComponents.Execute<string>(null!));
        }

        #endregion

        #region Tarjan-Specific Edge Cases

        [Fact]
        public void TarjanSCC_DeepRecursion_HandlesCorrectlyWithoutStackOverflow()
        {
            // Arrange: Create a deep chain that would cause stack overflow in naive recursive implementations
            const int depth = 5000;
            var builder = new GraphBuilder<int>();
            
            for (int i = 0; i < depth - 1; i++)
            {
                builder.AddEdge(i, i + 1);
            }
            // Close the cycle to create one large SCC
            builder.AddEdge(depth - 1, 0);
            var graph = builder.Build();

            // Act & Assert (should not throw StackOverflowException)
            var result = TarjanStronglyConnectedComponents.Execute(graph);
            
            Assert.Single(result.Components);
            Assert.Equal(depth, result.Components.First().Size);
            Assert.False(result.Components.First().IsTrivial);
        }

        [Fact]
        public void TarjanSCC_ComplexConnectedComponents_MaintainsCorrectTopologicalOrdering()
        {
            // Arrange: Graph where SCC discovery order matters
            // Two SCCs connected: {A,B} → {C,D,E}
            var graph = new GraphBuilder<string>()
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .AddEdge("B", "C")  // Connection between SCCs
                .AddEdge("C", "D")
                .AddEdge("D", "E")
                .AddEdge("E", "C")
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(2, result.Components.Length);
            Assert.All(result.Components, c => Assert.False(c.IsTrivial));
            
            var ab_scc = result.Components.Single(c => c.Nodes.Contains("A"));
            Assert.Equal(2, ab_scc.Size);
            Assert.Contains("A", ab_scc.Nodes);
            Assert.Contains("B", ab_scc.Nodes);
            
            var cde_scc = result.Components.Single(c => c.Nodes.Contains("C"));
            Assert.Equal(3, cde_scc.Size);
            Assert.Contains("C", cde_scc.Nodes);
            Assert.Contains("D", cde_scc.Nodes);
            Assert.Contains("E", cde_scc.Nodes);
        }

        #endregion

        #region Comparison with Literature Examples

        [Fact]
        public void TarjanSCC_WikipediaExample_ReturnsCorrectSCCs()
        {
            // Arrange: Classic Wikipedia SCC example
            // Expected SCCs: {a,b,e}, {c,d}, {f,g}, {h}
            var graph = new GraphBuilder<string>()
                .AddEdge("a", "b")
                .AddEdge("b", "e")
                .AddEdge("e", "a")  // SCC: {a,b,e}
                .AddEdge("b", "f")
                .AddEdge("e", "f")
                .AddEdge("f", "g")
                .AddEdge("g", "f")  // SCC: {f,g}
                .AddEdge("c", "d")
                .AddEdge("d", "c")  // SCC: {c,d}
                .AddEdge("c", "g")
                .AddEdge("d", "h")
                .AddNode("h")       // SCC: {h}
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(4, result.Components.Length);
            Assert.Equal(4, result.Statistics.TotalComponents);
            Assert.Equal(1, result.Statistics.TrivialComponents);
            Assert.Equal(3, result.Statistics.NonTrivialComponents);
            Assert.Equal(3, result.Statistics.LargestComponentSize);

            // Verify specific SCCs
            var abe_scc = result.Components.Single(c => c.Nodes.Contains("a"));
            Assert.Equal(3, abe_scc.Size);
            Assert.Contains("a", abe_scc.Nodes);
            Assert.Contains("b", abe_scc.Nodes);
            Assert.Contains("e", abe_scc.Nodes);

            var cd_scc = result.Components.Single(c => c.Nodes.Contains("c"));
            Assert.Equal(2, cd_scc.Size);
            Assert.Contains("c", cd_scc.Nodes);
            Assert.Contains("d", cd_scc.Nodes);

            var fg_scc = result.Components.Single(c => c.Nodes.Contains("f"));
            Assert.Equal(2, fg_scc.Size);
            Assert.Contains("f", fg_scc.Nodes);
            Assert.Contains("g", fg_scc.Nodes);

            var h_scc = result.Components.Single(c => c.Nodes.Contains("h"));
            Assert.Single(h_scc.Nodes);
            Assert.Equal("h", h_scc.Nodes.Single());
            Assert.True(h_scc.IsTrivial);
        }

        [Fact]
        public void TarjanSCC_CLRSTextbookExample_ReturnsCorrectSCCs()
        {
            // Arrange: Example from Introduction to Algorithms (CLRS) textbook
            // Graph with nodes a,b,c,d,e,f,g,h forming specific SCC structure
            var graph = new GraphBuilder<string>()
                .AddEdge("a", "b")
                .AddEdge("b", "c")
                .AddEdge("c", "d")
                .AddEdge("d", "c")  // SCC: {c,d}
                .AddEdge("b", "e")
                .AddEdge("e", "a")  
                .AddEdge("b", "f")  // SCC: {a,b,e}
                .AddEdge("e", "f")
                .AddEdge("f", "g")
                .AddEdge("g", "h")
                .AddEdge("h", "g")  // SCC: {g,h}
                .Build();

            // Act
            var result = TarjanStronglyConnectedComponents.Execute(graph);

            // Assert
            Assert.Equal(4, result.Components.Length);
            
            // Find and verify each SCC
            var abe_scc = result.Components.Single(c => c.Nodes.Contains("a") && c.Size == 3);
            Assert.Contains("a", abe_scc.Nodes);
            Assert.Contains("b", abe_scc.Nodes);
            Assert.Contains("e", abe_scc.Nodes);
            Assert.False(abe_scc.IsTrivial);

            var cd_scc = result.Components.Single(c => c.Nodes.Contains("c"));
            Assert.Equal(2, cd_scc.Size);
            Assert.Contains("c", cd_scc.Nodes);
            Assert.Contains("d", cd_scc.Nodes);
            Assert.False(cd_scc.IsTrivial);

            var gh_scc = result.Components.Single(c => c.Nodes.Contains("g"));
            Assert.Equal(2, gh_scc.Size);
            Assert.Contains("g", gh_scc.Nodes);
            Assert.Contains("h", gh_scc.Nodes);
            Assert.False(gh_scc.IsTrivial);

            var f_scc = result.Components.Single(c => c.Nodes.Contains("f") && c.Size == 1);
            Assert.Single(f_scc.Nodes);
            Assert.Equal("f", f_scc.Nodes.Single());
            Assert.True(f_scc.IsTrivial);
        }

        #endregion
    }
}
