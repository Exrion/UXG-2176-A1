using UnityEngine;

namespace Assignment1
{
    public class BulletScript : MonoBehaviour
    {
        private bool isFlying = false;
        private float speed, distance;
        private Vector2 direction;

        private float flightDist;
        private AutoBot owner;

        public void Initialize(AutoBot owner, float speed, float distance, Vector2 direction)
        {
            isFlying = true;
            this.speed = speed;
            this.distance = distance;
            this.direction = direction;
            this.owner = owner;

            flightDist = 0;
        }

        public AutoBot GetOwner()
        {
            return owner;
        }

        void FixedUpdate()
        {
            if (isFlying)
            {
                Vector3 movement = (Vector3)direction.normalized * speed * Time.fixedDeltaTime;
                this.transform.position += movement;

                flightDist += movement.magnitude;

                if (flightDist >= distance)
                {
                    owner.DestroyBullet(this);
                }
            }
        }

        public void DoOnHit()
        {
            owner.DestroyBullet(this);
        }
    }
}