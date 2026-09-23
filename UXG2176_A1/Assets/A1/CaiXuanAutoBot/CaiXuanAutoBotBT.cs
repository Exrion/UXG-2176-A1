using Assignment1;
using System;
using System.Collections.Generic;
using CaiXuan_BehaviourTree;
using UnityEngine;

namespace CaiXuanAutoBot
{
    // https://medium.com/geekculture/how-to-create-a-simple-behaviour-tree-in-unity-c-3964c84c060e
    public class CaiXuanAutoBotBT : CaiXuan_BehaviourTree.Tree
    {
        protected override Node SetupTree()
        {
            Node root = new Selector(new List<Node>
            {
                new Reload(),
                new Dodge(),
                new Selector(new List<Node>
                {
                    new ShootFar(),
                    new ShootNear(),
                    new Melee()
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
        private const int AngleSteps = 16;
        private const int HorizonSteps = 16;
        private List<Vector3> currentPositions = new();
        private List<Vector3> previousPositions = new();

        public override NodeState Evaluate()
        {
            var controller = (AutoBotController)parent.GetData("controller");
            var projectiles = controller.GetEnemyProjectilePosition();
            var playerPos = controller.GetAutoBotPosition();

            if (projectiles.Count == 0)
            {
                return NodeState.Failure;
            }   

            if (projectiles.Count != currentPositions.Count)
            {
                currentPositions = new List<Vector3>(projectiles);
                previousPositions = new List<Vector3>(currentPositions);
                return NodeState.Failure;
            }
            else
            {
                previousPositions = new List<Vector3>(currentPositions);
                currentPositions = new List<Vector3>(projectiles);
            }

            var moveDir = GetDodgeDir();
            if (moveDir == Vector3.zero)
            {
                return NodeState.Failure;
            }
            controller.SetMoveDir(moveDir);
            return NodeState.Running;
        }

        private Vector3 GetDodgeDir()
        {
            Vector3 bestDir = Vector3.zero;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < AngleSteps; i++)
            {
                float angle = (float)(i * 2.0 * Math.PI / AngleSteps);
                var dir = new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle));

                float score = EvalDir(dir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }

            return bestDir;
        }

        private float EvalDir(Vector3 dir)
        {
            float worstClearance = float.MaxValue;
            var controller = (AutoBotController)parent.GetData("controller");

            int count = 0;
            foreach (var p in currentPositions)
            {
                for (int t = 0; t <= HorizonSteps; t++)
                {
                    Vector3 botFuture = controller.GetAutoBotPosition() + dir * 5f * Time.deltaTime;
                    Vector3 projFuture = ((p - previousPositions[count]).normalized * 7f * Time.deltaTime) + p;

                    float dist = Vector2.Distance(botFuture, projFuture);
                    float clearance = dist - (1f + .5f * .3f);
                    float timeWeight = 1f / (1f + t * 0.15f);
                    float weighted = clearance * timeWeight;

                    if (weighted < worstClearance)
                        worstClearance = weighted;
                }
                count++;
            }

            return currentPositions.Count == 0 ? 0f : worstClearance;
        }
    }

    public class Chase : Node
    {
        private const float chaseDistanceScalar = .9f;

        public override NodeState Evaluate()
        {
            var controller = (AutoBotController)parent.GetData("controller");
            float currentDistance = Vector3.Distance(controller.GetAutoBotPosition(), controller.GetEnemyPosition());
            if (currentDistance < controller.GetProjectileMaxDistance() * chaseDistanceScalar)
            {
                controller.SetMoveDir(Vector3.zero);
                return NodeState.Failure;
            }
            else
            {
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
        private const float retreatDistanceScalar = 1.2f;
        private const float retreatQuadrantThreshold = 0.5f;
        private readonly Vector2 mapHalfSize = new(8f, 4f);

        public override NodeState Evaluate()
        {
            var controller = (AutoBotController)parent.GetData("controller");
            float currentDistance = Vector3.Distance(controller.GetAutoBotPosition(), controller.GetEnemyPosition());
            if (currentDistance > controller.GetProjectileMaxDistance() * retreatDistanceScalar)
            {
                controller.SetMoveDir(Vector3.zero);
                return NodeState.Failure;
            }
            else
            {
                var enemyPos = controller.GetEnemyPosition();
                var playerPos = controller.GetAutoBotPosition();
                var diff = enemyPos - playerPos;

                // Bot Left
                if (playerPos.x < -(mapHalfSize.x * retreatQuadrantThreshold) && playerPos.y < -(mapHalfSize.y * retreatQuadrantThreshold))
                {
                    diff.x += diff.x * playerPos.x / -mapHalfSize.x + playerPos.x / -mapHalfSize.x;
                    diff.y += diff.y * playerPos.y / -mapHalfSize.y + playerPos.y / -mapHalfSize.y;
                }
                // Bot Right
                else if (playerPos.x < (mapHalfSize.x * retreatQuadrantThreshold) && playerPos.y < -(mapHalfSize.y * retreatQuadrantThreshold))
                {
                    diff.x += diff.x * playerPos.x / mapHalfSize.x + playerPos.x / mapHalfSize.x;
                    diff.y += diff.y * playerPos.y / -mapHalfSize.y + playerPos.y / -mapHalfSize.y;
                }
                // Top Left
                else if (playerPos.x < -(mapHalfSize.x * retreatQuadrantThreshold) && playerPos.y < (mapHalfSize.y * retreatQuadrantThreshold))
                {
                    diff.x += diff.x * playerPos.x / -mapHalfSize.x + playerPos.x / -mapHalfSize.x;
                    diff.y += diff.y * playerPos.y / mapHalfSize.y + playerPos.y / mapHalfSize.y;
                }
                // Top Right
                else if (playerPos.x < (mapHalfSize.x * retreatQuadrantThreshold) && playerPos.y < (mapHalfSize.y * retreatQuadrantThreshold))
                {
                    diff.x += diff.x * playerPos.x / mapHalfSize.x + playerPos.x / mapHalfSize.x;
                    diff.y += diff.y * playerPos.y / mapHalfSize.y + playerPos.y / mapHalfSize.y;
                }
                controller.SetMoveDir(-diff);
            }
            Debug.Log("CX Bot:\t Retreat");
            return NodeState.Success;
        }
    }

    public class ShootFar : Node
    {
        private const float shootDistanceScalar = 0.8f;
        private const float enemyVelocity = 5f;
        private const float bulletVelocity = 7f;
        private Vector2 previousEnemyPos = Vector2.zero;
        private Vector2 enemyDirection = Vector2.zero;

        public override NodeState Evaluate()
        {
            var controller = (AutoBotController)parent.GetData("controller");
            var projLeft = controller.GetProjectileLeft();
            float currentDistance = Vector3.Distance(controller.GetAutoBotPosition(), controller.GetEnemyPosition());
            UpdateEnemyData(controller);

            if (projLeft > 0 && currentDistance > controller.GetProjectileMaxDistance() * shootDistanceScalar && currentDistance <= controller.GetProjectileMaxDistance())
            {
                var enemyVel = enemyVelocity * enemyDirection;
                var dist = Vector2.Distance(previousEnemyPos, controller.GetAutoBotPosition());
                var bulletTime = dist / bulletVelocity;

                Vector2 enemyPos = enemyVelocity == 0 ? controller.GetEnemyPosition() : previousEnemyPos + enemyVel * bulletTime;
                Vector2 playerPos = controller.GetAutoBotPosition();
                var diff = enemyPos - playerPos;
                controller.SetFacing(diff);
                if (projLeft > 0)
                {
                    controller.Fire(diff);
                    return NodeState.Success;
                }
                previousEnemyPos = Vector2.zero;
                enemyDirection = Vector2.zero;
                return NodeState.Failure;
            }
            previousEnemyPos = Vector2.zero;
            enemyDirection = Vector2.zero;
            return NodeState.Failure;
        }

        private void UpdateEnemyData(AutoBotController controller)
        {
            enemyDirection = ((Vector2)controller.GetEnemyPosition() - previousEnemyPos).normalized;
            previousEnemyPos = controller.GetEnemyPosition();
        }
    }

    public class ShootNear : Node
    {
        private const float shootDistanceScalar = 0.25f;

        public override NodeState Evaluate()
        {
            var controller = (AutoBotController)parent.GetData("controller");
            var projLeft = controller.GetProjectileLeft();
            float currentDistance = Vector3.Distance(controller.GetAutoBotPosition(), controller.GetEnemyPosition());

            if (projLeft > 0 && currentDistance > controller.GetProjectileMaxDistance() * shootDistanceScalar && currentDistance <= controller.GetProjectileMaxDistance())
            {
                var enemyPos = controller.GetEnemyPosition();
                var playerPos = controller.GetAutoBotPosition();
                var diff = enemyPos - playerPos;
                controller.SetFacing(diff);
                if (projLeft >= 2)
                {
                    controller.Fire(diff);
                    controller.Fire(diff);
                    return NodeState.Success;
                }
            }
            return NodeState.Failure;
        }
    }

    public class Melee : Node
    {
        private const float meleeDistanceScalar = 0.1f;

        public override NodeState Evaluate()
        {
            var controller = (AutoBotController)parent.GetData("controller");
            float currentDistance = Vector3.Distance(controller.GetAutoBotPosition(), controller.GetEnemyPosition());

            if (currentDistance <= controller.GetProjectileMaxDistance() * meleeDistanceScalar)
            {
                if (controller.IsMeleeAttackAvailable())
                {
                    controller.MeleeAttack();
                }
            }
            return NodeState.Failure;
        }
    }

    public class Reload : Node
    {
        public override NodeState Evaluate()
        {
            var controller = (AutoBotController)parent.GetData("controller");
            if (!controller.IsReloading() && controller.GetProjectileLeft() < 5)
            {
                controller.Reload();
                return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }
}