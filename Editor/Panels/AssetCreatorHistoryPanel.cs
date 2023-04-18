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

    internal sealed class AssetCreatorHistoryPanel : AssetCreatorPanel, IEditorSaveObject
    {
        private const string Subtitle = "Favorites";

        #region Fields

        private Rect
            listDividerRect,
            historyMainRect,
            subtitleBoxRect,
            subheaderBoxRect,
            favoritesMainRect,
            historyScrollViewRect,
            favoritesScrollViewRect;

        private Vector2
            historyScrollViewPos,
            favoritesScrollViewPos;

        private readonly List<Type>
            historyItems = new List<Type>(),
            favoritesItems = new List<Type>();

        private readonly GUIContent
            itemNameContentCache = new GUIContent();

        #endregion

        #region Properties

        private int HistoryCapacity =>
            AssetCreatorSettingsPanel.HistoryCapacity;

        private int FavoritesCapacity =>
            AssetCreatorSettingsPanel.FavoritesCapacity;

        #endregion

        #region Constructors

        ~AssetCreatorHistoryPanel()
        {
            Unsubscribe();
        }

        public AssetCreatorHistoryPanel()
        {
            Subscribe();
        }

        #endregion

        #region Event Listeners

        private void OnRequestClearHistory() =>
            historyItems.Clear();

        private void OnRequestClearFavorites() =>
            favoritesItems.Clear();

        private void OnAddedFavorite(Type type)
        {
            favoritesItems.Remove(type);

            favoritesItems.Add(type);

            if (favoritesItems.Count > FavoritesCapacity)
            {
                favoritesItems.RemoveAt(0);
            }
        }

        private void OnRemovedFavorite(Type type)
        {
            favoritesItems.Remove(type);
        }

        private void OnCreatedAsset(ScriptableObject asset)
        {
            if (asset == null)
                return;

            var type = asset.GetType();

            historyItems.Remove(type);

            historyItems.Add(type);

            if (historyItems.Count > HistoryCapacity)
            {
                historyItems.RemoveAt(0);
            }
        }

        #endregion

        #region Methods

        private void Subscribe()
        {
            CreatedAsset += OnCreatedAsset;
            AddFavorite += OnAddedFavorite;
            RemoveFavorite += OnRemovedFavorite;
            AssetCreatorSettingsPanel.ClearHistory += OnRequestClearHistory;
            AssetCreatorSettingsPanel.ClearFavorites += OnRequestClearFavorites;
        }

        private void Unsubscribe()
        {
            CreatedAsset -= OnCreatedAsset;
            AddFavorite -= OnAddedFavorite;
            RemoveFavorite -= OnRemovedFavorite;
            AssetCreatorSettingsPanel.ClearHistory -= OnRequestClearHistory;
            AssetCreatorSettingsPanel.ClearFavorites -= OnRequestClearFavorites;
        }

        public bool IsTypeFavorited(Type type) =>
            favoritesItems.Contains(type);

        private void OnPanelGUIEx(ref Vector2 scrollViewPos, Rect mainRect, Rect scrollViewRect, List<Type> items, GUIContent icon)
        {
            BeginScrollView(mainRect, ref scrollViewPos, scrollViewRect);

            itemRect.x = 10;
            itemRect.y = 10;

            itemIconRect.x = 15;
            itemIconRect.y = 14;

            itemNameRect.x = 15 + (slh * 1.6f + 10);
            itemNameRect.y = 14;

            itemButtonRect.x = itemRect.width - 20;
            itemButtonRect.y = 16;

            for (int i = items.Count - 1; i >= 0; --i)
            {
                HandleEvent(i, out bool itemRemovedDuringLoop);

                if (itemRemovedDuringLoop)
                {
                    GUI.EndScrollView();
                    return;
                }

                MakeBox(itemRect, Color.gray);

                EditorGUI.BeginDisabledGroup(AssetCreatorEditorWindow.IsSingleInstanceAssetInstantiated(items[i]));

                // Used for UI representation
                GUI.Button(itemRect, "");

                EditorGUIUtility.SetIconSize(mediumIconSize);

                GUI.Label(itemIconRect, icon);

                itemNameContentCache.text = items[i].Name;
                itemNameContentCache.tooltip = items[i].Name;

                GUI.Label(itemNameRect, itemNameContentCache, EditorStyles.boldLabel);

                EditorGUI.EndDisabledGroup();

                EditorGUIUtility.SetIconSize(smallIconSize);

                IconButton
                (
                    itemButtonRect,
                    null,
                    UnityIcons.Close,
                    "Remove the asset from the list."
                );

                itemRect.y += slh * 2 + 5;
                itemIconRect.y = itemRect.y + 4;

                itemNameRect.y =
                itemButtonRect.y = itemIconRect.y + 2;
            }

            GUI.EndScrollView();

            void HandleEvent(int i, out bool removed)
            {
                var evt = Event.current;

                removed = false;

                if (evt == null)
                    return;

                if (evt.type != EventType.MouseDown)
                    return;

                var pos = evt.mousePosition;

                if (itemButtonRect.Contains(pos))
                {
                    items.RemoveAt(i);

                    removed = true;

                    return;
                }

                if (AssetCreatorEditorWindow.IsSingleInstanceAssetInstantiated(items[i]))
                    return;

                if (itemRect.Contains(pos))
                {
                    HandleAssetItemClickEvent(evt, items[i]);
                }
            }
        }

        #endregion

        #region AssetCreatorPanel Implementations

        protected override string Title =>
            "Asset History";

        protected override void MakeBoxes()
        {
            base.MakeBoxes();

            MakeBox(subheaderBoxRect, Color.black);
            MakeBox(historyMainRect);
            MakeBox(favoritesMainRect);
        }

        public override void OnPanelGUI(Vector2 size)
        {
            MakeRects(size);
            MakeBoxes();

            GUI.Label(TitleBoxRect, Title, TitleStyle);
            GUI.Label(subtitleBoxRect, Subtitle, TitleStyle);

            EditorGUIUtility.SetIconSize(panelButtonIconSize);

            IconButton
            (
                panelButtonRect,
                () => AssetCreatorEditorWindow.OpenSettingsPanel(),
                UnityIcons.Settings,
                "Open the editor settings."
            );

            OnPanelGUIEx(ref historyScrollViewPos, historyMainRect, historyScrollViewRect, historyItems, UnityIcons.ScriptableObject);
            OnPanelGUIEx(ref favoritesScrollViewPos, favoritesMainRect, favoritesScrollViewRect, favoritesItems, UnityIcons.FavoriteIcon);
        }

        protected override void MakeRects(Vector2 size)
        {
            base.MakeRects(size);

            float offsetX = PanelBoxRect.x;
            float offsetY = panelButtonRect.y + slh * 2;

            historyMainRect = new Rect
            {
                x = offsetX + 2,
                y = offsetY - 5,
                width = (PanelBoxRect.width - offsetX) + 6,
                height = (((PanelBoxRect.height - offsetY) + 14) * 0.5f) - 2 - slh
            };

            subheaderBoxRect = new Rect(HeaderBoxRect)
            {
                y = historyMainRect.height + historyMainRect.y + 2,
            };

            subtitleBoxRect = new Rect(TitleBoxRect)
            {
                y = subheaderBoxRect.y - 2,
            };

            offsetY = subtitleBoxRect.y + 5 + (slh * 2);

            favoritesMainRect = new Rect
            {
                x = offsetX + 2,
                y = offsetY - 5,
                width = historyMainRect.width,
                height = historyMainRect.height + 3
            };

            historyScrollViewRect = new Rect
            {
                width = historyMainRect.width - offsetX - 10,
                height = ((slh * 2 + 5) * historyItems.Count) + 14
            };
            favoritesScrollViewRect = new Rect(historyScrollViewRect)
            {
                height = ((slh * 2 + 5) * favoritesItems.Count) + 14
            };

            itemRect = new Rect(historyScrollViewRect)
            {
                height = slh * 2
            };

            itemIconRect = new Rect
            {
                width = slh * 1.6f,
                height = slh * 1.6f
            };

            itemNameRect = new Rect
            {
                width = itemRect.width - 79,
                height = slh * 1.6f
            };

            itemButtonRect = new Rect
            {
                width = slh * 1.3f,
                height = slh * 1.3f
            };
        }

        #endregion

        #region IEditorSaveObject Implementations

        public string ID =>
            nameof(AssetCreatorHistoryPanel);

        public bool Enabled
        {
            get;
            set;
        } = true;

        public void Reset()
        {
            Enabled = true;
            historyItems.Clear();
            favoritesItems.Clear();
        }

        public Map Serialize()
        {
            var map = new Map(ID);

            for (int i = 0; i < historyItems.Count; ++i)
            {
                map[$"h_{i}"] = historyItems[i].AssemblyQualifiedName;
            }
            for (int i = 0; i < favoritesItems.Count; ++i)
            {
                map[$"f_{i}"] = favoritesItems[i].AssemblyQualifiedName;
            }

            return map;
        }

        public void Serialize(Map map, bool overwrite = false)
        {
            if (overwrite)
            {
                for (int i = 0; i < historyItems.Count; ++i)
                {
                    map[$"h_{i}"] = historyItems[i].AssemblyQualifiedName;
                }

                for (int i = 0; i < favoritesItems.Count; ++i)
                {
                    map[$"f_{i}"] = favoritesItems[i].AssemblyQualifiedName;
                }
            }
            else
            {
                for (int i = 0; i < historyItems.Count; ++i)
                {
                    if (!map.HasKey($"h_{i}"))
                    {
                        map[$"h_{i}"] = historyItems[i].AssemblyQualifiedName;
                    }
                }

                for (int i = 0; i < favoritesItems.Count; ++i)
                {
                    if (!map.HasKey($"f_{i}"))
                    {
                        map[$"f_{i}"] = favoritesItems[i].AssemblyQualifiedName;
                    }
                }
            }
        }

        public void Deserialize(Map map)
        {
            bool
                foundAllHistory = false,
                foundAllFavorites = false;

            historyItems.Clear();
            favoritesItems.Clear();

            for (int i = 0; ; ++i)
            {
                if (!foundAllHistory)
                {
                    var str = map.GetString($"h_{i}");

                    if (str != null)
                    {
                        var deserializedType = Type.GetType(str, false);

                        if (deserializedType != null)
                        {
                            historyItems.Add(deserializedType);
                        }
                    }
                    else
                    {
                        foundAllHistory = true;
                    }
                }

                if (!foundAllFavorites)
                {
                    var str = map.GetString($"f_{i}");

                    if (str != null)
                    {
                        var deserializedType = Type.GetType(str, false);

                        if (deserializedType != null && !AssetCreatorEditorWindow.IsSingleInstanceAssetInstantiated(deserializedType))
                        {
                            favoritesItems.Add(deserializedType);
                        }
                    }
                    else
                    {
                        foundAllFavorites = true;
                    }
                }

                if (foundAllHistory && foundAllFavorites)
                    break;
            }
        }

        #endregion
    }
}
