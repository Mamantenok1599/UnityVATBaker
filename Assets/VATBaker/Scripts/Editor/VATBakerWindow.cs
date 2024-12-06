using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VATBaker.Scripts.Editor
{
    public class VATBakerWindow : EditorWindow
    {
        [MenuItem("Tools/VAT Baker")]
        public static void ShowWindow()
        {
            GetWindow<VATBakerWindow>("VAT Baker");
        }

        private GameObject _targetObject;
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        private Animator _animator;
        private List<AnimationClip> _animationClips = new List<AnimationClip>();
        private AnimationClip _selectedClip;
        private int _startFrame;
        private int _endFrame;
        private int _frameStep = 1;
        private int _textureResolution = 512;

        private bool _bakeNormals = true;

        private Mesh _bakedMesh;
        private string _outputPath = "Assets/VATBaker/Textures";
        private string _fileName = "VAT_Texture";

        private PreviewRenderUtility _previewRenderUtility;
        private GameObject _previewInstance;
        private AnimationClip _previewAnimationClip;
        private Animator _previewAnimator;
        private float _previewAnimationTime;

        private GameObject _previousTargetObject;
        private AnimationClip _previousSelectedClip;

        private Vector2 _previewRotation = new Vector2(120f, -20f);
        private float _previewZoom = 5f;
        private double _previewStartTime;

        private Bounds _previewBounds;

        private string[] languages = new[] {"English", "Русский", "简体中文", "日本語"};
        private string[] languageCodes = new[] {"en", "ru", "zh", "ja"};
        private int selectedLanguageIndex = 0;

        private void OnEnable()
        {
            LocalizationManager.LoadLocalization(languageCodes[selectedLanguageIndex]);
        }

        private void OnGUI()
        {
            int newLanguageIndex = EditorGUILayout.Popup("Language", selectedLanguageIndex, languages);
            if (newLanguageIndex != selectedLanguageIndex)
            {
                selectedLanguageIndex = newLanguageIndex;
                LocalizationManager.LoadLocalization(languageCodes[selectedLanguageIndex]);
            }

            EditorGUILayout.LabelField(LocalizationManager.Localize("LabelSelectTargetObject"), EditorStyles.boldLabel);
            GUIContent targetObjectContent = new GUIContent(
                LocalizationManager.Localize("LabelTargetObject"),
                LocalizationManager.Localize("TooltipSelectTargetObject")
            );
            _targetObject =
                (GameObject) EditorGUILayout.ObjectField(targetObjectContent, _targetObject, typeof(GameObject), true);

            bool hasErrors = false;

            if (_targetObject != null)
            {
                if (!CheckSkinnedMeshRenderer())
                {
                    hasErrors = true;
                }
                else if (!CheckAnimation())
                {
                    hasErrors = true;
                }
            }
            else
            {
                EditorGUILayout.HelpBox(LocalizationManager.Localize("HelpBoxSelectTargetObjectForBake"),
                    MessageType.Info);
                hasErrors = true;
            }

            if (_targetObject != _previousTargetObject || _selectedClip != _previousSelectedClip)
            {
                if (_targetObject != null)
                {
                    InitPreview();
                }
                else
                {
                    ClearPreview();
                }

                _previousTargetObject = _targetObject;
                _previousSelectedClip = _selectedClip;
            }

            if (!hasErrors)
            {
                DrawUI();
                EditorGUILayout.Space();
                Rect previewRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(true));
                DrawPreview(previewRect);
            }
        }

        void Update()
        {
            if (_previewRenderUtility != null)
            {
                Repaint();
            }
        }

        private void DrawUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(LocalizationManager.Localize("LabelAnimationSettings"), EditorStyles.boldLabel);

            string[] clipNames = _animationClips.Select(clip => clip.name).ToArray();
            int selectedClipIndex = _animationClips.IndexOf(_selectedClip);
            if (selectedClipIndex < 0) selectedClipIndex = 0;

            selectedClipIndex =
                EditorGUILayout.Popup(
                    new GUIContent(LocalizationManager.Localize("LabelAnimationClip"),
                        LocalizationManager.Localize("TooltipAnimationClip")),
                    selectedClipIndex, clipNames);
            _selectedClip = _animationClips[selectedClipIndex];

            EditorGUILayout.LabelField(LocalizationManager.Localize("LabelAnimationField") + _selectedClip.length +
                                       LocalizationManager.Localize("LabelSecond"));
            EditorGUILayout.LabelField(LocalizationManager.Localize("LabelFramerate") + _selectedClip.frameRate +
                                       " fps");

            float clipFrameRate = _selectedClip.frameRate;
            int totalFrames = Mathf.CeilToInt(_selectedClip.length * clipFrameRate);

            _startFrame = EditorGUILayout.IntSlider(
                new GUIContent(LocalizationManager.Localize("LabelStartFrame"),
                    LocalizationManager.Localize("TooltipStartFrame")), _startFrame, 0,
                totalFrames - 1);
            _endFrame = EditorGUILayout.IntSlider(
                new GUIContent(LocalizationManager.Localize("LabelEndFrame"),
                    LocalizationManager.Localize("TooltipEndFrame")), _endFrame, _startFrame + 1,
                totalFrames);
            _frameStep =
                EditorGUILayout.IntSlider(new GUIContent(LocalizationManager.Localize("LabelFrameStep"),
                        LocalizationManager.Localize("TooltipFrameStep")),
                    _frameStep, 1, 10);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(LocalizationManager.Localize("LabelTextureSettings"), EditorStyles.boldLabel);

            GUIContent[] resolutionOptions = new GUIContent[]
            {
                new GUIContent("256"),
                new GUIContent("512"),
                new GUIContent("1024"),
                new GUIContent("2048"),
                new GUIContent("4096")
            };

            _textureResolution = EditorGUILayout.IntPopup(
                new GUIContent(LocalizationManager.Localize("LabelTextureResolution"),
                    LocalizationManager.Localize("TooltipTextureResolution")),
                _textureResolution,
                resolutionOptions,
                new[] {256, 512, 1024, 2048, 4096});

            _bakeNormals =
                EditorGUILayout.Toggle(new GUIContent(LocalizationManager.Localize("LabelBakeNormals"),
                        LocalizationManager.Localize("TooltipBakeNormals")),
                    _bakeNormals);
            _outputPath =
                EditorGUILayout.TextField(new GUIContent(LocalizationManager.Localize("LabelOutputPath"),
                        LocalizationManager.Localize("TooltipOutputPath")),
                    _outputPath);
            _fileName = EditorGUILayout.TextField(
                new GUIContent(LocalizationManager.Localize("LabelFileName"),
                    LocalizationManager.Localize("TooltipFileName")), _fileName);

            EditorGUILayout.Space();
            if (GUILayout.Button(LocalizationManager.Localize("LabelBakeVAT")))
            {
                BakeAnimation();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(LocalizationManager.Localize("LabelPreview"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(LocalizationManager.Localize("TooltipPreview"));
        }

        private bool CheckSkinnedMeshRenderer()
        {
            _skinnedMeshRenderer = _targetObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (_skinnedMeshRenderer == null)
            {
                EditorGUILayout.HelpBox(
                    LocalizationManager.Localize("HelpBoxSkinnedMeshRenderer"),
                    MessageType.Error);
                return false;
            }

            if (_skinnedMeshRenderer.sharedMesh != null)
            {
                EditorGUILayout.LabelField(LocalizationManager.Localize("LabelVertexCount") +
                                           _skinnedMeshRenderer.sharedMesh.vertexCount);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    LocalizationManager.Localize("HelpBoxMesh"),
                    MessageType.Error);
                return false;
            }

            return true;
        }

        private bool CheckAnimation()
        {
            _animator = _targetObject.GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                EditorGUILayout.HelpBox(
                    LocalizationManager.Localize("HelpBoxAnimator"),
                    MessageType.Error);
                return false;
            }

            RuntimeAnimatorController controller = _animator.runtimeAnimatorController;

            if (controller != null)
            {
                _animationClips = controller.animationClips.ToList();
                if (_animationClips.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        LocalizationManager.Localize("HelpBoxAnimations"),
                        MessageType.Error);
                    return false;
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    LocalizationManager.Localize("HelpBoxAnimatorController"),
                    MessageType.Error);
                return false;
            }

            return true;
        }

        private void BakeAnimation()
        {
            if (_skinnedMeshRenderer == null || _selectedClip == null)
            {
                Debug.LogError("SkinnedMeshRenderer or AnimationClip is missing.");
                return;
            }

            float clipFrameRate = _selectedClip.frameRate;

            int startFrame = _startFrame;
            int endFrame = _endFrame;
            int frameStep = _frameStep;

            int frameCount = ((endFrame - startFrame) / frameStep) + 1;

            Mesh mesh = new Mesh();
            _skinnedMeshRenderer.BakeMesh(mesh);
            int vertexCount = mesh.vertexCount;

            int textureWidth = _textureResolution;
            int pixelsPerFrame = vertexCount;
            int rowsPerFrame = Mathf.CeilToInt((float) pixelsPerFrame / textureWidth);
            int textureHeight = Mathf.NextPowerOfTwo(frameCount * rowsPerFrame);

            Texture2D positionTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false);
            Texture2D normalTexture = null;
            if (_bakeNormals)
            {
                normalTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false);
            }

            Color[] positionColors = new Color[textureWidth * textureHeight];
            Color[] normalColors = null;
            if (_bakeNormals)
            {
                normalColors = new Color[textureWidth * textureHeight];
            }

            EditorUtility.DisplayProgressBar("Baking VAT", "Initializing...", 0f);

            GameObject bakingInstance = Instantiate(_targetObject);
            bakingInstance.hideFlags = HideFlags.HideAndDontSave;
            
            Animator animator = bakingInstance.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = null;

            SkinnedMeshRenderer smr = bakingInstance.GetComponentInChildren<SkinnedMeshRenderer>();

            if (_bakedMesh == null)
            {
                _bakedMesh = new Mesh();
            }

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                float progress = (float) frameIndex / frameCount;
                EditorUtility.DisplayProgressBar("Baking VAT", $"Processing frame {frameIndex + 1}/{frameCount}",
                    progress);

                int currentFrame = startFrame + frameIndex * frameStep;
                float time = currentFrame / clipFrameRate;
                _selectedClip.SampleAnimation(bakingInstance, time);
                smr.BakeMesh(_bakedMesh);

                Vector3[] vertices = _bakedMesh.vertices;
                Vector3[] normals = _bakedMesh.normals;

                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    int pixelIndexInFrame = vertexIndex;
                    int x = pixelIndexInFrame % textureWidth;
                    int y = frameIndex * rowsPerFrame + (pixelIndexInFrame / textureWidth);

                    int pixelIndex = y * textureWidth + x;

                    Vector3 position = vertices[vertexIndex];
                    positionColors[pixelIndex] = new Color(position.x, position.y, position.z, 1.0f);

                    if (_bakeNormals)
                    {
                        Vector3 normal = normals[vertexIndex];
                        normalColors[pixelIndex] = new Color(normal.x, normal.y, normal.z, 1.0f);
                    }
                }
            }

            positionTexture.SetPixels(positionColors);
            positionTexture.Apply();

            if (_bakeNormals)
            {
                normalTexture.SetPixels(normalColors);
                normalTexture.Apply();
            }

            string safeObjectName = SanitizeFileName(_targetObject != null ? _targetObject.name : "NoObject");
            string safeClipName = SanitizeFileName(_selectedClip != null ? _selectedClip.name : "NoClip");
            string safeFileName = SanitizeFileName(_fileName);
            string fullPositionPath = $"{_outputPath}/{safeFileName}_{safeObjectName}_{safeClipName}_Positions.exr";
            string fullNormalsPath = $"{_outputPath}/{safeFileName}_{safeObjectName}_{safeClipName}_Normals.exr";
            
            SaveTexture(positionTexture, fullPositionPath);
            if (_bakeNormals)
            {
                SaveTexture(normalTexture, fullNormalsPath);
            }

            EditorUtility.ClearProgressBar();
            DestroyImmediate(bakingInstance);
            AssetDatabase.Refresh();
            Debug.Log("VAT Baking completed successfully!");
        }
        
        private void SaveTexture(Texture2D texture, string path)
        {
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            byte[] bytes = texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            System.IO.File.WriteAllBytes(path, bytes);
        }

        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        private void InitPreview()
        {
            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.Cleanup();
                _previewRenderUtility = null;
            }


            _previewRenderUtility = new PreviewRenderUtility();
            _previewRenderUtility.cameraFieldOfView = 30f;

            var cameraData = _previewRenderUtility.camera.gameObject.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = _previewRenderUtility.camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            _previewRenderUtility.camera.allowHDR = true;
            _previewRenderUtility.camera.allowMSAA = true;

            if (_previewInstance != null)
            {
                GameObject.DestroyImmediate(_previewInstance);
            }

            _previewInstance = GameObject.Instantiate(_targetObject);
            _previewInstance.transform.position = Vector3.zero;
            _previewInstance.transform.rotation = Quaternion.identity;

            _previewRenderUtility.AddSingleGO(_previewInstance);

            _previewAnimator = _previewInstance.GetComponentInChildren<Animator>();
            if (_previewAnimator != null && _selectedClip != null)
            {
                _previewAnimator.runtimeAnimatorController = null;
                _previewAnimationClip = _selectedClip;
            }

            _previewBounds = GetObjectBounds(_previewInstance);

            Renderer[] renderers = _previewInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        materials[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    }
                    else if (!materials[i].shader.isSupported)
                    {
                        materials[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    }
                }

                renderer.sharedMaterials = materials;
            }

            _previewRotation = new Vector2(120f, -20f);
            _previewZoom = _previewBounds.size.magnitude;

            _previewAnimationTime = 0f;
            _previewStartTime = EditorApplication.timeSinceStartup;
        }

        private void DrawPreview(Rect previewRect)
        {
            if (_previewRenderUtility == null || _previewInstance == null)
            {
                return;
            }

            Event e = Event.current;

            if (e.type == EventType.MouseDrag && previewRect.Contains(e.mousePosition))
            {
                if (e.button == 0)
                {
                    _previewRotation.x -= e.delta.x;
                    _previewRotation.y -= e.delta.y;
                    Repaint();
                }
                else if (e.button == 1 || e.button == 2)
                {
                    _previewZoom -= e.delta.y * 0.05f;
                    _previewZoom = Mathf.Clamp(_previewZoom, 1f, 20f);
                    Repaint();
                }
            }

            if (_previewAnimator != null && _previewAnimationClip != null)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                _previewAnimationTime = (float) ((currentTime - _previewStartTime) % _previewAnimationClip.length);
                _previewAnimationClip.SampleAnimation(_previewInstance, _previewAnimationTime);
            }

            _previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);

            Quaternion camRotation = Quaternion.Euler(-_previewRotation.y, -_previewRotation.x, 0);
            Vector3 camPosition = _previewBounds.center + camRotation * new Vector3(0, 0, -_previewZoom);

            _previewRenderUtility.camera.transform.position = camPosition;
            _previewRenderUtility.camera.transform.LookAt(_previewBounds.center);
            _previewRenderUtility.camera.farClipPlane = 50f;

            _previewRenderUtility.lights[0].intensity = 1f;
            _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
            _previewRenderUtility.Render(true);

            Texture previewTexture = _previewRenderUtility.EndPreview();

            GUI.DrawTexture(previewRect, previewTexture, ScaleMode.StretchToFill, false);
        }

        private void ClearPreview()
        {
            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.Cleanup();
                _previewRenderUtility = null;
            }

            if (_previewInstance != null)
            {
                GameObject.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            _previewAnimator = null;
            _previewAnimationClip = null;
        }

        private Bounds GetObjectBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private void OnDisable()
        {
            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.Cleanup();
                _previewRenderUtility = null;
            }

            if (_previewInstance != null)
            {
                GameObject.DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }
        }
    }
}