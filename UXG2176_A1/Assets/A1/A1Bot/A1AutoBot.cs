using Assignment1;
using UnityEngine;

namespace A1.A1AutoBot
{

    public class A1AutoBot : AutoBotContestant
    {
        public override void Ready(AutoBot enemy, Color color)
        {
            base.Ready(enemy, color);
            autobot.SetAutoBotState(new A1AutoBotIdle(autobot));
        }
    }

    public class A1AutoBotIdle : AutoBotState
    {
        private float timer = 0.0f;
        public A1AutoBotIdle(AutoBotController controller) : base(controller)
        {
            controller.SetMoveDir(Vector3.zero);
            timer = 0.5f;
        }
        public override void DoActionUpdate(float dt)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                controller.SetAutoBotState(new A1AutoBotChase(controller));
            }
        }
    }
    public class A1AutoBotChase : AutoBotState
    {
        public A1AutoBotChase(AutoBotController controller) : base(controller)
        {
        }

        public override void DoActionUpdate(float dt)
        {
            var enemyPos = controller.GetEnemyPosition();
            var playerPos = controller.GetAutoBotPosition();

            var diff = enemyPos - playerPos;
            var projectileLeft = controller.GetProjectileLeft();
            controller.SetMoveDir(diff);
            if (!controller.IsReloading() && projectileLeft <= 0)
            {
                controller.Reload();
            }
            if (diff.magnitude <= controller.GetProjectileMaxDistance() && projectileLeft > 0)
            {
                controller.SetAutoBotState(new A1AutoBotShoot(controller));
            }
            else if (diff.magnitude <= 1 && controller.IsMeleeAttackAvailable())
            {
                controller.SetAutoBotState(new A1AutoBotRun(controller));
            }
        }
    }

    public class A1AutoBotRun : AutoBotState
    {
        private float timer = 0f;
        private Vector3[] escapePt = new Vector3[4]{
            new Vector3(-7, 3, 0),
            new Vector3(7, 3, 0),
            new Vector3(7, -3, 0),
            new Vector3(-7, -3, 0)
        };

        int cornerTarget = 0;
        public A1AutoBotRun(AutoBotController controller) : base(controller)
        {
            timer = 1f;
            cornerTarget = -1;
        }

        public override void DoActionUpdate(float dt)
        {
            var playerPos = controller.GetAutoBotPosition();
            var enemyPos = controller.GetEnemyPosition();
            var diff = playerPos - enemyPos;

            if(diff.magnitude <= 1 && controller.IsDashAvailable())
            {
                controller.SetFacing(-playerPos.normalized);
                controller.Dash();
                controller.SetAutoBotState(new A1AutoBotIdle(controller));
            }
            // check if already near edge
            if (cornerTarget != -1)
            {
                // run from enemy
                diff = escapePt[cornerTarget] - playerPos;
            }
            else
            {
                if (playerPos.x <= -7 || playerPos.x >= 7
                    || playerPos.y <= -3 || playerPos.y >= 3)
                {
                    // check player quadrant
                    if (playerPos.x >= 0)
                    {
                        cornerTarget = playerPos.y >= 0 ? 1 : 2;
                    }
                    else
                    {
                        cornerTarget = playerPos.y >= 0 ? 0 : 3;
                    }
                    return;
                }
            }
            
            controller.SetMoveDir(diff);
            timer -= dt;
            if(timer <= 0.0f)
            {
                controller.SetAutoBotState(new A1AutoBotChase(controller));
            }
        }
    }
    public class A1AutoBotShoot : AutoBotState
    {
        public A1AutoBotShoot(AutoBotController controller) : base(controller)
        {
            controller.SetMoveDir(Vector3.zero);
        }

        public override void DoActionUpdate(float dt)
        {
            var enemyPos = controller.GetEnemyPosition();
            var playerPos = controller.GetAutoBotPosition();

            var diff = enemyPos - playerPos;
            controller.SetFacing(diff);
            controller.Fire(diff);

            controller.SetAutoBotState(new A1AutoBotRun(controller));
        }
    }
}
