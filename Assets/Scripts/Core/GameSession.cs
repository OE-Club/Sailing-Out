using System;
using UnityEngine;

namespace SailingOut.Core
{
    /// <summary>
    /// Owns the run lifecycle. Gameplay systems can observe state changes without
    /// knowing about one another or modifying this component's internal state.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Booting;

        public event Action<GameState, GameState> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ChangeState(GameState.Ready);
        }

        public void StartRun()
        {
            if (State == GameState.Playing)
            {
                return;
            }

            ChangeState(GameState.Playing);
        }

        public void PauseRun()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            ChangeState(GameState.Paused);
        }

        public void ResumeRun()
        {
            if (State != GameState.Paused)
            {
                return;
            }

            ChangeState(GameState.Playing);
        }

        public void EndRun()
        {
            if (State == GameState.GameOver)
            {
                return;
            }

            ChangeState(GameState.GameOver);
        }

        public void ResetRun()
        {
            ChangeState(GameState.Ready);
        }

        private void ChangeState(GameState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            GameState previousState = State;
            State = nextState;
            StateChanged?.Invoke(previousState, nextState);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

