﻿namespace Asteroids
{
    public enum ExecutionOrder
    {
        O1_GlobalMementoManager = -1,
        O1_EventManager = -1,
        O2_Normal = 0,
        O3_LifeUI = 1,
        O4_GameManager = 2,
        O5_EnemySpawner = 3,
        O6_Score = 4,
    }
}