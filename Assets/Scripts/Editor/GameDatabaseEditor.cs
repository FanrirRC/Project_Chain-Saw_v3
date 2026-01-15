using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Data;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class GameDatabaseEditor : EditorWindow
{
    private PropertyTree _propertyTree;

    [MenuItem("Tools/Game Database Editor")]
    private static void OpenWindow()
    {
        var win = GetWindow<GameDatabaseEditor>();
        win.titleContent = new GUIContent("Game Database Editor");
        win.minSize = new Vector2(820, 480);
        win.Show();
    }

    private enum Category
    {
        PlayerData,
        Enemies,
        Skills,
        StatusEffects,
        Items,
    }

    private class CategoryConfig
    {
        public string DisplayName;
        public string FolderPath;
        public Type AssetType;
        public string DefaultFileName;
    }

    private static readonly Dictionary<Category, CategoryConfig> Configs = new()
    {
        {
            Category.PlayerData,
            new CategoryConfig {
                DisplayName = "Player Data",
                FolderPath = "Assets/GameData/UnitStats/1.Players",
                AssetType = typeof(CharacterBaseStats),
                DefaultFileName = "New Player Character"
            }
        },
        {
            Category.Enemies,
            new CategoryConfig {
                DisplayName = "Enemies",
                FolderPath = "Assets/GameData/UnitStats/2.Enemies",
                AssetType = typeof(EnemyBaseStats),
                DefaultFileName = "New Enemy"
            }
        },
        {
            Category.Skills,
            new CategoryConfig {
                DisplayName = "Skills",
                FolderPath = "Assets/GameData/Skills",
                AssetType = typeof(SkillDefinition),
                DefaultFileName = "New Skill"
            }
        },
        {
            Category.StatusEffects,
            new CategoryConfig {
                DisplayName = "Status Effects",
                FolderPath = "Assets/GameData/StatusEffects",
                AssetType = typeof(StatusEffectDefinition),
                DefaultFileName = "New Status Effect"
            }
        },
        {
            Category.Items,
            new CategoryConfig {
                DisplayName = "Items",
                FolderPath = "Assets/GameData/Items",
                AssetType = typeof(ItemDefinition),
                DefaultFileName = "New Item"
            }
        },
    };

    private Vector2 _catScroll;
    private Vector2 _listScroll;
    private Vector2 _inspectorScroll;
    private float _leftWidth = 190f;
    private float _middleWidth = 260f;
    private const float _splitterThickness = 2f;

    private Category _selectedCategory = Category.PlayerData;
    private UnityEditor.IMGUI.Controls.SearchField _searchField;
    private readonly Dictionary<Category, string> _searchByCategory = new();
    private UnityEngine.Object _selectedAsset;
    private Editor _selectedEditor;

    private readonly Dictionary<Category, List<UnityEngine.Object>> _assetsByCategory = new();

    private string _assetNameBuffer = string.Empty;

    private void OnEnable()
    {
        _searchField = new UnityEditor.IMGUI.Controls.SearchField
        {
            autoSetFocusOnFindCommand = true
        };

        foreach (Category c in Enum.GetValues(typeof(Category)))
        {
            if (!_searchByCategory.ContainsKey(c)) _searchByCategory[c] = string.Empty;
        }
    }

    private void OnGUI()
    {
        EnsureCache();

        EditorGUILayout.BeginHorizontal();

        DrawLeftCategoryPane();
        DrawVerticalSeparator();
        DrawMiddleListPane();
        DrawVerticalSeparator();
        DrawRightInspectorPane();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftCategoryPane()
    {
        using (new GUILayout.VerticalScope(GUILayout.Width(_leftWidth)))
        {
            SirenixEditorGUI.BeginHorizontalToolbar();
            if (EditorGUILayout.DropdownButton(new GUIContent("Create New"), FocusType.Passive))
            {
                ShowCreateMenu();
            }
            SirenixEditorGUI.EndHorizontalToolbar();

            _catScroll = EditorGUILayout.BeginScrollView(_catScroll);

            foreach (var kvp in Configs)
            {
                DrawCategoryButton(kvp.Key, kvp.Value.DisplayName);
            }

            EditorGUILayout.EndScrollView();

            _leftWidth = Mathf.Clamp(EditorGUILayout.Slider(_leftWidth, 120f, 360f), 120f, 360f);
        }
    }

    private void DrawCategoryButton(Category category, string label)
    {
        bool isSelected = _selectedCategory == category;
        var style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal
        };

        if (GUILayout.Button(label, style, GUILayout.Height(28)))
        {
            _selectedCategory = category;

            var list = GetAssets(category);
            var filtered = ApplyFilter(list, _searchByCategory[_selectedCategory]).ToList();
            SelectAsset(filtered.FirstOrDefault());
        }
    }

    private void DrawMiddleListPane()
    {
        using (new GUILayout.VerticalScope(GUILayout.Width(_middleWidth)))
        {
            SirenixEditorGUI.BeginHorizontalToolbar();

            string current = _searchByCategory[_selectedCategory];
            current = _searchField.OnToolbarGUI(current);
            if (_searchByCategory[_selectedCategory] != current)
            {
                _searchByCategory[_selectedCategory] = current;
                var filtered = ApplyFilter(GetAssets(_selectedCategory), current).ToList();
                if (!filtered.Contains(_selectedAsset)) SelectAsset(filtered.FirstOrDefault());
            }

            SirenixEditorGUI.EndHorizontalToolbar();

            var visible = ApplyFilter(GetAssets(_selectedCategory), _searchByCategory[_selectedCategory]);

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            foreach (var asset in visible)
            {
                bool isCurrent = _selectedAsset == asset;
                var style = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal
                };

                if (GUILayout.Button(asset.name, style, GUILayout.Height(24)))
                {
                    SelectAsset(asset);
                }
            }

            if (!visible.Any())
            {
                GUILayout.Space(6);
                EditorGUILayout.HelpBox("No entries match this search.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            _middleWidth = Mathf.Clamp(EditorGUILayout.Slider(_middleWidth, 180f, 520f), 180f, 640f);
        }
    }

    private IEnumerable<UnityEngine.Object> ApplyFilter(IEnumerable<UnityEngine.Object> source, string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return source;
        return source.Where(o => o != null && o.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void DrawRightInspectorPane()
    {
        using (new GUILayout.VerticalScope())
        {
            SirenixEditorGUI.BeginHorizontalToolbar();
            GUILayout.Label(Configs[_selectedCategory].DisplayName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_selectedAsset == null))
            {
                if (SirenixEditorGUI.ToolbarButton("Delete Current"))
                {
                    if (_selectedAsset != null)
                    {
                        var path = AssetDatabase.GetAssetPath(_selectedAsset);
                        if (EditorUtility.DisplayDialog("Delete Asset",
                            $"Delete '{_selectedAsset.name}'?\n{path}", "Delete", "Cancel"))
                        {
                            var toRemove = _selectedAsset;
                            ClearSelection();
                            AssetDatabase.DeleteAsset(path);
                            AssetDatabase.SaveAssets();

                            foreach (var list in _assetsByCategory.Values)
                                list.Remove(toRemove);
                        }
                    }
                }
            }
            SirenixEditorGUI.EndHorizontalToolbar();

            if (_selectedAsset != null) // Renames the asset file
            {
                EditorGUILayout.Space(4);
                EditorGUI.BeginChangeCheck();
                _assetNameBuffer = EditorGUILayout.DelayedTextField(new GUIContent("Asset File Name"), _assetNameBuffer);
                if (EditorGUI.EndChangeCheck())
                {
                    TryRenameSelectedAsset(_assetNameBuffer);
                }
                EditorGUILayout.Space(4);
            }

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            if (_selectedAsset != null)
            {
                _propertyTree?.UpdateTree();
                _propertyTree?.Draw(false);
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select an entry on the left to edit.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void TryRenameSelectedAsset(string desiredName)
    {
        if (_selectedAsset == null) return;
        desiredName = (desiredName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(desiredName)) { _assetNameBuffer = _selectedAsset.name; return; }
        if (desiredName == _selectedAsset.name) return;
        string path = AssetDatabase.GetAssetPath(_selectedAsset);
        if (string.IsNullOrEmpty(path)) { _assetNameBuffer = _selectedAsset.name; return; }
        string folder = Path.GetDirectoryName(path)?.Replace("\\", "/");
        if (string.IsNullOrEmpty(folder)) { _assetNameBuffer = _selectedAsset.name; return; }
        // Make unique if needed
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{desiredName}.asset");
        string uniqueName = Path.GetFileNameWithoutExtension(uniquePath);
        string err = AssetDatabase.RenameAsset(path, uniqueName);
        if (!string.IsNullOrEmpty(err))
        {
            Debug.LogWarning($"Rename failed: {err}");
            _assetNameBuffer = _selectedAsset.name;
            return;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        // Re-sort list (so renamed assets stay ordered)
        var list = GetAssets(_selectedCategory);
        list.Sort((a, b) => string.Compare(a != null ? a.name : "", b != null ? b.name : "", StringComparison.OrdinalIgnoreCase));
        // Keep buffer synced
        _assetNameBuffer = _selectedAsset.name;
        Repaint();
    }

    private void DrawVerticalSeparator()
    {
        var rect = GUILayoutUtility.GetRect(_splitterThickness, 9999f, _splitterThickness, 9999f, GUILayout.Width(_splitterThickness));
        if (Event.current.type == EventType.Repaint)
        {
            var c = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.7f, 0.7f, 0.7f);
            EditorGUI.DrawRect(rect, c);
        }
    }

    private void ShowCreateMenu()
    {
        var menu = new GenericMenu();

        foreach (var kvp in Configs)
        {
            var cat = kvp.Key;
            var cfg = kvp.Value;
            menu.AddItem(new GUIContent(cfg.DisplayName), false, () => CreateAssetForCategory(cat));
        }

        menu.DropDown(GUILayoutUtility.GetLastRect());
    }

    private void CreateAssetForCategory(Category cat)
    {
        var cfg = Configs[cat];

        if (!AssetDatabase.IsValidFolder(cfg.FolderPath))
        {
            Directory.CreateDirectory(cfg.FolderPath);
            AssetDatabase.Refresh();
        }

        var asset = ScriptableObject.CreateInstance(cfg.AssetType);

        var field = cfg.AssetType.GetField("characterName") ??
                    cfg.AssetType.GetField("skillName") ??
                    cfg.AssetType.GetField("itemName") ??
                    cfg.AssetType.GetField("enemyName") ??
                    cfg.AssetType.GetField("statusName");
        if (field != null && field.FieldType == typeof(string))
        {
            field.SetValue(asset, cfg.DefaultFileName);
        }

        var fileName = string.IsNullOrWhiteSpace(cfg.DefaultFileName)
            ? $"New {cfg.AssetType.Name}"
            : cfg.DefaultFileName;

        var path = AssetDatabase.GenerateUniqueAssetPath($"{cfg.FolderPath}/{fileName}.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(asset);

        var list = GetAssets(cat);
        list.Add(asset);
        list.Sort((a, b) => string.Compare(a != null ? a.name : "", b != null ? b.name : "", StringComparison.OrdinalIgnoreCase));
        SelectAsset(asset);
    }

    private void EnsureCache()
    {
        if (_assetsByCategory.Count == 0)
        {
            foreach (var kvp in Configs)
            {
                _assetsByCategory[kvp.Key] = FindAssetsOfType(kvp.Value.AssetType, kvp.Value.FolderPath);
            }
        }
    }

    private List<UnityEngine.Object> GetAssets(Category cat)
    {
        EnsureCache();
        return _assetsByCategory[cat];
    }

    private static List<UnityEngine.Object> FindAssetsOfType(Type type, string folder)
    {
        var guids = AssetDatabase.FindAssets($"t:{type.Name}", new[] { folder });
        return guids
            .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(o => o != null)
            .OrderBy(o => o.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void SelectAsset(UnityEngine.Object obj)
    {
        if (_selectedAsset == obj) return;

        ClearSelection();
        _selectedAsset = obj;
        _assetNameBuffer = _selectedAsset != null ? _selectedAsset.name : string.Empty;

        if (_selectedAsset != null)
        {
            _propertyTree = PropertyTree.Create(_selectedAsset);
        }
    }

    private void ClearSelection()
    {
        if (_selectedEditor != null)
        {
            DestroyImmediate(_selectedEditor);
            _selectedEditor = null;
        }

        if (_propertyTree != null)
        {
            _propertyTree.Dispose();
            _propertyTree = null;
        }
        _selectedAsset = null;

        _assetNameBuffer = string.Empty;
    }
}
