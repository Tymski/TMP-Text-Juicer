using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.TextJuicer
{
    [CustomEditor(typeof(TMP_TextJuicer), true)]
    public sealed class TMP_TextJuicerInspector : Editor
    {
        TMP_TextJuicer textJuicer;

        SerializedProperty textComponent;
        SerializedProperty animationControlled;
        SerializedProperty playWhenReady;
        SerializedProperty loop;
        SerializedProperty playForever;
        SerializedProperty duration;
        SerializedProperty delay;
        SerializedProperty progress;
        SerializedProperty updateMode;

        void OnEnable()
        {
            textJuicer = (TMP_TextJuicer)target;

            textComponent = serializedObject.FindProperty("tmpText");
            duration = serializedObject.FindProperty("duration");
            delay = serializedObject.FindProperty("delay");
            progress = serializedObject.FindProperty("progress");
            playWhenReady = serializedObject.FindProperty("playWhenReady");
            loop = serializedObject.FindProperty("loop");
            playForever = serializedObject.FindProperty("playForever");
            animationControlled = serializedObject.FindProperty("animationControlled");
            updateMode = serializedObject.FindProperty("updateMode");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(textComponent);
            EditorGUILayout.PropertyField(duration);
            EditorGUILayout.PropertyField(delay);
            EditorGUILayout.PropertyField(animationControlled);
            if (animationControlled.boolValue)
            {
                EditorGUILayout.PropertyField(progress);
            }
            else
            {
                EditorGUILayout.PropertyField(playWhenReady);
                EditorGUILayout.PropertyField(loop);
                EditorGUILayout.PropertyField(playForever);
                EditorGUILayout.PropertyField(updateMode);
            }

            serializedObject.ApplyModifiedProperties();

            if (animationControlled.boolValue) return;
            if (!Application.isPlaying) return;

            if (!textJuicer.IsPlaying)
            {
                if (GUILayout.Button("Play")) textJuicer.Play();
            }
            else
            {
                if (GUILayout.Button("Stop")) textJuicer.Stop();
            }
        }
    }
}
