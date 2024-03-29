﻿using Asteroids.PowerUps;

using Enderlook.GOAP;
using Enderlook.StateMachine;

using UnityEngine;

namespace Asteroids.Entities.Enemies
{
    public sealed partial class Boss
    {
        private void CreateAndAddPickUpPowerUpAbility(StateMachineBuilder<object, object, object> builder, StateBuilder<object, object, object>[] builders, int index)
        {
            Node<int> node = new Node<int>(
                (BossState _, ref BossState now) => {
                    if (now.HasPowerUpInScene)
                    {
                        now.HasPowerUpInScene = false;
                        return SatisfactionResult.Satisfied;
                    }
                    return SatisfactionResult.NotProgressed;
                },
                (ref BossState worldState) => {
                    if (worldState.HasPowerUpInScene)
                    {
                        worldState.HasPowerUpInScene = false;
                        return true;
                    }
                    return false;
                },
                worldState => lifes - worldState.BossHealth,
                (int _, ref BossState worldState) => worldState.BossHealth = Mathf.Min(worldState.BossHealth + healthRestoredPerPowerUp, lifes),
                null,
                cost => cost
            );
            actions[index] = node;

            Transform powerUp = null;
            builders[index] = builder.In(node)
                .OnEntry(() =>
                {
                    FindPowerUp();

                    if (powerUp == null)
                        WorldIsNotAsExpected();
                })
                .OnExit(() => powerUp = null)
                .OnUpdate(() =>
                {
                    if (powerUp == null)
                        // Player picked the power up, look for a new one.
                        FindPowerUp();

                    if (powerUp == null)
                        WorldIsNotAsExpected();
                    else
                        MoveAndRotateTowards(powerUp.position);
                });

            void FindPowerUp()
            {
                float distance = float.PositiveInfinity;

                foreach (PowerUpTemplate.PickupBehaviour pickup in FindObjectsOfType<PowerUpTemplate.PickupBehaviour>())
                {
                    float newDistance = (pickup.transform.position - transform.position).sqrMagnitude;
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        powerUp = pickup.transform;
                    }
                }
            }
        }
    }
}