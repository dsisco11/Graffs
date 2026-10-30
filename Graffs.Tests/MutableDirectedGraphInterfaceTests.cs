using System.Collections;
using Graffs.Algorithms;
using Graffs.Implementations;

namespace Graffs.Tests;

/// <summary>Verifies the mutable graph preserves the immutable directed-graph collection contract.</summary>
public class MutableDirectedGraphInterfaceTests
{
    #region Public API

    #region Collections and Identity

    /// <summary>Verifies every interface collection is immutable and independent of later mutations.</summary>
    [Fact]
    public void InterfaceCollections_AreStableSnapshotsWhileConcreteNeighborsStayLive()
    {
        var mutable = new MutableDirectedGraph<string>();
        IDirectedGraph<string> graph = mutable;
        mutable.AddEdge("dependency", "dependent");
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var incoming = graph.GetDependencies("dependent");
        var outgoing = graph.GetDependents("dependency");
        var incomingEdges = graph.GetIncomingEdges("dependent");
        var outgoingEdges = graph.GetOutgoingEdges("dependency");
        var live = mutable.GetDependents("dependency");

        Assert.Throws<NotSupportedException>(() => ((ICollection<string>)nodes).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<string>)incoming).Add("injected"));
        Assert.Throws<NotSupportedException>(() => ((ICollection<string>)outgoing).Remove("dependent"));
        Assert.Throws<NotSupportedException>(() => ((ICollection<IGraphEdge<string>>)edges).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<IGraphEdge<string>>)incomingEdges).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<IGraphEdge<string>>)outgoingEdges).Clear());

        // Retain all snapshots through edge insertion, node removal, and graph reset.
        mutable.AddEdge("dependency", "new");
        Assert.Equal(2, live.Count);
        Assert.Single(outgoing);
        mutable.RemoveNode("dependent");
        mutable.Clear();
        Assert.Empty(live);
        Assert.Equal(new[] { "dependency", "dependent" }, nodes.Order());
        Assert.Equal("dependency", Assert.Single(incoming));
        Assert.Equal("dependent", Assert.Single(outgoing));
        foreach (var savedEdges in new[] { edges, incomingEdges, outgoingEdges })
        {
            var edge = Assert.Single(savedEdges);
            Assert.Equal("dependency", edge.Source);
            Assert.Equal("dependent", edge.Target);
        }
        Assert.Empty(graph.Nodes);
        Assert.Empty(graph.Edges);
    }

    /// <summary>Verifies comparer-aware immutable set membership and canonical edge endpoint references.</summary>
    [Fact]
    public void InterfaceCollections_UseComparerAndOriginalRegistrations()
    {
        var mutable = new MutableDirectedGraph<string>(StringComparer.OrdinalIgnoreCase);
        IDirectedGraph<string> graph = mutable;
        var source = new string("Source".ToCharArray());
        var target = new string("Target".ToCharArray());
        mutable.AddNode(source);
        mutable.AddNode(target);
        mutable.AddEdge("SOURCE", "TARGET");

        Assert.True(graph.Nodes.Contains("source"));
        Assert.True(graph.GetDependencies("TARGET").Contains("SOURCE"));
        Assert.True(graph.GetDependents("SOURCE").Contains("TARGET"));
        Assert.Same(source, Assert.Single(graph.GetDependencies("target")));
        Assert.Same(target, Assert.Single(graph.GetDependents("source")));
        Assert.Contains(graph.Nodes, node => ReferenceEquals(node, source));
        Assert.Contains(graph.Nodes, node => ReferenceEquals(node, target));
        foreach (var edges in new[] { graph.Edges, graph.GetOutgoingEdges("SOURCE"), graph.GetIncomingEdges("TARGET") })
        {
            var edge = Assert.Single(edges);
            Assert.Same(source, edge.Source);
            Assert.Same(target, edge.Target);
        }

        // Empty sets must retain the same comparer as populated interface snapshots.
        Assert.Empty(graph.GetDependencies("missing"));
        mutable.AddEdge(target, source);
        Assert.True(graph.HasPath("SOURCE", "target"));
        mutable.RemoveNode("SOURCE");
        Assert.Single(graph.Nodes);
        Assert.Empty(graph.Edges);
    }

    /// <summary>Verifies interface snapshots preserve distinct value-equal references and path identity.</summary>
    [Fact]
    public void ReferenceComparer_IsPreservedBySetsEdgesAndReachability()
    {
        var mutable = new MutableDirectedGraph<string>(ReferenceEqualityComparer.Instance);
        IDirectedGraph<string> graph = mutable;
        var source = new string('x', 3);
        var target = new string('x', 3);
        var absent = new string('x', 3);
        mutable.AddEdge(source, target);
        Assert.Equal(2, graph.Nodes.Count);
        Assert.True(graph.Nodes.Contains(source));
        Assert.True(graph.Nodes.Contains(target));
        Assert.False(graph.Nodes.Contains(absent));
        Assert.True(graph.GetDependencies(target).Contains(source));
        Assert.False(graph.GetDependencies(target).Contains(target));
        Assert.False(graph.GetDependencies(target).Contains(absent));
        Assert.True(graph.GetDependents(source).Contains(target));
        Assert.False(graph.GetDependents(source).Contains(source));
        Assert.True(graph.HasPath(source, target));
        Assert.False(graph.HasPath(target, source));
        Assert.False(graph.HasPath(source, absent));
        Assert.Same(source, Assert.Single(graph.Edges).Source);
        Assert.Same(target, Assert.Single(graph.Edges).Target);
    }

    /// <summary>Verifies count and both enumeration interfaces follow current node registrations.</summary>
    [Fact]
    public void CountAndEnumeration_ReflectCurrentRegistrations()
    {
        var mutable = new MutableDirectedGraph<int>();
        IDirectedGraph<int> graph = mutable;
        Assert.True(graph.Count == 0);
        Assert.Empty(graph);
        mutable.AddNode(3);
        mutable.AddEdge(1, 2);
        Assert.Equal(3, graph.Count);
        Assert.Equal(new[] { 1, 2, 3 }, graph.Order());
        Assert.Equal(new[] { 1, 2, 3 }, ((IEnumerable)graph).Cast<int>().Order());
        mutable.RemoveNode(2);
        Assert.Equal(2, graph.Count);
        Assert.Equal(new[] { 1, 3 }, graph.Order());
        mutable.Clear();
        Assert.True(graph.Count == 0);
        Assert.Empty(graph);
    }

    #endregion

    #region Queries and Algorithms

    /// <summary>Verifies reachability respects direction, comparer equality, cycles, and isolated nodes.</summary>
    [Fact]
    public void HasPath_UsesCurrentDirectedRelationshipsAndComparer()
    {
        var mutable = new MutableDirectedGraph<string>(StringComparer.OrdinalIgnoreCase);
        IDirectedGraph<string> graph = mutable;
        mutable.AddEdge("A", "B");
        mutable.AddEdge("B", "C");
        mutable.AddNode("isolated");
        Assert.True(graph.HasPath("a", "c"));
        Assert.True(graph.HasPath("a", "A"));
        Assert.True(graph.HasPath("ISOLATED", "isolated"));
        Assert.False(graph.HasPath("c", "a"));
        Assert.False(graph.HasPath("a", "isolated"));
        Assert.False(graph.HasPath("a", "absent"));
        Assert.False(graph.HasPath("absent", "a"));
        Assert.False(graph.HasPath("absent", "absent"));
        mutable.AddEdge("C", "A");
        mutable.AddEdge("C", "C");
        Assert.True(graph.HasPath("c", "b"));
        Assert.False(graph.HasPath("b", "isolated"));
        mutable.RemoveEdge("b", "c");
        Assert.False(graph.HasPath("a", "c"));
        mutable.Clear();
        Assert.False(graph.HasPath("a", "a"));
    }

    /// <summary>Verifies missing-node collection behavior and null validation through the graph interface.</summary>
    [Fact]
    public void InterfaceQueries_ReturnEmptyForMissingNodesAndRejectNull()
    {
        IDirectedGraph<string> graph = new MutableDirectedGraph<string>();
        Assert.Empty(graph.GetDependencies("missing"));
        Assert.Empty(graph.GetDependents("missing"));
        Assert.Empty(graph.GetOutgoingEdges("missing"));
        Assert.Empty(graph.GetIncomingEdges("missing"));
        Assert.False(graph.HasEdge("missing", "other"));
        Assert.Throws<ArgumentNullException>(() => graph.GetDependencies(null!));
        Assert.Throws<ArgumentNullException>(() => graph.GetDependents(null!));
        Assert.Throws<ArgumentNullException>(() => graph.GetOutgoingEdges(null!));
        Assert.Throws<ArgumentNullException>(() => graph.GetIncomingEdges(null!));
        Assert.Throws<ArgumentNullException>(() => graph.HasPath(null!, "missing"));
        Assert.Throws<ArgumentNullException>(() => graph.HasPath("missing", null!));
    }

    /// <summary>Verifies existing algorithms consume the graph interface and observe later mutations.</summary>
    [Fact]
    public void ExistingAlgorithms_AcceptMutableGraphs()
    {
        var graph = new MutableDirectedGraph<int>();
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        Assert.Equal(new[] { 1, 2, 3 }, TopologicalSort.KahnSort(graph).SortedNodes);
        Assert.Equal(new[] { 1, 2, 3 }, TopologicalSort.DfsSort(graph).SortedNodes);
        Assert.Equal(new[] { 1, 2, 3 }, PathFinding.FindShortestPath(graph, 1, 3).Path);
        Assert.False(CycleDetection.HasCycles(graph));
        graph.AddEdge(3, 1);
        Assert.True(CycleDetection.HasCycles(graph));
        Assert.True(TopologicalSort.KahnSort(graph).HasCycles);
        graph.RemoveEdge(3, 1);
        Assert.False(CycleDetection.HasCycles(graph));

        var strings = new MutableDirectedGraph<string>();
        strings.AddEdge("compile", "test");
        Assert.Equal(new[] { "compile", "test" }, TopologicalSort.KahnSort(strings).SortedNodes);
    }

    #endregion

    #endregion
}
