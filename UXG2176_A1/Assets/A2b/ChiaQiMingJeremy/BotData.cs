using Assignment1;
using System.Collections.Generic;
using UnityEngine;

public static class BotData
{
    // Player Data
    public static float playerSize = 1f;
    public static float projSize = 0.3f;
    public static float attackRangeMult = 0.9f;
    public static float meleeRange = 2f;
    public static float projDangerRange = 1.5f;
    public static int reloadThreshold = 2;
    public static bool doPositiveNormal = true;

    // Enemy Data
    private static Vector3 lastEnemyPos = Vector3.zero;
    public static Vector3 enemyVelocity = Vector3.zero;

    public static void UpdateParams(Vector3 currEnemyPos)
    {
        // update enemy data
        enemyVelocity = (currEnemyPos - lastEnemyPos) / Time.fixedDeltaTime;
        lastEnemyPos = currEnemyPos;
    }

    public static bool AreProjectilesNearby(List<Vector3> enemyBulletsPos, Vector3 playerPos)
    {
        foreach (Vector3 bulletPos in enemyBulletsPos)
        {
            float dist = Vector3.Distance(bulletPos, playerPos);
            if (dist <= projDangerRange)
            {
                return true;
            }
        }
        return false;
    }

    public static Vector3 GetNearestProjectile(List<Vector3> enemyBulletsPos, Vector3 playerPos)
    {
        Vector3 nearestBulletPos = Vector3.zero;
        float minDist = float.MaxValue;
        float collisionRadius = playerSize + projSize;
        foreach (Vector3 bulletPos in enemyBulletsPos)
        {
            float centreToCentreDist = Vector3.Distance(bulletPos, playerPos);
            float dangerDist = centreToCentreDist - collisionRadius;
            if (dangerDist < minDist)
            {
                nearestBulletPos = bulletPos;
                minDist = dangerDist;
            }
        }

        return nearestBulletPos;
    }

    public static float GetNearestProjectileDist(List<Vector3> enemyBulletsPos, Vector3 playerPos)
    {
        float minDist = float.MaxValue;
        float collisionRadius = playerSize + projSize;
        foreach (Vector3 bullet in enemyBulletsPos)
        {
            float centreToCentreDist = Vector3.Distance(bullet, playerPos);
            float dangerDist = centreToCentreDist - collisionRadius;
            if (dangerDist < minDist)
            {
                minDist = dangerDist;
            }
        }

        return minDist;
    }
}
