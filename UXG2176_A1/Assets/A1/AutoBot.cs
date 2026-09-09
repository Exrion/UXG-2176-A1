using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assignment1
{
    public class AutoBot : MonoBehaviour, AutoBotController
    {
        // player
        public SpriteRenderer autoBotSR;
        public int HP { get; set; }
        public const int MaxHP = 10;
        private const float speed = 5f;
        private Vector3 moveDir = Vector3.zero;

        // melee 
        private const float meleeDuration = 0.2f;
        private const float meleeCD = 1f;
        private float meleeCDTimer = 0f;
        private float meleeHitCDTimer = 0f;
        private float meleeDurationTimer = 0f;
        public GameObject meleeAtk;


        // projectile
        private const int projectileCount = 5;
        private float reloadTime = 2f;
        private const float bulletSpeed = 7;
        private const float bulletDistance = 3;
        private int currentProjectile = projectileCount;
        private bool isReloading = false;
        private List<BulletScript> activeBullets = new List<BulletScript>();

        // Dash
        private const float DashSpeed = 12;
        private const float DashDuration = 0.5f;
        private const float DashCD = 2f;
        private bool isDashing;
        private float dashCDTimer = 0f;

        public BulletScript bulletPrefab;

        private AutoBot enemy;

        private AutoBotState currentState;

        public void Ready(AutoBot enemy, Color color)
        {
            this.enemy = enemy;
            isReloading = false;
            HP = MaxHP;
            autoBotSR.color = color;
            meleeCDTimer = 0f;
            meleeHitCDTimer = 0f;
            meleeDurationTimer = 0f;
            currentProjectile = projectileCount;
            meleeAtk.SetActive(false);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (dashCDTimer > 0)
            {
                if (DashCD - dashCDTimer < DashDuration)
                {
                    transform.position += transform.up * (DashSpeed * dt);
                }
                dashCDTimer -= dt;
                if (dashCDTimer <= 0)
                {
                    isDashing = false;
                }
            }

            if (!isDashing && moveDir.sqrMagnitude > 0)
            {
                transform.position += moveDir * (speed * dt);
            }
            transform.position = new Vector3(
                Math.Clamp(transform.position.x, -8f, 8f),
                Math.Clamp(transform.position.y, -4f, 4f),
                transform.position.z);

            if (meleeHitCDTimer > 0)
            {
                meleeHitCDTimer -= dt;
            }

            if (meleeCDTimer > 0)
            {
                meleeCDTimer -= dt;
            }

            if (meleeDurationTimer > 0)
            {
                meleeDurationTimer -= dt;
                if (meleeDurationTimer < 0)
                {
                    meleeAtk.SetActive(false);
                }
            }

            currentState?.DoActionUpdate(dt);
        }

        public void SetAutoBotState(AutoBotState state)
        {
            currentState = state;
            Debug.Log($"{name} ChangeState : {state.GetType()}");
        }

        public Vector3 GetAutoBotPosition()
        {
            return transform.position;
        }

        public int GetProjectileLeft()
        {
            return currentProjectile;
        }

        public bool IsReloading()
        {
            return isReloading;
        }

        public void Reload()
        {
            if (isReloading)
            {
                return;
            }

            isReloading = true;
            StartCoroutine(CoReload());
        }

        public IEnumerator CoReload()
        {
            yield return new WaitForSeconds(reloadTime);
            currentProjectile = projectileCount;
            isReloading = false;
        }

        public float GetProjectileMaxDistance()
        {
            return bulletDistance;
        }

        public void Fire(Vector3 dir)
        {
            if (currentProjectile > 0)
            {
                var direction = dir.normalized;
                var b = Instantiate(bulletPrefab, transform.parent);
                b.transform.position = transform.position + direction * 0.5f;
                b.Initialize(this, bulletSpeed, bulletDistance, direction);
                --currentProjectile;
                activeBullets.Add(b);
            }
            else
            {
                Reload();
            }
        }

        public void DestroyBullet(BulletScript bullet)
        {
            activeBullets.Remove(bullet);
            Destroy(bullet.gameObject);
        }

        public bool IsMeleeAttackAvailable()
        {
            return meleeCDTimer <= 0;
        }

        public void MeleeAttack()
        {
            if (IsMeleeAttackAvailable())
            {
                meleeDurationTimer = meleeDuration;
                meleeCDTimer = meleeCD;
                meleeAtk.SetActive(true);
            }
        }

        public bool IsDashAvailable()
        {
            return dashCDTimer <= 0;
        }

        public void Dash()
        {
            if (IsDashAvailable())
            {
                isDashing = true;
                dashCDTimer = DashCD;
            }
        }

        public void SetFacing(Vector3 dir)
        {
            if (dir.magnitude == 0)
            {
                return;
            }
            transform.up = dir.normalized;
        }

        public void SetMoveDir(Vector3 dir)
        {
            if (isDashing)
            {
                return;
            }

            moveDir = dir.normalized;
        }

        public Vector3 GetEnemyPosition()
        {
            return enemy.transform.position;
        }

        private List<BulletScript> GetBullets()
        {
            return activeBullets;
        }

        public List<Vector3> GetEnemyProjectilePosition()
        {
            List<Vector3> bulletPos = new();
            var bullets = enemy.GetBullets();
            foreach (var bullet in bullets)
            {
                bulletPos.Add(bullet.transform.position);
            }

            return bulletPos;
        }



        private void OnTriggerEnter2D(Collider2D collision)
        {
            var damager = collision.gameObject.GetComponent<BulletScript>();
            if (damager != null)
            {
                if (damager.GetOwner() != this)
                {
                    TakeDamage(1);
                    damager.DoOnHit();
                }
            }

            if (collision.gameObject.name == "MeleeAtk")
            {
                TakeDamage(1);
            }
        }

        private void TakeDamage(int damage)
        {
            HP -= damage;
            if (HP <= 0)
            {
                gameObject.SetActive(false);
            }
        }

        public bool IsDead()
        {
            return HP <= 0;
        }

        public void Clear()
        {
            var bullets = GetComponentsInChildren<BulletScript>();
            foreach (var bullet in bullets)
            {
                Destroy(bullet.gameObject);
            }
            gameObject.SetActive(false);
        }
    }
}