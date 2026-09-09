using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTree
{
    public class Sequence : Node
    {
        public Sequence() : base() { }
        public Sequence(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            bool anyChildRunning = false;

            foreach (var child in children)
            {
                switch (child.Evaluate())
                {
                    case NodeState.Failure:
                        state = NodeState.Failure;
                        return state;
                    case NodeState.Success:
                        continue;
                    case NodeState.Running:
                        anyChildRunning = true;
                        continue;
                    default:
                        state = NodeState.Success;
                        break;
                }
            }

            state = anyChildRunning ? NodeState.Running : NodeState.Success;
            return state;
        }
    }
}