using System.Diagnostics;

using Graffs;
using Graffs.Results;

namespace Graffs.Algorithms;

/// <summary>
/// Implementation of Tarjan's algorithm for finding strongly connected components.
/// Provides linear-time complexity O(V + E) for SCC detection.
/// </summary>
public static class TarjanStronglyConnectedComponents
{
    /// <summary>
    /// Finds all strongly connected components using Tarjan's algorithm.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes in the graph</typeparam>
    /// <param name="graph">The directed graph to analyze</param>
    /// <returns>Connectivity result with all strongly connected components</returns>
    public static ConnectivityResult<TNode> Execute<TNode>(IDirectedGraph<TNode> graph) where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);

        var stopwatch = Stopwatch.StartNew();
        const string algorithmName = "Tarjan's SCC Algorithm";

        try
        {
            if (graph.Count == 0)
                return ConnectivityResult<TNode>.Empty(stopwatch.Elapsed, algorithmName);

            var tarjanData = new TarjanData<TNode>();
            var components = new List<StronglyConnectedComponent<TNode>>();

            // Initialize all nodes as unvisited
            foreach (var node in graph.Nodes)
            {
                tarjanData.Index[node] = -1;
                tarjanData.LowLink[node] = -1; // Initialize LowLink as well
            }

            // Run Tarjan's algorithm from each unvisited node
            foreach (var node in graph.Nodes)
            {
                if (tarjanData.Index[node] == -1)
                {
                    StrongConnect(graph, node, tarjanData, components);
                }
            }

            return ConnectivityResult<TNode>.WithComponents(components, stopwatch.Elapsed, algorithmName);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Core iterative function of Tarjan's algorithm.
    /// Uses explicit stack to avoid stack overflow on deep graphs.
    /// </summary>
    private static void StrongConnect<TNode>(
        IDirectedGraph<TNode> graph,
        TNode startNode,
        TarjanData<TNode> data,
        List<StronglyConnectedComponent<TNode>> components) where TNode : notnull
    {
        var callStack = new Stack<TarjanStackFrame<TNode>>();
        callStack.Push(new TarjanStackFrame<TNode>(startNode, StackFrameState.Initialize));

        while (callStack.Count > 0)
        {
            var frame = callStack.Pop();
            var node = frame.Node;

            switch (frame.State)
            {
                case StackFrameState.Initialize:
                    // Set the depth index for node to the smallest unused index
                    data.Index[node] = data.CurrentIndex;
                    data.LowLink[node] = data.CurrentIndex;
                    data.CurrentIndex++;
                    data.Stack.Push(node);
                    data.OnStack.Add(node);

                    // Start processing successors
                    frame.Successors = graph.GetDependents(node).ToArray();
                    frame.SuccessorIndex = 0;
                    frame.State = StackFrameState.ProcessingSuccessors;
                    callStack.Push(frame);
                    break;

                case StackFrameState.ProcessingSuccessors:
                    if (frame.SuccessorIndex < frame.Successors!.Length)
                    {
                        var successor = frame.Successors[frame.SuccessorIndex];
                        frame.SuccessorIndex++;
                        
                        if (data.Index.GetValueOrDefault(successor, -1) == -1)
                        {
                            // Successor has not yet been visited; process it recursively
                            // Push current frame back to continue with remaining successors
                            callStack.Push(frame);
                            
                            // Push frame to handle lowlink update after successor is processed
                            var updateFrame = new TarjanStackFrame<TNode>(node, StackFrameState.UpdateLowLink);
                            updateFrame.CurrentSuccessor = successor;
                            callStack.Push(updateFrame);
                            
                            // Process the successor
                            callStack.Push(new TarjanStackFrame<TNode>(successor, StackFrameState.Initialize));
                        }
                        else if (data.OnStack.Contains(successor))
                        {
                            // Successor is in stack and hence in the current SCC
                            data.LowLink[node] = Math.Min(data.LowLink[node], data.Index[successor]);
                            // Continue processing remaining successors
                            callStack.Push(frame);
                        }
                        else
                        {
                            // Successor already processed and not in current SCC path
                            // Continue processing remaining successors
                            callStack.Push(frame);
                        }
                    }
                    else
                    {
                        // Finished processing all successors
                        frame.State = StackFrameState.CheckRoot;
                        callStack.Push(frame);
                    }
                    break;

                case StackFrameState.UpdateLowLink:
                    // Update lowlink after processing a successor
                    if (frame.CurrentSuccessor != null)
                    {
                        data.LowLink[node] = Math.Min(data.LowLink[node], data.LowLink[frame.CurrentSuccessor]);
                    }
                    break;

                case StackFrameState.CheckRoot:
                    // If node is a root node, pop the stack and generate an SCC
                    if (data.LowLink[node] == data.Index[node])
                    {
                        var componentNodes = new List<TNode>();
                        TNode w;
                        do
                        {
                            w = data.Stack.Pop();
                            data.OnStack.Remove(w);
                            componentNodes.Add(w);
                        } while (!w.Equals(node));

                        // Determine if this is a trivial component
                        bool isTrivial = componentNodes.Count == 1 && !graph.HasEdge(componentNodes[0], componentNodes[0]);

                        var component = isTrivial
                            ? StronglyConnectedComponent<TNode>.Trivial(componentNodes[0])
                            : StronglyConnectedComponent<TNode>.NonTrivial(componentNodes);

                        components.Add(component);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Data structure for Tarjan's algorithm state.
    /// </summary>
    private sealed class TarjanData<TNode> where TNode : notnull
    {
        public Dictionary<TNode, int> Index { get; } = new();
        public Dictionary<TNode, int> LowLink { get; } = new();
        public Stack<TNode> Stack { get; } = new();
        public HashSet<TNode> OnStack { get; } = new();
        public int CurrentIndex { get; set; } = 0;
    }

    /// <summary>
    /// Represents the processing state of a node in the iterative algorithm.
    /// </summary>
    private enum StackFrameState
    {
        Initialize,
        ProcessingSuccessors,
        UpdateLowLink,
        CheckRoot
    }

    /// <summary>
    /// Represents a stack frame for the iterative implementation of Tarjan's algorithm.
    /// </summary>
    private sealed class TarjanStackFrame<TNode> where TNode : notnull
    {
        public TNode Node { get; }
        public StackFrameState State { get; set; }
        public TNode[]? Successors { get; set; }
        public int SuccessorIndex { get; set; }
        public TNode? CurrentSuccessor { get; set; }

        public TarjanStackFrame(TNode node, StackFrameState state)
        {
            Node = node;
            State = state;
        }
    }
}
