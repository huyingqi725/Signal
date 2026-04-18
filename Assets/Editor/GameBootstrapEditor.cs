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
        private SerializedProperty robotInputRouterProperty;
        private SerializedProperty gridWidthProperty;
        private SerializedProperty gridHeightProperty;
        private SerializedProperty robotSpawnGridPositionProperty;
        private SerializedProperty robotSpawnDirectionProperty;
        private SerializedProperty goalGridPositionProperty;
        private SerializedProperty nextSceneNameProperty;
        private SerializedProperty nextLevelDelayProperty;
        private SerializedProperty blockedCellsProperty;
        private SerializedProperty trapCellsProperty;
        private SerializedProperty trapsStartActiveProperty;
        private SerializedProperty trapToggleIntervalTicksProperty;
        private SerializedProperty restartDelayProperty;

        private void OnEnable()
        {
            tickManagerProperty = serializedObject.FindProperty("tickManager");
            gridViewProperty = serializedObject.FindProperty("gridView");
            robotViewProperty = serializedObject.FindProperty("robotView");
            robotInputRouterProperty = serializedObject.FindProperty("robotInputRouter");
            gridWidthProperty = serializedObject.FindProperty("gridWidth");
            gridHeightProperty = serializedObject.FindProperty("gridHeight");
            robotSpawnGridPositionProperty = serializedObject.FindProperty("robotSpawnGridPosition");
            robotSpawnDirectionProperty = serializedObject.FindProperty("robotSpawnDirection");
            goalGridPositionProperty = serializedObject.FindProperty("goalGridPosition");
            nextSceneNameProperty = serializedObject.FindProperty("nextSceneName");
            nextLevelDelayProperty = serializedObject.FindProperty("nextLevelDelay");
            blockedCellsProperty = serializedObject.FindProperty("blockedCells");
            trapCellsProperty = serializedObject.FindProperty("trapCells");
            trapsStartActiveProperty = serializedObject.FindProperty("trapsStartActive");
            trapToggleIntervalTicksProperty = serializedObject.FindProperty("trapToggleIntervalTicks");
            restartDelayProperty = serializedObject.FindProperty("restartDelay");
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
            DrawTransitionSetup();
            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(restartDelayProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSceneReferences()
        {
            EditorGUILayout.LabelField("Scene References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tickManagerProperty);
            EditorGUILayout.PropertyField(gridViewProperty);
            EditorGUILayout.PropertyField(robotViewProperty);
            EditorGUILayout.PropertyField(robotInputRouterProperty);
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
            EditorGUILayout.LabelField("Trap Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(trapsStartActiveProperty);
            EditorGUILayout.PropertyField(trapToggleIntervalTicksProperty);
            EditorGUILayout.HelpBox("点击下面的格子即可切换是否为尖刺。紫色是尖刺，出生点/终点/障碍物不能设置为尖刺。尖刺每经过指定数量的玩家 Tick 才会切换一次显隐。", MessageType.None);

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

                    string label = isSpawn ? "S" : isGoal ? "G" : isBlocked ? "X" : isTrap ? "T" : string.Empty;

                    using (new EditorGUI.DisabledScope(isSpawn || isGoal || isBlocked))
                    {
                        if (GUILayout.Button(label, GUILayout.Width(CellButtonSize), GUILayout.Height(CellButtonSize)))
                        {
                            ToggleTrapCell(cell);
                        }
                    }

                    GUI.backgroundColor = previousColor;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Clear Trap Cells"))
            {
                trapCellsProperty.arraySize = 0;
            }

            EditorGUILayout.PropertyField(trapCellsProperty, true);
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
            for (int i = 0; i < trapCellsProperty.arraySize; i++)
            {
                if (trapCellsProperty.GetArrayElementAtIndex(i).vector2IntValue == cell)
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

        private void ToggleTrapCell(Vector2Int cell)
        {
            for (int i = 0; i < trapCellsProperty.arraySize; i++)
            {
                if (trapCellsProperty.GetArrayElementAtIndex(i).vector2IntValue == cell)
                {
                    trapCellsProperty.DeleteArrayElementAtIndex(i);
                    return;
                }
            }

            int newIndex = trapCellsProperty.arraySize;
            trapCellsProperty.InsertArrayElementAtIndex(newIndex);
            trapCellsProperty.GetArrayElementAtIndex(newIndex).vector2IntValue = cell;
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
