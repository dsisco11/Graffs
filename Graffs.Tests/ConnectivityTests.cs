using System;
using System.Collections.Immutable;
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
    /// Comprehensive tests for connectivity analysis algorithms.
    /// </summary>
    public sealed class ConnectivityTests
    {
        #region Reachability Analysis Tests

        [Fact]
        public void FindReachableNodes_SingleNode_ReturnsSelfOnly()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder.AddNode("A").Build();

            // Act
            var result = ConnectivityAnalysis.FindReachableNodes(graph, "A");

            // Assert
            Assert.Equal("A", result.StartNode);
            Assert.Single(result.ReachableNodes);
            Assert.Contains("A", result.ReachableNodes);
            Assert.Equal(1, result.ReachableCount);
            Assert.Equal(0, result.GetDistance("A"));
            Assert.Equal(0, result.MaxDistance);
            Assert.Equal(0, result.AverageDistance);
            Assert.True(result.ProcessingTimeMs >= 0);
        }

        [Fact]
        public void FindReachableNodes_LinearChain_ReturnsAllDownstream()
        {
            // Arrange: A → B → C → D
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindReachableNodes(graph, "A");

            // Assert
            Assert.Equal("A", result.StartNode);
            Assert.Equal(4, result.ReachableCount);
            Assert.Contains("A", result.ReachableNodes);
            Assert.Contains("B", result.ReachableNodes);
            Assert.Contains("C", result.ReachableNodes);
            Assert.Contains("D", result.ReachableNodes);
            
            // Check distances
            Assert.Equal(0, result.GetDistance("A"));
            Assert.Equal(1, result.GetDistance("B"));
            Assert.Equal(2, result.GetDistance("C"));
            Assert.Equal(3, result.GetDistance("D"));
            Assert.Equal(3, result.MaxDistance);
        }

        [Fact]
        public void FindReachableNodes_FromMiddleOfChain_ReturnsPartialReachability()
        {
            // Arrange: A → B → C → D
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindReachableNodes(graph, "B");

            // Assert
            Assert.Equal("B", result.StartNode);
            Assert.Equal(3, result.ReachableCount);
            Assert.Contains("B", result.ReachableNodes);
            Assert.Contains("C", result.ReachableNodes);
            Assert.Contains("D", result.ReachableNodes);
            Assert.DoesNotContain("A", result.ReachableNodes);
            
            // Check distances
            Assert.Equal(0, result.GetDistance("B"));
            Assert.Equal(1, result.GetDistance("C"));
            Assert.Equal(2, result.GetDistance("D"));
            Assert.Equal(-1, result.GetDistance("A")); // Not reachable
        }

        [Fact]
        public void FindReachableNodes_DiamondPattern_ReachesAllNodes()
        {
            // Arrange: A → B, A → C, B → D, C → D
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("A", "C")
                .AddEdge("B", "D")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindReachableNodes(graph, "A");

            // Assert
            Assert.Equal(4, result.ReachableCount);
            Assert.Equal(0, result.GetDistance("A"));
            Assert.Equal(1, result.GetDistance("B"));
            Assert.Equal(1, result.GetDistance("C"));
            Assert.Equal(2, result.GetDistance("D")); // Shortest path from A
        }

        [Fact]
        public void FindReachableNodes_DisconnectedGraph_ReachesOnlyConnectedComponent()
        {
            // Arrange: A → B and C → D (disconnected)
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindReachableNodes(graph, "A");

            // Assert
            Assert.Equal(2, result.ReachableCount);
            Assert.Contains("A", result.ReachableNodes);
            Assert.Contains("B", result.ReachableNodes);
            Assert.DoesNotContain("C", result.ReachableNodes);
            Assert.DoesNotContain("D", result.ReachableNodes);
        }

        [Fact]
        public void FindReachableNodes_CyclicGraph_HandlesAllNodes()
        {
            // Arrange: A → B → C → A (cycle)
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "A")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindReachableNodes(graph, "A");

            // Assert
            Assert.Equal(3, result.ReachableCount);
            Assert.Contains("A", result.ReachableNodes);
            Assert.Contains("B", result.ReachableNodes);
            Assert.Contains("C", result.ReachableNodes);
            
            // In a cycle, all nodes should be reachable
            Assert.Equal(0, result.GetDistance("A"));
            Assert.Equal(1, result.GetDistance("B"));
            Assert.Equal(2, result.GetDistance("C"));
        }

        [Fact]
        public void FindReachableNodes_NodeNotInGraph_ThrowsArgumentException()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder.AddNode("A").Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ConnectivityAnalysis.FindReachableNodes(graph, "B"));
        }

        [Fact]
        public void FindReachableNodes_NullArguments_ThrowsArgumentNullException()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder.AddNode("A").Build();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ConnectivityAnalysis.FindReachableNodes<string>(null!, "A"));
            Assert.Throws<ArgumentNullException>(() => 
                ConnectivityAnalysis.FindReachableNodes(graph, null!));
        }

        #endregion

        #region Strongly Connected Components Tests

        [Fact]
        public void FindStronglyConnectedComponents_EmptyGraph_ReturnsEmptyResult()
        {
            // Arrange
            var graph = new AdjacencyListGraph<string>(Array.Empty<IGraphEdge<string>>());

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(0, result.ComponentCount);
            Assert.Empty(result.Components);
            Assert.True(result.ProcessingTimeMs >= 0);
            Assert.False(result.IsStronglyConnected);
            Assert.False(result.HasMultipleComponents);
        }

        [Fact]
        public void FindStronglyConnectedComponents_SingleNode_ReturnsSingleTrivialComponent()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder.AddNode("A").Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(1, result.ComponentCount);
            Assert.True(result.IsStronglyConnected);
            Assert.False(result.HasMultipleComponents);
            
            var component = result.Components[0];
            Assert.True(component.IsTrivial);
            Assert.True(component.IsSingleNode);
            Assert.False(component.HasCycles);
            Assert.Contains("A", component.Nodes);
        }

        [Fact]
        public void FindStronglyConnectedComponents_SingleNodeWithSelfLoop_ReturnsNonTrivialComponent()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddNode("A")
                .AddEdge("A", "A")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(1, result.ComponentCount);
            Assert.True(result.IsStronglyConnected);
            
            var component = result.Components[0];
            Assert.False(component.IsTrivial);
            Assert.True(component.IsSingleNode);
            Assert.True(component.HasCycles);
            Assert.Contains("A", component.Nodes);
        }

        [Fact]
        public void FindStronglyConnectedComponents_LinearChain_ReturnsMultipleTrivialComponents()
        {
            // Arrange: A → B → C
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(3, result.ComponentCount);
            Assert.False(result.IsStronglyConnected);
            Assert.True(result.HasMultipleComponents);
            
            // All components should be trivial
            Assert.All(result.Components, c => Assert.True(c.IsTrivial));
            Assert.All(result.Components, c => Assert.True(c.IsSingleNode));
            Assert.All(result.Components, c => Assert.False(c.HasCycles));
            
            // Check that all nodes are covered
            var allNodes = result.Components.SelectMany(c => c.Nodes).ToList();
            Assert.Contains("A", allNodes);
            Assert.Contains("B", allNodes);
            Assert.Contains("C", allNodes);
        }

        [Fact]
        public void FindStronglyConnectedComponents_SimpleCycle_ReturnsSingleNonTrivialComponent()
        {
            // Arrange: A → B → A
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
            
            var component = result.Components[0];
            Assert.False(component.IsTrivial);
            Assert.False(component.IsSingleNode);
            Assert.True(component.HasCycles);
            Assert.Equal(2, component.Size);
            Assert.Contains("A", component.Nodes);
            Assert.Contains("B", component.Nodes);
        }

        [Fact]
        public void FindStronglyConnectedComponents_ComplexGraph_ReturnsCorrectComponents()
        {
            // Arrange: A → B → C → B (cycle B-C) and A also connects to isolated D
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "B")
                .AddEdge("A", "D")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(3, result.ComponentCount); // A, (B-C), D
            Assert.False(result.IsStronglyConnected);
            Assert.True(result.HasMultipleComponents);
            
            // Find the non-trivial component (B-C cycle)
            var nonTrivialComponent = result.NonTrivialComponents.Single();
            Assert.Equal(2, nonTrivialComponent.Size);
            Assert.Contains("B", nonTrivialComponent.Nodes);
            Assert.Contains("C", nonTrivialComponent.Nodes);
            
            // Check trivial components
            var trivialComponents = result.TrivialComponents.ToList();
            Assert.Equal(2, trivialComponents.Count);
            var trivialNodes = trivialComponents.SelectMany(c => c.Nodes).ToList();
            Assert.Contains("A", trivialNodes);
            Assert.Contains("D", trivialNodes);
        }

        [Fact]
        public void FindStronglyConnectedComponents_NullGraph_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ConnectivityAnalysis.FindStronglyConnectedComponents<string>(null!));
        }

        #endregion

        #region Path Existence Tests

        [Fact]
        public void HasPath_SameNode_ReturnsTrue()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder.AddNode("A").Build();

            // Act & Assert
            Assert.True(ConnectivityAnalysis.HasPath(graph, "A", "A"));
        }

        [Fact]
        public void HasPath_DirectEdge_ReturnsTrue()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder.AddEdge("A", "B").Build();

            // Act & Assert
            Assert.True(ConnectivityAnalysis.HasPath(graph, "A", "B"));
            Assert.False(ConnectivityAnalysis.HasPath(graph, "B", "A"));
        }

        [Fact]
        public void HasPath_IndirectPath_ReturnsTrue()
        {
            // Arrange: A → B → C
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .Build();

            // Act & Assert
            Assert.True(ConnectivityAnalysis.HasPath(graph, "A", "C"));
            Assert.False(ConnectivityAnalysis.HasPath(graph, "C", "A"));
        }

        [Fact]
        public void HasPath_NoPath_ReturnsFalse()
        {
            // Arrange: A → B and C → D (disconnected)
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("C", "D")
                .Build();

            // Act & Assert
            Assert.False(ConnectivityAnalysis.HasPath(graph, "A", "C"));
            Assert.False(ConnectivityAnalysis.HasPath(graph, "B", "D"));
        }

        [Fact]
        public void HasPath_NodeNotInGraph_ReturnsFalse()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder.AddNode("A").Build();

            // Act & Assert
            Assert.False(ConnectivityAnalysis.HasPath(graph, "A", "B"));
            Assert.False(ConnectivityAnalysis.HasPath(graph, "B", "A"));
        }

        #endregion

        #region Reverse Reachability Tests

        [Fact]
        public void FindNodesReaching_SingleNode_ReturnsSelfOnly()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder.AddNode("A").Build();

            // Act
            var result = ConnectivityAnalysis.FindNodesReaching(graph, "A");

            // Assert
            Assert.Single(result);
            Assert.Contains("A", result);
        }

        [Fact]
        public void FindNodesReaching_LinearChain_ReturnsAllUpstream()
        {
            // Arrange: A → B → C → D
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("C", "D")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindNodesReaching(graph, "D");

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Contains("A", result);
            Assert.Contains("B", result);
            Assert.Contains("C", result);
            Assert.Contains("D", result);
        }

        [Fact]
        public void FindNodesReaching_NodeNotInGraph_ReturnsEmpty()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder.AddNode("A").Build();

            // Act
            var result = ConnectivityAnalysis.FindNodesReaching(graph, "B");

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Result Type Tests

        [Fact]
        public void ConnectivityResult_AreInSameComponent_WorksCorrectly()
        {
            // Arrange: A → B → A (cycle) and isolated C
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .AddNode("C")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.True(result.AreInSameComponent("A", "B"));
            Assert.False(result.AreInSameComponent("A", "C"));
            Assert.False(result.AreInSameComponent("B", "C"));
        }

        [Fact]
        public void ConnectivityResult_FindComponentContaining_WorksCorrectly()
        {
            // Arrange
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "A")
                .AddNode("C")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            var componentA = result.FindComponentContaining("A");
            var componentC = result.FindComponentContaining("C");
            
            Assert.NotNull(componentA);
            Assert.NotNull(componentC);
            Assert.NotEqual(componentA, componentC);
            Assert.Contains("A", componentA.Nodes);
            Assert.Contains("B", componentA.Nodes);
            Assert.Contains("C", componentC.Nodes);
        }

        [Fact]
        public void ReachabilityResult_GetNodesAtDistance_WorksCorrectly()
        {
            // Arrange: A → B → C and A → D
            var builder = new GraphBuilder<string>();
            var graph = builder
                .AddEdge("A", "B")
                .AddEdge("B", "C")
                .AddEdge("A", "D")
                .Build();

            // Act
            var result = ConnectivityAnalysis.FindReachableNodes(graph, "A");

            // Assert
            var nodesAtDistance0 = result.GetNodesAtDistance(0).ToList();
            var nodesAtDistance1 = result.GetNodesAtDistance(1).ToList();
            var nodesAtDistance2 = result.GetNodesAtDistance(2).ToList();
            
            Assert.Single(nodesAtDistance0);
            Assert.Contains("A", nodesAtDistance0);
            
            Assert.Equal(2, nodesAtDistance1.Count);
            Assert.Contains("B", nodesAtDistance1);
            Assert.Contains("D", nodesAtDistance1);
            
            Assert.Single(nodesAtDistance2);
            Assert.Contains("C", nodesAtDistance2);
        }

        #endregion

        #region Large Graph Tests

        [Fact]
        public void FindReachableNodes_ModerateGraph_HandlesEfficiently()
        {
            // Arrange: Create a moderate-sized graph with 50 nodes in 5 layers
            var builder = new GraphBuilder<int>();
            
            // Layer structure: each layer has 10 nodes, each node connects to 3 nodes in next layer
            for (int layer = 0; layer < 4; layer++)
            {
                for (int node = 0; node < 10; node++)
                {
                    int currentNode = layer * 10 + node;
                    
                    // Connect to 3 nodes in next layer
                    for (int target = 0; target < 3; target++)
                    {
                        int targetNode = (layer + 1) * 10 + ((node + target) % 10);
                        builder.AddEdge(currentNode, targetNode);
                    }
                }
            }
            
            var graph = builder.Build();

            // Act
            var result = ConnectivityAnalysis.FindReachableNodes(graph, 0); // Start from first node

            // Assert
            Assert.Equal(50, graph.Count); // Verify graph size
            Assert.True(result.ReachableCount > 10); // Should reach many nodes
            Assert.True(result.ProcessingTimeMs < 1000); // Should complete quickly
            Assert.Equal(0, result.GetDistance(0)); // Start node at distance 0
            Assert.True(result.MaxDistance <= 4); // Maximum 4 hops in our layered structure
        }

        [Fact]
        public void FindStronglyConnectedComponents_ModerateGraph_HandlesEfficiently()
        {
            // Arrange: Create a graph with multiple components and some cycles
            var builder = new GraphBuilder<int>();
            
            // Create 5 separate components with 4 nodes each
            for (int component = 0; component < 5; component++)
            {
                int baseNode = component * 4;
                
                if (component % 2 == 0)
                {
                    // Even components: create cycles
                    builder.AddEdge(baseNode, baseNode + 1)
                          .AddEdge(baseNode + 1, baseNode + 2)
                          .AddEdge(baseNode + 2, baseNode + 3)
                          .AddEdge(baseNode + 3, baseNode); // Complete the cycle
                }
                else
                {
                    // Odd components: create linear chains
                    builder.AddEdge(baseNode, baseNode + 1)
                          .AddEdge(baseNode + 1, baseNode + 2)
                          .AddEdge(baseNode + 2, baseNode + 3);
                }
            }
            
            var graph = builder.Build();

            // Act
            var result = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

            // Assert
            Assert.Equal(20, graph.Count); // 5 components × 4 nodes each
            Assert.True(result.ComponentCount >= 5); // At least 5 components
            Assert.True(result.ProcessingTimeMs < 1000); // Should complete quickly
            
            // Should have some non-trivial components (the cycles)
            Assert.True(result.NonTrivialComponents.Any());
            
            // Should have some trivial components (the linear chains create multiple trivial SCCs)
            Assert.True(result.TrivialComponents.Any());
        }

        [Fact]
        public void ConnectivityAnalysis_ComplexConnectedGraph_HandlesCorrectly()
        {
            // Arrange: Create a complex but manageable graph structure
            var builder = new GraphBuilder<string>();
            
            // Create a "hub and spoke" pattern with interconnected spokes
            string hub = "Hub";
            builder.AddNode(hub);
            
            // Create 8 spokes, each with 3 nodes
            for (int spoke = 0; spoke < 8; spoke++)
            {
                string spoke1 = $"S{spoke}_1";
                string spoke2 = $"S{spoke}_2";
                string spoke3 = $"S{spoke}_3";
                
                // Hub connects to first node of each spoke
                builder.AddEdge(hub, spoke1);
                
                // Create spoke chain
                builder.AddEdge(spoke1, spoke2);
                builder.AddEdge(spoke2, spoke3);
                
                // Add some cross-connections between adjacent spokes
                if (spoke < 7)
                {
                    string nextSpoke1 = $"S{spoke + 1}_1";
                    builder.AddEdge(spoke3, nextSpoke1);
                }
            }
            
            // Close the ring by connecting last spoke back to first
            builder.AddEdge("S7_3", "S0_1");
            
            var graph = builder.Build();

            // Act - Test multiple operations
            var reachabilityFromHub = ConnectivityAnalysis.FindReachableNodes(graph, hub);
            var sccResult = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);
            var hasPathTest = ConnectivityAnalysis.HasPath(graph, hub, "S7_3");

            // Assert
            Assert.Equal(25, graph.Count); // 1 hub + 8 spokes × 3 nodes each
            
            // Hub should reach all nodes
            Assert.Equal(25, reachabilityFromHub.ReachableCount);
            
            // Should have reasonable processing times
            Assert.True(reachabilityFromHub.ProcessingTimeMs < 1000);
            Assert.True(sccResult.ProcessingTimeMs < 1000);
            
            // Path should exist from hub to any spoke node
            Assert.True(hasPathTest);
            
            // Should detect the strongly connected components correctly
            Assert.True(sccResult.ComponentCount > 0);
        }

        #endregion
    }
}
