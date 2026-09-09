using Assignment1;
using BehaviourTree;
using System.Collections.Generic;
using UnityEngine;

namespace BitBotBehaviourTree
{
    // https://medium.com/geekculture/how-to-create-a-simple-behaviour-tree-in-unity-c-3964c84c060e
    public class BitBotBT : BehaviourTree.Tree
    {
        protected override Node SetupTree()
        {
            Node root = new Selector(new List<Node>
            {
                new Dodge(),
                new Selector(new List<Node>
                {
                    new ShootFar(),
                    new ShootNear()
                }),
                new Selector(new List<Node>
                {
                    new Chase(),
                    new Retreat()
                })
            });
            return root;
        }
    }

    public class Dodge : Node
    {
        public override NodeState Evaluate()
        {
            var controller = (AutoBotController)parent.GetData("controller");
            var projectiles = controller.GetEnemyProjectilePosition();

            return base.Evaluate();
        }
    }

    public class Chase : Node
    {
        public override NodeState Evaluate()
        {
            var controller = (AutoBotController)parent.GetData("controller");
            float currentDistance = Vector3.Distance(controller.GetAutoBotPosition(), controller.GetEnemyPosition());
            if (currentDistance < controller.GetProjectileMaxDistance() * .75f)
            {
                return NodeState.Failure;
            }
            else
            {
                if (controller.GetProjectileLeft() == 0)
                {
                    return NodeState.Failure;
                }
                var enemyPos = controller.GetEnemyPosition();
                var playerPos = controller.GetAutoBotPosition();
                var diff = enemyPos - playerPos;
                controller.SetMoveDir(diff);
            }
            return NodeState.Success;
        }
    }

    public class Retreat : Node
    {
        public override NodeState Evaluate()
        {
            return base.Evaluate();
        }
    }

    public class ShootFar : Node
    {
        public override NodeState Evaluate()
        {
            return base.Evaluate();
        }
    }

    public class ShootNear : Node
    {
        public override NodeState Evaluate()
        {
            return base.Evaluate();
        }
    }
}