using System;
using UnityEditor;
using UnityEngine;

namespace Ikonoclast.AssetCreator.Editor
{
    using Ikonoclast.Common;
    using Ikonoclast.Common.Editor;

    using static Ikonoclast.Common.Editor.PanelUtils;
    using static Ikonoclast.Common.Editor.CustomEditorHelper;

    internal sealed class AssetCreatorSettingsPanel : AssetCreatorPanel, IEditorSaveObject
    {
        internal enum Visibility
        {
            Hidden = 0,
            Disabled
        }

        #region Events

        public static event Action
            ClearHistory,
            ClearFavorites,
            HistoryCapacityChanged,
            FavoritesCapacityChanged,
            OverwriteExistingChanged,
            PingAssetOnCreationChanged,
            SingleInstanceAssetVisibilityChanged;

        #endregion

        #region Fields

        private Rect
            settingsItemBoxRect,
            settingsItemNameRect,
            settingsItemFieldRect,
            clearHistoryButtonRect,
            clearFavoritesButtonRect;

        private static int
            _historyCapacity = 10,
            _favoritesCapacity = 10;

        private static bool
            _overwriteExisting = false,
            _pingAssetOnCreation = false;

        private static Visibility _singleInstanceAssetVisibility = Visibility.Disabled;

        private static readonly GUIContent labelContent = new GUIContent();

        #endregion

        #region Properties

        public static int SettingsCount => 5;

        public static int HistoryCapacity
        {
            get => _historyCapacity;
            private set
            {
                if (value < 1)
                {
                    value = 1;
                }

                if (_historyCapacity != value)
                {
                    _historyCapacity = value;

                    HistoryCapacityChanged?.Invoke();
                }
            }
        }

        public static int FavoritesCapacity
        {
            get => _favoritesCapacity;
            private set
            {
                if (value < 1)
                {
                    value = 1;
                }

                if (_favoritesCapacity != value)
                {
                    _favoritesCapacity = value;

                    FavoritesCapacityChanged?.Invoke();
                }
            }
        }

        public static bool OverwriteExisting
        {
            get => _overwriteExisting;
            private set
            {
                if (_overwriteExisting != value)
                {
                    _overwriteExisting = value;

                    OverwriteExistingChanged?.Invoke();
                }
            }
        }

        public static bool PingAssetOnCreation
        {
            get => _pingAssetOnCreation;
            private set
            {
                if (_pingAssetOnCreation != value)
                {
                    _pingAssetOnCreation = value;

                    PingAssetOnCreationChanged?.Invoke();
                }
            }
        }

        public static Visibility SingleInstanceAssetVisibility
        {
            get => _singleInstanceAssetVisibility;
            private set
            {
                if (_singleInstanceAssetVisibility != value)
                {
                    _singleInstanceAssetVisibility = value;

                    SingleInstanceAssetVisibilityChanged?.Invoke();
                }
            }
        }

        #endregion

        #region Constructors

        ~AssetCreatorSettingsPanel()
        {
            Unsubscribe();
        }

        public AssetCreatorSettingsPanel() : base()
        {
            Reset();
            Subscribe();
        }

        #endregion

        #region Methods

        private void Subscribe()
        {
            CreatedAsset += OnCreatedAsset;
        }

        private void Unsubscribe()
        {
            CreatedAsset -= OnCreatedAsset;
        }

        private object MakeSetting<T>(string label, T value, string tooltip = null)
        {
            MakeBox(settingsItemBoxRect);

            labelContent.text = label;
            labelContent.tooltip = tooltip ?? string.Empty;

            GUI.Label(settingsItemNameRect, labelContent);

            object result;

            if (value is int @int)
            {
                settingsItemFieldRect.height = slh;
                settingsItemFieldRect.width = (settingsItemBoxRect.width * 0.35f) - slh;
                settingsItemFieldRect.x = settingsItemNameRect.x + settingsItemBoxRect.width * 0.65f;

                result = EditorGUI.IntField(settingsItemFieldRect, @int);
            }
            else if (value is Enum @enum)
            {
                settingsItemFieldRect.height = slh;
                settingsItemFieldRect.width = (settingsItemBoxRect.width * 0.35f) - slh;
                settingsItemFieldRect.x = settingsItemNameRect.x + settingsItemBoxRect.width * 0.65f;

                result = EditorGUI.EnumPopup(settingsItemFieldRect, @enum);
            }
            else if (value is bool @bool)
            {
                settingsItemFieldRect.width =
                settingsItemFieldRect.height = slh;
                settingsItemFieldRect.x = settingsItemBoxRect.width - slh;

                result = EditorGUI.Toggle(settingsItemFieldRect, @bool);
            }
            else if (value is float @float)
            {
                settingsItemFieldRect.height = slh;
                settingsItemFieldRect.width = (settingsItemBoxRect.width * 0.35f) - slh;
                settingsItemFieldRect.x = settingsItemNameRect.x + settingsItemBoxRect.width * 0.65f;

                result = EditorGUI.FloatField(settingsItemFieldRect, @float);
            }
            else result = default(T);

            var deltaY = settingsItemBoxRect.height + 5;

            settingsItemBoxRect.y += deltaY;
            settingsItemNameRect.y += deltaY;
            settingsItemFieldRect.y += deltaY;

            return result;
        }

        #endregion

        #region Event Listeners

        private void OnCreatedAsset(ScriptableObject obj)
        {
            if (PingAssetOnCreation)
            {
                FocusAndPing(obj);
            }
        }

        #endregion

        #region SettingsPanel Implementations

        protected override string Title =>
            "Editor Settings";

        protected override void MakeBoxes()
        {
            base.MakeBoxes();

            MakeBox(mainRect);
        }

        public override void OnPanelGUI(Vector2 size)
        {
            MakeRects(size);
            MakeBoxes();

            GUI.Label(TitleBoxRect, Title, TitleStyle);

            EditorGUIUtility.SetIconSize(panelButtonIconSize);

            IconButton
            (
                panelButtonRect,
                () => AssetCreatorEditorWindow.OpenHistoryPanel(),
                UnityIcons.ConsoleWindow,
                "Open the asset history."
            );

            BeginScrollView(mainRect, ref scrollViewPosition, scrollViewRect);

            HistoryCapacity = (int)MakeSetting("History Capacity", HistoryCapacity);
            FavoritesCapacity = (int)MakeSetting("Favorites Capacity", FavoritesCapacity);
            OverwriteExisting = (bool)MakeSetting("Overwrite Existing", OverwriteExisting);
            PingAssetOnCreation = (bool)MakeSetting("Ping Asset On Creation", PingAssetOnCreation);
            SingleInstanceAssetVisibility = (Visibility)MakeSetting(
                "Single Instance Asset Visibility",
                SingleInstanceAssetVisibility,
                "Determines how instantiated single instance assets will be displayed in the explorer.");

            GUI.EndScrollView();

            if (GUI.Button(clearHistoryButtonRect, "Clear History"))
            {
                ClearHistory?.Invoke();
            }
            else if (GUI.Button(clearFavoritesButtonRect, "Clear Favorites"))
            {
                ClearFavorites?.Invoke();
            }
        }

        protected override void MakeRects(Vector2 size)
        {
            base.MakeRects(size);

            float offsetX = PanelBoxRect.x;
            float offsetY = panelButtonRect.y + slh * 2;

            mainRect = new Rect
            {
                x = offsetX + 2,
                y = offsetY - 5,
                width = (PanelBoxRect.width - offsetX) + 6,
                height = (PanelBoxRect.height - offsetY) + 17 - (slh * 2)
            };

            scrollViewRect = new Rect
            {
                width = mainRect.width - offsetX - 10,
                height = ((slh * 2 + 5) * SettingsCount) + 14
            };

            settingsItemBoxRect = new Rect
            {
                x = 10,
                y = 10,
                width = scrollViewRect.width,
                height = slh * 2
            };

            settingsItemFieldRect = new Rect
            {
                x = settingsItemBoxRect.width - slh,
                y = slh + 2,
            };

            settingsItemNameRect = new Rect
            {
                x = settingsItemBoxRect.x + 5,
                y = 11,
                width = settingsItemBoxRect.width - slh,
                height = slh * 2
            };

            clearHistoryButtonRect = new Rect
            {
                x = mainRect.x,
                y = PanelBoxRect.height - slh - 4.75f,
                width = mainRect.width * 0.5f,
                height = (slh * 2) - 5
            };
            clearFavoritesButtonRect = new Rect(clearHistoryButtonRect)
            {
                x = clearHistoryButtonRect.x + clearHistoryButtonRect.width,
            };
        }

        #endregion

        #region IEditorSaveObject Implementations

        string IIdentity<string>.ID =>
            nameof(AssetCreatorSettingsPanel);

        bool IEditorSaveObject.Enabled
        {
            get;
            set;
        }

        public void Reset()
        {
            HistoryCapacity = 10;
            FavoritesCapacity = 10;
            OverwriteExisting = false;
            PingAssetOnCreation = false;
            SingleInstanceAssetVisibility = Visibility.Disabled;
        }

        public Map Serialize()
        {
            var map = new Map("Settings");

            Serialize(map, true);

            return map;
        }

        public void Serialize(Map map, bool overwrite)
        {
            if (overwrite)
            {
                map[nameof(HistoryCapacity)] = HistoryCapacity;
                map[nameof(FavoritesCapacity)] = FavoritesCapacity;
                map[nameof(OverwriteExisting)] = OverwriteExisting;
                map[nameof(PingAssetOnCreation)] = PingAssetOnCreation;
                map[nameof(SingleInstanceAssetVisibility)] = SingleInstanceAssetVisibility;
            }
            else
            {
                TryAddValue(nameof(HistoryCapacity), HistoryCapacity);
                TryAddValue(nameof(FavoritesCapacity), FavoritesCapacity);
                TryAddValue(nameof(OverwriteExisting), OverwriteExisting);
                TryAddValue(nameof(PingAssetOnCreation), PingAssetOnCreation);
                TryAddValue(nameof(SingleInstanceAssetVisibility), SingleInstanceAssetVisibility);

                void TryAddValue(string key, object value)
                {
                    if (!map.HasKey(key))
                    {
                        map[key] = value;
                    }
                }
            }
        }

        public void Deserialize(Map map)
        {
            HistoryCapacity = map.GetInt32(nameof(HistoryCapacity));
            FavoritesCapacity = map.GetInt32(nameof(FavoritesCapacity));
            OverwriteExisting = map.GetRawBoolean(nameof(OverwriteExisting));
            PingAssetOnCreation = map.GetRawBoolean(nameof(PingAssetOnCreation));
            SingleInstanceAssetVisibility = (Visibility)map.GetInt32(nameof(SingleInstanceAssetVisibility));

            if (HistoryCapacity < 10)
            {
                HistoryCapacity = 10;
            }
            else if (HistoryCapacity > 100)
            {
                HistoryCapacity = 100;
            }

            if (FavoritesCapacity < 5)
            {
                FavoritesCapacity = 5;
            }
            else if (FavoritesCapacity > 30)
            {
                FavoritesCapacity = 30;
            }
        }

        #endregion
    }
}
