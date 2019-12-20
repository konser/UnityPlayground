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
        private Dictionary<GraphNode<T>, float> _costDic;

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
            _costDic = new Dictionary<GraphNode<T>, float>();
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

        public float CostToNeibour(GraphNode<T> neibourNode)
        {
            return _costDic[neibourNode];
        }

        public void AddNeibour(GraphNode<T> neibour,float cost = 0)
        {
            _neibourList.Add(neibour);
            _costDic[neibour] = cost;
        }

        public void AddNeibours(GraphNode<T>[] neibours,params float[] costs)
        {
            _neibourList.AddRange(neibours);

            for (int i = 0; i < neibours.Length; i++)
            {
                _costDic[neibours[i]] = costs[i];
            }
        }

        //public void AddNeibours(IEnumerable<GraphNode<T>> neibours)
        //{
        //    _neibourList.AddRange(neibours);
        //}

        public void RemoveNeibour(GraphNode<T> node)
        {
            _neibourList.Remove(node);
            _costDic.Remove(node);
        }
    }
}
