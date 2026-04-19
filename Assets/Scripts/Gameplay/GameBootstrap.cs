using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TuringSignal.Audio;
using TuringSignal.Core.Data;
using TuringSignal.Core.Tick;
using TuringSignal.Grid;
using TuringSignal.Input;
using TuringSignal.View;

namespace TuringSignal.Gameplay
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private TickManager tickManager;
        [SerializeField] private GridView gridView;
        [SerializeField] private RobotView robotView;
        [SerializeField] private TrapView trapView;
        [SerializeField] private RobotInputRouter robotInputRouter;
        [SerializeField] private GameAudio gameAudio;

        [Header("Grid Setup")]
        [SerializeField] private int gridWidth = 18;
        [SerializeField] private int gridHeight = 12;

        [Header("Robot Setup")]
        [SerializeField] private Vector2Int robotSpawnGridPosition = new Vector2Int(1, 1);
        [SerializeField] private Direction robotSpawnDirection = Direction.Right;

        [Header("Goal Setup")]
        [SerializeField] private Vector2Int goalGridPosition = new Vector2Int(16, 10);
        [SerializeField] private string nextSceneName = string.Empty;
        [SerializeField] private float nextLevelDelay = 0.3f;

        [Header("Level Blockers")]
        [SerializeField] private Vector2Int[] blockedCells = new Vector2Int[0];

        [Header("Trap Setup")]
        [SerializeField] private Vector2Int[] trapCells = new Vector2Int[0];
        [SerializeField] private bool trapsStartActive;
        [SerializeField] private int trapToggleIntervalTicks = 3;

        [Header("Interactable Setup")]
        [SerializeField] private InteractablePlacement[] interactablePlacements = new InteractablePlacement[0];

        [Header("Failure")]
        [SerializeField] private float restartDelay = 0.2f;

        private GridMap gridMap;
        private RobotLogic robotLogic;
        private GridItemLogic[] interactableItems = new GridItemLogic[0];
        private bool isRestarting;
        private bool isTransitioning;
        private bool areTrapsActive;
        private int trapToggleTickCounter;

        private void OnValidate()
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);

            robotSpawnGridPosition = new Vector2Int(
                Mathf.Clamp(robotSpawnGridPosition.x, 0, gridWidth - 1),
                Mathf.Clamp(robotSpawnGridPosition.y, 0, gridHeight - 1));

            goalGridPosition = new Vector2Int(
                Mathf.Clamp(goalGridPosition.x, 0, gridWidth - 1),
                Mathf.Clamp(goalGridPosition.y, 0, gridHeight - 1));

            trapToggleIntervalTicks = Mathf.Max(1, trapToggleIntervalTicks);

            SanitizeBlockedCells();
            SanitizeTrapCells();
            SanitizeInteractablePlacements();
            UpdateGridPreview();
        }

        private void Awake()
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            gameAudio = gameAudio != null ? gameAudio : FindFirstObjectByType<GameAudio>();
            gridMap = new GridMap(gridWidth, gridHeight);

            for (int i = 0; i < blockedCells.Length; i++)
            {
                gridMap.SetWalkable(blockedCells[i], false);
            }

            for (int i = 0; i < trapCells.Length; i++)
            {
                gridMap.SetTrap(trapCells[i], true);
            }

            interactableItems = new GridItemLogic[interactablePlacements.Length];

            for (int i = 0; i < interactablePlacements.Length; i++)
            {
                InteractablePlacement placement = interactablePlacements[i];
                GridItemLogic itemLogic = new GridItemLogic(gridMap, placement.interactableId, placement.gridPosition);
                interactableItems[i] = itemLogic;
                gridMap.SetInteractable(placement.gridPosition, itemLogic);
            }

            Vector2Int clampedSpawnPosition = new Vector2Int(
                Mathf.Clamp(robotSpawnGridPosition.x, 0, gridWidth - 1),
                Mathf.Clamp(robotSpawnGridPosition.y, 0, gridHeight - 1));

            areTrapsActive = trapsStartActive;
            trapToggleTickCounter = 0;
            UpdateGridPreview();

            robotLogic = new RobotLogic(gridMap, clampedSpawnPosition, robotSpawnDirection);

            if (robotView != null)
            {
                robotView.Bind(gridView, robotLogic);
            }

            if (trapView != null)
            {
                trapView.Initialize(gridView, trapCells, areTrapsActive);
            }

            if (robotInputRouter != null)
            {
                robotInputRouter.Initialize(tickManager, robotLogic);
            }

            if (gameAudio != null)
            {
                gameAudio.Initialize(robotInputRouter, robotLogic);
            }

            if (tickManager != null)
            {
                tickManager.OnDecisionWindowStarted += HandleDecisionWindowStarted;
                tickManager.OnTickExecuted += HandleTickExecuted;
            }

            robotLogic.OnMoveBlocked += HandleMoveBlocked;
        }

        private void OnDestroy()
        {
            if (tickManager != null)
            {
                tickManager.OnDecisionWindowStarted -= HandleDecisionWindowStarted;
                tickManager.OnTickExecuted -= HandleTickExecuted;
            }

            if (robotLogic != null)
            {
                robotLogic.OnMoveBlocked -= HandleMoveBlocked;
            }
        }

        private void HandleDecisionWindowStarted(int tickIndex)
        {
            robotLogic.BeginDecisionWindow();
        }

        private void HandleTickExecuted(int tickIndex)
        {
            robotLogic.ExecutePendingIntent();

            if (isTransitioning || isRestarting)
            {
                return;
            }

            if (IsRobotOnActiveTrap())
            {
                Debug.Log("Robot hit an active trap.");
                PlayDeathAudio();
                RestartCurrentLevel();
                return;
            }

            if (robotLogic.GridPosition == goalGridPosition)
            {
                LoadNextLevel();
            }

            trapToggleTickCounter++;

            if (trapToggleTickCounter >= trapToggleIntervalTicks)
            {
                areTrapsActive = !areTrapsActive;
                trapToggleTickCounter = 0;

                if (trapView != null)
                {
                    trapView.SetTrapState(areTrapsActive);
                }
            }

            UpdateGridPreview();
        }

        private void HandleMoveBlocked(Vector2Int targetCell)
        {
            Debug.Log($"Move blocked at {targetCell}.");
            PlayDeathAudio();
            RestartCurrentLevel();
        }

        private void RestartCurrentLevel()
        {
            if (isRestarting || isTransitioning)
            {
                return;
            }

            isRestarting = true;
            StartCoroutine(RestartLevelCoroutine());
        }

        private IEnumerator RestartLevelCoroutine()
        {
            if (restartDelay > 0f)
            {
                yield return new WaitForSeconds(restartDelay);
            }

            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }

        private void SanitizeBlockedCells()
        {
            if (blockedCells == null)
            {
                blockedCells = new Vector2Int[0];
                return;
            }

            List<Vector2Int> validCells = new List<Vector2Int>(blockedCells.Length);
            HashSet<Vector2Int> uniqueCells = new HashSet<Vector2Int>();

            for (int i = 0; i < blockedCells.Length; i++)
            {
                Vector2Int cell = blockedCells[i];

                if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight)
                {
                    continue;
                }

                if (cell == robotSpawnGridPosition)
                {
                    continue;
                }

                if (cell == goalGridPosition)
                {
                    continue;
                }

                if (!uniqueCells.Add(cell))
                {
                    continue;
                }

                validCells.Add(cell);
            }

            blockedCells = validCells.ToArray();
        }

        private void SanitizeTrapCells()
        {
            if (trapCells == null)
            {
                trapCells = new Vector2Int[0];
                return;
            }

            List<Vector2Int> validCells = new List<Vector2Int>(trapCells.Length);
            HashSet<Vector2Int> uniqueCells = new HashSet<Vector2Int>();

            for (int i = 0; i < trapCells.Length; i++)
            {
                Vector2Int cell = trapCells[i];

                if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight)
                {
                    continue;
                }

                if (cell == robotSpawnGridPosition || cell == goalGridPosition)
                {
                    continue;
                }

                if (ContainsCell(blockedCells, cell))
                {
                    continue;
                }

                if (!uniqueCells.Add(cell))
                {
                    continue;
                }

                validCells.Add(cell);
            }

            trapCells = validCells.ToArray();
        }

        private void SanitizeInteractablePlacements()
        {
            if (interactablePlacements == null)
            {
                interactablePlacements = new InteractablePlacement[0];
                return;
            }

            List<InteractablePlacement> validPlacements = new List<InteractablePlacement>(interactablePlacements.Length);
            HashSet<Vector2Int> uniqueCells = new HashSet<Vector2Int>();

            for (int i = 0; i < interactablePlacements.Length; i++)
            {
                InteractablePlacement placement = interactablePlacements[i];

                if (placement == null)
                {
                    continue;
                }

                Vector2Int cell = placement.gridPosition;

                if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight)
                {
                    continue;
                }

                if (cell == robotSpawnGridPosition || cell == goalGridPosition)
                {
                    continue;
                }

                if (ContainsCell(blockedCells, cell) || ContainsCell(trapCells, cell))
                {
                    continue;
                }

                if (!uniqueCells.Add(cell))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(placement.interactableId))
                {
                    placement.interactableId = $"Item_{cell.x}_{cell.y}";
                }

                validPlacements.Add(placement);
            }

            interactablePlacements = validPlacements.ToArray();
        }

        private void UpdateGridPreview()
        {
            if (gridView == null)
            {
                return;
            }

            Vector2Int clampedSpawnPosition = new Vector2Int(
                Mathf.Clamp(robotSpawnGridPosition.x, 0, gridWidth - 1),
                Mathf.Clamp(robotSpawnGridPosition.y, 0, gridHeight - 1));

            Vector2Int clampedGoalPosition = new Vector2Int(
                Mathf.Clamp(goalGridPosition.x, 0, gridWidth - 1),
                Mathf.Clamp(goalGridPosition.y, 0, gridHeight - 1));

            gridView.ConfigurePreview(
                gridWidth,
                gridHeight,
                clampedSpawnPosition,
                clampedGoalPosition,
                blockedCells,
                trapCells,
                areTrapsActive,
                GetInteractablePreviewCells());
        }

        private void LoadNextLevel()
        {
            if (isTransitioning || isRestarting)
            {
                return;
            }

            isTransitioning = true;
            StartCoroutine(LoadNextLevelCoroutine());
        }

        private IEnumerator LoadNextLevelCoroutine()
        {
            if (nextLevelDelay > 0f)
            {
                yield return new WaitForSeconds(nextLevelDelay);
            }

            if (!string.IsNullOrWhiteSpace(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }

            Scene currentScene = SceneManager.GetActiveScene();
            int nextBuildIndex = currentScene.buildIndex + 1;

            if (nextBuildIndex >= 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextBuildIndex);
                yield break;
            }

            Debug.LogWarning("Reached goal, but no next scene is configured.");
            isTransitioning = false;
        }

        private bool IsRobotOnActiveTrap()
        {
            return areTrapsActive && gridMap.HasTrapAt(robotLogic.GridPosition);
        }

        private static bool ContainsCell(Vector2Int[] cells, Vector2Int targetCell)
        {
            if (cells == null)
            {
                return false;
            }

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == targetCell)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int[] GetInteractablePreviewCells()
        {
            if (Application.isPlaying && interactableItems != null && interactableItems.Length > 0)
            {
                List<Vector2Int> runtimeCells = new List<Vector2Int>(interactableItems.Length);

                for (int i = 0; i < interactableItems.Length; i++)
                {
                    GridItemLogic item = interactableItems[i];

                    if (item == null || item.IsConsumed)
                    {
                        continue;
                    }

                    runtimeCells.Add(item.GridPosition);
                }

                return runtimeCells.ToArray();
            }

            if (interactablePlacements == null || interactablePlacements.Length == 0)
            {
                return new Vector2Int[0];
            }

            Vector2Int[] cells = new Vector2Int[interactablePlacements.Length];

            for (int i = 0; i < interactablePlacements.Length; i++)
            {
                cells[i] = interactablePlacements[i].gridPosition;
            }

            return cells;
        }

        private void PlayDeathAudio()
        {
            if (gameAudio == null)
            {
                return;
            }

            gameAudio.PlayDeath();
        }
    }
}
