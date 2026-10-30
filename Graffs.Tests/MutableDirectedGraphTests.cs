using System.Collections;
using Graffs.Implementations;

namespace Graffs.Tests;

/// <summary>Verifies mutable relationships, identity, safe traversal, and local index access.</summary>
public class MutableDirectedGraphTests
{
    #region Public API

    #region Relationships

    /// <summary>Verifies edge direction, automatic registration, duplicate handling, and isolated nodes.</summary>
    [Fact]
    public void Relationships_RegisterEndpointsAndUseDependencyDirection()
    {
        var graph = new MutableDirectedGraph<string>();
        Assert.True(graph.AddNode("isolated"));
        Assert.False(graph.AddNode("isolated"));
        Assert.True(graph.AddEdge("dependency", "dependent"));
        Assert.False(graph.AddEdge("dependency", "dependent"));
        Assert.True(graph.ContainsNode("dependency"));
        Assert.True(graph.ContainsNode("dependent"));
        Assert.Equal("dependency", Assert.Single(graph.GetDependencies("dependent")));
        Assert.Equal("dependent", Assert.Single(graph.GetDependents("dependency")));
        Assert.Empty(graph.GetDependencies("dependency"));
        Assert.Empty(graph.GetDependents("dependent"));
        Assert.Empty(graph.GetDependencies("isolated"));
        Assert.Empty(graph.GetDependents("isolated"));
        Assert.True(graph.HasEdge("dependency", "dependent"));
        Assert.False(graph.HasEdge("dependent", "dependency"));
        Assert.True(graph.RemoveEdge("dependency", "dependent"));
        Assert.False(graph.RemoveEdge("dependency", "dependent"));
        Assert.Empty(graph.GetDependencies("dependent"));
        Assert.Empty(graph.GetDependents("dependency"));
        Assert.True(graph.ContainsNode("dependency"));
        Assert.True(graph.ContainsNode("dependent"));
    }

    /// <summary>Verifies absent queries and mutations do not implicitly register nodes.</summary>
    [Fact]
    public void MissingNodes_ReturnEmptyAndFalse()
    {
        var graph = new MutableDirectedGraph<int>();
        var missingView = graph.GetDependents(1);
        Assert.Empty(graph.GetDependencies(1));
        Assert.Empty(missingView);
        Assert.Empty(graph.GetDependenciesSnapshot(1));
        Assert.Empty(graph.GetDependentsSnapshot(1));
        Assert.False(graph.ContainsNode(1));
        Assert.False(graph.HasEdge(1, 2));
        Assert.False(graph.RemoveEdge(1, 2));
        Assert.False(graph.RemoveNode(1));
        graph.AddEdge(1, 2);
        Assert.Empty(missingView);
        Assert.False(graph.RemoveEdge(3, 2));
        Assert.False(graph.RemoveEdge(1, 3));
        Assert.False(graph.ContainsNode(3));
    }

    /// <summary>Verifies incident cleanup handles cycles, bidirectional edges, and self-edges.</summary>
    [Fact]
    public void RemoveNode_CleansBothIndexesAndPreservesUnrelatedEdges()
    {
        var graph = new MutableDirectedGraph<int>();
        graph.AddEdge(1, 1);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 1);
        graph.AddEdge(3, 1);
        graph.AddEdge(2, 3);
        graph.AddEdge(8, 9);
        var incoming = graph.GetDependencies(1);
        var outgoing = graph.GetDependents(1);

        Assert.True(graph.RemoveNode(1));
        Assert.False(graph.RemoveNode(1));
        Assert.False(graph.ContainsNode(1));
        Assert.Empty(incoming);
        Assert.Empty(outgoing);
        Assert.Empty(graph.GetDependencies(2));
        Assert.Equal(3, Assert.Single(graph.GetDependents(2)));
        Assert.Equal(2, Assert.Single(graph.GetDependencies(3)));
        Assert.Empty(graph.GetDependents(3));
        Assert.True(graph.HasEdge(8, 9));
        graph.AddEdge(1, 2);
        Assert.Empty(incoming);
        Assert.Empty(outgoing);
    }

    /// <summary>Verifies Clear empties retained views and starts fresh registrations.</summary>
    [Fact]
    public void Clear_EmptiesOldViewsAndAllowsReuse()
    {
        var graph = new MutableDirectedGraph<int>();
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 2);
        graph.AddNode(3);
        var incoming = graph.GetDependencies(2);
        var outgoing = graph.GetDependents(1);
        var snapshot = graph.GetDependenciesSnapshot(2);
        graph.Clear();
        graph.Clear();
        Assert.Empty(incoming);
        Assert.Empty(outgoing);
        Assert.Equal(new[] { 1, 2 }, snapshot.Order());
        Assert.False(graph.ContainsNode(1));
        Assert.False(graph.ContainsNode(2));
        Assert.False(graph.ContainsNode(3));
        graph.AddEdge(1, 2);
        Assert.Empty(incoming);
        Assert.Empty(outgoing);
        Assert.Equal(1, Assert.Single(graph.GetDependencies(2)));
    }

    /// <summary>Checks null validation for every node argument, including absent endpoints.</summary>
    [Fact]
    public void NullArguments_ThrowBeforeAddingEndpoints()
    {
        var graph = new MutableDirectedGraph<string>();
        Assert.Throws<ArgumentNullException>(() => graph.AddNode(null!));
        Assert.Throws<ArgumentNullException>(() => graph.ContainsNode(null!));
        Assert.Throws<ArgumentNullException>(() => graph.RemoveNode(null!));
        Assert.Throws<ArgumentNullException>(() => graph.GetDependencies(null!));
        Assert.Throws<ArgumentNullException>(() => graph.GetDependents(null!));
        Assert.Throws<ArgumentNullException>(() => graph.GetDependenciesSnapshot(null!));
        Assert.Throws<ArgumentNullException>(() => graph.GetDependentsSnapshot(null!));
        Assert.Throws<ArgumentNullException>(() => graph.AddEdge("new", null!));
        Assert.Throws<ArgumentNullException>(() => graph.AddEdge(null!, "new"));
        Assert.False(graph.ContainsNode("new"));
        Assert.Throws<ArgumentNullException>(() => graph.RemoveEdge("absent", null!));
        Assert.Throws<ArgumentNullException>(() => graph.RemoveEdge(null!, "absent"));
        Assert.Throws<ArgumentNullException>(() => graph.HasEdge("absent", null!));
        Assert.Throws<ArgumentNullException>(() => graph.HasEdge(null!, "absent"));
    }

    #endregion

    #region Identity

    /// <summary>Verifies comparer equivalence preserves the first registration's reference in both indexes.</summary>
    [Fact]
    public void CustomComparer_PreservesOriginalReferencesAcrossMutations()
    {
        var graph = new MutableDirectedGraph<string>(StringComparer.OrdinalIgnoreCase);
        var originalDependency = new string("Dependency".ToCharArray());
        var originalDependent = new string("Dependent".ToCharArray());
        graph.AddNode(originalDependency);
        graph.AddNode(originalDependent);
        Assert.False(graph.AddNode("DEPENDENCY"));
        Assert.True(graph.AddEdge("DEPENDENCY", "DEPENDENT"));
        Assert.False(graph.AddEdge("dependency", "dependent"));
        Assert.True(graph.ContainsNode("dependency"));
        Assert.True(graph.HasEdge("dependency", "dependent"));
        Assert.Same(originalDependency, Assert.Single(graph.GetDependencies("DEPENDENT")));
        Assert.Same(originalDependent, Assert.Single(graph.GetDependents("DEPENDENCY")));
        Assert.Same(originalDependency, Assert.Single(graph.GetDependenciesSnapshot("dependent")));
        Assert.Same(originalDependent, Assert.Single(graph.GetDependentsSnapshot("dependency")));
        Assert.True(graph.RemoveEdge("dependency", "dependent"));
        Assert.True(graph.AddEdge("dependency", "dependent"));
        Assert.Same(originalDependency, Assert.Single(graph.GetDependencies("dependent")));
        Assert.True(graph.RemoveNode("DEPENDENCY"));
        Assert.Empty(graph.GetDependencies("dependent"));
        var replacement = new string("dependency".ToCharArray());
        graph.AddEdge(replacement, "DEPENDENT");
        Assert.Same(replacement, Assert.Single(graph.GetDependencies("dependent")));
        Assert.Same(originalDependent, Assert.Single(graph.GetDependents("dependency")));
    }

    /// <summary>Verifies automatic registration canonicalizes comparer-equal endpoints, including self-edges.</summary>
    [Fact]
    public void AutomaticRegistration_RetainsFirstEndpointForEquivalentSelfEdge()
    {
        var graph = new MutableDirectedGraph<string>(StringComparer.OrdinalIgnoreCase);
        var first = new string("Node".ToCharArray());
        Assert.True(graph.AddEdge(first, "NODE"));
        Assert.Same(first, Assert.Single(graph.GetDependencies("node")));
        Assert.Same(first, Assert.Single(graph.GetDependents("node")));
        Assert.True(graph.RemoveNode("NODE"));
        Assert.Empty(graph.GetDependencies("node"));
        Assert.Empty(graph.GetDependents("node"));
    }

    /// <summary>Verifies explicit reference identity keeps value-equal instances as distinct nodes.</summary>
    [Fact]
    public void ReferenceComparer_KeepsValueEqualRegistrationsDistinct()
    {
        var graph = new MutableDirectedGraph<string>(ReferenceEqualityComparer.Instance);
        var first = new string('x', 3);
        var second = new string('x', 3);
        var third = new string('x', 3);
        Assert.Equal(first, second);
        Assert.NotSame(first, second);
        Assert.True(graph.AddNode(first));
        Assert.True(graph.AddNode(second));
        Assert.True(graph.AddEdge(first, second));
        Assert.False(graph.ContainsNode(third));
        Assert.False(graph.HasEdge(first, third));
        Assert.False(graph.RemoveEdge(third, second));
        Assert.Same(first, Assert.Single(graph.GetDependencies(second)));
        Assert.Same(second, Assert.Single(graph.GetDependents(first)));
        Assert.True(graph.RemoveNode(first));
        Assert.True(graph.ContainsNode(second));
        Assert.Empty(graph.GetDependencies(second));
    }

    #endregion

    #region Traversal

    /// <summary>Verifies live queries reuse protected wrappers and track local additions and removals.</summary>
    [Fact]
    public void LiveViews_AreCachedAndCannotExposeMutableSets()
    {
        var graph = new MutableDirectedGraph<int>();
        graph.AddNode(1);
        graph.AddNode(2);
        var outgoing = graph.GetDependents(1);
        var incoming = graph.GetDependencies(2);
        Assert.Same(outgoing, graph.GetDependents(1));
        Assert.Same(incoming, graph.GetDependencies(2));
        Assert.False(outgoing is ISet<int>);
        Assert.True(((ICollection<int>)outgoing).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((ICollection<int>)outgoing).Add(99));
        Assert.Throws<NotSupportedException>(() => ((IList<int>)outgoing).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList)outgoing).Add(99));
        Assert.False(incoming is ISet<int>);
        Assert.True(((ICollection<int>)incoming).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((ICollection<int>)incoming).Remove(1));
        graph.AddEdge(1, 2);
        Assert.Equal(2, Assert.Single(outgoing));
        Assert.Equal(1, Assert.Single(incoming));
        Assert.Equal(new[] { 2 }, ((IEnumerable)outgoing).Cast<int>());
        graph.RemoveEdge(1, 2);
        Assert.Empty(outgoing);
        Assert.Empty(incoming);
    }

    /// <summary>Verifies stable snapshots support mutating either direct neighborhood during traversal.</summary>
    [Fact]
    public void Snapshots_StayReadOnlyAndStableDuringMutation()
    {
        var graph = new MutableDirectedGraph<int>();
        graph.AddEdge(1, 2);
        graph.AddEdge(1, 3);
        graph.AddEdge(4, 1);
        graph.AddEdge(5, 1);
        var incoming = graph.GetDependenciesSnapshot(1);
        var outgoing = graph.GetDependentsSnapshot(1);
        Assert.Throws<NotSupportedException>(() => ((ICollection<int>)incoming).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<int>)outgoing)[0] = 99);

        // Traverse each saved neighborhood while removing its live incident relationships.
        foreach (var node in incoming)
            Assert.True(graph.RemoveEdge(node, 1));
        foreach (var node in outgoing)
            Assert.True(graph.RemoveNode(node));
        graph.AddEdge(6, 1);
        graph.AddEdge(1, 7);
        graph.Clear();
        Assert.Equal(new[] { 4, 5 }, incoming.Order());
        Assert.Equal(new[] { 2, 3 }, outgoing.Order());
    }

    #endregion

    #region Index Locality

    /// <summary>Verifies dense neighborhood indexes remain correct after repeated non-tail removals.</summary>
    [Fact]
    public void NeighborhoodChurn_PreservesRemainingNeighborsAndAllowsReinsertion()
    {
        var graph = new MutableDirectedGraph<int>();
        for (var node = 1; node <= 1000; node++)
        {
            graph.AddEdge(0, node);
            graph.AddEdge(node, 0);
        }

        // Removing low-index entries repeatedly exercises replacement by the last live neighbor.
        for (var node = 1; node < 1000; node++)
        {
            Assert.True(graph.RemoveEdge(0, node));
            Assert.True(graph.RemoveEdge(node, 0));
        }
        Assert.Equal(1000, Assert.Single(graph.GetDependenciesSnapshot(0)));
        Assert.Equal(1000, Assert.Single(graph.GetDependentsSnapshot(0)));
        Assert.True(graph.AddEdge(0, 1));
        Assert.True(graph.AddEdge(1, 0));
        Assert.True(graph.RemoveNode(1000));
        Assert.Equal(1, Assert.Single(graph.GetDependencies(0)));
        Assert.Equal(1, Assert.Single(graph.GetDependents(0)));
        Assert.True(graph.RemoveNode(0));
        Assert.Empty(graph.GetDependencies(1));
        Assert.Empty(graph.GetDependents(1));
    }

    /// <summary>Checks bounded comparer work and excludes unrelated keys across graph sizes.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public void LocalOperations_TouchOnlyEndpointsAndIncidentKeys(int unrelatedNodes)
    {
        var comparer = new LocalityComparer();
        var graph = new MutableDirectedGraph<int>(comparer);
        graph.AddEdge(1, 2);
        graph.AddEdge(3, 1);
        for (var i = 0; i < unrelatedNodes; i++)
            graph.AddEdge(100 + i, 101 + i);

        // Limit hashing and comparisons to the local component after constructing unrelated data.
        comparer.RestrictToLocalKeys = true;
        IDirectedGraph<int> directed = graph;
        Action[] operations =
        [
            () => Assert.True(graph.ContainsNode(1)),
            () => Assert.True(graph.HasEdge(1, 2)),
            () => Assert.Equal(2, Assert.Single(graph.GetDependents(1))),
            () => Assert.Equal(3, Assert.Single(graph.GetDependencies(1))),
            () => Assert.Equal(2, Assert.Single(graph.GetDependentsSnapshot(1))),
            () => Assert.Equal(3, Assert.Single(graph.GetDependenciesSnapshot(1))),
            () => Assert.Equal(2, Assert.Single(directed.GetDependents(1))),
            () => Assert.Equal(3, Assert.Single(directed.GetDependencies(1))),
            () => Assert.Equal(2, Assert.Single(directed.GetOutgoingEdges(1)).Target),
            () => Assert.Equal(3, Assert.Single(directed.GetIncomingEdges(1)).Source),
            () => Assert.True(directed.HasEdge(1, 2)),
            () => Assert.True(graph.AddEdge(1, 4)),
            () => Assert.True(graph.RemoveEdge(1, 4)),
            () => Assert.True(graph.RemoveNode(1))
        ];
        foreach (var operation in operations)
        {
            comparer.Calls = 0;
            operation();
            Assert.InRange(comparer.Calls, 1, 32);
        }
        Assert.Empty(graph.GetDependencies(2));
        Assert.Empty(graph.GetDependents(3));
    }

    /// <summary>Reconciles both indexes with a reference relation model through mixed mutations.</summary>
    [Fact]
    public void MixedMutations_MaintainBothIndexes()
    {
        var random = new Random(1907);
        var graph = new MutableDirectedGraph<int>();
        var nodes = new HashSet<int>();
        var edges = new HashSet<(int Dependency, int Dependent)>();

        // Reconcile after every operation so stale reverse entries cannot be hidden by later updates.
        for (var step = 0; step < 500; step++)
        {
            var source = random.Next(12);
            var target = random.Next(12);
            switch (random.Next(5))
            {
                case 0:
                    Assert.Equal(nodes.Add(source), graph.AddNode(source));
                    break;
                case 1:
                    nodes.Add(source);
                    nodes.Add(target);
                    Assert.Equal(edges.Add((source, target)), graph.AddEdge(source, target));
                    break;
                case 2:
                    Assert.Equal(edges.Remove((source, target)), graph.RemoveEdge(source, target));
                    break;
                case 3:
                    Assert.Equal(nodes.Remove(source), graph.RemoveNode(source));
                    edges.RemoveWhere(edge => edge.Dependency == source || edge.Dependent == source);
                    break;
                default:
                    graph.Clear();
                    nodes.Clear();
                    edges.Clear();
                    break;
            }

            for (var node = 0; node < 12; node++)
            {
                Assert.Equal(nodes.Contains(node), graph.ContainsNode(node));
                Assert.Equal(edges.Where(edge => edge.Dependent == node).Select(edge => edge.Dependency).Order(),
                    graph.GetDependencies(node).Order());
                Assert.Equal(edges.Where(edge => edge.Dependency == node).Select(edge => edge.Dependent).Order(),
                    graph.GetDependents(node).Order());
            }
        }
    }

    #endregion

    #endregion

    #region Private

    /// <summary>Counts index operations and rejects hashing or comparing unrelated graph keys.</summary>
    private sealed class LocalityComparer : IEqualityComparer<int>
    {
        #region Public API

        /// <summary>Gets or sets whether only the local component may be accessed.</summary>
        public bool RestrictToLocalKeys { get; set; }

        /// <summary>Gets or sets the number of comparer invocations.</summary>
        public int Calls { get; set; }

        /// <summary>Checks equality and records every key accessed by an index.</summary>
        public bool Equals(int x, int y)
        {
            Calls++;
            if (RestrictToLocalKeys)
            {
                Assert.InRange(x, 1, 4);
                Assert.InRange(y, 1, 4);
            }
            return x == y;
        }

        /// <summary>Returns collision-free hashes for the test domain while tracking access.</summary>
        public int GetHashCode(int obj)
        {
            Calls++;
            if (RestrictToLocalKeys)
                Assert.InRange(obj, 1, 4);
            return obj;
        }

        #endregion
    }

    #endregion
}
