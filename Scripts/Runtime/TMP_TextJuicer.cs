using BrunoMikoski.TextJuicer.Modifiers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BrunoMikoski.TextJuicer
{
    [ExecuteInEditMode]
    [AddComponentMenu("UI/Text Juicer")]
    public sealed class TMP_TextJuicer : MonoBehaviour
    {
        [SerializeField]
        TMP_Text tmpText;
        TMP_Text TmpText
        {
            get
            {
                if (tmpText == null)
                {
                    tmpText = GetComponent<TMP_Text>();
                    if (tmpText == null) tmpText = GetComponentInChildren<TMP_Text>();
                }
                return tmpText;
            }
        }

        RectTransform rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null) rectTransform = (RectTransform)transform;
                return rectTransform;
            }
        }

        public string Text
        {
            get { return TmpText.text; }
            set
            {
                TmpText.text = value;
                SetDirty();
                UpdateIfDirty();
            }
        }

        [SerializeField] float duration = 0.1f;
        [SerializeField] float delay = 0.05f;
        [SerializeField][Range(0.0f, 1.0f)] float progress;
        public float Progress { get { return progress; } }
        [SerializeField] bool playWhenReady = true;
        [SerializeField] bool loop;
        [SerializeField] bool playForever;
        [SerializeField] bool animationControlled;
        [SerializeField] AnimatorUpdateMode updateMode;

        bool isPlaying;
        public bool IsPlaying
        {
            get { return isPlaying; }
        }

        CharacterData[] charactersData;
        TextJuicerVertexModifier[] vertexModifiers;
        TMP_MeshInfo[] cachedMeshInfo;
        TMP_TextInfo textInfo;
        string cachedText = string.Empty;

        float internalTime;
        float realTotalAnimationTime;
        bool isDirty = true;
        bool dispatchedAfterReadyMethod;
        bool updateGeometry;
        bool updateVertexData;
        bool forceUpdate;

        #region Unity Methods

        void OnValidate()
        {
            cachedText = string.Empty;
            SetDirty();
            if (tmpText == null) tmpText = GetComponent<TMP_Text>();
            if (tmpText == null) tmpText = GetComponentInChildren<TMP_Text>();
        }

        void Awake()
        {
            if (!animationControlled && Application.isPlaying) SetProgress(0);
        }

        void OnDisable()
        {
            forceUpdate = true;
        }

        public void Update()
        {
            if (!IsAllComponentsReady()) return;

            UpdateIfDirty();

            if (!dispatchedAfterReadyMethod)
            {
                AfterIsReady();
                dispatchedAfterReadyMethod = true;
            }

            CheckProgress();
            UpdateTime();
            if (IsPlaying || animationControlled || forceUpdate) ApplyModifiers();
        }

        #endregion

        #region Interaction Methods
        public void Restart()
        {
            internalTime = 0;
        }

        public void Play()
        {
            Play(true);
        }

        public void Play(bool fromBeginning = true)
        {
            if (!IsAllComponentsReady())
            {
                playWhenReady = true;
                return;
            }
            if (fromBeginning)
                Restart();

            isPlaying = true;
        }

        public void Complete()
        {
            if (IsPlaying)
                progress = 1.0f;
        }

        public void Stop()
        {
            isPlaying = false;
        }

        public void SetProgress(float targetProgress)
        {
            progress = targetProgress;
            internalTime = progress * realTotalAnimationTime;
            UpdateTime();
            ApplyModifiers();
            tmpText.havePropertiesChanged = true;
        }

        public void SetPlayForever(bool shouldPlayForever)
        {
            playForever = shouldPlayForever;
        }

        public CustomYieldInstruction WaitForCompletion()
        {
            return new TextJuicer_WaitForCompletion(this);
        }

        #endregion

        #region Internal
        void AfterIsReady()
        {
            if (!Application.isPlaying) return;

            if (playWhenReady)
                Play();
            else
                SetProgress(progress);
        }

        bool IsAllComponentsReady()
        {
            if (TmpText == null) return false;
            if (TmpText.textInfo == null) return false;
            if (TmpText.mesh == null) return false;
            if (TmpText.textInfo.meshInfo == null) return false;
            return true;
        }


        void ApplyModifiers()
        {
            if (charactersData == null) return;

            tmpText.ForceMeshUpdate(true);
            for (int i = 0; i < charactersData.Length; i++)
            {
                ModifyCharacter(i, cachedMeshInfo);
            }

            if (updateGeometry)
            {
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    TmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }
            }

            if (updateVertexData) TmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        void ModifyCharacter(int info, TMP_MeshInfo[] meshInfo)
        {
            for (int i = 0; i < vertexModifiers.Length; i++)
            {
                vertexModifiers[i].ModifyCharacter(charactersData[info],
                                                    TmpText,
                                                    textInfo,
                                                    progress,
                                                    meshInfo);
            }
        }

        void CheckProgress()
        {
            if (!IsPlaying) return;

            if (updateMode == AnimatorUpdateMode.Normal) internalTime += Time.deltaTime;
            if (updateMode == AnimatorUpdateMode.UnscaledTime) internalTime += Time.unscaledDeltaTime;

            if (internalTime < realTotalAnimationTime) return;
            if (playForever) return;

            if (loop)
            {
                internalTime = 0;
            }
            else
            {
                internalTime = realTotalAnimationTime;
                progress = 1.0f;
                Stop();
                OnAnimationCompleted();
            }
        }

        void OnAnimationCompleted()
        {
        }

        void UpdateTime()
        {
            if (!IsPlaying || animationControlled)
                internalTime = progress * realTotalAnimationTime;
            else
                progress = internalTime / realTotalAnimationTime;

            if (charactersData == null) return;

            for (int i = 0; i < charactersData.Length; i++)
            {
                charactersData[i].UpdateTime(internalTime);
            }
        }

        void UpdateIfDirty()
        {
            if (!isDirty) return;
            if (!gameObject.activeInHierarchy) return;
            if (!gameObject.activeSelf) return;

            TextJuicerVertexModifier[] currentComponents = GetComponents<TextJuicerVertexModifier>();
            if (vertexModifiers == null || vertexModifiers != currentComponents)
            {
                vertexModifiers = currentComponents;

                for (int i = 0; i < vertexModifiers.Length; i++)
                {
                    TextJuicerVertexModifier vertexModifier = vertexModifiers[i];

                    if (!updateGeometry && vertexModifier.ModifyGeometry)
                        updateGeometry = true;

                    if (!updateVertexData && vertexModifier.ModifyVertex)
                        updateVertexData = true;
                }
            }

            if (string.IsNullOrEmpty(cachedText) || !cachedText.Equals(TmpText.text))
            {
                TmpText.ForceMeshUpdate();
                textInfo = TmpText.textInfo;
                cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

                List<CharacterData> newCharacterDataList = new();
                int indexCount = 0;
                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible)
                        continue;

                    CharacterData characterData = new(indexCount,
                                                                     delay * indexCount,
                                                                     duration,
                                                                     playForever,
                                                                     textInfo.characterInfo[i].materialReferenceIndex,
                                                                     textInfo.characterInfo[i].vertexIndex);
                    newCharacterDataList.Add(characterData);
                    indexCount += 1;
                }

                charactersData = newCharacterDataList.ToArray();
                realTotalAnimationTime = duration +
                                         charactersData.Length * delay;

                cachedText = TmpText.text;
            }

            isDirty = false;
        }

        public void SetDirty()
        {
            isDirty = true;
        }
        #endregion
    }
}
