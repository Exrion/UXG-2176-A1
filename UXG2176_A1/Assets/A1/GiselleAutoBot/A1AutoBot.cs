using A1.A1AutoBot;
using Assignment1;
using UnityEngine;

namespace GiselleAutoBot
{

    public class GiselleAutoBot : AutoBotContestant
    {
        public override void Ready(AutoBot enemy, Color color)
        {
            base.Ready(enemy, color);
            autobot.SetAutoBotState(new AutoBotEngage(autobot));
        }
    }

    public interface IApproachStrategy
    {
        void Approach(AutoBotController controller, Vector3 diff, Vector3 playerPos);
    }

    public class AggressiveApproach : IApproachStrategy
    {
        public void Approach(AutoBotController controller, Vector3 diff, Vector3 playerPos)
        {
            var dir = ApproahWalls.ApplyWallAvoidance(diff.normalized, playerPos);
            controller.SetMoveDir(dir);
        }
    }

    public class CautiousApproach : IApproachStrategy
    {
        public void Approach(AutoBotController controller, Vector3 diff, Vector3 playerPos)
        {
            var perpendicular = new Vector3(-diff.y, diff.x, 0).normalized;
            var weave = Mathf.Sin(Time.time * 5f);
            var dir = (diff.normalized + perpendicular * weave * 0.5f).normalized;
            dir = ApproahWalls.ApplyWallAvoidance(dir, playerPos);
            controller.SetMoveDir(dir);
        }
    }

    internal static class ApproahWalls
    {
        private const float wallMargin = 1f;
        private const float wallPushStrength = 0.4f;

        public static Vector3 ApplyWallAvoidance(Vector3 dir, Vector3 playerPos)
        {
            if (playerPos.x <= -8f + wallMargin) dir += Vector3.right * wallPushStrength;
            if (playerPos.x >= 8f - wallMargin) dir += Vector3.left * wallPushStrength;
            if (playerPos.y <= -4f + wallMargin) dir += Vector3.up * wallPushStrength;
            if (playerPos.y >= 4f - wallMargin) dir += Vector3.down * wallPushStrength;
            return dir.normalized;
        }
    }

    public class AutoBotEngage : AutoBotState
    {
        private const float meleeRange = 1f;
        private const float bulletDangerRange = 1.5f;
        private const float wallProximityMargin = 1.5f;

        private static readonly IApproachStrategy aggressiveApproach = new AggressiveApproach();
        private static readonly IApproachStrategy cautiousApproach = new CautiousApproach();

        public AutoBotEngage(AutoBotController controller) : base(controller) { }

        public override void DoActionUpdate(float dt)
        {
            var enemyPos = controller.GetEnemyPosition();
            var playerPos = controller.GetAutoBotPosition();
            var diff = enemyPos - playerPos;
            var distance = diff.magnitude;
            var maxRange = controller.GetProjectileMaxDistance();

            controller.SetFacing(diff);

            // keep the clip topped up whenever we're not actively using it
            if (!controller.IsReloading() && controller.GetProjectileLeft() <= 0)
            {
                controller.Reload();
            }

            // priority 1: survive incoming fire
            if (IsBulletIncoming())
            {
                // cornered with no room to sidestep? panic-dash instead of evading normally
                if (IsNearWall(playerPos) && controller.IsDashAvailable())
                {
                    controller.SetFacing(-diff); // Dash moves along transform.up
                    controller.Dash();
                    return;
                }

                controller.SetAutoBotState(new AutoBotEvade(controller));
                return;
            }

            // priority 2: shoot if we're in range and loaded
            if (distance <= maxRange && controller.GetProjectileLeft() > 0)
            {
                controller.SetAutoBotState(new AutoBotShoot(controller));
                return;
            }

            // priority 3: melee when we cant shoot (empty/reloading)
            if (distance <= meleeRange && controller.IsMeleeAttackAvailable())
            {
                controller.SetAutoBotState(new AutoBotMelee(controller));
                return;
            }

            // otherwise, close the distance - pick a strategy based on how vulnerable
            // we currently are (empty clip or mid-reload = can't fight back if caught,
            // so approach more carefully). This only uses data AutoBotController
            // already exposes, so it stays within the sanctioned interface.
            bool vulnerable = controller.IsReloading() || controller.GetProjectileLeft() <= 0;
            var strategy = vulnerable ? cautiousApproach : aggressiveApproach;
            strategy.Approach(controller, diff, playerPos);
        }

        private bool IsBulletIncoming()
        {
            var playerPos = controller.GetAutoBotPosition();
            foreach (var bulletPos in controller.GetEnemyProjectilePosition())
            {
                if ((bulletPos - playerPos).magnitude < bulletDangerRange)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsNearWall(Vector3 pos)
        {
            return pos.x <= -8f + wallProximityMargin || pos.x >= 8f - wallProximityMargin ||
                   pos.y <= -4f + wallProximityMargin || pos.y >= 4f - wallProximityMargin;
        }

    }

    public class AutoBotShoot : AutoBotState
    {
        public AutoBotShoot(AutoBotController controller) : base(controller)
        {
            controller.SetMoveDir(Vector3.zero);
            var diff = controller.GetEnemyPosition() - controller.GetAutoBotPosition();
            controller.SetFacing(diff);

            while (controller.GetProjectileLeft() > 0)
            {
                controller.Fire(diff);
            }
        }

        public override void DoActionUpdate(float dt)
        {
            controller.SetAutoBotState(new AutoBotEngage(controller));
        }
    }

    public class AutoBotMelee : AutoBotState
    {
        public AutoBotMelee(AutoBotController controller) : base(controller)
        {
            var diff = controller.GetEnemyPosition() - controller.GetAutoBotPosition();
            controller.SetFacing(diff);
            controller.MeleeAttack();
        }

        public override void DoActionUpdate(float dt)
        {
            controller.SetAutoBotState(new AutoBotEngage(controller));
        }
    }

    public class AutoBotEvade : AutoBotState
    {
        private const float evadeDuration = 0.3f;
        // matches the arena bounds enforced in AutoBot.FixedUpdate()
        private const float arenaMinX = -8f, arenaMaxX = 8f;
        private const float arenaMinY = -4f, arenaMaxY = 4f;

        private float timer;

        public AutoBotEvade(AutoBotController controller) : base(controller)
        {
            timer = evadeDuration;

            var playerPos = controller.GetAutoBotPosition();
            var awayFromEnemy = playerPos - controller.GetEnemyPosition();

            // rotate 90 degrees so we dodge sideways, off the bullet's line of travel
            var evadeDir = new Vector3(-awayFromEnemy.y, awayFromEnemy.x, 0).normalized;

            // if that direction would run us into a wall, dodge the other way instead
            var testPos = playerPos + evadeDir * 2f;
            if (testPos.x < arenaMinX || testPos.x > arenaMaxX ||
                testPos.y < arenaMinY || testPos.y > arenaMaxY)
            {
                evadeDir = -evadeDir;
            }

            controller.SetMoveDir(evadeDir);
        }

        public override void DoActionUpdate(float dt)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                controller.SetAutoBotState(new AutoBotEngage(controller));
            }
        }

    }
}
