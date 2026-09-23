using System.Collections.Generic;
using UnityEngine;
using Assignment1;

public class WillieAutoBot : AutoBotContestant
{
    public override void Ready(AutoBot enemy, Color color)
    {
        base.Ready(enemy, color);

        autobot.SetAutoBotState(
            new WillieCombatState(autobot, enemy)
        );
    }
}

public class WillieCombatState : AutoBotState
{
    private int side;
    private AutoBot enemy;

    private float strafeTimer;
    private bool strafing;

    // Maximum amount of time the bot can continuously strafe.
    private const float MaxStrafeTime = 0.7f;

    // Time spent moving towards the enemy after strafing.
    private const float ApproachTime = 0.4f;

    public WillieCombatState(
        AutoBotController controller,
        AutoBot enemy)
        : base(controller)
    {
        this.enemy = enemy;

        side =
            controller.GetAutoBotPosition().x >= 0f
            ? -1
            : 1;

        strafeTimer = 0f;
        strafing = true;
    }

    public override void DoActionUpdate(float dt)
    {
        if (enemy.IsDead())
        {
            controller.SetAutoBotState(
                new WillieVictoryState(controller)
            );

            return;
        }

        Vector3 enemyPosition =
            controller.GetEnemyPosition();

        Vector3 myPosition =
            controller.GetAutoBotPosition();

        Vector3 toEnemy =
            enemyPosition - myPosition;

        toEnemy.z = 0f;

        float distance =
            toEnemy.magnitude;

        if (distance < 0.01f)
        {
            toEnemy = Vector3.right;
            distance = 0.01f;
        }

        toEnemy.Normalize();

        //Projectile danger check. If a projectile is too close, switch to dodge state.
        if (WillieUtility.ProjectileDanger(controller))
        {
            controller.SetAutoBotState(
                new WillieDodgeState(
                    controller,
                    side,
                    enemy
                )
            );

            return;
        }

        //Melee attack check. If the enemy is within melee range and melee attack is available, switch to melee state.
        if (distance < 0.8f)
        {
            if (controller.IsMeleeAttackAvailable())
            {
                controller.SetAutoBotState(
                    new WillieMeleeState(
                        controller,
                        side,
                        enemy
                    )
                );

                return;
            }
        }

        //Reload check. If the bot is out of projectiles, switch to reload state.
        if (controller.GetProjectileLeft() <= 0)
        {
            controller.SetAutoBotState(
                new WillieReloadState(
                    controller,
                    side,
                    enemy
                )
            );

            return;
        }

        //Strafe timer update. Switch between strafing and moving towards the enemy based on the timer.
        strafeTimer += dt;

        if (strafing && strafeTimer >= MaxStrafeTime)
        {
            strafing = false;
            strafeTimer = 0f;
        }
        else if (!strafing && strafeTimer >= ApproachTime)
        {
            strafing = true;
            strafeTimer = 0f;
        }

        //Movement. If strafing, move perpendicular to the direction to the enemy. If not strafing, move towards the enemy.
        Vector3 movement;

        if (strafing)
        {
            movement =
                WillieUtility.GetStrafeCombatMovement(
                    myPosition,
                    enemyPosition,
                    side
                );
        }
        else
        {
            // Briefly move towards the enemy.
            movement =
                toEnemy;
        }

        controller.SetMoveDir(movement);

        // Always face enemy.
        controller.SetFacing(toEnemy);

        //Shoot if within range. If the enemy is within the maximum projectile distance, fire a projectile.
        if (distance <= controller.GetProjectileMaxDistance())
        {
            controller.Fire(toEnemy);
        }
    }
}

public class WillieDodgeState : AutoBotState
{
    private float timer;
    private int side;
    private AutoBot enemy;

    public WillieDodgeState(
        AutoBotController controller,
        int side,
        AutoBot enemy)
        : base(controller)
    {
        this.side = side;
        this.enemy = enemy;

        timer = 0f;
    }

    public override void DoActionUpdate(float dt)
    {
        //Victory check. If the enemy is dead, switch to victory state.
        if (enemy.IsDead())
        {
            controller.SetAutoBotState(
                new WillieVictoryState(controller)
            );

            return;
        }

        timer += dt;

        Vector3 position =
            controller.GetAutoBotPosition();

        Vector3 toEnemy =
            WillieUtility.GetDirectionToEnemy(controller);

        Vector3 strafe =
            WillieUtility.GetStrafeDirection(
                controller,
                side
            );

        //Move mostly sideways, but also slightly away from the enemy.
        Vector3 movement =
            strafe * 0.9f
            - toEnemy * 0.2f;

        controller.SetMoveDir(movement);
        controller.SetFacing(toEnemy);

        //Emergency dash if projectile is extremely close.
        List<Vector3> projectiles =
            controller.GetEnemyProjectilePosition();

        foreach (Vector3 projectile in projectiles)
        {
            if (Vector3.Distance(position, projectile) < 0.8f)
            {
                if (controller.IsDashAvailable())
                {
                    controller.Dash();
                }

                break;
            }
        }

        if (timer >= 0.5f)
        {
            controller.SetAutoBotState(
                new WillieCombatState(
                    controller,
                    enemy
                )
            );
        }
    }
}

public class WillieMeleeState : AutoBotState
{
    private float timer;
    private bool attacked;
    private int side;
    private AutoBot enemy;

    public WillieMeleeState(
        AutoBotController controller,
        int side,
        AutoBot enemy)
        : base(controller)
    {
        this.side = side;
        this.enemy = enemy;

        timer = 0f;
        attacked = false;
    }

    public override void DoActionUpdate(float dt)
    {
        //Victory check. If the enemy is dead, switch to victory state.
        if (enemy.IsDead())
        {
            controller.SetAutoBotState(
                new WillieVictoryState(controller)
            );

            return;
        }

        timer += dt;

        Vector3 position =
            controller.GetAutoBotPosition();

        Vector3 toEnemy =
            WillieUtility.GetDirectionToEnemy(controller);

        Vector3 strafe =
            WillieUtility.GetStrafeDirection(
                controller,
                side
            );

        //Attack if melee is available and not already attacked.
        if (!attacked &&
            controller.IsMeleeAttackAvailable())
        {
            controller.SetFacing(toEnemy);
            controller.MeleeAttack();

            attacked = true;
        }

        //Move after attacking to avoid being hit by the enemy.
        Vector3 movement =
            -toEnemy * 0.8f
            + strafe * 0.8f;

        controller.SetMoveDir(movement);
        controller.SetFacing(toEnemy);

        //Return to combat state after a short delay.
        if (timer >= 0.25f)
        {
            controller.SetAutoBotState(
                new WillieCombatState(
                    controller,
                    enemy
                )
            );
        }
    }
}

public class WillieReloadState : AutoBotState
{
    private int side;
    private AutoBot enemy;

    public WillieReloadState(
        AutoBotController controller,
        int side,
        AutoBot enemy)
        : base(controller)
    {
        this.side = side;
        this.enemy = enemy;
    }

    public override void DoActionUpdate(float dt)
    {
        //Same victory check as in other states. If the enemy is dead, switch to victory state.
        if (enemy.IsDead())
        {
            controller.SetAutoBotState(
                new WillieVictoryState(controller)
            );

            return;
        }

        Vector3 position =
            controller.GetAutoBotPosition();

        Vector3 toEnemy =
            WillieUtility.GetDirectionToEnemy(controller);

        float distance =
            Vector3.Distance(
                position,
                controller.GetEnemyPosition()
            );

        Vector3 strafe =
            WillieUtility.GetStrafeDirection(
                controller,
                side
            );

        //Keep moving while reloading, but also try to stay away from the enemy.
        Vector3 movement;

        if (distance < 1.8f)
        {
            movement =
                -toEnemy * 0.8f
                + strafe * 0.8f;
        }
        else
        {
            movement = strafe;
        }

        controller.SetMoveDir(movement);
        controller.SetFacing(toEnemy);

        //Dodge if a projectile is too close while reloading.
        if (WillieUtility.ProjectileDanger(controller))
        {
            controller.SetAutoBotState(
                new WillieDodgeState(
                    controller,
                    side,
                    enemy
                )
            );

            return;
        }

        //Reload if not already reloading.
        if (!controller.IsReloading())
        {
            controller.Reload();
        }
        //Return to combat state after reloading.
        if (controller.GetProjectileLeft() > 0)
        {
            controller.SetAutoBotState(
                new WillieCombatState(
                    controller,
                    enemy
                )
            );
        }
    }
}

//Victory state. Stop all actions.
public class WillieVictoryState : AutoBotState
{
    public WillieVictoryState(
        AutoBotController controller)
        : base(controller)
    {
    }

    public override void DoActionUpdate(float dt)
    {
        controller.SetMoveDir(Vector3.zero);
    }
}


public static class WillieUtility
{
    //Get direction to enemy. If the enemy is at the same position, return right.

    public static Vector3 GetDirectionToEnemy(
        AutoBotController controller)
    {
        Vector3 position =
            controller.GetAutoBotPosition();

        Vector3 enemy =
            controller.GetEnemyPosition();

        Vector3 direction =
            enemy - position;

        direction.z = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector3.right;
        }

        return direction.normalized;
    }

    //Combat movement. Move away from the enemy while strafing to the side. If too close, move away from the enemy while strafing.
    public static Vector3 GetCombatMovement(
       Vector3 player,
       Vector3 enemy,
       int side)
    {
        Vector3 toEnemy =
            enemy - player;

        toEnemy.z = 0f;

        float distance =
            toEnemy.magnitude;

        if (distance < 0.01f)
        {
            return Vector3.right * side;
        }

        Vector3 forward =
            toEnemy.normalized;

        Vector3 perpendicular =
            new Vector3(
                -forward.y,
                forward.x,
                0f
            );

        perpendicular *= side;

        Vector3 movement;

        //Too close. Strongly move away from the enemy while strafing.
        if (distance < 2.0f)
        {
            movement =
                -forward * 0.9f
                + perpendicular * 0.7f;
        }

        //Continue moving away from the enemy while strafing, but less strongly.
        else if (distance < 4.0f)
        {
            movement =
                -forward * 0.55f
                + perpendicular * 0.9f;
        }

        //Strafe while slowly retreating from the enemy.
        else
        {
            movement =
                -forward * 0.25f
                + perpendicular;
        }

        if (movement.sqrMagnitude < 0.01f)
        {
            movement = -forward;
        }

        return movement.normalized;
    }

    public static Vector3 GetStrafeCombatMovement(
    Vector3 player,
    Vector3 enemy,
    int side)
    {
        Vector3 toEnemy =
            enemy - player;

        toEnemy.z = 0f;

        if (toEnemy.sqrMagnitude < 0.01f)
        {
            return Vector3.right * side;
        }

        toEnemy.Normalize();

        Vector3 perpendicular =
            new Vector3(
                -toEnemy.y,
                toEnemy.x,
                0f
            );

        perpendicular *= side;

        return perpendicular.normalized;
    }
    public static bool IsAtSideBoundary(Vector3 position)
    {
        return position.x <= -7.5f ||
               position.x >= 7.5f;
    }
    public static Vector3 GetBoundaryEscapeDirection(Vector3 position)
    {
        // Left wall
        if (position.x <= -7.5f)
        {
            return Vector3.right;
        }

        // Right wall
        if (position.x >= 7.5f)
        {
            return Vector3.left;
        }

        return Vector3.zero;
    }

    //Projectile danger check. If any projectile is within 1.5 units, return true. Else, return false.
    public static bool ProjectileDanger(
        AutoBotController controller)
    {
        Vector3 position =
            controller.GetAutoBotPosition();

        List<Vector3> projectiles =
            controller.GetEnemyProjectilePosition();

        foreach (Vector3 projectile in projectiles)
        {
            float distance =
                Vector3.Distance(
                    position,
                    projectile
                );

            if (distance < 1.5f)
            {
                return true;
            }
        }

        return false;
    }

    //Strafe direction. Get a direction perpendicular to the direction to the enemy. If side is negative, strafe left. If side is positive, strafe right.
    public static Vector3 GetStrafeDirection(
        AutoBotController controller,
        int side)
    {
        Vector3 toEnemy =
            GetDirectionToEnemy(controller);

        Vector3 strafe =
            new Vector3(
                -toEnemy.y,
                toEnemy.x,
                0f
            );

        if (side < 0)
        {
            strafe *= -1f;
        }

        return strafe.normalized;
    }
}
