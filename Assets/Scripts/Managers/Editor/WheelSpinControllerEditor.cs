using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(WheelSpinController))]
public class WheelSpinControllerEditor : Editor
{
    SerializedProperty wheelType;
    SerializedProperty wheelRect;
    SerializedProperty arrowRect;
    SerializedProperty segments;
    SerializedProperty startOffsetAngle;
    SerializedProperty spinDuration;
    SerializedProperty extraSpins;
    SerializedProperty spinEase;

    private void OnEnable()
    {
        wheelType = serializedObject.FindProperty("wheelType");
        wheelRect = serializedObject.FindProperty("wheelRect");
        arrowRect = serializedObject.FindProperty("arrowRect");
        segments = serializedObject.FindProperty("segments");
        startOffsetAngle = serializedObject.FindProperty("startOffsetAngle");
        spinDuration = serializedObject.FindProperty("spinDuration");
        extraSpins = serializedObject.FindProperty("extraSpins");
        spinEase = serializedObject.FindProperty("spinEase");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Wheel Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(wheelType);
        EditorGUILayout.PropertyField(wheelRect);
        EditorGUILayout.PropertyField(arrowRect);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spin Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spinDuration);
        EditorGUILayout.PropertyField(extraSpins);
        EditorGUILayout.PropertyField(spinEase);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Angle Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startOffsetAngle);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Segments (CLOCKWISE ORDER)", EditorStyles.boldLabel);

        WheelType type = (WheelType)wheelType.enumValueIndex;

        for (int i = 0; i < segments.arraySize; i++)
        {
            SerializedProperty segment = segments.GetArrayElementAtIndex(i);
            SerializedProperty segmentType = segment.FindPropertyRelative("type");
            SerializedProperty featureName = segment.FindPropertyRelative("featureName");
            SerializedProperty valueText = segment.FindPropertyRelative("valueText");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Segment {i}", EditorStyles.miniBoldLabel);

            if (type == WheelType.MainWheel)
            {
                // Filtered options for Main Wheel
                string[] options = { "Credits", "Free Games", "Multiplier Wheel" };
                int[] values = { (int)WheelSegmentType.Credits, (int)WheelSegmentType.FreeGames, (int)WheelSegmentType.MultiplierWheel };

                int currentIndex = segmentType.enumValueIndex;
                // If it was MiniMultiplier, reset to Credits
                if (currentIndex == (int)WheelSegmentType.MiniMultiplier) currentIndex = (int)WheelSegmentType.Credits;

                int selectedIndex = EditorGUILayout.IntPopup("Type", currentIndex, options, values);
                segmentType.enumValueIndex = selectedIndex;

                if (selectedIndex == (int)WheelSegmentType.FreeGames)
                {
                    EditorGUILayout.PropertyField(featureName);
                }
                
                EditorGUILayout.PropertyField(valueText);
            }
            else
            {
                // Mini Wheel: Always MiniMultiplier
                segmentType.enumValueIndex = (int)WheelSegmentType.MiniMultiplier;
                
                SerializedProperty serverIndex = segment.FindPropertyRelative("serverIndex");
                EditorGUILayout.PropertyField(serverIndex, new GUIContent("Server Index"));
                
                EditorGUILayout.LabelField("Type", "Mini Multiplier");
                // valueText removed for Mini Wheel as requested
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                segments.DeleteArrayElementAtIndex(i);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add New Segment"))
        {
            segments.InsertArrayElementAtIndex(segments.arraySize);
        }

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
            debugTargetIndex = EditorGUILayout.IntField("Target Index", debugTargetIndex);
            if (GUILayout.Button("Spin to Index"))
            {
                var controller = (WheelSpinController)target;
                var method = typeof(WheelSpinController).GetMethod("SpinToIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(controller, new object[] { debugTargetIndex, null });
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private int debugTargetIndex = 0;
}
