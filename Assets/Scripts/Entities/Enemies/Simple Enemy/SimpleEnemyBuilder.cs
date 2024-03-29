﻿using Asteroids.Scene;
using Asteroids.Utils;

using Enderlook.Enumerables;
using Enderlook.Unity.Components.ScriptableSound;

using System.Collections.Generic;

using UnityEngine;

using Resources = Asteroids.Utils.Resources;

namespace Asteroids.Entities.Enemies
{
    public sealed partial class SimpleEnemyBuilder : IPool<GameObject, (Vector3 position, Vector3 speed)>
    {
        private static readonly List<Vector2> physicsShape = new List<Vector2>();
        private static readonly BuilderFactoryPool<GameObject, SimpleEnemyFlyweight, (Vector3 position, Vector3 speed)>.Initializer initialize = Initialize;
        private static readonly BuilderFactoryPool<GameObject, SimpleEnemyFlyweight, (Vector3 position, Vector3 speed)>.Deinitializer deinitialize = Deinitialize;

        private readonly Dictionary<Sprite, string> reverseSprites = new Dictionary<Sprite, string>();
        private readonly BuilderFactoryPool<GameObject, SimpleEnemyFlyweight, (Vector3 position, Vector3 speed)> builder;
        public SimpleEnemyFlyweight Flyweight {
            get => builder.flyweight;
            set => builder.flyweight = value;
        }

        private string id;

        public SimpleEnemyBuilder(string id)
        {
            builder = new BuilderFactoryPool<GameObject, SimpleEnemyFlyweight, (Vector3 position, Vector3 speed)>
                {
                    constructor = InnerConstruct,
                    commonInitializer = (in SimpleEnemyFlyweight flyweight, GameObject enemy, in (Vector3 position, Vector3 speed) parameter)
                        => CommonInitialize(flyweight, enemy, parameter, reverseSprites),
                    initializer = initialize,
                    deinitializer = deinitialize
                };

            this.id = id;

            GameSaver.SubscribeEnemy(
                id,
                (states) =>
                {
                    foreach (EnemyState state in states)
                        state.Load(this, Create(default));
                }
            );
        }

        public static GameObject Construct(in SimpleEnemyFlyweight flyweight, in (Vector3 position, Vector3 speed) parameter, IPool<GameObject, (Vector3 position, Vector3 speed)> pool, string id, Dictionary<Sprite, string> reverseSprites)
        {
            GameObject enemy = ConstructButNotSave(flyweight, pool, out Rigidbody2D rigidbody, out SpriteRenderer spriteRenderer);

            GameSaver.SubscribeEnemy(id, () => new EnemyState(rigidbody, reverseSprites[spriteRenderer.sprite]));

            return enemy;
        }

        public static GameObject ConstructButNotSave(SimpleEnemyFlyweight flyweight, IPool<GameObject, (Vector3 position, Vector3 speed)> pool, out Rigidbody2D rigidbody, out SpriteRenderer spriteRenderer)
        {
            GameObject enemy = new GameObject(flyweight.name)
            {
                layer = flyweight.Layer,
            };
            rigidbody = enemy.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0;
            rigidbody.mass = flyweight.Mass;

            spriteRenderer = enemy.AddComponent<SpriteRenderer>();
            PolygonCollider2D collider = enemy.AddComponent<PolygonCollider2D>();
            enemy.AddComponent<ScreenWrapper>();

            SimpleSoundPlayer player = SimpleSoundPlayer.CreateOneTimePlayer(flyweight.DeathSound, false, false);
            player.GetComponent<AudioSource>().outputAudioMixerGroup = flyweight.AudioMixerGroup;

            ExecuteOnDeath executeOnDeath = enemy.AddComponent<ExecuteOnDeath>();
            executeOnDeath.flyweight = flyweight;
            executeOnDeath.pool = pool;
            executeOnDeath.player = player;

            Memento.TrackForRewind(pool, rigidbody, spriteRenderer, collider);

            return enemy;
        }

        private GameObject InnerConstruct(in SimpleEnemyFlyweight flyweight, in (Vector3 position, Vector3 speed) parameter)
            => Construct(flyweight, parameter, this, id, reverseSprites);

        public static void CommonInitialize(in SimpleEnemyFlyweight flyweight, GameObject enemy, in (Vector3 position, Vector3 speed) parameter, Dictionary<Sprite, string> reverseSprites)
        {
            // Don't use Rigidbody to set position because it has one frame delay
            enemy.transform.position = parameter.position;

            enemy.transform.localScale *= flyweight.Scale;

            Rigidbody2D rigidbody = enemy.GetComponent<Rigidbody2D>();
            rigidbody.position = parameter.position;
            rigidbody.velocity = parameter.speed;
            rigidbody.rotation = 0;

            string path = flyweight.Sprites.RandomPick();
            Sprite sprite = Resources.Load<Sprite>(path);
            reverseSprites[sprite] = path;

            enemy.GetComponent<SpriteRenderer>().sprite = sprite;

            PolygonCollider2D collider = enemy.GetComponent<PolygonCollider2D>();
            int count = sprite.GetPhysicsShapeCount();
            for (int i = 0; i < count; i++)
            {
                sprite.GetPhysicsShape(i, physicsShape);
                collider.SetPath(i, physicsShape);
            }
        }

        public static void Initialize(in SimpleEnemyFlyweight flyweight, GameObject enemy, in (Vector3 position, Vector3 speed) parameter)
            => enemy.SetActive(true);

        public static void Deinitialize(GameObject enemy) => enemy.SetActive(false);

        public GameObject Create((Vector3 position, Vector3 speed) parameter) => builder.Create(parameter);

        public void Store(GameObject obj) => builder.Store(obj);

        public void ExtractIfHas(GameObject obj) => builder.ExtractIfHas(obj);
    }
}