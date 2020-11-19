﻿using Asteroids.Scene;
using Asteroids.Utils;

using System;

using UnityEngine;

namespace Asteroids.Entities.Enemies
{
    public partial class SimpleEnemyBuilder
    {
        private readonly struct Memento
        {
            /* This struct is in charge of storing and setting the enemy state for rewinding
            * Technically, the create and set memento methods should be members of the Originator class
            * according to the pure Memento pattern.
            * However, that makes a bit convulted the SimpleEnemyBuilder class and increase its responsabilities amount.
            * 
            * This is why me add that logic in the Memento type and rewind here. So for the SimpleEnemyBuilder's point of view, it's only Memento.TrackForRewind.
            * Anyway, the implementation is not exposed because the Memento type is a nested type of the SimpleEnemyBuilder class
            * which allow us to access the private state of the SimpleEnemyBuilder without exposing it to other non-related classes.
            * 
            * This make easier to organice code.
            */

            // Cache delegate to reduce allocations
            private static readonly Func<Memento, Memento, float, Memento> interpolateMementos = InterpolateMementos;

            public readonly bool enabled;
            public readonly Vector3 position;
            public readonly float rotation;
            public readonly Vector2 velocity;
            public readonly float angularVelocity;
            public readonly Sprite sprite;

            private Memento(bool enabled, Vector3 position, float rotation, Vector2 velocity, float angularVelocity, Sprite sprite)
            {
                this.enabled = enabled;
                this.position = position;
                this.rotation = rotation;
                this.velocity = velocity;
                this.angularVelocity = angularVelocity;
                this.sprite = sprite;
            }

            private Memento(Rigidbody2D rigidbody, SpriteRenderer spriteRenderer) : this(
                rigidbody.gameObject.activeSelf,
                rigidbody.position,
                rigidbody.rotation,
                rigidbody.velocity,
                rigidbody.angularVelocity,
                spriteRenderer.sprite
            ) { }

            public static void TrackForRewind(IPool<GameObject, (Vector3 position, Vector3 speed)> pool, Rigidbody2D rigidbody, SpriteRenderer spriteRenderer, PolygonCollider2D collider) => GlobalMementoManager.Subscribe(
                    () => new Memento(rigidbody, spriteRenderer),
                    (memento) => ConsumeMemento(memento, pool, rigidbody, spriteRenderer, collider),
                    interpolateMementos
               );

            private static void ConsumeMemento(Memento? memento, IPool<GameObject, (Vector3 position, Vector3 speed)> pool, Rigidbody2D rigidbody, SpriteRenderer spriteRenderer, PolygonCollider2D collider)
            {
                if (memento.HasValue)
                {
                    Memento memento_ = memento.Value;
                    if (memento_.enabled)
                    {
                        // Since enemies are pooled, we must force the pool to give us control of this instance in case it was in his control.
                        pool.ExtractIfHas(rigidbody.gameObject); // Read from rigidbody to reduce closure size

                        rigidbody.position = memento_.position;
                        rigidbody.rotation = memento_.rotation;
                        rigidbody.velocity = memento_.velocity;
                        rigidbody.angularVelocity = memento_.angularVelocity;
                        spriteRenderer.sprite = memento_.sprite;

                        int count = memento_.sprite.GetPhysicsShapeCount();
                        for (int i = 0; i < count; i++)
                        {
                            memento_.sprite.GetPhysicsShape(i, physicsShape);
                            collider.SetPath(i, physicsShape);
                        }
                    }
                    else
                        pool.Store(rigidbody.gameObject); // Read from rigidbody to reduce closure size
                }
                else
                    pool.Store(rigidbody.gameObject); // Read from rigidbody to reduce closure size
            }

            private static Memento InterpolateMementos(
                Memento a,
                Memento b,
                float delta
                )
            {
                // Handle resurrection
                if (a.enabled != b.enabled)
                    return delta > .5f ? b : a;

                // Handle screen wrapping
                float height = Camera.main.orthographicSize * 2;
                height *= .35f; // Allow offset error. Warning, this value may cause problem depending on GlobalMementoManager.storePerSecond and maximum enemy speed
                if (Mathf.Abs(a.position.y - b.position.y) > height || Mathf.Abs(a.position.x - b.position.x) > height * Camera.main.aspect)
                    return delta > .5f ? b : a;

                Debug.Assert(a.enabled == b.enabled);
                return new Memento(
                     a.enabled,
                     Vector3.Lerp(a.position, b.position, delta),
                     Mathf.LerpAngle(a.rotation, b.rotation, delta),
                     Vector2.Lerp(a.velocity, b.velocity, delta),
                     Mathf.Lerp(a.angularVelocity, b.angularVelocity, delta),
                     delta > .5f ? b.sprite : a.sprite
               );
            }
        }
    }
}