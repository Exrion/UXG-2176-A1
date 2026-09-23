using System.Collections.Generic;
using Assignment1;
using UnityEngine;

namespace A1.JethroMak
{
    public class JethroAutoBot : AutoBotContestant
    {
        public override void Ready(AutoBot enemy, Color color)
        {
            base.Ready(enemy, color);

            autobot.SetAutoBotState(new JethroRush(autobot));
        }
    }


    public class JethroRush : AutoBotState
    {
        public JethroRush(AutoBotController controller) : base(controller)
        {
        }

        public override void DoActionUpdate(float dt)
        {
            Vector3 myPos = controller.GetAutoBotPosition();
            Vector3 enemyPos = controller.GetEnemyPosition();

            Vector3 toEnemy = enemyPos - myPos;
            float distance = toEnemy.magnitude;

            // Dodge if an enemy bullet gets very close.
            if (DodgeBullet())
            {
                return;
            }

            // Face the enemy.
            controller.SetFacing(toEnemy);

            // Get close before firing.
            if (distance <= 1.5f)
            {
                controller.SetAutoBotState(
                    new JethroAttack(controller)
                );

                return;
            }

            // Move towards enemy.
            // A little sideways movement makes us harder to hit.
            Vector3 forward = toEnemy.normalized;

            Vector3 sideways = new Vector3(
                -forward.y,
                forward.x,
                0f
            );

            Vector3 moveDirection =
                forward + sideways * 0.15f;

            controller.SetMoveDir(moveDirection);
        }


        private bool DodgeBullet()
        {
            List<Vector3> bullets =
                controller.GetEnemyProjectilePosition();

            if (bullets.Count == 0)
            {
                return false;
            }

            Vector3 myPos =
                controller.GetAutoBotPosition();

            Vector3 enemyPos =
                controller.GetEnemyPosition();

            Vector3 closestBullet = bullets[0];

            float closestDistance =
                Vector3.Distance(
                    myPos,
                    closestBullet
                );

            for (int i = 1; i < bullets.Count; i++)
            {
                float distance =
                    Vector3.Distance(
                        myPos,
                        bullets[i]
                    );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBullet = bullets[i];
                }
            }

            // Ignore bullets that are still far away.
            if (closestDistance > 1.5f)
            {
                return false;
            }

            Vector3 bulletDirection =
                closestBullet - enemyPos;

            Vector3 dodgeDirection =
                new Vector3(
                    -bulletDirection.y,
                    bulletDirection.x,
                    0f
                );

            // Don't dodge outside the arena.
            Vector3 testPos =
                myPos + dodgeDirection.normalized;

            if (Mathf.Abs(testPos.x) > 7f ||
                Mathf.Abs(testPos.y) > 3f)
            {
                dodgeDirection = -dodgeDirection;
            }

            controller.SetFacing(dodgeDirection);
            controller.SetMoveDir(dodgeDirection);

            // Only dash if the bullet is extremely close.
            if (closestDistance < 0.8f &&
                controller.IsDashAvailable())
            {
                controller.Dash();
            }

            return true;
        }
    }


    public class JethroAttack : AutoBotState
    {
        public JethroAttack(AutoBotController controller)
            : base(controller)
        {
        }

        public override void DoActionUpdate(float dt)
        {
            Vector3 myPos =
                controller.GetAutoBotPosition();

            Vector3 enemyPos =
                controller.GetEnemyPosition();

            Vector3 direction =
                enemyPos - myPos;

            float distance =
                direction.magnitude;

            // Stop and face the enemy.
            controller.SetMoveDir(Vector3.zero);
            controller.SetFacing(direction);

            // If enemy is close enough, melee as well.
            if (distance <= 1.8f &&
                controller.IsMeleeAttackAvailable())
            {
                controller.MeleeAttack();
            }

            // Fire all bullets at once.
            int bullets =
                controller.GetProjectileLeft();

            for (int i = 0; i < bullets; i++)
            {
                controller.Fire(direction);
            }

            // Start reloading.
            if (!controller.IsReloading())
            {
                controller.Reload();
            }

            // Dodge while reloading.
            controller.SetAutoBotState(
                new JethroReload(controller)
            );
        }
    }


    public class JethroReload : AutoBotState
    {
        private int dodgeSide = 1;

        public JethroReload(AutoBotController controller)
            : base(controller)
        {
        }

        public override void DoActionUpdate(float dt)
        {
            // If reload has finished and our bullets are back,
            // attack again.
            if (!controller.IsReloading() &&
                controller.GetProjectileLeft() > 0)
            {
                controller.SetAutoBotState(
                    new JethroRush(controller)
                );

                return;
            }

            // Safety check.
            // If somehow we have no bullets but reload stopped,
            // start the reload again.
            if (!controller.IsReloading() &&
                controller.GetProjectileLeft() <= 0)
            {
                controller.Reload();
            }

            DodgeAndMove();
        }


        private void DodgeAndMove()
        {
            Vector3 myPos =
                controller.GetAutoBotPosition();

            Vector3 enemyPos =
                controller.GetEnemyPosition();

            Vector3 toEnemy =
                enemyPos - myPos;


            // If the enemy comes close while we are reloading,
            // melee them and move backwards.
            if (toEnemy.magnitude <= 1.8f &&
                controller.IsMeleeAttackAvailable())
            {
                controller.SetFacing(toEnemy);
                controller.MeleeAttack();

                // Move away while still facing the enemy.
                controller.SetMoveDir(-toEnemy);

                return;
            }

            List<Vector3> bullets =
                controller.GetEnemyProjectilePosition();


            // If there is an enemy bullet,
            // dodge sideways from it.
            if (bullets.Count > 0)
            {
                Vector3 closestBullet = bullets[0];

                float closestDistance =
                    Vector3.Distance(
                        myPos,
                        closestBullet
                    );


                for (int i = 1; i < bullets.Count; i++)
                {
                    float distance =
                        Vector3.Distance(
                            myPos,
                            bullets[i]
                        );

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestBullet = bullets[i];
                    }
                }


                if (closestDistance < 2f)
                {
                    Vector3 bulletDirection =
                        closestBullet - enemyPos;

                    Vector3 dodgeDirection =
                        new Vector3(
                            -bulletDirection.y,
                            bulletDirection.x,
                            0f
                        );

                    dodgeDirection *= dodgeSide;


                    Vector3 testPos =
                        myPos +
                        dodgeDirection.normalized * 1.5f;


                    // Flip direction if we're about to hit a wall.
                    if (Mathf.Abs(testPos.x) > 7f ||
                        Mathf.Abs(testPos.y) > 3f)
                    {
                        dodgeSide *= -1;
                        dodgeDirection = -dodgeDirection;
                    }


                    controller.SetFacing(dodgeDirection);
                    controller.SetMoveDir(dodgeDirection);


                    // Emergency dash only.
                    if (closestDistance < 0.8f &&
                        controller.IsDashAvailable())
                    {
                        controller.Dash();
                    }

                    return;
                }
            }


            // No dangerous bullet.
            // Circle around the enemy while reloading.
            if (toEnemy.sqrMagnitude > 0.001f)
            {
                Vector3 forward =
                    toEnemy.normalized;

                Vector3 sideways =
                    new Vector3(
                        -forward.y,
                        forward.x,
                        0f
                    );

                sideways *= dodgeSide;


                // Move slightly away while circling.
                Vector3 moveDirection =
                    sideways - forward * 0.35f;


                // Move towards the centre if we're near an edge.
                if (Mathf.Abs(myPos.x) > 6.2f)
                {
                    moveDirection.x +=
                        -Mathf.Sign(myPos.x);
                }

                if (Mathf.Abs(myPos.y) > 2.6f)
                {
                    moveDirection.y +=
                        -Mathf.Sign(myPos.y);
                }


                controller.SetFacing(toEnemy);
                controller.SetMoveDir(moveDirection);
            }
        }
    }
}