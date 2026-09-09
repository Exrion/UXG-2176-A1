using System.Collections.Generic;
using UnityEngine;

namespace Assignment1
{
    public interface AutoBotController
    {
        public void SetAutoBotState(AutoBotState state);
        public Vector3 GetAutoBotPosition(); // get own position
        public int GetProjectileLeft(); // get projectile count
        public bool IsReloading(); // check if projectile is reloading
        public void Reload(); // reload the projectile count
        public float GetProjectileMaxDistance(); // get the projectile max distance
        public void Fire(Vector3 dir); // fire projectile base on given dir;
        public bool IsMeleeAttackAvailable(); // check if melee attack is ready
        public void MeleeAttack(); // attack base on the current up vector
        public bool IsDashAvailable(); // check if dash is in cooldown
        public void Dash(); // dash X distance base on current up vector
        public void SetFacing(Vector3 dir);
        public void SetMoveDir(Vector3 dir); // move to attack position at fixed preset speed
        public Vector3 GetEnemyPosition(); // get the enemy position
        public List<Vector3> GetEnemyProjectilePosition(); // get the list of active enemy projectile and their position
    }
}