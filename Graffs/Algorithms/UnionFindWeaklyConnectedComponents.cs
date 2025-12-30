using System.Diagnostics;

using Graffs;
using Graffs.Results;

namespace Graffs.Algorithms;

/// <summary>
/// Implementation of Union-Find algorithm for finding weakly connected components.
/// Uses path compression and union by rank for optimal performance.
/// </summary>
public static class UnionFindWeaklyConnectedComponents
{
    /// <summary>
    /// Finds weakly connected components in a directed graph by treating it as undirected.
    /// Uses Union-Find (Disjoint Set Union) algorithm for optimal performance.
    /// 
    /// Algorithm Complexity:
    /// - Time: O(E * α(V)) where E is edges, V is vertices, and α is the inverse Ackermann function
    /// - Space: O(V) for the Union-Find structure
    /// - α(V) is effectively constant for all practical purposes (≤ 5 for any conceivable graph size)
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <returns>Connectivity result with all weakly connected components</returns>
    public static ConnectivityResult<TNode> Execute<TNode>(IDirectedGraph<TNode> graph) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);

        var stopwatch = Stopwatch.StartNew();
        const string algorithmName = "Union-Find Weakly Connected Components";

        try
        {
            if (graph.Count == 0)
                return ConnectivityResult<TNode>.Empty(stopwatch.Elapsed, algorithmName);

            // Use Union-Find for efficient component detection
            var unionFind = new UnionFind<TNode>(graph.Nodes);
            
            // Union all connected nodes (treating directed edges as undirected)
            foreach (var node in graph.Nodes)
            {
                foreach (var dependent in graph.GetDependents(node))
                {
                    unionFind.Union(node, dependent);
                }
            }

            // Group nodes by their root representative
            var componentMap = new Dictionary<TNode, List<TNode>>();
            foreach (var node in graph.Nodes)
            {
                var root = unionFind.Find(node);
                if (!componentMap.ContainsKey(root))
                    componentMap[root] = new List<TNode>();
                componentMap[root].Add(node);
            }

            // Convert to strongly connected components format for consistency
            var components = componentMap.Values
                .Select(nodes => 
                {
                    if (nodes.Count == 1)
                    {
                        // Single node is trivial only if it has no self-loop
                        var node = nodes[0];
                        bool hasEdge = graph.HasEdge(node, node);
                        return hasEdge 
                            ? StronglyConnectedComponent<TNode>.NonTrivial(nodes)
                            : StronglyConnectedComponent<TNode>.Trivial(node);
                    }
                    else
                    {
                        return StronglyConnectedComponent<TNode>.NonTrivial(nodes);
                    }
                })
                .ToList();

            return ConnectivityResult<TNode>.WithComponents(components, stopwatch.Elapsed, algorithmName);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Union-Find (Disjoint Set Union) data structure with path compression and union by rank.
    /// Provides near-constant time operations for connectivity queries.
    /// </summary>
    /// <typeparam name="TNode">The type of elements in the sets</typeparam>
    private sealed class UnionFind<TNode> where TNode : notnull
    {
        private readonly Dictionary<TNode, TNode> _parent;
        private readonly Dictionary<TNode, int> _rank;

        public UnionFind(IEnumerable<TNode> elements)
        {
            _parent = new Dictionary<TNode, TNode>();
            _rank = new Dictionary<TNode, int>();

            // Initialize each element as its own parent (representative)
            foreach (var element in elements)
            {
                _parent[element] = element;
                _rank[element] = 0;
            }
        }

        /// <summary>
        /// Finds the representative (root) of the set containing the element.
        /// Uses path compression for optimization.
        /// </summary>
        /// <param name="element">The element to find</param>
        /// <returns>The representative of the set</returns>
        public TNode Find(TNode element)
        {
            if (!_parent[element].Equals(element))
            {
                // Path compression: make the parent point directly to the root
                _parent[element] = Find(_parent[element]);
            }
            return _parent[element];
        }

        /// <summary>
        /// Unites the sets containing the two elements.
        /// Uses union by rank for optimization.
        /// </summary>
        /// <param name="element1">First element</param>
        /// <param name="element2">Second element</param>
        public void Union(TNode element1, TNode element2)
        {
            var root1 = Find(element1);
            var root2 = Find(element2);

            if (root1.Equals(root2))
                return; // Already in the same set

            // Union by rank: attach the smaller tree under the root of the larger tree
            if (_rank[root1] < _rank[root2])
            {
                _parent[root1] = root2;
            }
            else if (_rank[root1] > _rank[root2])
            {
                _parent[root2] = root1;
            }
            else
            {
                _parent[root2] = root1;
                _rank[root1]++;
            }
        }

        /// <summary>
        /// Checks if two elements are in the same connected component.
        /// </summary>
        /// <param name="element1">First element</param>
        /// <param name="element2">Second element</param>
        /// <returns>True if elements are connected, false otherwise</returns>
        public bool Connected(TNode element1, TNode element2)
        {
            return Find(element1).Equals(Find(element2));
        }
    }
}
