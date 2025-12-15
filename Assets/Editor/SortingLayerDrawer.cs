using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SortingLayerAttribute))]
public class SortingLayerDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        if (property.propertyType != SerializedPropertyType.Integer) {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        SortingLayer[] layers = SortingLayer.layers;
        string[] layerNames = new string[layers.Length];
        int[] layerIDs = new int[layers.Length];
        int currentIndex = 0;

        for (int i = 0; i < layers.Length; i++) {
            layerNames[i] = layers[i].name;
            layerIDs[i] = layers[i].id;

            if (layers[i].id == property.intValue) {
                currentIndex = i;
            }
        }

        int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, layerNames);
        property.intValue = layerIDs[selectedIndex];
    }
}