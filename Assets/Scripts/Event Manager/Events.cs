﻿namespace Asteroids.Events
{
    public readonly struct LevelTerminationEvent
    {
        public readonly bool HasWon;

        public bool HasLost => !HasWon;

        public LevelTerminationEvent(bool hasWon) => HasWon = hasWon;

        public static LevelTerminationEvent Win => new LevelTerminationEvent(true);

        public static LevelTerminationEvent Lose => new LevelTerminationEvent(false);
    }

    public readonly struct PlayerHealthChangedEvent
    {
        public readonly bool HasIncreased;

        public PlayerHealthChangedEvent(bool hasIncreased) => HasIncreased = hasIncreased;

        public static PlayerHealthChangedEvent Increase => new PlayerHealthChangedEvent(true);

        public static PlayerHealthChangedEvent Decrease => new PlayerHealthChangedEvent(false);
    }

    public readonly struct EnemyDestroyedEvent
    {
        public readonly int Score;

        public EnemyDestroyedEvent(int score) => Score = score;
    }

    public readonly struct EnemySplittedEvent
    {
        public readonly int Amount;

        public EnemySplittedEvent(int amount) => Amount = amount;
    }

    public readonly struct ScoreHasChangedEvent
    {
        public readonly int NewScore;

        public ScoreHasChangedEvent(int newScore) => NewScore = newScore;
    }

    public readonly struct PauseEvent
    {
        public readonly bool IsPaused;

        public bool IsPlaying => !IsPaused;

        public PauseEvent(bool isPaused) => IsPaused = isPaused;

        public static PauseEvent Pause => new PauseEvent(true);

        public static PauseEvent Play => new PauseEvent(false);
    }
}
