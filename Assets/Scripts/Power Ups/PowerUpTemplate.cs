﻿using Asteroids.Entities.Enemies;
using Asteroids.Entities.Player;
using Asteroids.Scene;

using Enderlook.Unity.Attributes;

using System.Collections.Generic;

using UnityEngine;

namespace Asteroids.PowerUps
{
    public abstract class PowerUpTemplate : ScriptableObject
    {
        private static List<Vector2> physicsShape = new List<Vector2>();

        [field: SerializeField, IsProperty, Tooltip("Power up sprite.")]
        protected Sprite sprite { get; private set; }

        [field: SerializeField, IsProperty, Tooltip("Power up pickup sound.")]
        protected AudioClip sound { get; private set; }

        [field: SerializeField, IsProperty, Tooltip("Scale of power up.")]
        protected float scale { get; private set; }

        public GameObject CreatePickup(AudioSource audioSource, int layer)
        {
            // We don't pool power ups because only a single one can be active at the same time
            // Pooling a single power up would be an overkill
            // Also, the exercise didn't request for pooling, factory nor builder patterns in power ups
            // Only decorator

            GameObject powerUp = new GameObject("Power up");
            powerUp.transform.localScale *= scale;

            powerUp.AddComponent<Rigidbody2D>().gravityScale = 0;

            SpriteRenderer renderer = powerUp.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            audioSource.clip = sound;
            audioSource.pitch = 1;

            PolygonCollider2D collider = powerUp.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true;
            int count = sprite.GetPhysicsShapeCount();
            for (int i = 0; i < count; i++)
            {
                sprite.GetPhysicsShape(i, physicsShape);
                collider.SetPath(i, physicsShape);
            }

            PickupBehaviour pickupBehaviour = powerUp.AddComponent<PickupBehaviour>();
            pickupBehaviour.pickup = GetPickup(audioSource);

            return powerUp;
        }

        protected virtual IPickup GetPickup(AudioSource audioSource) => new PickupDecorable(audioSource);

        public sealed class PickupBehaviour : MonoBehaviour
        {
            private Renderer[] renderers;

            private float destroyTime = 5;
            private float destroyIn;

            public IPickup pickup;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
            private void Awake()
            {
                renderers = GetComponentsInChildren<Renderer>();
                destroyIn = destroyTime;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
            private void Update()
            {
                if (IsVisible())
                    destroyIn = destroyTime;
                destroyIn -= Time.deltaTime;
                if (destroyIn < 0)
                    Destroy(gameObject);
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Unity.")]
            private void OnTriggerEnter2D(Collider2D collision)
            {
                if (collision.gameObject.GetComponent<PlayerController>() != null)
                {
                    pickup.PickUp();
                    Destroy(gameObject);
                }
                else if (collision.gameObject.GetComponent<Boss>() != null)
                {
                    pickup.BossPickup();
                    Destroy(gameObject);
                }
            }

            private bool IsVisible()
            {
                foreach (Renderer renderer in renderers)
                    if (renderer.isVisible)
                        return true;
                return false;
            }
        }

        private sealed class PickupDecorable : IPickup
        {
            // This class could be prefectly inlined in the PickupBehaviour,
            // but we need it to make the decorator pattern.
            // Alternatively, PickupBehaviour could implement IPickup, and make player responsability to pickup powerups,
            // thought that defeats SOLID.

            private AudioSource audioSource;

            public PickupDecorable(AudioSource audioSource) => this.audioSource = audioSource;

            void IPickup.PickUp()
            {
                EventManager.Raise(new OnPowerUpPickedEvent(true));
                audioSource.Play();
            }

            void IPickup.BossPickup()
            {
                EventManager.Raise(new OnPowerUpPickedEvent(false));
                audioSource.Play();
            }
        }
    }
}
