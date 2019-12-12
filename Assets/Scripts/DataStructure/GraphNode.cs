using System.Collections.Generic;
using System.Linq;

namespace DataStructure
{
    /// <summary>
    /// 图节点
    /// </summary>
    [System.Serializable]
    public class GraphNode<T>
    {
        private List<GraphNode<T>> _neibourList;
        private T _value;

        public GraphNode(T value, IEnumerable<GraphNode<T>> neibours = null)
        {
            _value = value;
            if (neibours == null)
            {
                _neibourList = new List<GraphNode<T>>();
            }
            else
            {
                _neibourList = neibours.ToList();
            }
        }

        public GraphNode(T value, GraphNode<T>[] neibours) : this(value, (IEnumerable<GraphNode<T>>)neibours)
        {
        }

        public T value
        {
            get { return _value; }
        }

        public int neiboursCount
        {
            get { return _neibourList.Count; }
        }

        public List<GraphNode<T>> neibours
        {
            get { return _neibourList; }
        }

        public void AddNeibour(GraphNode<T> neibour)
        {
            _neibourList.Add(neibour);
        }

        public void AddNeibours(params GraphNode<T>[] neibours)
        {
            _neibourList.AddRange(neibours);
        }

        public void AddNeibours(IEnumerable<GraphNode<T>> neibours)
        {
            _neibourList.AddRange(neibours);
        }

        public void RemoveNeibour(GraphNode<T> node)
        {
            _neibourList.Remove(node);
        }
    }
}
