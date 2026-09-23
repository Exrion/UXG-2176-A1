using System.Collections.Generic;
using UnityEngine;

namespace CaiXuan_BehaviourTree
{
    public enum NodeState 
    {
        Running,
        Success,
        Failure
    }

    public class Node
    {
        public NodeState state;
        public Node parent;
        protected List<Node> children = new();
        private Dictionary<string, object> _dataCtx = new();

        public Node()
        {
            parent = null;
        }

        public Node(List<Node> children)
        {
            foreach (var child in children)
            {
                Attach(child);
            }
        }

        private void Attach(Node child)
        {
            child.parent = this;
            children.Add(child);
        }

        public virtual NodeState Evaluate()
        {
            return NodeState.Failure;
        }

        public void SetData(string k, object v)
        {
            _dataCtx[k] = v;
        }

        public object GetData(string k)
        {
            object val = null;
            if (_dataCtx.TryGetValue(k, out val))
            {
                return val;
            }

            Node node = parent;
            if (node != null)
            {
                val = node.GetData(k);
            }
            return val;
        }

        public bool ClearData(string k)
        {
            bool cleared = false;
            if (_dataCtx.ContainsKey(k))
            {
                _dataCtx.Remove(k);
                return true;
            }

            Node node = parent;
            if (node != null)
            {
                cleared = node.ClearData(k);
            }
            return cleared;
        }
    }
}
