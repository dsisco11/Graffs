# Graffs

A high-performance directed graph library for .NET 8.

## Features

- **Graph Implementations**: Immutable adjacency list, adjacency matrix, compact CSR format, and mutable adjacency list with forward and reverse indexes
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

## Core Concepts

### Nodes and Node Identity

Graffs graphs are generic over node type (`TNode`). Nodes can be any type that is non-null and properly implements equality (via `Equals` and `GetHashCode`). The graph uses node equality to identify and distinguish nodes.

**Simple nodes** - Use primitive types like `string` or `int` directly as node identifiers:

```csharp
// Strings as node keys
var graph = new GraphBuilder<string>()
    .AddEdge("A", "B")
    .AddEdge("B", "C")
    .Build();

// Integers as node keys
var graph = new GraphBuilder<int>()
    .AddEdge(1, 2)
    .AddEdge(2, 3)
    .Build();
```

**Rich nodes** - Use custom types implementing `IGraphNode<TKey>` for nodes that carry additional data:

```csharp
// IGraphNode<TKey> provides a standard way to expose node identity
public interface IGraphNode<out TKey> where TKey : notnull
{
    TKey Id { get; }              // Unique identifier for this node
    string? DisplayName { get; }  // Optional human-readable name
}

// Example: A task node with metadata
public class TaskNode : IGraphNode<string>
{
    public string Id { get; }
    public string? DisplayName { get; }
    public TimeSpan EstimatedDuration { get; }

    public TaskNode(string id, string? displayName = null, TimeSpan? duration = null)
    {
        Id = id;
        DisplayName = displayName;
        EstimatedDuration = duration ?? TimeSpan.Zero;
    }

    // Important: Override Equals/GetHashCode based on Id for graph operations
    public override bool Equals(object? obj) => obj is TaskNode other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

// Use rich nodes in a graph
var compile = new TaskNode("compile", "Compile Source", TimeSpan.FromMinutes(2));
var test = new TaskNode("test", "Run Tests", TimeSpan.FromMinutes(5));
var deploy = new TaskNode("deploy", "Deploy App", TimeSpan.FromMinutes(1));

var graph = new DependencyGraphBuilder<TaskNode>()
    .DependsOn(test, compile)    // test depends on compile
    .DependsOn(deploy, test)     // deploy depends on test
    .Build();
```

> **Note**: `IGraphNode<TKey>` is optional. It provides a standardized interface for node identification and display, which can be useful for debugging, logging, or when building tooling around graphs. Your nodes work with Graffs as long as they implement proper equality semantics.

## Quick Start

```csharp
using Graffs;
using Graffs.Builders;
using Graffs.Algorithms;

// Build a dependency graph using string keys
var graph = new DependencyGraphBuilder<string>()
    .DependsOn("C", "A")  // C depends on A (edge: A → C)
    .DependsOn("C", "B")  // C depends on B (edge: B → C)
    .DependsOn("D", "C")  // D depends on C (edge: C → D)
    .Build();

// Topological sort - returns nodes in dependency order
var result = TopologicalSort.KahnSort(graph);
// Result: [A, B, C, D] or [B, A, C, D] (dependencies come before dependents)

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
| `MutableDirectedGraph<T>` | Incremental relationships | O(1) avg | O(V + E) |

### Mutable Relationships

`Graffs.Implementations.MutableDirectedGraph<TNode>` stores edges in the same direction as other Graffs graphs: **dependency → dependent**. It supports cycles and self-edges and implements `IDirectedGraph<TNode>` for use with existing algorithms. Keep the graph unchanged while an algorithm runs.

```csharp
using Graffs.Implementations;

var graph = new MutableDirectedGraph<string>(StringComparer.OrdinalIgnoreCase);
graph.AddNode("compile");                       // Optional: register an isolated node.
graph.AddEdge("compile", "test");               // Registers missing endpoints.
graph.AddEdge("test", "deploy");

var dependents = graph.GetDependents("compile"); // Live read-only view: test.
var dependencies = graph.GetDependencies("test"); // Live read-only view: compile.
bool exists = graph.HasEdge("COMPILE", "TEST");  // Uses the supplied comparer.

// Copy only this neighborhood before changing relationships during traversal.
foreach (var dependent in graph.GetDependentsSnapshot("compile"))
    graph.RemoveEdge("compile", dependent);

graph.RemoveNode("test");                       // Removes all incident edges.
graph.Clear();
```

`AddNode` and `AddEdge` return whether a new registration or edge was added. Duplicate edges are harmless. `RemoveEdge` and `RemoveNode` return `false` when absent; removing an edge retains its endpoints. `ContainsNode` checks registration. All node arguments must be non-null.

The supplied comparer (or `EqualityComparer<TNode>.Default`) defines identity in every index. Neighbor results retain the **first registered node instance**, including when later calls use different but comparer-equal instances. Removal followed by re-registration establishes a new instance. Use `ReferenceEqualityComparer.Instance` when distinct object references must remain distinct nodes even if their values compare equal. Equality and hash codes must remain stable while nodes are registered.

Ordinary neighbor queries return cached live read-only views without copying or exposing mutable sets. Views reflect subsequent relationship changes; removing their node or calling `Clear` empties them permanently, even if the node is registered again. Missing-node queries return an empty collection that stays empty. Enumeration order is unspecified. Use `GetDependenciesSnapshot` or `GetDependentsSnapshot` when changing the enumerated neighborhood during traversal: these return stable read-only copies of the direct neighbors, retaining the original node references rather than cloning objects. The graph is not thread safe.

Through `IDirectedGraph<TNode>`, `GetDependencies` and `GetDependents` return **immutable snapshots**, preserving that interface's collection guarantees and the graph's comparer. Each node and direction caches its snapshot: the first read after that neighborhood changes copies only its current neighbors; subsequent reads reuse the result. The concrete methods above continue to return live views. `Nodes` and `Edges` cache immutable snapshots of the whole graph. Registering or removing a node invalidates `Nodes`; adding or removing a relationship invalidates `Edges`. Mutations only discard affected cached references, and snapshots rebuild on the next read. Duplicate additions and missing removals preserve caches. Previously returned snapshots stay unchanged. Reading a valid cached property takes O(1) time; rebuilding whole-graph snapshots visits the graph.

`GetIncomingEdges` and `GetOutgoingEdges` snapshot only the requested node's incident edges on each call. `Count` reads the node count directly, and graph enumeration visits current registrations without copying. `HasPath` searches forward relationships using the supplied comparer, terminates on cycles, and treats a registered node as reachable from itself. Existing algorithms retain their own equality rules; passing a graph does not change an algorithm's comparer behavior.

```csharp
IDirectedGraph<string> directed = graph;
var savedDependents = directed.GetDependents("compile"); // Immutable neighborhood snapshot.
var savedNodes = directed.Nodes;                         // Immutable whole-node snapshot.
graph.AddEdge("compile", "package");                    // Saved collections stay unchanged.
```

Node and edge lookup and edge insertion/removal take expected amortized O(1) time with normal hash-table behavior. Edge changes update only the endpoints' sets; neither direction of neighbor lookup scans other nodes or rebuilds the graph. Removing a node visits only its incoming and outgoing relationships. A snapshot copies only the selected neighborhood. Each neighborhood uses a dense list and a hash index, so traversal visits current neighbors even after many removals. Storage is O(V + E), with allocated capacity retained after removals; `Clear` visits the entire graph. Hash collisions and hash-table resizing can affect individual operation costs.

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
