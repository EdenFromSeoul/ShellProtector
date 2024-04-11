#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Random = System.Random;

namespace Shell.Protector
{
    public class ShellProtectorHelper : EditorWindow
    {
        private SerializedObject _serializedObject;
        private ShellProtector _root;
        private ShellProtectorTester _tester;

        [SerializeField]
        private Animator _animator;
        private readonly LanguageManager _languageManager = LanguageManager.GetInstance();
        private ReorderableList _materialList;
        private ReorderableList _textureList;

        private SerializedProperty _rounds;
        private SerializedProperty _filter;
        private SerializedProperty _keySize;
        private SerializedProperty _keySizeIndex;
        private SerializedProperty _animationSpeed;
        private SerializedProperty _deleteFolders;
        private SerializedProperty _parameterMultiplexing;

        bool option;

        [SerializeField] private bool showPassword;


        private List<string> _shaders = new();

        private readonly string[] _filters = { "Point", "Bilinear" };
        private readonly string[] _languages = { "English", "한국어" };
        private readonly string[] _keySizes = new string[5];
        private bool _needRefresh;
        private Vector2 _scrollPos;

        [MenuItem("Window/ShellProtector Helper")]
        public static void ShowWindow()
        {
            GetWindow<ShellProtectorHelper>("ShellProtector Helper");
        }

        private string Lang(string key) => _languageManager == null
            ? ""
            : _languageManager.GetLang(_tester ? _tester.lang : _root ? _root.lang : "eng", key);

        private static string GenerateRandomString(int length)
        {
            const string chars =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+=|\\/?.>,<~`\'\" ";
            var builder = new StringBuilder();

            var random = new Random();
            for (var i = 0; i < length; i++)
            {
                var index = random.Next(chars.Length);
                builder.Append(chars[index]);
            }

            return builder.ToString();
        }

        private void OnGUI()
        {
            // animator selector
            // _animator = EditorGUILayout.ObjectField("Avatar", _animator, typeof(Animator), true) as Animator;
            var newAnimator = EditorGUILayout.ObjectField("Avatar", _animator, typeof(Animator), true) as Animator;

            if (newAnimator != _animator)
            {
                _animator = newAnimator;
                _needRefresh = true;
                _tester = null;
                _root = null;
            }

            if (_animator == null)
            {
                return;
            }

            if (_tester == null)
            {
                _tester = FindObjectOfType<ShellProtectorTester>();
            }

            if (_tester != null)
            {
                GUILayout.Space(10);

                if (_tester.user_key_length == 0)
                {
                    GUILayout.Label(Lang("It's okay for the 0-digit password to be the same as the original."));
                }
                else
                {
                    GUILayout.Label(Lang("If it looks like its original appearance when pressed, it's a success."));
                    if (GUILayout.Button(Lang("Check encryption success")))
                        _tester.CheckEncryption();
                }

                GUILayout.Label(Lang("Press it before uploading."));
                if (GUILayout.Button(Lang("Done & Reset")))
                {
                    _tester.ResetEncryption();
                    DestroyImmediate(_tester.GetComponent<ShellProtectorTester>());
                }

                return;
            }

            // animator 안에서 ShellProtectorTester를 찾아보고 없으면 생성
            if (_root == null)
            {
                _root = _animator.gameObject.GetComponent<ShellProtector>();
                if (_root == null)
                {
                    _root = _animator.gameObject.AddComponent<ShellProtector>();
                    RefreshSerializedObject();
                }
            }

            if (_serializedObject == null || _needRefresh)
            {
                RefreshSerializedObject();
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Lang("Languages: "));
            GUILayout.FlexibleSpace();

            _root.lang_idx = EditorGUILayout.Popup(_root.lang_idx, _languages, GUILayout.Width(100));
            _root.lang = _root.lang_idx switch
            {
                0 => "eng",
                1 => "kor",
                _ => "eng"
            };

            GUILayout.EndHorizontal();

            var materials = _animator.gameObject.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials)
                .ToArray();

            materials = materials.Distinct().ToArray();


            GUILayout.Label(Lang("Decteced shaders:") + string.Join(", ", _shaders), EditorStyles.boldLabel);
            GUILayout.Space(20);
            _serializedObject.Update();
            _materialList.DoLayoutList();

            if (_materialList.count == 0)
            {
                {
                    _materialList.serializedProperty.arraySize = materials.Length;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        _materialList.serializedProperty.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
                    }
                }
            }

            // 오류가 발생하면 강제로 리프레시하는 버튼
            if (GUILayout.Button(Lang("Refresh materials")))
            {
                RefreshSerializedObject();
                Repaint();
                return;
            }

            GUILayout.Label(Lang("Password"), EditorStyles.boldLabel);
            _keySizes[0] = Lang("0 (Minimal security)");
            _keySizes[1] = Lang("4 (Low security)");
            _keySizes[2] = Lang("8 (Middle security)");
            _keySizes[3] = Lang("12 (Hight security)");
            _keySizes[4] = Lang("16 (Unbreakable security)");


            if (_keySize.intValue < 16)
            {
                var length = 16 - _keySize.intValue;
                GUILayout.BeginHorizontal();
                _root.pwd = GUILayout.TextField(_root.pwd, length, GUILayout.Width(100));

                if (_root.pwd.Length != length)
                    _root.pwd = GenerateRandomString(length);

                if (GUILayout.Button(Lang("Generate")))
                    _root.pwd = GenerateRandomString(length);
                GUILayout.FlexibleSpace();
                GUILayout.Label(Lang("A password that you don't need to memorize. (max:") + length + ")",
                    EditorStyles.wordWrappedLabel);
                GUILayout.EndHorizontal();
            }

            if (_keySize.intValue > 0)
            {
                GUILayout.BeginHorizontal();
                _root.pwd2 = showPassword
                    ? GUILayout.TextField(_root.pwd2, _keySize.intValue, GUILayout.Width(100))
                    : GUILayout.PasswordField(_root.pwd2, '*', _keySize.intValue, GUILayout.Width(100));
                showPassword = GUILayout.Toggle(showPassword, Lang("Show"));
                GUILayout.FlexibleSpace();
                GUILayout.Label(Lang("This password should be memorized. (max:") + _keySize.intValue + ")",
                    EditorStyles.wordWrappedLabel);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(Lang("Max password length"), EditorStyles.boldLabel);
            _keySizeIndex.intValue = EditorGUILayout.Popup(_keySizeIndex.intValue, _keySizes, GUILayout.Width(150));

            _keySize.intValue = _keySizeIndex.intValue switch
            {
                0 => 0,
                1 => 4,
                2 => 8,
                3 => 12,
                4 => 16,
                _ => _keySize.intValue
            };

            GUILayout.Space(20);

            option = EditorGUILayout.Foldout(option, Lang("Options"));
            if (option)
            {
                GUILayout.Label(Lang("Texture filter"), EditorStyles.boldLabel);
                _filter.intValue = EditorGUILayout.Popup(_filter.intValue, _filters, GUILayout.Width(100));


                GUILayout.Label(Lang("Initial animation speed"), EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                _animationSpeed.floatValue =
                    GUILayout.HorizontalSlider(_animationSpeed.floatValue, 1.0f, 128.0f, GUILayout.Width(100));
                _animationSpeed.floatValue =
                    EditorGUILayout.FloatField("", _animationSpeed.floatValue, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.Label(Lang("Avatar first load animation speed"), EditorStyles.wordWrappedLabel);
                GUILayout.EndHorizontal();

                GUILayout.Label(Lang("Delete folders that already exists when at creation time"),
                    EditorStyles.boldLabel);
                _deleteFolders.boolValue = EditorGUILayout.Toggle(_deleteFolders.boolValue);

                GUILayout.Label(Lang("parameter-multiplexing"), EditorStyles.boldLabel);
                GUILayout.Label(Lang("The OSC program must always be on, but it consumes fewer parameters."),
                    EditorStyles.wordWrappedLabel);
                _parameterMultiplexing.boolValue = EditorGUILayout.Toggle(_parameterMultiplexing.boolValue);

                GUILayout.Space(10);
            }

            if (_materialList.count == 0)
                GUI.enabled = false;

            if (GUILayout.Button(Lang("Encrypt!")))
                _root.Encrypt();

            GUI.enabled = true;

            _serializedObject.ApplyModifiedProperties();

            GUILayout.EndScrollView();
        }

        private void OnEnable()
        {
            _animator = FindObjectOfType<Animator>();
            _root = FindObjectOfType<ShellProtector>();
            if (_root == null)
            {
                return;
            }

            RefreshSerializedObject();

            _shaders = ShaderManager.GetInstance().CheckShader();
        }

        private void RefreshSerializedObject()
        {
            _serializedObject = new SerializedObject(_root);
            _materialList = new ReorderableList(_serializedObject, _serializedObject.FindProperty("material_list"),
                true,
                true, true, true);
            _materialList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Materials"); };
            _materialList.drawElementCallback = (rect, index, _, _) =>
            {
                var element = _materialList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    element, GUIContent.none);
            };
            _textureList = new ReorderableList(_serializedObject, _serializedObject.FindProperty("texture_list"), true,
                true, true, true);
            _textureList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Textures"); };
            _textureList.drawElementCallback = (rect, index, _, _) =>
            {
                var element = _textureList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    element, GUIContent.none);
            };
            _rounds = _serializedObject.FindProperty("rounds");
            _filter = _serializedObject.FindProperty("filter");
            _keySize = _serializedObject.FindProperty("key_size");
            _keySizeIndex = _serializedObject.FindProperty("key_size_idx");
            _animationSpeed = _serializedObject.FindProperty("animation_speed");
            _deleteFolders = _serializedObject.FindProperty("delete_folders");
            _parameterMultiplexing = _serializedObject.FindProperty("parameter_multiplexing");
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}

#endif