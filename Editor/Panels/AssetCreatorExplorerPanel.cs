using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Ikonoclast.AssetCreator.Editor
{
    using Ikonoclast.Common;
    using Ikonoclast.Common.Editor;

    using static Ikonoclast.Common.Editor.PanelUtils;
    using static Ikonoclast.Common.Editor.CustomEditorHelper;

    internal sealed class AssetCreatorExplorerPanel : AssetCreatorPanel
    {
        private const float
            ItemWidth = 132f,
            ItemHeight = 123f;

        private const double SearchBuffer = 0.1d;

        #region Fields

        private Rect
            assetCountRect,
            inCartIconRect,
            searchFieldRect,
            favoriteIconRect;

        private int assetsPerRow;

        private string searchText = "";
        private bool searchExecuted = false;
        private double timeSinceKeypress = 0;

        private readonly List<Type>
            searchResults = new List<Type>();

        private readonly HashSet<Type>
            singleInstanceInstantiatedTypes = new HashSet<Type>();

        private readonly GUIStyle
            assetTextStyle = new GUIStyle(BaseStyle)
            {
                fontSize = 12,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleCenter,
            },
            searchTextStyle = new GUIStyle(TextFieldStyle);

        private readonly TextGenerator
            textGenerator = new TextGenerator();

        private readonly TextGenerationSettings
            textGenerationSettings = new TextGenerationSettings
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
            };

        #endregion

        #region Constructors

        ~AssetCreatorExplorerPanel()
        {
            Unsubscribe();
        }

        public AssetCreatorExplorerPanel()
        {
            var assetTextFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            searchTextStyle.padding.left = 20;

            assetTextStyle.font = assetTextFont;

            textGenerationSettings.font = assetTextFont;

            lastSize = Vector2.negativeInfinity;

            Subscribe();
        }

        #endregion

        #region Methods

        private void Search()
        {
            searchResults.Clear();
            singleInstanceInstantiatedTypes.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
                return;

            foreach (var type in AssetCreatorEditorWindow.TypeCache)
            {
                if (type.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    searchResults.Add(type);

                    if (AssetCreatorEditorWindow.IsSingleInstanceAssetInstantiated(type))
                    {
                        singleInstanceInstantiatedTypes.Add(type);
                    }
                }
            }

            if (AssetCreatorSettingsPanel.SingleInstanceAssetVisibility == AssetCreatorSettingsPanel.Visibility.Hidden)
            {
                searchResults.RemoveAll(type => AssetCreatorEditorWindow.IsSingleInstanceAssetInstantiated(type));
            }
        }

        private void GetAll()
        {
            searchResults.Clear();
            singleInstanceInstantiatedTypes.Clear();

            foreach (var type in AssetCreatorEditorWindow.TypeCache)
            {
                searchResults.Add(type);

                if (AssetCreatorEditorWindow.IsSingleInstanceAssetInstantiated(type))
                {
                    singleInstanceInstantiatedTypes.Add(type);
                }
            }

            if (AssetCreatorSettingsPanel.SingleInstanceAssetVisibility == AssetCreatorSettingsPanel.Visibility.Hidden)
            {
                searchResults.RemoveAll(type => AssetCreatorEditorWindow.IsSingleInstanceAssetInstantiated(type));
            }
        }

        private void Subscribe()
        {
            AssetCreatorSettingsPanel.SingleInstanceAssetVisibilityChanged += OnSingleInstanceAssetVisibilityChanged;
        }

        private void Unsubscribe()
        {
            AssetCreatorSettingsPanel.SingleInstanceAssetVisibilityChanged -= OnSingleInstanceAssetVisibilityChanged;
        }

        private void HandleEvent()
        {
            var evt = Event.current;

            if (!(evt.type == EventType.KeyDown || evt.type == EventType.KeyUp))
            {
                if (!searchExecuted && EditorApplication.timeSinceStartup - timeSinceKeypress > SearchBuffer)
                {
                    Search();

                    searchExecuted = true;
                }
                else if (string.IsNullOrEmpty(searchText) && searchResults.Count == 0)
                {
                    GetAll();
                }
            }
            else
            {
                timeSinceKeypress = EditorApplication.timeSinceStartup;
                searchExecuted = false;
            }
        }

        private void MakeText(string text)
        {
            if ((itemNameRect.width - 3) > textGenerator.GetPreferredWidth(text, textGenerationSettings))
            {
                GUI.Label(itemNameRect, text, assetTextStyle);

                return;
            }

            assetTextStyle.alignment = TextAnchor.MiddleLeft;

            GUI.Label(itemNameRect, text, assetTextStyle);

            assetTextStyle.alignment = TextAnchor.MiddleCenter;
        }

        #endregion

        #region Event Listeners

        private void OnSingleInstanceAssetVisibilityChanged()
        {
            switch (AssetCreatorSettingsPanel.SingleInstanceAssetVisibility)
            {
                case AssetCreatorSettingsPanel.Visibility.Hidden:
                    searchResults.RemoveAll(type => AssetCreatorEditorWindow.IsSingleInstanceAssetInstantiated(type));
                    break;

                case AssetCreatorSettingsPanel.Visibility.Disabled:
                    searchResults.AddRange(singleInstanceInstantiatedTypes);
                    break;
            }
        }

        #endregion

        #region AssetCreatorPanel Implementations

        protected override string Title =>
            "Asset Explorer";

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

            var cartCount = AssetCreatorEditorWindow.CartCount;

            EditorGUI.BeginDisabledGroup(cartCount == 0);

            IconButton
            (
                panelButtonRect,
                () => AssetCreatorEditorWindow.OpenCartPanel(),
                UnityIcons.ScriptableObject,
                "Open the asset cart."
            );

            GUI.Label(assetCountRect, cartCount.ToString(), EditorStyles.boldLabel);

            EditorGUI.EndDisabledGroup();

            EditorGUIUtility.AddCursorRect(searchFieldRect, MouseCursor.Text);

            searchText = EditorGUI.TextField(searchFieldRect, searchText, searchTextStyle);

            EditorGUIUtility.SetIconSize(searchIconSize);

            GUI.Label(searchFieldRect, UnityIcons.Search);

            HandleEvent();

            if (assetsPerRow == 0)
                return;

            BeginScrollView(mainRect, ref scrollViewPosition, scrollViewRect);

            for (int i = 1; i <= searchResults.Count; ++i)
            {
                var type = searchResults[i - 1];

                bool disabled = AssetCreatorEditorWindow.IsSingleInstanceAssetInstantiated(type);

                EditorGUI.BeginDisabledGroup(disabled);

                EditorGUIUtility.SetIconSize(largeIconSize);
                MakeBox(itemButtonRect, Color.grey);
                IconButton
                (
                    itemButtonRect,
                    () => HandleAssetItemClickEvent(Event.current, type),
                    UnityIcons.ScriptableObject,
                    disabled
                        ? $"{type.Name} [{nameof(ISingleInstanceAsset)}]"
                        : type.Name
                );

                MakeText(type.Name);

                EditorGUIUtility.SetIconSize(smallIconSize);

                if (AssetCreatorEditorWindow.IsTypeInCart(type))
                {
                    GUI.Label(inCartIconRect, UnityIcons.P4_CheckOutLocal);
                }

                if (AssetCreatorEditorWindow.IsTypeFavorited(type))
                {
                    GUI.Label(favoriteIconRect, UnityIcons.FavoriteIcon);
                }

                if ((i % assetsPerRow) == 0)
                {
                    itemNameRect.x =
                    itemButtonRect.x = 10;

                    inCartIconRect.x = 12;
                    favoriteIconRect.x = 88;

                    itemNameRect.y += ItemHeight;
                    inCartIconRect.y += ItemHeight;
                    itemButtonRect.y += ItemHeight;
                    favoriteIconRect.y += ItemHeight;
                }
                else
                {
                    itemNameRect.x += ItemWidth;
                    inCartIconRect.x += ItemWidth;
                    itemButtonRect.x += ItemWidth;
                    favoriteIconRect.x += ItemWidth;
                }

                EditorGUI.EndDisabledGroup();
            }

            GUI.EndScrollView();
        }

        protected override void MakeRects(Vector2 size)
        {
            base.MakeRects(size);

            float offsetX = PanelBoxRect.x;
            float offsetY = panelButtonRect.y;

            searchFieldRect = new Rect
            {
                x = 155,
                y = offsetY + 2.5f,
                width = PanelBoxRect.width - 180,
                height = slh * 1.25f
            };

            assetCountRect = new Rect
            {
                x = panelButtonRect.x + 14,
                y = panelButtonRect.y - 6,
                width = (AssetCreatorEditorWindow.CartCount > 9) ? 16f : 10f,
                height = 10,
            };

            offsetY += slh * 2;

            mainRect = new Rect
            {
                x = offsetX + 2,
                y = offsetY - 5,
                width = (PanelBoxRect.width - offsetX) + 6,
                height = (PanelBoxRect.height - offsetY) + 14
            };

            assetsPerRow = (int)(size.x / ItemWidth);

            if (assetsPerRow == 0)
            {
                assetsPerRow = 3;
            }

            scrollViewRect = new Rect
            {
                width = mainRect.width - offsetX - 10,
                height = ItemHeight * Mathf.Ceil((searchResults.Count + (assetsPerRow - 1)) / assetsPerRow) + 14
            };

            lastSize = size;

            itemButtonRect = new Rect
            {
                x = 10,
                y = 10,
                width = 100,
                height = 100
            };

            inCartIconRect = new Rect
            {
                x = 12,
                y = 11,
                width = slh * 1.2f,
                height = slh * 1.2f
            };

            favoriteIconRect = new Rect(inCartIconRect)
            {
                x = 88,
                y = 11,
            };

            itemNameRect = new Rect
            {
                x = 10,
                y = 110,
                width = 100,
                height = slh
            };
        }

        #endregion
    }
}