using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Ikonoclast.AssetCreator.Editor
{
    using Ikonoclast.Common;
    using Ikonoclast.Common.Editor;

    using static Ikonoclast.Common.Editor.CustomEditorHelper;
    using static Ikonoclast.Common.Editor.AssetDatabaseExtensions;

    internal sealed class AssetCreatorEditorWindow : EditorWindow
    {
        private class AssetDatabaseWatcher : AssetPostprocessor
        {
            public static event Action Changed;

            private static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                if (importedAssets.Any(s => s.EndsWith(".asset")))
                {
                    Changed?.Invoke();
                }

                if (deletedAssets.Any(s => s.EndsWith(".asset")))
                {
                    Changed?.Invoke();
                }
            }
        }

        #region Fields

        private static int?
            mainWindowID,
            sideWindowID;

        private static Panel
            mainPanel,
            sidePanel;

        private static AssetCreatorCartPanel cartPanel;
        private static AssetCreatorHistoryPanel historyPanel;
        private static AssetCreatorSettingsPanel settingsPanel;
        private static AssetCreatorExplorerPanel explorerPanel;

        private static Rect
            mainPanelWindowRect,
            sidePanelWindowRect;

        private static Vector2
            mainPanelSize,
            sidePanelSize;

        private static Rect lastPosition = Rect.zero;

        private static readonly List<Type> _typeCache = new List<Type>();

        private static readonly HashSet<Type> singleInstanceInstantiatedTypes = new HashSet<Type>();

        #endregion

        #region Properties

        public static int CartCount =>
            cartPanel.Count;

        public static IEnumerable<Type> TypeCache =>
            _typeCache;

        #endregion

        #region Methods

        [MenuItem("Ikonoclast/Asset Creator")]
        public static void OpenEditor()
        {
            if (HasOpenInstances<AssetCreatorEditorWindow>())
                return;

            var window = GetWindow<AssetCreatorEditorWindow>();

            lastPosition = window.position;

            window.maximized = false;
            window.minSize = new Vector2(450, 225);
            window.titleContent = new GUIContent("Asset Creator");

            sidePanelSize = new Vector2
            {
                x = lastPosition.size.x * 0.35f,
                y = lastPosition.size.y
            };

            mainPanelSize = new Vector2
            {
                x = (lastPosition.size.x * 0.65f) - 10,
                y = lastPosition.size.y
            };

            window.Show();
        }

        internal static void OpenCartPanel()
        {
            if (mainPanel != cartPanel)
            {
                mainPanel = cartPanel;
            }
        }

        internal static void OpenHistoryPanel()
        {
            if (sidePanel != historyPanel)
            {
                sidePanel = historyPanel;
            }
        }

        internal static void OpenExplorerPanel()
        {
            if (mainPanel != explorerPanel)
            {
                mainPanel = explorerPanel;
            }
        }

        internal static void OpenSettingsPanel()
        {
            if (sidePanel != settingsPanel)
            {
                sidePanel = settingsPanel;
            }
        }

        internal static bool IsTypeInCart(Type type) =>
            cartPanel.IsTypeInCart(type);

        internal static bool IsTypeFavorited(Type type) =>
            historyPanel.IsTypeFavorited(type);

        internal static bool IsSingleInstanceAsset(Type type) =>
            typeof(ISingleInstanceAsset).IsAssignableFrom(type);

        internal static bool IsSingleInstanceAssetInstantiated(Type type) =>
            singleInstanceInstantiatedTypes.Contains(type);

        private void Subscribe()
        {
            AssetDatabaseWatcher.Changed += OnAssetDatabaseChanged;
        }

        private void Unsubscribe()
        {
            AssetDatabaseWatcher.Changed -= OnAssetDatabaseChanged;
        }

        private void HandleWindowReposition()
        {
            if (position == lastPosition)
                return;

            lastPosition = position;

            var size = position.size;

            sidePanelSize = new Vector2
            {
                x = size.x * 0.35f,
                y = size.y
            };

            mainPanelSize = new Vector2
            {
                x = (size.x * 0.65f) - 10,
                y = size.y
            };
        }

        private static void LoadConfigurations()
        {
            string path = Path.Combine
            (
                Directory.GetCurrentDirectory(),
                "Packages\\com.ikonoclast.asset-creator\\Editor\\.configurations.json"
            );

            if (!File.Exists(path))
            {
                Debug.LogWarning("Could not find asset creator configuration file.");
            }
            else
            {
                var json = File.ReadAllText(path);

                if (!string.IsNullOrEmpty(json))
                {
                    var configuration = JsonConvert.DeserializeObject<Map>(json);
                    historyPanel.Deserialize(configuration);
                    settingsPanel.Deserialize(configuration);
                }
            }
        }

        private static void SaveConfigurations()
        {
            var configurations = new Map("Configurations");

            historyPanel.Serialize(configurations, true);
            settingsPanel.Serialize(configurations, true);

            var json = JsonConvert.SerializeObject(configurations);

            File.WriteAllText
            (
                Path.Combine
                (
                    Directory.GetCurrentDirectory(),
                    "Packages\\com.ikonoclast.asset-creator\\Editor\\.configurations.json"
                ),
                json
            );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RepopulateTypeCache()
        {
            _typeCache.Clear();

            var type = typeof(ICreatableAsset);
            var scriptableObjectType = typeof(ScriptableObject);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                _typeCache.AddRange(assembly.GetTypes().Where(p => typeof(ScriptableObject).IsAssignableFrom(p)
                                                                   && typeof(ICreatableAsset).IsAssignableFrom(p)
                                                                   && p.IsClass
                                                                   && !p.IsAbstract));
            }
        }

        #endregion

        #region Event Listeners

        private void OnAssetDatabaseChanged()
        {
            RepopulateTypeCache();

            singleInstanceInstantiatedTypes.Clear();

            foreach (var type in TypeCache)
            {
                if (IsSingleInstanceAsset(type) && AssetExists(type))
                {
                    singleInstanceInstantiatedTypes.Add(type);
                }
            }
        }

        #endregion

        private void OnGUI()
        {
            HandleEvent(out bool close);

            if (close)
                return;

            HandleWindows();
            HandleWindowReposition();

            void HandleEvent(out bool close)
            {
                close = false;

                var evt = Event.current;

                if (evt == null)
                    return;

                if (GUIUtility.keyboardControl != 0)
                    return;

                if (evt.type != EventType.KeyDown)
                    return;

                switch (evt.keyCode)
                {
                    case KeyCode.Escape:
                    {
                        close = true;
                        Close();
                        return;
                    }

                    case KeyCode.Tab:
                    {
                        if (mainPanel == cartPanel)
                        {
                            OpenExplorerPanel();
                        }
                        else if (mainPanel == explorerPanel)
                        {
                            OpenCartPanel();
                        }

                        break;
                    }
                }
            }
            void HandleWindows()
            {
                mainWindowID ??= GenerateUniqueSessionID();
                sideWindowID ??= GenerateUniqueSessionID();

                sidePanelWindowRect = new Rect(0, 0, sidePanelSize.x, sidePanelSize.y);
                mainPanelWindowRect = new Rect(sidePanelSize.x, 0, mainPanelSize.x, mainPanelSize.y);

                BeginWindows();

                GUI.Window(sideWindowID.Value, sidePanelWindowRect, (_) => sidePanel.OnPanelGUI(sidePanelSize), "", EditorStyles.inspectorDefaultMargins);
                GUI.Window(mainWindowID.Value, mainPanelWindowRect, (_) => mainPanel.OnPanelGUI(mainPanelSize), "", EditorStyles.inspectorDefaultMargins);

                EndWindows();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            OnAssetDatabaseChanged();

            cartPanel = new AssetCreatorCartPanel();
            settingsPanel = new AssetCreatorSettingsPanel();
            sidePanel = historyPanel = new AssetCreatorHistoryPanel();
            mainPanel = explorerPanel = new AssetCreatorExplorerPanel();

            LoadConfigurations();
            SetDefaultGUIColor(GUI.color);
            SetDefaultGUIContentColor(GUI.contentColor);

            EditorGUIUtility.SetIconSize(Vector2.zero);
        }

        private void OnDisable()
        {
            Unsubscribe();
            SaveConfigurations();
        }

        private void OnInspectorUpdate()
        {
            // Only update the GUI if the mouse is hovering.
            if (mouseOverWindow == this)
            {
                Repaint();
            }
        }
    }
}
