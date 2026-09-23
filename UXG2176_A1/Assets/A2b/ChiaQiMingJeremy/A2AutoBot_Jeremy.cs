using UnityEngine;
using System.Collections.Generic;

namespace Assignment1
{
    public class A2AutoBot_Jeremy : AutoBotContestant
    {
        public override void Ready(AutoBot enemy, Color color)
        {
            base.Ready(enemy, color);
            autobot.SetAutoBotState(new A2AutoBotEngage(autobot));
        }
    }

    public class A2AutoBotEvade : AutoBotState
    {
        public A2AutoBotEvade(AutoBotController controller) : base(controller)
        {
        }

        public override void DoActionUpdate(float dt)
        {
            // if enemy projectiles are nearby
            if (BotData.AreProjectilesNearby(controller.GetEnemyProjectilePosition(), controller.GetAutoBotPosition()))
            {
                // reload if low on projectiles
                if (controller.GetProjectileLeft() <= BotData.reloadThreshold)
                {
                    controller.Reload();
                }
            }
            // switch to engage if no enemy projectiles nearby
            else
            {
                if (!controller.IsReloading())
                {
                    controller.SetAutoBotState(new A2AutoBotEngage(controller));
                    return;
                }
            }

            // calculate direction to move to best avoid enemy projectiles
            Vector3 evadeDir = CalculateEvadeDir();
            controller.SetMoveDir(evadeDir);
            Vector3 enemyDir = controller.GetEnemyPosition() - controller.GetAutoBotPosition();
            float projDist = BotData.GetNearestProjectileDist(controller.GetEnemyProjectilePosition(), controller.GetAutoBotPosition());

            // if projectile gets too close to the player, move diagonally backwards
            if (projDist < BotData.projDangerRange * 0.5f)
            {
                Vector3 projDir = BotData.GetNearestProjectile(controller.GetEnemyProjectilePosition(), controller.GetAutoBotPosition()) - controller.GetAutoBotPosition();
                controller.SetMoveDir(evadeDir - projDir);
            }
            else
            {
                // set player to face enemy once far enough from projectile
                if (projDist >= (BotData.projDangerRange + BotData.playerSize + BotData.projSize))
                {
                    controller.SetFacing(enemyDir);
                }
            }

            // if enemy is nearby, switch to melee if available
            if (enemyDir.magnitude < BotData.meleeRange)
            {
                if (controller.IsMeleeAttackAvailable())
                {
                    controller.SetFacing(enemyDir);
                    controller.SetAutoBotState(new A2AutoBotMelee(controller));
                }
            }
        }

        private Vector3 CalculateEvadeDir()
        {
            List<Vector3> enemyBulletsPos = controller.GetEnemyProjectilePosition();
            Vector3 evadeDir = Vector3.zero;
            float collisionRadius = BotData.playerSize + BotData.projSize;

            // cycle thru each enemy bullet
            foreach (Vector3 bulletPos in enemyBulletsPos)
            {
                // calculate distance from player to enemy bullet
                Vector3 bulletDir = bulletPos - controller.GetAutoBotPosition();
                float distToBullet = bulletDir.magnitude;
                distToBullet -= collisionRadius;

                // if bullet is within danger range
                if (distToBullet < (BotData.projDangerRange + collisionRadius))
                {
                    Vector3 normal = Vector3.zero;
                    Vector3 playerPos = controller.GetAutoBotPosition();

                    normal = new Vector3(-bulletDir.y, bulletDir.x, 0f);
                    normal.Normalize();
                    float weight = 1f - (distToBullet / BotData.projDangerRange);
                    evadeDir += normal * weight;
                }
            }

            // if bullets are all far away, evade based on enemy pos
            if (evadeDir.magnitude < 0.1f)
            {
                Vector3 enemyDir = controller.GetEnemyPosition() - controller.GetAutoBotPosition();
                Vector3 playerPos = controller.GetAutoBotPosition();

                if (Mathf.Abs(playerPos.x) > 6f || Mathf.Abs(playerPos.y) > 2.5f)
                {
                    BotData.doPositiveNormal = !BotData.doPositiveNormal;
                }

                if (BotData.doPositiveNormal)
                {
                    evadeDir = new Vector3(-enemyDir.y, enemyDir.x, 0f);
                }
                else
                {
                    evadeDir = new Vector3(enemyDir.y, -enemyDir.x, 0f);
                }
            }

            return evadeDir.normalized;
        }
    }

    public class A2AutoBotEngage : AutoBotState
    {
        public A2AutoBotEngage(AutoBotController controller) : base(controller)
        {
        }

        public override void DoActionUpdate(float dt)
        {
            // change state to evade if projectile is too close
            if (BotData.AreProjectilesNearby(controller.GetEnemyProjectilePosition(), controller.GetAutoBotPosition()))
            {
                controller.SetAutoBotState(new A2AutoBotEvade(controller));
            }
            else
            {
                // calc dir and dist to enemy
                Vector3 enemyDir = controller.GetEnemyPosition() - controller.GetAutoBotPosition();
                float dist = Vector3.Magnitude(enemyDir);

                // if enemy is within melee range and melee is available, melee
                if (dist < BotData.meleeRange)
                {
                    if (controller.IsMeleeAttackAvailable())
                    {
                        controller.SetAutoBotState(new A2AutoBotMelee(controller));
                        return;
                    }
                }

                // if enemy is within attack range, check for remaining bullets
                if (dist <= controller.GetProjectileMaxDistance() * BotData.attackRangeMult)
                {
                    // if player has enough bullets, shoot
                    if (controller.GetProjectileLeft() > 0)
                    {
                        controller.SetAutoBotState(new A2AutoBotShoot(controller));
                    }
                    // if player does not have enough bullets, reload and change to evade
                    else
                    {
                        controller.Reload();
                        controller.SetAutoBotState(new A2AutoBotEvade(controller));
                    }
                }
                // if enemy is not within attack range, move towards enemy
                else
                {
                    enemyDir.Normalize();
                    controller.SetMoveDir(enemyDir);
                }
            }
        }
    }

    public class A2AutoBotShoot : AutoBotState
    {
        public A2AutoBotShoot(AutoBotController controller) : base(controller)
        {
        }

        public override void DoActionUpdate(float dt)
        {
            BotData.UpdateParams(controller.GetEnemyPosition());

            // change state to evade if projectile is too close
            if (BotData.AreProjectilesNearby(controller.GetEnemyProjectilePosition(), controller.GetAutoBotPosition()))
            {
                controller.SetAutoBotState(new A2AutoBotEvade(controller));
                return;
            }

            // if no projectiles left, reload
            if (controller.GetProjectileLeft() <= 0)
            {
                controller.Reload();
                controller.SetAutoBotState(new A2AutoBotEvade(controller));
                return;
            }

            //// predict enemy position based on movement velocity
            //Vector3 predictedPos = controller.GetEnemyPosition() + BotData.enemyVelocity * BotData.predictionTime;
            //Vector3 fireDir = predictedPos - controller.GetAutoBotPosition();
            //fireDir.Normalize();

            //// shoot at predicted position
            //controller.Fire(fireDir);

            // calc dir to enemy
            Vector3 enemyDir = controller.GetEnemyPosition() - controller.GetAutoBotPosition();
            enemyDir.Normalize();

            // shoot in enemy direction
            controller.Fire(enemyDir);
        }
    }

    public class A2AutoBotMelee : AutoBotState
    {
        public A2AutoBotMelee(AutoBotController controller) : base(controller)
        {
        }

        public override void DoActionUpdate(float dt)
        {
            BotData.UpdateParams(controller.GetEnemyPosition());

            // melee in the direction of the enemy
            Vector3 enemyDir = controller.GetEnemyPosition() - controller.GetAutoBotPosition();
            controller.SetFacing(enemyDir);
            controller.MeleeAttack();

            // switch to evade after melee attack
            controller.SetAutoBotState(new A2AutoBotEvade(controller));
        }
    }
}
