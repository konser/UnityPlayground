using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DataStructure
{
    /// <summary>
    /// 无向图
    /// </summary>

    [System.Serializable]
    public class UndirectedGraph<T>
    {
        private List<GraphNode<T>> _nodeList;
        public UndirectedGraph(IEnumerable<GraphNode<T>> nodes = null)
        {
            if (nodes == null)
            {
                _nodeList = new List<GraphNode<T>>();
            }
            else
            {
                _nodeList = nodes.ToList();
            }
        }

        public UndirectedGraph(params GraphNode<T>[] nodes)
        {
            _nodeList = nodes.ToList();
        }

        public int nodeCount
        {
            get
            {
                return _nodeList.Count;
            }
        }

        public List<GraphNode<T>> nodeList
        {
            get
            {
                return _nodeList;
            }
        }

        /// <summary>
        /// 向图中添加一对节点（一条边）
        /// </summary>
        public void AddPair(GraphNode<T> first,GraphNode<T> second)
        {
            if (!_nodeList.Contains(first))
            {
                _nodeList.Add(first);
            }

            if (!_nodeList.Contains(second))
            {
                _nodeList.Add(second);
            }

            AddNodeNeibour(first,second);
            AddNodeNeibour(second,first);
        }

        /// <summary>
        /// 深度优先遍历
        /// </summary>
        public void IterateDFS(GraphNode<T> startNode,Action<GraphNode<T>> action)
        {
            if (action == null)
            {
                return;
            }
            foreach (GraphNode<T> node in new DepthFirstIterator<T>(startNode).Iterate())
            {
                action.Invoke(node);
            }
        }

        public IEnumerator IterateDFSCoroutine(GraphNode<T> startNode, Action<GraphNode<T>> action)
        {
            if (action == null)
            {
                yield break;
            }
            foreach (GraphNode<T> node in new DepthFirstIterator<T>(startNode).Iterate())
            {
                action.Invoke(node);
                yield return null;
            }
        }
        /// <summary>
        /// 广度优先遍历
        /// </summary>
        public void IterateBFS(GraphNode<T> startNode, Action<GraphNode<T>> action)
        {
            if (action == null)
            {
                return;
            }
            int count = 0;
            var it = new BreadthFirstIterator<T>(startNode);
            foreach (GraphNode<T> node in it.Iterate())
            {
                action.Invoke(node);
                count++;
            }
            Debug.Log(count);
        }

        public IEnumerator IterateBFSCoroutine(GraphNode<T> startNode, Action<GraphNode<T>> action)
        {
            if (action == null)
            {
                yield break;
            }
            var it = new BreadthFirstIterator<T>(startNode);
            foreach (GraphNode<T> node in it.Iterate())
            {
                action.Invoke(node);
                yield return null;
            }
        }

        private void AddNodeNeibour(GraphNode<T> first, GraphNode<T> second)
        {
            if (!first.neibours.Contains(second))
            {
                first.AddNeibour(second);
            }
        }
    }

    // DFS
    class DepthFirstIterator<T>
    {
        private readonly GraphNode<T> root;
        private readonly HashSet<GraphNode<T>> visited = new HashSet<GraphNode<T>>();
        public DepthFirstIterator(GraphNode<T> rootVertex)
        {
            root = rootVertex;
        }

        public IEnumerable<GraphNode<T>> Iterate()
        {
            visited.Clear();

            return DepthFirstSearch(root, visited);
        }

        private IEnumerable<GraphNode<T>> DepthFirstSearch(GraphNode<T> vertex, HashSet<GraphNode<T>> visited)
        {
            if (visited.Contains(vertex))
                yield break;

            visited.Add(vertex);

            yield return vertex;

            foreach (GraphNode<T> neighbor in vertex.neibours.SelectMany(n => DepthFirstSearch(n, visited)))
                yield return neighbor;
        }
    }

    // BFS
    class BreadthFirstIterator<T>
    {
        private readonly GraphNode<T> root;
        private readonly Queue<GraphNode<T>> queue = new Queue<GraphNode<T>>();
        private readonly HashSet<GraphNode<T>> visited = new HashSet<GraphNode<T>>();

        public BreadthFirstIterator(GraphNode<T> rootVertex)
        {
            root = rootVertex;
        }

        public IEnumerable<GraphNode<T>> Iterate()
        {
            queue.Clear();
            visited.Clear();
            return BreadthFirstSearch();
        }

        private IEnumerable<GraphNode<T>> BreadthFirstSearch()
        {
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                GraphNode<T> node = queue.Dequeue();
                visited.Add(node);
                yield return node;
                foreach (GraphNode<T> neibour in node.neibours)
                {
                    if (!visited.Contains(neibour) && !queue.Contains(neibour) && neibour != null)
                    {
                        queue.Enqueue(neibour);
                    }
                }
            }
        }
    }
}