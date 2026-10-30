using Graffs.Implementations;

namespace Graffs.Tests;

/// <summary>Verifies immutable snapshot caches invalidate only when their represented data changes.</summary>
public class MutableDirectedGraphCacheTests
{
    #region Public API

    #region Repeated Access and No-op Mutations

    /// <summary>Verifies repeated access and mutations without effects retain all cached snapshots.</summary>
    [Fact]
    public void RepeatedQueriesAndNoOpMutations_PreserveSnapshots()
    {
        var mutable = new MutableDirectedGraph<int>();
        IDirectedGraph<int> graph = mutable;
        mutable.AddEdge(1, 2);
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var incoming = graph.GetDependencies(2);
        var outgoing = graph.GetDependents(1);
        var emptyIncoming = graph.GetDependencies(1);

        Assert.False(mutable.AddNode(1));
        Assert.False(mutable.AddEdge(1, 2));
        Assert.False(mutable.RemoveEdge(2, 1));
        Assert.False(mutable.RemoveEdge(1, 99));
        Assert.False(mutable.RemoveEdge(99, 2));
        Assert.False(mutable.RemoveNode(99));
        Assert.Same(nodes, graph.Nodes);
        Assert.Same(edges, graph.Edges);
        Assert.Same(incoming, graph.GetDependencies(2));
        Assert.Same(outgoing, graph.GetDependents(1));
        Assert.Same(emptyIncoming, graph.GetDependencies(1));
        Assert.Same(graph.GetDependencies(99), graph.GetDependencies(99));
        Assert.Same(graph.GetDependents(99), graph.GetDependents(99));
    }

    /// <summary>Verifies isolated registration changes invalidate Nodes without invalidating Edges or neighbors.</summary>
    [Fact]
    public void IsolatedNodeChanges_InvalidateOnlyNodes()
    {
        var mutable = new MutableDirectedGraph<int>();
        IDirectedGraph<int> graph = mutable;
        mutable.AddEdge(1, 2);
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var incoming = graph.GetDependencies(2);
        var outgoing = graph.GetDependents(1);

        Assert.True(mutable.AddNode(3));
        var addedNodes = graph.Nodes;
        Assert.NotSame(nodes, addedNodes);
        Assert.Equal(new[] { 1, 2 }, nodes.Order());
        Assert.Equal(new[] { 1, 2, 3 }, addedNodes.Order());
        Assert.Same(edges, graph.Edges);
        Assert.Same(incoming, graph.GetDependencies(2));
        Assert.Same(outgoing, graph.GetDependents(1));
        Assert.True(mutable.RemoveNode(3));
        Assert.NotSame(addedNodes, graph.Nodes);
        Assert.Equal(new[] { 1, 2 }, graph.Nodes.Order());
        Assert.Same(edges, graph.Edges);
        Assert.Same(incoming, graph.GetDependencies(2));
        Assert.Same(outgoing, graph.GetDependents(1));
    }

    #endregion

    #region Relationship Changes

    /// <summary>Verifies an edge update invalidates only the source's outgoing and target's incoming snapshots.</summary>
    [Fact]
    public void EdgeChanges_InvalidateOnlyChangedDirections()
    {
        var mutable = new MutableDirectedGraph<int>();
        IDirectedGraph<int> graph = mutable;
        mutable.AddEdge(1, 2);
        mutable.AddEdge(2, 3);
        mutable.AddEdge(3, 4);
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var incoming = Enumerable.Range(1, 4).ToDictionary(node => node, graph.GetDependencies);
        var outgoing = Enumerable.Range(1, 4).ToDictionary(node => node, graph.GetDependents);

        Assert.True(mutable.AddEdge(1, 3));
        var addedEdges = graph.Edges;
        var changedIncoming = graph.GetDependencies(3);
        var changedOutgoing = graph.GetDependents(1);
        Assert.Same(nodes, graph.Nodes);
        Assert.NotSame(edges, addedEdges);
        Assert.NotSame(incoming[3], changedIncoming);
        Assert.NotSame(outgoing[1], changedOutgoing);
        for (var node = 1; node <= 4; node++)
        {
            if (node != 3)
                Assert.Same(incoming[node], graph.GetDependencies(node));
            if (node != 1)
                Assert.Same(outgoing[node], graph.GetDependents(node));
        }
        Assert.Equal(3, edges.Count);
        Assert.Equal(new[] { 1, 2 }, changedIncoming.Order());
        Assert.Equal(new[] { 2, 3 }, changedOutgoing.Order());

        Assert.True(mutable.RemoveEdge(1, 3));
        Assert.Same(nodes, graph.Nodes);
        Assert.NotSame(addedEdges, graph.Edges);
        Assert.NotSame(changedIncoming, graph.GetDependencies(3));
        Assert.NotSame(changedOutgoing, graph.GetDependents(1));
        Assert.Equal(2, Assert.Single(graph.GetDependencies(3)));
        Assert.Equal(2, Assert.Single(graph.GetDependents(1)));
        Assert.Equal(4, addedEdges.Count);
        Assert.Equal(new[] { 1, 2 }, changedIncoming.Order());
        Assert.Equal(new[] { 2, 3 }, changedOutgoing.Order());
        for (var node = 1; node <= 4; node++)
        {
            if (node != 3)
                Assert.Same(incoming[node], graph.GetDependencies(node));
            if (node != 1)
                Assert.Same(outgoing[node], graph.GetDependents(node));
        }
    }

    /// <summary>Verifies automatic registration invalidates node snapshots while preserving untouched directions.</summary>
    [Fact]
    public void AutomaticRegistration_RefreshesNodesAndAffectedRelationships()
    {
        var mutable = new MutableDirectedGraph<int>();
        IDirectedGraph<int> graph = mutable;
        var emptyNodes = graph.Nodes;
        var emptyEdges = graph.Edges;
        mutable.AddEdge(1, 2);
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var outgoing = graph.GetDependents(1);
        var incoming = graph.GetDependencies(2);
        Assert.NotSame(emptyNodes, nodes);
        Assert.Empty(emptyNodes);
        Assert.Empty(emptyEdges);

        mutable.AddEdge(3, 2);
        Assert.NotSame(nodes, graph.Nodes);
        Assert.NotSame(edges, graph.Edges);
        Assert.Same(outgoing, graph.GetDependents(1));
        Assert.NotSame(incoming, graph.GetDependencies(2));
        Assert.Equal(1, Assert.Single(incoming));
        var existingIncoming = graph.GetDependencies(2);
        var existingNodes = graph.Nodes;
        mutable.AddEdge(4, 5);
        Assert.NotSame(existingNodes, graph.Nodes);
        Assert.Same(existingIncoming, graph.GetDependencies(2));
        Assert.Same(outgoing, graph.GetDependents(1));
        Assert.Equal(5, graph.Count);
        Assert.Equal(3, graph.Edges.Count);
    }

    /// <summary>Verifies self-edge mutations invalidate both directions of exactly one registered node.</summary>
    [Fact]
    public void SelfEdgeChanges_RefreshBothDirectionsAndRetainOldSnapshots()
    {
        var mutable = new MutableDirectedGraph<int>();
        IDirectedGraph<int> graph = mutable;
        mutable.AddEdge(1, 2);
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var incoming = graph.GetDependencies(1);
        var outgoing = graph.GetDependents(1);
        var targetIncoming = graph.GetDependencies(2);
        mutable.AddEdge(1, 1);
        var selfIncoming = graph.GetDependencies(1);
        var selfOutgoing = graph.GetDependents(1);
        Assert.Same(nodes, graph.Nodes);
        Assert.NotSame(edges, graph.Edges);
        Assert.NotSame(incoming, selfIncoming);
        Assert.NotSame(outgoing, selfOutgoing);
        Assert.Same(targetIncoming, graph.GetDependencies(2));
        Assert.Equal(1, Assert.Single(selfIncoming));
        Assert.Equal(new[] { 1, 2 }, selfOutgoing.Order());
        Assert.False(mutable.AddEdge(1, 1));
        Assert.Same(selfIncoming, graph.GetDependencies(1));
        Assert.Same(selfOutgoing, graph.GetDependents(1));

        mutable.RemoveEdge(1, 1);
        Assert.NotSame(selfIncoming, graph.GetDependencies(1));
        Assert.NotSame(selfOutgoing, graph.GetDependents(1));
        Assert.Same(targetIncoming, graph.GetDependencies(2));
        Assert.Same(nodes, graph.Nodes);
        Assert.Empty(incoming);
        Assert.Equal(2, Assert.Single(outgoing));
        Assert.Equal(1, Assert.Single(selfIncoming));
        Assert.Equal(new[] { 1, 2 }, selfOutgoing.Order());
    }

    /// <summary>Verifies node removal invalidates only directly affected neighbor directions, including cycles.</summary>
    [Fact]
    public void RemoveNode_RefreshesOnlyIncidentDirections()
    {
        var mutable = new MutableDirectedGraph<int>();
        IDirectedGraph<int> graph = mutable;
        foreach (var (source, target) in new[] { (1, 2), (2, 1), (2, 2), (3, 2), (2, 4), (3, 4), (8, 9) })
            mutable.AddEdge(source, target);
        var keys = new[] { 1, 2, 3, 4, 8, 9 };
        var incoming = keys.ToDictionary(node => node, graph.GetDependencies);
        var outgoing = keys.ToDictionary(node => node, graph.GetDependents);
        var nodes = graph.Nodes;
        var edges = graph.Edges;

        mutable.RemoveNode(2);
        Assert.NotSame(nodes, graph.Nodes);
        Assert.NotSame(edges, graph.Edges);
        Assert.NotSame(incoming[1], graph.GetDependencies(1));
        Assert.NotSame(incoming[4], graph.GetDependencies(4));
        Assert.NotSame(outgoing[1], graph.GetDependents(1));
        Assert.NotSame(outgoing[3], graph.GetDependents(3));
        foreach (var key in new[] { 3, 8, 9 })
            Assert.Same(incoming[key], graph.GetDependencies(key));
        foreach (var key in new[] { 4, 8, 9 })
            Assert.Same(outgoing[key], graph.GetDependents(key));
        Assert.Equal(new[] { 1, 2, 3 }, incoming[2].Order());
        Assert.Equal(new[] { 1, 2, 4 }, outgoing[2].Order());
        Assert.Empty(graph.GetDependencies(2));
        Assert.Empty(graph.GetDependents(2));
        Assert.Equal(3, Assert.Single(graph.GetDependencies(4)));
        Assert.Equal(4, Assert.Single(graph.GetDependents(3)));
        Assert.Equal(7, edges.Count);
        Assert.Equal(2, graph.Edges.Count);
    }

    #endregion

    #region Reset and Comparer Identity

    /// <summary>Verifies Clear refreshes changed data and repeated empty clears preserve warm caches.</summary>
    [Fact]
    public void Clear_RefreshesChangedDataAndPreservesEmptyNoOpCaches()
    {
        var mutable = new MutableDirectedGraph<int>();
        IDirectedGraph<int> graph = mutable;
        mutable.AddEdge(1, 2);
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var incoming = graph.GetDependencies(2);
        var outgoing = graph.GetDependents(1);
        mutable.Clear();
        Assert.NotSame(nodes, graph.Nodes);
        Assert.NotSame(edges, graph.Edges);
        Assert.Equal(new[] { 1, 2 }, nodes.Order());
        Assert.Single(edges);
        Assert.Equal(1, Assert.Single(incoming));
        Assert.Equal(2, Assert.Single(outgoing));
        var emptyNodes = graph.Nodes;
        var emptyEdges = graph.Edges;
        var missingIncoming = graph.GetDependencies(2);
        var missingOutgoing = graph.GetDependents(1);
        mutable.Clear();
        Assert.Same(emptyNodes, graph.Nodes);
        Assert.Same(emptyEdges, graph.Edges);
        Assert.Same(missingIncoming, graph.GetDependencies(2));
        Assert.Same(missingOutgoing, graph.GetDependents(1));

        // Clearing only isolated registrations changes Nodes while retaining the empty edge cache.
        mutable.AddNode(3);
        var isolatedNodes = graph.Nodes;
        Assert.Same(emptyEdges, graph.Edges);
        mutable.Clear();
        Assert.NotSame(isolatedNodes, graph.Nodes);
        Assert.Same(emptyEdges, graph.Edges);
    }

    /// <summary>Verifies comparer aliases share caches and re-registration retains the new canonical instance.</summary>
    [Fact]
    public void ComparerAliases_ShareCachesAndPreserveCanonicalReferences()
    {
        var mutable = new MutableDirectedGraph<string>(StringComparer.OrdinalIgnoreCase);
        IDirectedGraph<string> graph = mutable;
        var source = new string("Source".ToCharArray());
        var target = new string("Target".ToCharArray());
        mutable.AddEdge(source, target);
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var incoming = graph.GetDependencies("TARGET");
        var outgoing = graph.GetDependents("SOURCE");
        Assert.False(mutable.AddNode("SOURCE"));
        Assert.False(mutable.AddEdge("source", "target"));
        Assert.Same(nodes, graph.Nodes);
        Assert.Same(edges, graph.Edges);
        Assert.Same(incoming, graph.GetDependencies("target"));
        Assert.Same(outgoing, graph.GetDependents("source"));
        Assert.True(incoming.Contains("SOURCE"));
        Assert.True(outgoing.Contains("TARGET"));
        Assert.Same(source, Assert.Single(incoming));
        Assert.Same(target, Assert.Single(outgoing));

        mutable.RemoveNode("SOURCE");
        var replacement = new string("source".ToCharArray());
        mutable.AddEdge(replacement, "TARGET");
        Assert.NotSame(incoming, graph.GetDependencies("target"));
        Assert.Same(replacement, Assert.Single(graph.GetDependencies("target")));
        Assert.Same(replacement, Assert.Single(graph.Edges).Source);
        Assert.Same(target, Assert.Single(graph.Edges).Target);
        Assert.Same(source, Assert.Single(incoming));
        Assert.Same(source, Assert.Single(edges).Source);
    }

    #endregion

    #region Cached Lookup Locality

    /// <summary>Verifies warm neighbor lookups hash only the queried node and warm global access does no comparer work.</summary>
    [Fact]
    public void WarmSnapshots_DoNotRevisitNeighborsOrGlobalIndexes()
    {
        var comparer = new SnapshotAccessComparer();
        var mutable = new MutableDirectedGraph<int>(comparer);
        IDirectedGraph<int> graph = mutable;
        for (var node = 1; node <= 100; node++)
        {
            mutable.AddEdge(0, node);
            mutable.AddEdge(node, 0);
        }
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var incoming = graph.GetDependencies(0);
        var outgoing = graph.GetDependents(0);

        // Rebuilding either neighbor set would hash keys other than the single dictionary lookup key.
        comparer.OnlyAllowZero = true;
        comparer.Calls = 0;
        Assert.Same(nodes, graph.Nodes);
        Assert.Same(edges, graph.Edges);
        Assert.Equal(0, comparer.Calls);
        Assert.Same(incoming, graph.GetDependencies(0));
        Assert.Same(outgoing, graph.GetDependents(0));
        Assert.InRange(comparer.Calls, 1, 4);

        comparer.OnlyAllowZero = false;
        mutable.AddEdge(1000, 1001);
        comparer.OnlyAllowZero = true;
        comparer.Calls = 0;
        Assert.Same(incoming, graph.GetDependencies(0));
        Assert.Same(outgoing, graph.GetDependents(0));
        Assert.InRange(comparer.Calls, 1, 4);
    }

    #endregion

    #endregion

    #region Private

    /// <summary>Detects any comparer access beyond the one node requested by a cached neighborhood lookup.</summary>
    private sealed class SnapshotAccessComparer : IEqualityComparer<int>
    {
        #region Public API

        /// <summary>Gets or sets whether keys beyond the center node are forbidden.</summary>
        public bool OnlyAllowZero { get; set; }

        /// <summary>Gets or sets the count of hashing and equality operations.</summary>
        public int Calls { get; set; }

        /// <summary>Compares keys while enforcing the permitted lookup neighborhood.</summary>
        public bool Equals(int x, int y)
        {
            Calls++;
            if (OnlyAllowZero)
            {
                Assert.Equal(0, x);
                Assert.Equal(0, y);
            }
            return x == y;
        }

        /// <summary>Hashes keys while rejecting accidental snapshot reconstruction from other neighbors.</summary>
        public int GetHashCode(int obj)
        {
            Calls++;
            if (OnlyAllowZero)
                Assert.Equal(0, obj);
            return obj;
        }

        #endregion
    }

    #endregion
}
