# Graffs

A high-performance directed graph library for .NET 8.

## Features

- **Graph Implementations**: Adjacency list, adjacency matrix, and compact CSR format
- **Topological Sorting**: Kahn's algorithm and DFS-based sorting with cycle detection
- **Cycle Detection**: Johnson's algorithm for finding all elementary cycles
- **Connectivity Analysis**: Strongly connected components (Tarjan's) and weakly connected components (Union-Find)
- **Path Finding**: BFS shortest path and DFS all simple paths
- **Graph Slicing**: Dependency chain analysis and parallel execution level partitioning
- **Fluent Builders**: Easy graph construction with `GraphBuilder<T>` and `DependencyGraphBuilder<T>`

## Installation

```bash
dotnet add package Graffs --version 0.1.0
```

## Quick Start

```csharp
using Graffs;
using Graffs.Builders;
using Graffs.Algorithms;

// Build a dependency graph
var graph = new DependencyGraphBuilder<string>()
    .DependsOn("C", "A")  // C depends on A
    .DependsOn("C", "B")  // C depends on B
    .DependsOn("D", "C")  // D depends on C
    .Build();

// Topological sort
var result = TopologicalSort.KahnSort(graph);
// Result: [A, B, C, D] or [B, A, C, D]

// Check for cycles
bool hasCycles = CycleDetection.HasCycles(graph);

// Find strongly connected components
var sccResult = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);
```

## Graph Implementations

| Implementation            | Best For           | Edge Query | Memory   |
| ------------------------- | ------------------ | ---------- | -------- |
| `AdjacencyListGraph<T>`   | Sparse graphs      | O(1) avg   | O(V + E) |
| `AdjacencyMatrixGraph<T>` | Dense graphs       | O(1)       | O(V²)    |
| `CompactGraph<T>`         | Memory-constrained | O(log E)   | O(V + E) |

## Algorithms

### Topological Sort

```csharp
// Kahn's algorithm (BFS-based)
var result = TopologicalSort.KahnSort(graph);

// DFS-based with cycle detection
var result = TopologicalSort.DfsSort(graph);

// Stable sort with custom ordering
var result = TopologicalSort.StableSort(graph, StringComparer.Ordinal);
```

### Cycle Detection

```csharp
// Quick check
bool hasCycles = CycleDetection.HasCycles(graph);

// Find all cycles
var result = CycleDetection.FindAllCycles(graph);
foreach (var cycle in result.Cycles)
{
    Console.WriteLine(cycle.ToPathString());
}
```

### Connectivity Analysis

```csharp
// Strongly connected components
var scc = ConnectivityAnalysis.FindStronglyConnectedComponents(graph);

// Weakly connected components
var wcc = ConnectivityAnalysis.FindWeaklyConnectedComponents(graph);

// Reachability
var reachable = ConnectivityAnalysis.FindReachableNodes(graph, startNode);
```

### Path Finding

```csharp
// Shortest path
var path = PathFinding.FindShortestPath(graph, source, target);

// All simple paths
var paths = PathFinding.FindAllSimplePaths(graph, source, target, maxPaths: 100);
```

### Graph Slicing

```csharp
// Analyze dependency structure for parallel execution
var slicing = GraphSlicer.AnalyzeDependencyStructure(graph);
foreach (var level in slicing.Levels)
{
    // Nodes at each level can be processed in parallel
    Console.WriteLine($"Level {level.LevelIndex}: {string.Join(", ", level.Nodes)}");
}
```

## License

MIT License - see [LICENSE](LICENSE) for details.
