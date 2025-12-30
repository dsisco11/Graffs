using System;
using System.Linq;
using Xunit;
using Graffs.Analysis;
using Graffs.Builders;
using Graffs.Results;

namespace Graffs.Tests;

/// <summary>
/// Integration tests demonstrating graph slicing on realistic scenarios.
/// These tests validate the design document requirements and show practical applications.
/// </summary>
public sealed class GraphSlicingIntegrationTests
{
    #region Design Document Example Tests

    [Fact]
    public void GraphSlicer_SoftwareBuildDependencies_IdentifiesIndependentModules()
    {
        // Arrange: Simulate a software build dependency graph
        // Module A: Core (no deps) → Utils → BusinessLogic
        // Module B: Config (no deps) → Settings
        // Module C: Tests depend on both BusinessLogic and Settings
        var graph = new GraphBuilder<string>()
            // Module A chain
            .AddEdge("Core", "Utils")
            .AddEdge("Utils", "BusinessLogic")
            // Module B chain  
            .AddEdge("Config", "Settings")
            // Cross-module dependency
            .AddEdge("BusinessLogic", "Tests")
            .AddEdge("Settings", "Tests")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert - Should identify optimal build parallelization
        Assert.True(result.IsValid);
        Assert.Equal(6, result.TotalNodes);
        Assert.Equal(4, result.MaxDependencyDepth); // Core → Utils → BusinessLogic → Tests

        // Should identify that Core and Config can be built in parallel (independent chains)
        var rootLevel = result.Levels[0];
        Assert.Equal(2, rootLevel.NodeCount);
        Assert.Contains("Core", rootLevel.Nodes);
        Assert.Contains("Config", rootLevel.Nodes);

        // Level 1: Utils and Settings can be built in parallel after their roots
        var level1 = result.Levels[1];
        Assert.Equal(2, level1.NodeCount);
        Assert.Contains("Utils", level1.Nodes);
        Assert.Contains("Settings", level1.Nodes);

        // Level 2: BusinessLogic (depends on Utils)
        var level2 = result.Levels[2];
        Assert.Single(level2.Nodes);
        Assert.Contains("BusinessLogic", level2.Nodes);

        // Level 3: Tests (depends on both BusinessLogic and Settings)
        var level3 = result.Levels[3];
        Assert.Single(level3.Nodes);
        Assert.Contains("Tests", level3.Nodes);
        Assert.Contains("BusinessLogic", level3.LevelDependencies);
        Assert.Contains("Settings", level3.LevelDependencies);
    }

    [Fact]
    public void GraphSlicer_DataProcessingPipeline_IdentifiesParallelProcessingOpportunities()
    {
        // Arrange: Data processing pipeline with multiple data sources
        // Source1 → Extract1 → Transform1 → Load1
        // Source2 → Extract2 → Transform2 → Load2  
        // Load1, Load2 → Validate → Report
        var graph = new GraphBuilder<string>()
            // Pipeline 1
            .AddEdge("Source1", "Extract1")
            .AddEdge("Extract1", "Transform1")
            .AddEdge("Transform1", "Load1")
            // Pipeline 2
            .AddEdge("Source2", "Extract2")
            .AddEdge("Extract2", "Transform2")
            .AddEdge("Transform2", "Load2")
            // Convergence
            .AddEdge("Load1", "Validate")
            .AddEdge("Load2", "Validate")
            .AddEdge("Validate", "Report")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert - Should identify parallel processing opportunities
        Assert.True(result.IsValid);
        Assert.Equal(10, result.TotalNodes);
        Assert.Equal(6, result.MaxDependencyDepth);

        // Sources can be processed in parallel
        var level0 = result.Levels[0];
        Assert.Equal(2, level0.NodeCount);
        Assert.Contains("Source1", level0.Nodes);
        Assert.Contains("Source2", level0.Nodes);

        // Extract operations can be done in parallel
        var level1 = result.Levels[1];
        Assert.Equal(2, level1.NodeCount);
        Assert.Contains("Extract1", level1.Nodes);
        Assert.Contains("Extract2", level1.Nodes);

        // Transform operations can be done in parallel
        var level2 = result.Levels[2];
        Assert.Equal(2, level2.NodeCount);
        Assert.Contains("Transform1", level2.Nodes);
        Assert.Contains("Transform2", level2.Nodes);

        // Load operations can be done in parallel
        var level3 = result.Levels[3];
        Assert.Equal(2, level3.NodeCount);
        Assert.Contains("Load1", level3.Nodes);
        Assert.Contains("Load2", level3.Nodes);

        // Validate must wait for both loads
        var level4 = result.Levels[4];
        Assert.Single(level4.Nodes);
        Assert.Contains("Validate", level4.Nodes);
        Assert.Contains("Load1", level4.LevelDependencies);
        Assert.Contains("Load2", level4.LevelDependencies);

        // Report is the final step
        var level5 = result.Levels[5];
        Assert.Single(level5.Nodes);
        Assert.Contains("Report", level5.Nodes);
        Assert.Contains("Validate", level5.LevelDependencies);
        Assert.True(level5.IsLeafLevel);
    }

    [Fact]
    public void GraphSlicer_MicroservicesArchitecture_IdentifiesServiceGroups()
    {
        // Arrange: Microservices dependency graph
        // Group 1: UserService → AuthService → NotificationService
        // Group 2: ProductService → InventoryService  
        // Group 3: OrderService depends on both AuthService and InventoryService → PaymentService
        var graph = new GraphBuilder<string>()
            // Service group 1
            .AddEdge("UserService", "AuthService")
            .AddEdge("AuthService", "NotificationService")
            // Service group 2
            .AddEdge("ProductService", "InventoryService")
            // Cross-group dependencies
            .AddEdge("AuthService", "OrderService")
            .AddEdge("InventoryService", "OrderService")
            .AddEdge("OrderService", "PaymentService")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert - Should identify service deployment groups
        Assert.True(result.IsValid);
        Assert.Equal(7, result.TotalNodes);

        // Core services that can be deployed independently
        var level0 = result.Levels[0];
        Assert.Equal(2, level0.NodeCount);
        Assert.Contains("UserService", level0.Nodes);
        Assert.Contains("ProductService", level0.Nodes);

        // Find the OrderService level and verify its dependencies
        var orderServiceLevel = result.Levels.First(l => l.Nodes.Contains("OrderService"));
        Assert.Contains("AuthService", orderServiceLevel.LevelDependencies);
        Assert.Contains("InventoryService", orderServiceLevel.LevelDependencies);

        // PaymentService should be at the highest level
        var paymentServiceLevel = result.Levels.First(l => l.Nodes.Contains("PaymentService"));
        Assert.True(paymentServiceLevel.IsLeafLevel);
        Assert.Contains("OrderService", paymentServiceLevel.LevelDependencies);
    }

    #endregion

    #region Real-World Complexity Tests

    [Fact]
    public void GraphSlicer_ComplexSoftwareSystem_HandlesMultipleIndependentModules()
    {
        // Arrange: Large software system with multiple independent modules
        var builder = new GraphBuilder<string>();

        // Module 1: Frontend stack (5 components)
        builder.AddEdge("HTML", "CSS")
               .AddEdge("CSS", "JavaScript")
               .AddEdge("JavaScript", "ReactComponents")
               .AddEdge("ReactComponents", "Frontend");

        // Module 2: Backend API (4 components)  
        builder.AddEdge("Database", "ORM")
               .AddEdge("ORM", "BusinessLayer")
               .AddEdge("BusinessLayer", "APIControllers");

        // Module 3: DevOps/Infrastructure (3 components)
        builder.AddEdge("Infrastructure", "Deployment")
               .AddEdge("Deployment", "Monitoring");

        // Module 4: Testing (depends on multiple modules)
        builder.AddEdge("Frontend", "IntegrationTests")
               .AddEdge("APIControllers", "IntegrationTests")
               .AddEdge("IntegrationTests", "E2ETests");

        var graph = builder.Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(14, result.TotalNodes);

        // Should identify 3 independent chains at the root level
        var rootLevel = result.Levels[0];
        Assert.Equal(3, rootLevel.NodeCount);
        Assert.Contains("HTML", rootLevel.Nodes);
        Assert.Contains("Database", rootLevel.Nodes);
        Assert.Contains("Infrastructure", rootLevel.Nodes);

        // Should identify opportunities for parallel development
        // Level 1 should have multiple components that can be worked on in parallel
        var level1 = result.Levels[1];
        Assert.True(level1.NodeCount >= 2);

        // Integration tests should depend on multiple modules
        var integrationTestLevel = result.Levels.First(l => l.Nodes.Contains("IntegrationTests"));
        Assert.Contains("Frontend", integrationTestLevel.LevelDependencies);
        Assert.Contains("APIControllers", integrationTestLevel.LevelDependencies);
    }

    [Fact]
    public void GraphSlicer_CloudArchitecture_IdentifiesDeploymentLayers()
    {
        // Arrange: Cloud infrastructure deployment dependencies
        var graph = new GraphBuilder<string>()
            // Foundation layer
            .AddEdge("VPC", "Subnets")
            .AddEdge("Subnets", "SecurityGroups")
            .AddEdge("SecurityGroups", "LoadBalancer")
            // Data layer
            .AddEdge("VPC", "DatabaseSubnet")
            .AddEdge("DatabaseSubnet", "Database")
            // Application layer  
            .AddEdge("LoadBalancer", "WebServers")
            .AddEdge("Database", "WebServers")
            .AddEdge("WebServers", "ApplicationServers")
            // Monitoring layer
            .AddEdge("ApplicationServers", "Monitoring")
            .AddEdge("WebServers", "Logging")
            .AddEdge("Monitoring", "Alerting")
            .AddEdge("Logging", "Alerting")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert - Should identify deployment sequence
        Assert.True(result.IsValid);
        Assert.Equal(11, result.TotalNodes);

        // VPC should be the foundation (level 0)
        var level0 = result.Levels[0];
        Assert.Single(level0.Nodes);
        Assert.Contains("VPC", level0.Nodes);

        // Verify proper layering - WebServers should depend on both LoadBalancer and Database
        var webServersLevel = result.Levels.First(l => l.Nodes.Contains("WebServers"));
        Assert.Contains("LoadBalancer", webServersLevel.LevelDependencies);
        Assert.Contains("Database", webServersLevel.LevelDependencies);

        // Alerting should be at the top level, depending on both Monitoring and Logging
        var alertingLevel = result.Levels.First(l => l.Nodes.Contains("Alerting"));
        Assert.Contains("Monitoring", alertingLevel.LevelDependencies);
        Assert.Contains("Logging", alertingLevel.LevelDependencies);
        Assert.True(alertingLevel.IsLeafLevel);
    }

    #endregion

    #region Performance and Scalability Tests

    [Fact]
    public void GraphSlicer_LargeHierarchicalSystem_PerformsEfficiently()
    {
        // Arrange: Create a large hierarchical system (500 nodes in 10 levels)
        var builder = new GraphBuilder<int>();
        
        const int levelsCount = 10;
        const int nodesPerLevel = 50;
        
        // Create hierarchical structure: each node in level N connects to 2 nodes in level N+1
        for (int level = 0; level < levelsCount - 1; level++)
        {
            for (int nodeIndex = 0; nodeIndex < nodesPerLevel; nodeIndex++)
            {
                int currentNode = level * nodesPerLevel + nodeIndex;
                
                // Connect to 2 nodes in next level
                int nextLevel = level + 1;
                int nextNode1 = nextLevel * nodesPerLevel + (nodeIndex * 2) % nodesPerLevel;
                int nextNode2 = nextLevel * nodesPerLevel + (nodeIndex * 2 + 1) % nodesPerLevel;
                
                builder.AddEdge(currentNode, nextNode1);
                if (nextNode1 != nextNode2) // Avoid self-loops
                {
                    builder.AddEdge(currentNode, nextNode2);
                }
            }
        }

        var graph = builder.Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(500, result.TotalNodes);
        Assert.Equal(10, result.MaxDependencyDepth);
        Assert.True(result.ProcessingTime.TotalMilliseconds < 5000); // Should complete in reasonable time

        // Verify level structure
        Assert.Equal(10, result.TotalLevels);
        
        // Each level should have approximately 50 nodes
        foreach (var level in result.Levels)
        {
            Assert.True(level.NodeCount > 0);
            Assert.True(level.NodeCount <= nodesPerLevel);
        }

        // Level 0 should have exactly 50 nodes (roots)
        Assert.Equal(50, result.Levels[0].NodeCount);
        
        // Last level should be all leaf nodes
        var lastLevel = result.Levels[result.Levels.Length - 1];
        Assert.True(lastLevel.IsLeafLevel);
    }

    #endregion

    #region Optimization Verification Tests

    [Fact]
    public void GraphSlicer_ReusesExistingAlgorithms_ProducesConsistentResults()
    {
        // Arrange: Create a graph that exercises all major algorithms
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("A", "C")
            .AddEdge("B", "D")
            .AddEdge("C", "D")
            .AddEdge("E", "F") // Independent chain
            .Build();

        // Act - Run slicing multiple times to ensure consistency
        var result1 = GraphSlicer.AnalyzeDependencyStructure(graph);
        var result2 = GraphSlicer.AnalyzeDependencyStructure(graph);
        var result3 = GraphSlicer.AnalyzeDependencyStructure(graph, StringComparer.Ordinal);

        // Assert - Results should be consistent
        Assert.True(result1.IsValid);
        Assert.True(result2.IsValid);
        Assert.True(result3.IsValid);

        // Same structure
        Assert.Equal(result1.TotalNodes, result2.TotalNodes);
        Assert.Equal(result1.TotalLevels, result2.TotalLevels);
        Assert.Equal(result1.IndependentChains.Length, result2.IndependentChains.Length);

        // Same structure with comparer
        Assert.Equal(result1.TotalNodes, result3.TotalNodes);
        Assert.Equal(result1.TotalLevels, result3.TotalLevels);
        Assert.Equal(result1.IndependentChains.Length, result3.IndependentChains.Length);

        // Should identify 2 independent chains (A-B-C-D diamond and E-F linear)
        Assert.Equal(2, result1.IndependentChains.Length);
        Assert.Equal(2, result2.IndependentChains.Length);
        Assert.Equal(2, result3.IndependentChains.Length);
    }

    #endregion

    #region Immutability and Thread Safety Tests

    [Fact]
    public void GraphSlicer_ResultsAreImmutable_CannotBeModified()
    {
        // Arrange
        var graph = new GraphBuilder<string>()
            .AddEdge("A", "B")
            .AddEdge("B", "C")
            .Build();

        // Act
        var result = GraphSlicer.AnalyzeDependencyStructure(graph);

        // Assert - Verify immutability (compilation test - these should not compile if mutable)
        // result.Levels = ImmutableArray<DependencyLevel<string>>.Empty; // Should not compile
        // result.IndependentChains[0].Nodes.Add("D"); // Should not compile
        // result.Levels[0].Nodes.Add("X"); // Should not compile

        // Verify collections are truly immutable
        Assert.IsType<System.Collections.Immutable.ImmutableArray<DependencyLevel<string>>>(result.Levels);
        Assert.IsType<System.Collections.Immutable.ImmutableArray<IndependentChain<string>>>(result.IndependentChains);
        Assert.IsType<System.Collections.Immutable.ImmutableArray<string>>(result.Levels[0].Nodes);
        Assert.IsType<System.Collections.Immutable.ImmutableHashSet<string>>(result.Levels[0].LevelDependencies);
    }

    #endregion
}
