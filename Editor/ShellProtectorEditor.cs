﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Text;

namespace Shell.Protector
{
    [CustomEditor(typeof(ShellProtector))]
    [CanEditMultipleObjects]
    public class ShellProtectorEditor : Editor
    {
        ReorderableList material_list;
        ReorderableList texture_list;

        SerializedProperty rounds;
        SerializedProperty filter;
        SerializedProperty algorithm;

        bool debug = false;
        bool option = false;

        readonly string[] filters = new string[2];
        readonly string[] enc_funcs = new string[1];
        // Start is called before the first frame update

        List<string> shaders = new List<string>();

        void OnEnable()
        {
            material_list = new ReorderableList(serializedObject, serializedObject.FindProperty("material_list"), true, true, true, true);
            material_list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Material List");
            material_list.drawElementCallback = (rect, index, is_active, is_focused) =>
            {
                SerializedProperty element = material_list.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            texture_list = new ReorderableList(serializedObject, serializedObject.FindProperty("texture_list"), true, true, true, true);
            texture_list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Texture List");
            texture_list.drawElementCallback = (rect, index, is_active, is_focused) =>
            {
                SerializedProperty element = texture_list.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            rounds = serializedObject.FindProperty("rounds");
            filter = serializedObject.FindProperty("filter");
            algorithm = serializedObject.FindProperty("algorithm");

            filters[0] = "Point";
            filters[1] = "Bilinear";

            enc_funcs[0] = "XXTEA algorithm";

            shaders = ShaderManager.GetInstance().CheckShader();
        }

        public override void OnInspectorGUI()
        {
            ShellProtector root = target as ShellProtector;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Directory", EditorStyles.boldLabel);
            root.asset_dir = GUILayout.TextField(root.asset_dir, GUILayout.Width(300));
            GUILayout.EndHorizontal();
            GUILayout.Label("Decteced shaders:" + string.Join(", ", shaders), EditorStyles.boldLabel);
            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Password (max:12)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Mixing alphabets and special characters makes it more secure.", EditorStyles.wordWrappedLabel);
            GUILayout.EndHorizontal();

            root.pwd = GUILayout.PasswordField(root.pwd, '*', 12, GUILayout.Width(100));

            serializedObject.Update();
            material_list.DoLayoutList();

            option = EditorGUILayout.Foldout(option, "Options");
            if(option)
            {
                GUILayout.Label("Encrytion algorithm", EditorStyles.boldLabel);
                algorithm.intValue = EditorGUILayout.Popup(algorithm.intValue, enc_funcs, GUILayout.Width(120));

                if (algorithm.intValue == 1)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Rounds", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("As the number goes up, security increases, but performance decreases.", EditorStyles.wordWrappedLabel);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    rounds.intValue = (int)GUILayout.HorizontalSlider(rounds.intValue, 30, 48, GUILayout.Width(100));
                    rounds.intValue = EditorGUILayout.IntField(rounds.intValue, GUILayout.Width(50));
                    rounds.intValue = (rounds.intValue > 48) ? 48 : (rounds.intValue < 30) ? 30 : rounds.intValue;
                    GUILayout.EndHorizontal();
                }
                GUILayout.Label("Texture filter", EditorStyles.boldLabel);
                filter.intValue = EditorGUILayout.Popup(filter.intValue, filters, GUILayout.Width(100));

                GUILayout.Space(30);
            }

            if (GUILayout.Button("Encrypt!"))
                root.Encrypt();


            debug = EditorGUILayout.Foldout(debug, "Debug");
            if(debug)
            {
                GUILayout.Space(10);
                if (GUILayout.Button("XXTEA test"))
                    root.Test2();
                GUILayout.Space(10);

                texture_list.DoLayoutList();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Encrypt"))
                {
                    Texture2D last = null;
                    for (int i = 0; i < texture_list.count; i++)
                    {
                        SerializedProperty element = texture_list.serializedProperty.GetArrayElementAtIndex(i);
                        Texture2D texture = element.objectReferenceValue as Texture2D;

                        root.SetRWEnableTexture(texture);

                        Texture2D encrypted_texture = root.GetEncryptTexture().TextureEncryptXXTEA(texture, root.MakeKeyBytes(root.pwd));

                        if (root.asset_dir[root.asset_dir.Length - 1] == '/')
                            root.asset_dir = root.asset_dir.Remove(root.asset_dir.Length - 1);

                        last = encrypted_texture;

                        if (!AssetDatabase.IsValidFolder(root.asset_dir + '/' + root.gameObject.name))
                            AssetDatabase.CreateFolder(root.asset_dir, root.gameObject.name);
                        if (!AssetDatabase.IsValidFolder(root.asset_dir + '/' + root.gameObject.name + "/mat"))
                            AssetDatabase.CreateFolder(root.asset_dir + '/' + root.gameObject.name, "mat");

                        AssetDatabase.CreateAsset(encrypted_texture, root.asset_dir + '/' + root.gameObject.name + '/' + texture.name + "_encrypt.asset");
                        AssetDatabase.SaveAssets();

                        AssetDatabase.Refresh();
                    }
                    if(last != null)
                        Selection.activeObject = last;
                }
                if (GUILayout.Button("Decrypt"))
                {
                    Texture2D last = null;
                    for (int i = 0; i < texture_list.count; i++)
                    {
                        SerializedProperty textureProperty = texture_list.serializedProperty.GetArrayElementAtIndex(i);
                        Texture2D texture = textureProperty.objectReferenceValue as Texture2D;

                        root.SetRWEnableTexture(texture);

                        Texture2D tmp = root.GetEncryptTexture().TextureDecryptXXTEA(texture, root.MakeKeyBytes(root.pwd));

                        if (root.asset_dir[root.asset_dir.Length - 1] == '/')
                            root.asset_dir = root.asset_dir.Remove(root.asset_dir.Length - 1);

                        if (!AssetDatabase.IsValidFolder(root.asset_dir + '/' + root.gameObject.name))
                            AssetDatabase.CreateFolder(root.asset_dir, root.gameObject.name);
                        if (!AssetDatabase.IsValidFolder(root.asset_dir + '/' + root.gameObject.name + "/mat"))
                            AssetDatabase.CreateFolder(root.asset_dir + '/' + root.gameObject.name, "mat");

                        System.IO.File.WriteAllBytes(root.asset_dir + '/' + root.gameObject.name + '/' + texture.name + "_decrypt.png", tmp.EncodeToPNG());
                        last = (Texture2D)AssetDatabase.LoadAssetAtPath(root.asset_dir + '/' + root.gameObject.name + '/' + texture.name + "_decrypt.png", typeof(Texture2D));

                        AssetDatabase.Refresh();
                    }
                    if (last != null)
                        Selection.activeObject = last;
                }
                GUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}