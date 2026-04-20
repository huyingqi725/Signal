using TuringSignal.Core.Data;
using TuringSignal.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TuringSignal.Gameplay.Editor
{
    [CustomEditor(typeof(GameBootstrap))]
    public sealed class GameBootstrapEditor : UnityEditor.Editor
    {
        private const float CellButtonSize = 26f;

        private SerializedProperty tickManagerProperty;
        private SerializedProperty gridViewProperty;
        private SerializedProperty robotViewProperty;
        private SerializedProperty goalViewProperty;
        private SerializedProperty trapViewProperty;
        private SerializedProperty robotInputRouterProperty;
        private SerializedProperty gameAudioProperty;
        private SerializedProperty keyLockViewProperty;
        private SerializedProperty gridWidthProperty;
        private SerializedProperty gridHeightProperty;
        private SerializedProperty robotSpawnGridPositionProperty;
        private SerializedProperty robotSpawnDirectionProperty;
        private SerializedProperty goalGridPositionProperty;
        private SerializedProperty nextSceneNameProperty;
        private SerializedProperty nextLevelDelayProperty;
        private SerializedProperty blockedCellsProperty;
        private SerializedProperty oddTrapCellsProperty;
        private SerializedProperty evenTrapCellsProperty;
        private SerializedProperty interactablePlacementsProperty;
        private SerializedProperty restrictInteractToFacingInteractableProperty;
        private SerializedProperty restartDelayProperty;
        private SerializedProperty trapDebugLogsProperty;

        private void OnEnable()
        {
            tickManagerProperty = serializedObject.FindProperty("tickManager");
            gridViewProperty = serializedObject.FindProperty("gridView");
            robotViewProperty = serializedObject.FindProperty("robotView");
            goalViewProperty = serializedObject.FindProperty("goalView");
            trapViewProperty = serializedObject.FindProperty("trapView");
            robotInputRouterProperty = serializedObject.FindProperty("robotInputRouter");
            gameAudioProperty = serializedObject.FindProperty("gameAudio");
            keyLockViewProperty = serializedObject.FindProperty("keyLockView");
            gridWidthProperty = serializedObject.FindProperty("gridWidth");
            gridHeightProperty = serializedObject.FindProperty("gridHeight");
            robotSpawnGridPositionProperty = serializedObject.FindProperty("robotSpawnGridPosition");
            robotSpawnDirectionProperty = serializedObject.FindProperty("robotSpawnDirection");
            goalGridPositionProperty = serializedObject.FindProperty("goalGridPosition");
            nextSceneNameProperty = serializedObject.FindProperty("nextSceneName");
            nextLevelDelayProperty = serializedObject.FindProperty("nextLevelDelay");
            blockedCellsProperty = serializedObject.FindProperty("blockedCells");
            oddTrapCellsProperty = serializedObject.FindProperty("oddTrapCells");
            evenTrapCellsProperty = serializedObject.FindProperty("evenTrapCells");
            interactablePlacementsProperty = serializedObject.FindProperty("interactablePlacements");
            restrictInteractToFacingInteractableProperty = serializedObject.FindProperty("restrictInteractToFacingInteractable");
            restartDelayProperty = serializedObject.FindProperty("restartDelay");
            trapDebugLogsProperty = serializedObject.FindProperty("trapDebugLogs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSceneReferences();
            EditorGUILayout.Space(6f);
            DrawGridSetup();
            EditorGUILayout.Space(6f);
            DrawRobotSetup();
            EditorGUILayout.Space(6f);
            DrawGoalSetup();
            EditorGUILayout.Space(6f);
            DrawBlockedCellEditor();
            EditorGUILayout.Space(6f);
            DrawTrapCellEditor();
            EditorGUILayout.Space(6f);
            DrawInteractableEditor();
            EditorGUILayout.Space(6f);
            DrawTransitionSetup();
            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(restartDelayProperty);
            EditorGUILayout.Space(6f);
            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSceneReferences()
        {
            EditorGUILayout.LabelField("Scene References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tickManagerProperty);
            EditorGUILayout.PropertyField(gridViewProperty);
            EditorGUILayout.PropertyField(robotViewProperty);
            DrawKeyLockViewField();
            EditorGUILayout.PropertyField(goalViewProperty);
            EditorGUILayout.PropertyField(trapViewProperty);
            EditorGUILayout.PropertyField(robotInputRouterProperty);
            EditorGUILayout.PropertyField(gameAudioProperty);
        }

        private void DrawKeyLockViewField()
        {
            SerializedProperty property = keyLockViewProperty;

            if (property == null)
            {
                property = serializedObject.FindProperty("keyLockView");
            }

            if (property != null)
            {
                EditorGUILayout.PropertyField(
                    property,
                    new GUIContent(
                        "Key Lock View",
                        "可选。挂 KeyLockView 的物体，用于地面钥匙 / 锁 / 机器人身上钥匙的 Sprite 显示。"));
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "未找到序列化字段 keyLockView。请确认 GameBootstrap.cs 里存在 [SerializeField] KeyLockView keyLockView，并已重新编译脚本。",
                    MessageType.Warning);
            }
        }

        private void DrawGridSetup()
        {
            EditorGUILayout.LabelField("Grid Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(gridWidthProperty);
            EditorGUILayout.PropertyField(gridHeightProperty);
        }

        private void DrawRobotSetup()
        {
            EditorGUILayout.LabelField("Robot Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(robotSpawnGridPositionProperty);
            EditorGUILayout.PropertyField(robotSpawnDirectionProperty);
        }

        private void DrawGoalSetup()
        {
            EditorGUILayout.LabelField("Goal Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(goalGridPositionProperty);
        }

        private void DrawTransitionSetup()
        {
            EditorGUILayout.LabelField("Level Transition", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(nextSceneNameProperty);
            EditorGUILayout.PropertyField(nextLevelDelayProperty);
        }

        private void DrawDebugSection()
        {
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            if (trapDebugLogsProperty != null)
            {
                EditorGUILayout.PropertyField(trapDebugLogsProperty);
            }
            else
            {
                EditorGUILayout.HelpBox("trapDebugLogs field not found on GameBootstrap.", MessageType.Warning);
            }
        }

        private void DrawBlockedCellEditor()
        {
            EditorGUILayout.LabelField("Level Blockers", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("点击下面的格子即可切换是否为障碍物。绿色是出生点，黄色是终点，红色是障碍物。", MessageType.None);

            int width = Mathf.Max(1, gridWidthProperty.intValue);
            int height = Mathf.Max(1, gridHeightProperty.intValue);
            Vector2Int spawnCell = robotSpawnGridPositionProperty.vector2IntValue;
            Vector2Int goalCell = goalGridPositionProperty.vector2IntValue;

            for (int y = height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    bool isSpawn = cell == spawnCell;
                    bool isGoal = cell == goalCell;
                    bool isBlocked = ContainsBlockedCell(cell);

                    Color previousColor = GUI.backgroundColor;

                    if (isSpawn)
                    {
                        GUI.backgroundColor = new Color(0.35f, 0.8f, 0.45f);
                    }
                    else if (isGoal)
                    {
                        GUI.backgroundColor = new Color(0.95f, 0.8f, 0.25f);
                    }
                    else if (isBlocked)
                    {
                        GUI.backgroundColor = new Color(0.85f, 0.35f, 0.35f);
                    }

                    string label = isSpawn ? "S" : isGoal ? "G" : isBlocked ? "X" : string.Empty;

                    using (new EditorGUI.DisabledScope(isSpawn || isGoal))
                    {
                        if (GUILayout.Button(label, GUILayout.Width(CellButtonSize), GUILayout.Height(CellButtonSize)))
                        {
                            ToggleBlockedCell(cell);
                        }
                    }

                    GUI.backgroundColor = previousColor;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear Blocked Cells"))
            {
                blockedCellsProperty.arraySize = 0;
            }

            if (GUILayout.Button("Fill Border Walls"))
            {
                FillBorderWalls(width, height, spawnCell, goalCell);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(blockedCellsProperty, true);
        }

        private void DrawTrapCellEditor()
        {
            DrawTrapGroupEditor(
                "Odd Trap Setup",
                "点击下面的格子即可切换是否为单数拍危险的 trap。橙色 O 是 odd trap，灰色 - 代表该格已经被 even trap 占用。",
                oddTrapCellsProperty,
                evenTrapCellsProperty,
                "O",
                new Color(0.95f, 0.55f, 0.2f));

            EditorGUILayout.Space(6f);

            DrawTrapGroupEditor(
                "Even Trap Setup",
                "点击下面的格子即可切换是否为双数拍危险的 trap。紫色 E 是 even trap，灰色 - 代表该格已经被 odd trap 占用。",
                evenTrapCellsProperty,
                oddTrapCellsProperty,
                "E",
                new Color(0.75f, 0.45f, 0.95f));
        }

        private void DrawInteractableEditor()
        {
            EditorGUILayout.LabelField("Interactable Setup", EditorStyles.boldLabel);
            if (restrictInteractToFacingInteractableProperty != null)
            {
                EditorGUILayout.PropertyField(restrictInteractToFacingInteractableProperty);
            }

            EditorGUILayout.HelpBox(
                "点击格子循环：空→I→KR→KB→LR→LB→婴儿锁(红U→红R→…→蓝L)→删。婴儿锁标签 BRU=红+面朝上。" +
                "机器人身上已有钥匙时不能再交互其它物体，必须把钥匙放进同色锁/婴儿锁。" +
                "勾选上方限制开关时，仅当前方有可交互物时才能按 E。",
                MessageType.None);

            int width = Mathf.Max(1, gridWidthProperty.intValue);
            int height = Mathf.Max(1, gridHeightProperty.intValue);
            Vector2Int spawnCell = robotSpawnGridPositionProperty.vector2IntValue;
            Vector2Int goalCell = goalGridPositionProperty.vector2IntValue;

            for (int y = height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    bool isSpawn = cell == spawnCell;
                    bool isGoal = cell == goalCell;
                    bool isBlocked = ContainsBlockedCell(cell);
                    bool isTrap = ContainsTrapCell(cell);
                    bool hasInteractable = ContainsInteractableCell(cell);
                    string interactableLabel = GetInteractableCellLabel(cell);

                    Color previousColor = GUI.backgroundColor;

                    if (isSpawn)
                    {
                        GUI.backgroundColor = new Color(0.35f, 0.8f, 0.45f);
                    }
                    else if (isGoal)
                    {
                        GUI.backgroundColor = new Color(0.95f, 0.8f, 0.25f);
                    }
                    else if (isBlocked)
                    {
                        GUI.backgroundColor = new Color(0.85f, 0.35f, 0.35f);
                    }
                    else if (isTrap)
                    {
                        GUI.backgroundColor = new Color(0.75f, 0.45f, 0.95f);
                    }
                    else if (hasInteractable)
                    {
                        GUI.backgroundColor = GetInteractableCellButtonColor(cell);
                    }

                    string label = isSpawn
                        ? "S"
                        : isGoal
                            ? "G"
                            : isBlocked
                                ? "X"
                                : isTrap
                                    ? "T"
                                    : hasInteractable
                                        ? interactableLabel
                                        : string.Empty;

                    using (new EditorGUI.DisabledScope(isSpawn || isGoal || isBlocked || isTrap))
                    {
                        if (GUILayout.Button(label, GUILayout.Width(CellButtonSize), GUILayout.Height(CellButtonSize)))
                        {
                            CycleInteractableCell(cell);
                        }
                    }

                    GUI.backgroundColor = previousColor;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Clear Interactables"))
            {
                interactablePlacementsProperty.arraySize = 0;
            }

            EditorGUILayout.PropertyField(interactablePlacementsProperty, true);
        }

        private bool ContainsBlockedCell(Vector2Int cell)
        {
            for (int i = 0; i < blockedCellsProperty.arraySize; i++)
            {
                if (blockedCellsProperty.GetArrayElementAtIndex(i).vector2IntValue == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsTrapCell(Vector2Int cell)
        {
            return ContainsTrapCell(oddTrapCellsProperty, cell) || ContainsTrapCell(evenTrapCellsProperty, cell);
        }

        private bool ContainsTrapCell(SerializedProperty trapProperty, Vector2Int cell)
        {
            if (trapProperty == null)
            {
                return false;
            }

            for (int i = 0; i < trapProperty.arraySize; i++)
            {
                if (trapProperty.GetArrayElementAtIndex(i).vector2IntValue == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsInteractableCell(Vector2Int cell)
        {
            for (int i = 0; i < interactablePlacementsProperty.arraySize; i++)
            {
                SerializedProperty placement = interactablePlacementsProperty.GetArrayElementAtIndex(i);
                SerializedProperty gridPosition = placement.FindPropertyRelative("gridPosition");

                if (gridPosition.vector2IntValue == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private void ToggleBlockedCell(Vector2Int cell)
        {
            for (int i = 0; i < blockedCellsProperty.arraySize; i++)
            {
                if (blockedCellsProperty.GetArrayElementAtIndex(i).vector2IntValue == cell)
                {
                    blockedCellsProperty.DeleteArrayElementAtIndex(i);
                    return;
                }
            }

            int newIndex = blockedCellsProperty.arraySize;
            blockedCellsProperty.InsertArrayElementAtIndex(newIndex);
            blockedCellsProperty.GetArrayElementAtIndex(newIndex).vector2IntValue = cell;
        }

        private int FindInteractablePlacementIndex(Vector2Int cell)
        {
            for (int i = 0; i < interactablePlacementsProperty.arraySize; i++)
            {
                SerializedProperty placement = interactablePlacementsProperty.GetArrayElementAtIndex(i);
                SerializedProperty gridPosition = placement.FindPropertyRelative("gridPosition");

                if (gridPosition.vector2IntValue == cell)
                {
                    return i;
                }
            }

            return -1;
        }

        private static char BabyLockFaceLetter(SerializedProperty placement)
        {
            SerializedProperty faceProp = placement.FindPropertyRelative("babyLockInteractionFace");

            if (faceProp == null)
            {
                return '?';
            }

            switch ((Direction)faceProp.enumValueIndex)
            {
                case Direction.Up:
                    return 'U';
                case Direction.Right:
                    return 'R';
                case Direction.Down:
                    return 'D';
                case Direction.Left:
                    return 'L';
                default:
                    return '?';
            }
        }

        private static string GetInteractableLabelForPlacement(SerializedProperty placement)
        {
            SerializedProperty roleProp = placement.FindPropertyRelative("role");
            SerializedProperty colorProp = placement.FindPropertyRelative("keyColor");
            var role = (InteractableRole)roleProp.enumValueIndex;
            var color = (KeyColor)colorProp.enumValueIndex;

            switch (role)
            {
                case InteractableRole.Key:
                    return color == KeyColor.Red ? "KR" : "KB";
                case InteractableRole.Lock:
                    return color == KeyColor.Red ? "LR" : "LB";
                case InteractableRole.BabyLock:
                    return (color == KeyColor.Red ? "BR" : "BB") + BabyLockFaceLetter(placement);
                default:
                    return "I";
            }
        }

        private string GetInteractableCellLabel(Vector2Int cell)
        {
            int index = FindInteractablePlacementIndex(cell);

            if (index < 0)
            {
                return string.Empty;
            }

            return GetInteractableLabelForPlacement(interactablePlacementsProperty.GetArrayElementAtIndex(index));
        }

        private Color GetInteractableCellButtonColor(Vector2Int cell)
        {
            int index = FindInteractablePlacementIndex(cell);

            if (index < 0)
            {
                return new Color(0.25f, 0.75f, 0.95f);
            }

            SerializedProperty placement = interactablePlacementsProperty.GetArrayElementAtIndex(index);
            var role = (InteractableRole)placement.FindPropertyRelative("role").enumValueIndex;
            var color = (KeyColor)placement.FindPropertyRelative("keyColor").enumValueIndex;

            if (role == InteractableRole.Key)
            {
                return color == KeyColor.Red ? new Color(0.95f, 0.35f, 0.35f) : new Color(0.35f, 0.55f, 0.95f);
            }

            if (role == InteractableRole.Lock)
            {
                return color == KeyColor.Red ? new Color(0.75f, 0.2f, 0.2f) : new Color(0.2f, 0.35f, 0.75f);
            }

            if (role == InteractableRole.BabyLock)
            {
                return color == KeyColor.Red ? new Color(0.95f, 0.55f, 0.25f) : new Color(0.25f, 0.65f, 0.75f);
            }

            return new Color(0.25f, 0.75f, 0.95f);
        }

        private void CycleInteractableCell(Vector2Int cell)
        {
            int index = FindInteractablePlacementIndex(cell);

            if (index < 0)
            {
                int newIndex = interactablePlacementsProperty.arraySize;
                interactablePlacementsProperty.InsertArrayElementAtIndex(newIndex);
                SerializedProperty newPlacement = interactablePlacementsProperty.GetArrayElementAtIndex(newIndex);
                newPlacement.FindPropertyRelative("gridPosition").vector2IntValue = cell;
                newPlacement.FindPropertyRelative("role").enumValueIndex = (int)InteractableRole.GenericItem;
                newPlacement.FindPropertyRelative("keyColor").enumValueIndex = (int)KeyColor.Red;
                newPlacement.FindPropertyRelative("interactableId").stringValue = $"Item_{cell.x}_{cell.y}";
                return;
            }

            SerializedProperty placement = interactablePlacementsProperty.GetArrayElementAtIndex(index);
            var role = (InteractableRole)placement.FindPropertyRelative("role").enumValueIndex;
            var color = (KeyColor)placement.FindPropertyRelative("keyColor").enumValueIndex;

            if (role == InteractableRole.GenericItem)
            {
                placement.FindPropertyRelative("role").enumValueIndex = (int)InteractableRole.Key;
                placement.FindPropertyRelative("keyColor").enumValueIndex = (int)KeyColor.Red;
                return;
            }

            if (role == InteractableRole.Key && color == KeyColor.Red)
            {
                placement.FindPropertyRelative("keyColor").enumValueIndex = (int)KeyColor.Blue;
                return;
            }

            if (role == InteractableRole.Key && color == KeyColor.Blue)
            {
                placement.FindPropertyRelative("role").enumValueIndex = (int)InteractableRole.Lock;
                placement.FindPropertyRelative("keyColor").enumValueIndex = (int)KeyColor.Red;
                return;
            }

            if (role == InteractableRole.Lock && color == KeyColor.Red)
            {
                placement.FindPropertyRelative("keyColor").enumValueIndex = (int)KeyColor.Blue;
                return;
            }

            if (role == InteractableRole.Lock && color == KeyColor.Blue)
            {
                placement.FindPropertyRelative("role").enumValueIndex = (int)InteractableRole.BabyLock;
                placement.FindPropertyRelative("keyColor").enumValueIndex = (int)KeyColor.Red;
                placement.FindPropertyRelative("babyLockInteractionFace").enumValueIndex = (int)Direction.Up;
                return;
            }

            if (role == InteractableRole.BabyLock)
            {
                SerializedProperty faceProp = placement.FindPropertyRelative("babyLockInteractionFace");

                if (faceProp.enumValueIndex < (int)Direction.Left)
                {
                    faceProp.enumValueIndex = faceProp.enumValueIndex + 1;
                    return;
                }

                faceProp.enumValueIndex = (int)Direction.Up;

                if (color == KeyColor.Red)
                {
                    placement.FindPropertyRelative("keyColor").enumValueIndex = (int)KeyColor.Blue;
                    return;
                }

                interactablePlacementsProperty.DeleteArrayElementAtIndex(index);
                return;
            }

            interactablePlacementsProperty.DeleteArrayElementAtIndex(index);
        }

        private void DrawTrapGroupEditor(string title, string helpText, SerializedProperty targetTrapProperty, SerializedProperty otherTrapProperty, string trapLabel, Color trapColor)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(helpText, MessageType.None);

            int width = Mathf.Max(1, gridWidthProperty.intValue);
            int height = Mathf.Max(1, gridHeightProperty.intValue);
            Vector2Int spawnCell = robotSpawnGridPositionProperty.vector2IntValue;
            Vector2Int goalCell = goalGridPositionProperty.vector2IntValue;

            for (int y = height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    bool isSpawn = cell == spawnCell;
                    bool isGoal = cell == goalCell;
                    bool isBlocked = ContainsBlockedCell(cell);
                    bool isOtherTrap = ContainsTrapCell(otherTrapProperty, cell);
                    bool isCurrentTrap = ContainsTrapCell(targetTrapProperty, cell);

                    Color previousColor = GUI.backgroundColor;

                    if (isSpawn)
                    {
                        GUI.backgroundColor = new Color(0.35f, 0.8f, 0.45f);
                    }
                    else if (isGoal)
                    {
                        GUI.backgroundColor = new Color(0.95f, 0.8f, 0.25f);
                    }
                    else if (isBlocked)
                    {
                        GUI.backgroundColor = new Color(0.85f, 0.35f, 0.35f);
                    }
                    else if (isOtherTrap)
                    {
                        GUI.backgroundColor = new Color(0.45f, 0.45f, 0.45f);
                    }
                    else if (isCurrentTrap)
                    {
                        GUI.backgroundColor = trapColor;
                    }

                    string label = isSpawn ? "S" : isGoal ? "G" : isBlocked ? "X" : isOtherTrap ? "-" : isCurrentTrap ? trapLabel : string.Empty;

                    using (new EditorGUI.DisabledScope(isSpawn || isGoal || isBlocked || isOtherTrap))
                    {
                        if (GUILayout.Button(label, GUILayout.Width(CellButtonSize), GUILayout.Height(CellButtonSize)))
                        {
                            ToggleTrapCell(targetTrapProperty, cell);
                        }
                    }

                    GUI.backgroundColor = previousColor;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button($"Clear {title}"))
            {
                targetTrapProperty.arraySize = 0;
            }

            EditorGUILayout.PropertyField(targetTrapProperty, true);
        }

        private void ToggleTrapCell(SerializedProperty trapProperty, Vector2Int cell)
        {
            for (int i = 0; i < trapProperty.arraySize; i++)
            {
                if (trapProperty.GetArrayElementAtIndex(i).vector2IntValue == cell)
                {
                    trapProperty.DeleteArrayElementAtIndex(i);
                    return;
                }
            }

            int newIndex = trapProperty.arraySize;
            trapProperty.InsertArrayElementAtIndex(newIndex);
            trapProperty.GetArrayElementAtIndex(newIndex).vector2IntValue = cell;
        }

        private void FillBorderWalls(int width, int height, Vector2Int spawnCell, Vector2Int goalCell)
        {
            blockedCellsProperty.arraySize = 0;

            for (int x = 0; x < width; x++)
            {
                AddBlockedCellIfAllowed(new Vector2Int(x, 0), spawnCell, goalCell);
                AddBlockedCellIfAllowed(new Vector2Int(x, height - 1), spawnCell, goalCell);
            }

            for (int y = 1; y < height - 1; y++)
            {
                AddBlockedCellIfAllowed(new Vector2Int(0, y), spawnCell, goalCell);
                AddBlockedCellIfAllowed(new Vector2Int(width - 1, y), spawnCell, goalCell);
            }
        }

        private void AddBlockedCellIfAllowed(Vector2Int cell, Vector2Int spawnCell, Vector2Int goalCell)
        {
            if (cell == spawnCell || cell == goalCell || ContainsBlockedCell(cell))
            {
                return;
            }

            int newIndex = blockedCellsProperty.arraySize;
            blockedCellsProperty.InsertArrayElementAtIndex(newIndex);
            blockedCellsProperty.GetArrayElementAtIndex(newIndex).vector2IntValue = cell;
        }
    }
}
