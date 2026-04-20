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
        [SerializeField] private GoalView goalView;
        [SerializeField] private TrapView trapView;
        [SerializeField] private RobotInputRouter robotInputRouter;
        [SerializeField] private GameAudio gameAudio;
        [Tooltip("可选。场景里挂有 KeyLockView 的物体，用于钥匙/锁/身上钥匙的美术显示。")]
        [SerializeField] private KeyLockView keyLockView;

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
        [SerializeField] private Vector2Int[] oddTrapCells = new Vector2Int[0];
        [SerializeField] private Vector2Int[] evenTrapCells = new Vector2Int[0];

        [Header("Interactable Setup")]
        [Tooltip("When enabled, E only sets interact intent if the cell in front of the robot has an interactable that can be used.")]
        [SerializeField] private bool restrictInteractToFacingInteractable = true;
        [SerializeField] private InteractablePlacement[] interactablePlacements = new InteractablePlacement[0];

        [Header("Failure")]
        [SerializeField] private float restartDelay = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool trapDebugLogs;

        private GridMap gridMap;
        private RobotLogic robotLogic;
        private GridItemLogic[] genericInteractables = new GridItemLogic[0];
        private KeyItemLogic[] keyInteractables = new KeyItemLogic[0];
        private LockItemLogic[] lockInteractables = new LockItemLogic[0];
        private bool isRestarting;
        private bool isTransitioning;
        private bool displayedOddTrapPhaseActive;

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

            SanitizeBlockedCells();
            SanitizeTrapCells(ref oddTrapCells, evenTrapCells);
            SanitizeTrapCells(ref evenTrapCells, oddTrapCells);
            SanitizeInteractablePlacements();
            ApplyTrapPhaseForTick(0);
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

            for (int i = 0; i < oddTrapCells.Length; i++)
            {
                gridMap.SetTrap(oddTrapCells[i], true);
            }

            for (int i = 0; i < evenTrapCells.Length; i++)
            {
                gridMap.SetTrap(evenTrapCells[i], true);
            }

            List<GridItemLogic> genericList = new List<GridItemLogic>();
            List<KeyItemLogic> keyList = new List<KeyItemLogic>();
            List<LockItemLogic> lockList = new List<LockItemLogic>();

            for (int i = 0; i < interactablePlacements.Length; i++)
            {
                InteractablePlacement placement = interactablePlacements[i];
                Vector2Int pos = placement.gridPosition;

                switch (placement.role)
                {
                    case InteractableRole.Key:
                        KeyItemLogic keyLogic = new KeyItemLogic(gridMap, placement.keyColor, pos);
                        keyList.Add(keyLogic);
                        gridMap.SetWalkable(pos, false);
                        gridMap.SetInteractable(pos, keyLogic);
                        break;
                    case InteractableRole.Lock:
                        LockItemLogic lockLogic = new LockItemLogic(gridMap, placement.keyColor, pos);
                        lockList.Add(lockLogic);
                        gridMap.SetWalkable(pos, false);
                        gridMap.SetInteractable(pos, lockLogic);
                        break;
                    default:
                        GridItemLogic itemLogic = new GridItemLogic(gridMap, placement.interactableId, pos);
                        genericList.Add(itemLogic);
                        gridMap.SetInteractable(pos, itemLogic);
                        break;
                }
            }

            genericInteractables = genericList.ToArray();
            keyInteractables = keyList.ToArray();
            lockInteractables = lockList.ToArray();

            Vector2Int clampedSpawnPosition = new Vector2Int(
                Mathf.Clamp(robotSpawnGridPosition.x, 0, gridWidth - 1),
                Mathf.Clamp(robotSpawnGridPosition.y, 0, gridHeight - 1));

            ApplyTrapPhaseForTick(0);
            UpdateGridPreview();

            robotLogic = new RobotLogic(gridMap, clampedSpawnPosition, robotSpawnDirection);

            if (robotView != null)
            {
                robotView.Bind(gridView, robotLogic);
            }

            if (keyLockView != null && gridView != null && robotView != null)
            {
                keyLockView.Initialize(gridView, robotView.transform, robotLogic, keyInteractables, lockInteractables);
            }

            if (goalView != null)
            {
                goalView.Initialize(gridView, goalGridPosition);
            }

            if (trapView != null)
            {
                trapView.Initialize(gridView, oddTrapCells, evenTrapCells, IsOddTrapPhase(0));
            }

            if (robotInputRouter != null)
            {
                robotInputRouter.Initialize(tickManager, robotLogic, restrictInteractToFacingInteractable);
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
            if (isTransitioning || isRestarting)
            {
                return;
            }

            robotLogic.BeginDecisionWindow();
        }

        private void HandleTickExecuted(int tickIndex)
        {
            if (isTransitioning || isRestarting)
            {
                return;
            }

            Vector2Int robotCellBefore = robotLogic.GridPosition;
            RobotIntent intentForTick = robotLogic.PendingIntent;
            ApplyTrapPhaseForTick(tickIndex);
            robotLogic.ExecutePendingIntent();
            Vector2Int robotCellAfter = robotLogic.GridPosition;
            int trapEvalTickIndex = GetTrapEvaluationTickIndex(tickIndex, intentForTick.Type);
            bool hitTrap = IsRobotOnActiveTrap(trapEvalTickIndex);
            bool atGoalCell = robotLogic.GridPosition == goalGridPosition;
            bool reachedGoal = atGoalCell && (!RequiresAllLocksForVictory || AreAllLocksFilled());
            LogTrapTickDebug(tickIndex, trapEvalTickIndex, intentForTick, robotCellBefore, robotCellAfter, hitTrap);
            UpdateGridPreview();
            StartCoroutine(ResolveTickOutcomeAfterVisuals(tickIndex, hitTrap, reachedGoal));
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

        private void SanitizeTrapCells(ref Vector2Int[] trapCells, Vector2Int[] otherTrapCells)
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

                if (ContainsCell(otherTrapCells, cell))
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

                if (ContainsCell(blockedCells, cell) || ContainsCell(oddTrapCells, cell) || ContainsCell(evenTrapCells, cell))
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

            bool oddTrapPhaseForPreview = Application.isPlaying && tickManager != null
                ? IsOddTrapPhase(tickManager.CurrentTickIndex)
                : IsOddTrapPhase(0);

            gridView.ConfigurePreview(
                gridWidth,
                gridHeight,
                clampedSpawnPosition,
                clampedGoalPosition,
                GetBlockedCellsForPreview(),
                oddTrapCells,
                evenTrapCells,
                Application.isPlaying ? displayedOddTrapPhaseActive : oddTrapPhaseForPreview,
                GetInteractablePreviewCells());
        }

        private Vector2Int[] GetBlockedCellsForPreview()
        {
            HashSet<Vector2Int> set = new HashSet<Vector2Int>();

            if (blockedCells != null)
            {
                for (int i = 0; i < blockedCells.Length; i++)
                {
                    set.Add(blockedCells[i]);
                }
            }

            if (Application.isPlaying)
            {
                if (lockInteractables != null)
                {
                    for (int i = 0; i < lockInteractables.Length; i++)
                    {
                        if (lockInteractables[i] != null)
                        {
                            set.Add(lockInteractables[i].GridPosition);
                        }
                    }
                }

                if (keyInteractables != null)
                {
                    for (int i = 0; i < keyInteractables.Length; i++)
                    {
                        if (keyInteractables[i] != null)
                        {
                            set.Add(keyInteractables[i].GridPosition);
                        }
                    }
                }
            }
            else if (interactablePlacements != null)
            {
                for (int i = 0; i < interactablePlacements.Length; i++)
                {
                    InteractablePlacement p = interactablePlacements[i];

                    if (p == null)
                    {
                        continue;
                    }

                    if (p.role == InteractableRole.Key || p.role == InteractableRole.Lock)
                    {
                        set.Add(p.gridPosition);
                    }
                }
            }

            Vector2Int[] merged = new Vector2Int[set.Count];
            int idx = 0;

            foreach (Vector2Int c in set)
            {
                merged[idx++] = c;
            }

            return merged;
        }

        private void StartGoalTransition()
        {
            if (isTransitioning || isRestarting)
            {
                return;
            }

            isTransitioning = true;
            if (robotInputRouter != null)
            {
                robotInputRouter.SetInputEnabled(false);
            }

            if (robotView != null)
            {
                robotView.EnterGoalIdleState();
            }

            if (goalView != null && robotView != null)
            {
                StartCoroutine(goalView.PlayGoalSequence(robotView.transform, robotView.MoveDuration, nextSceneName));
                return;
            }

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

        /// <summary>
        /// Which global tick index determines trap danger for the robot state *after* this execution.
        /// Move: landing occupies the new cell starting the next tick boundary → use executedTickIndex + 1
        /// (matches TickManager increment after OnTickExecuted).
        /// Interact (no move): robot stays on the same cell for this beat → use executedTickIndex.
        /// </summary>
        private static int GetTrapEvaluationTickIndex(int executedTickIndex, IntentType intentType)
        {
            return intentType == IntentType.Move ? executedTickIndex + 1 : executedTickIndex;
        }

        /// <summary>
        /// When the level defines at least one lock, victory requires every lock to have received its key
        /// in addition to standing on the goal cell.
        /// </summary>
        private bool RequiresAllLocksForVictory => lockInteractables != null && lockInteractables.Length > 0;

        private bool AreAllLocksFilled()
        {
            if (lockInteractables == null || lockInteractables.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < lockInteractables.Length; i++)
            {
                LockItemLogic lo = lockInteractables[i];

                if (lo != null && !lo.HasKeyPlaced)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsRobotOnActiveTrap(int tickIndex)
        {
            if (robotLogic == null)
            {
                return false;
            }

            Vector2Int robotCell = robotLogic.GridPosition;
            return IsOddTrapPhase(tickIndex)
                ? ContainsCell(oddTrapCells, robotCell)
                : ContainsCell(evenTrapCells, robotCell);
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
            if (Application.isPlaying)
            {
                List<Vector2Int> runtimeCells = new List<Vector2Int>(8);

                if (genericInteractables != null)
                {
                    for (int i = 0; i < genericInteractables.Length; i++)
                    {
                        GridItemLogic item = genericInteractables[i];

                        if (item == null || item.IsConsumed)
                        {
                            continue;
                        }

                        runtimeCells.Add(item.GridPosition);
                    }
                }

                if (runtimeCells.Count > 0)
                {
                    return runtimeCells.ToArray();
                }
            }

            if (interactablePlacements == null || interactablePlacements.Length == 0)
            {
                return new Vector2Int[0];
            }

            List<Vector2Int> editorCells = new List<Vector2Int>();

            for (int i = 0; i < interactablePlacements.Length; i++)
            {
                InteractablePlacement p = interactablePlacements[i];

                if (p != null && p.role == InteractableRole.GenericItem)
                {
                    editorCells.Add(p.gridPosition);
                }
            }

            return editorCells.ToArray();
        }

        private void PlayDeathAudio()
        {
            if (gameAudio == null)
            {
                return;
            }

            gameAudio.PlayDeath();
        }

        private void ApplyTrapPhaseForTick(int tickIndex)
        {
            displayedOddTrapPhaseActive = IsOddTrapPhase(tickIndex);

            if (trapView != null)
            {
                trapView.SetTrapPhase(displayedOddTrapPhaseActive);
            }
        }

        private static bool IsOddTrapPhase(int tickIndex)
        {
            return ((tickIndex + 1) % 2) == 1;
        }

        private IEnumerator ResolveTickOutcomeAfterVisuals(int tickIndex, bool hitTrap, bool reachedGoal)
        {
            float visualDelay = robotView != null ? robotView.MoveDuration : 0f;

            if (visualDelay > 0f)
            {
                yield return new WaitForSeconds(visualDelay);
            }

            if (isTransitioning || isRestarting)
            {
                yield break;
            }

            if (hitTrap)
            {
                Debug.Log("Robot hit an active trap.");
                PlayDeathAudio();
                RestartCurrentLevel();
                yield break;
            }

            if (reachedGoal)
            {
                StartGoalTransition();
                yield break;
            }

            ApplyTrapPhaseForTick(tickIndex + 1);
            UpdateGridPreview();
        }

        private void LogTrapTickDebug(
            int executedTickIndex,
            int trapEvalTickIndex,
            RobotIntent intentForTick,
            Vector2Int robotCellBefore,
            Vector2Int robotCellAfter,
            bool hitTrap)
        {
            if (!trapDebugLogs)
            {
                return;
            }

            bool oddPhaseExec = IsOddTrapPhase(executedTickIndex);
            bool oddPhaseEval = IsOddTrapPhase(trapEvalTickIndex);
            bool onOddTrapBefore = ContainsCell(oddTrapCells, robotCellBefore);
            bool onEvenTrapBefore = ContainsCell(evenTrapCells, robotCellBefore);
            bool onOddTrapAfter = ContainsCell(oddTrapCells, robotCellAfter);
            bool onEvenTrapAfter = ContainsCell(evenTrapCells, robotCellAfter);

            Debug.Log(
                $"[TrapDebug] ExecTick={executedTickIndex} ExecPhase={(oddPhaseExec ? "ODD" : "EVEN")} " +
                $"TrapEvalTick={trapEvalTickIndex} EvalPhase={(oddPhaseEval ? "ODD" : "EVEN")} " +
                $"Intent={intentForTick.Type}/{intentForTick.Direction} " +
                $"Before={robotCellBefore} After={robotCellAfter} " +
                $"BeforeTrap(O:{onOddTrapBefore},E:{onEvenTrapBefore}) " +
                $"AfterTrap(O:{onOddTrapAfter},E:{onEvenTrapAfter}) " +
                $"HitTrap={hitTrap}");
        }
    }
}
