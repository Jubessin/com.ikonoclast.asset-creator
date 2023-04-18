using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ikonoclast.AssetCreator.Editor
{
    using Ikonoclast.Common;
    using Ikonoclast.Common.Editor;

    using static Ikonoclast.Common.Editor.PanelUtils;
    using static Ikonoclast.Common.Editor.CustomEditorHelper;

    internal abstract class AssetCreatorPanel : Panel
    {
        #region Events

        public static event Action<Type>
            AddFavorite,
            RemoveFavorite,
            AddAssetToCart,
            RemoveAssetFromCart;

        public static event Action<ScriptableObject> CreatedAsset;

        #endregion

        #region Fields

        protected Rect
            mainRect,
            itemRect,
            itemIconRect,
            itemNameRect,
            itemButtonRect,
            scrollViewRect,
            panelButtonRect;

        protected Vector2 scrollViewPosition;

        protected readonly GUIContent
            quickCreateContent = new GUIContent
            {
                text = "Quick Create",
            },
            favoriteContent = new GUIContent
            {
                text = "Favorite",
            };

        protected readonly Vector2
            searchIconSize = new Vector2
            {
                x = 15,
                y = 15,
            },
            smallIconSize = new Vector2
            {
                x = 20,
                y = 20,
            },
            mediumIconSize = new Vector2
            {
                x = slh * 1.4f,
                y = slh * 1.4f,
            },
            largeIconSize = new Vector2
            {
                x = 95,
                y = 95,
            },
            panelButtonIconSize = new Vector2
            {
                x = 24,
                y = 24,
            };

        #endregion

        #region Properties

        protected Rect TitleBoxRect
        {
            get;
            set;
        }

        protected Rect HeaderBoxRect
        {
            get;
            set;
        }

        protected abstract string Title
        {
            get;
        }

        #endregion

        #region Methods

        protected void HandleAssetItemClickEvent(Event evt, Type assetType)
        {
            if (evt?.button == RIGHT_CLICK)
            {
                var quickCreate = new CTXMenu_Item(quickCreateContent, false, () => CreateAssetQuick(assetType));

                if (AssetCreatorEditorWindow.IsSingleInstanceAsset(assetType))
                {
                    CreateContextMenu(new CTXMenu_Item[] { quickCreate });
                }
                else
                {
                    CreateContextMenu(new CTXMenu_Item[]
                    {
                        quickCreate,
                        AssetCreatorEditorWindow.IsTypeFavorited(assetType)
                            ? new CTXMenu_Item(favoriteContent, true, () => RemoveFavorite?.Invoke(assetType))
                            : new CTXMenu_Item(favoriteContent, false, () => AddFavorite?.Invoke(assetType))
                    });
                }
            }
            else
            {
                if (AssetCreatorEditorWindow.IsTypeInCart(assetType))
                {
                    RaiseRemoveAssetFromCartEvent(assetType);
                }
                else
                {
                    RaiseAddAssetToCartEvent(assetType);
                }
            }
        }

        protected void RaiseAddFavoriteEvent(Type type) =>
            AddFavorite?.Invoke(type);

        protected void RaiseRemoveFavoriteEvent(Type type) =>
            RemoveFavorite?.Invoke(type);

        protected void RaiseAddAssetToCartEvent(Type type) =>
            AddAssetToCart?.Invoke(type);

        protected void RaiseRemoveAssetFromCartEvent(Type type) =>
            RemoveAssetFromCart?.Invoke(type);

        protected void RaiseCreatedAssetEvent(ScriptableObject asset) =>
            CreatedAsset?.Invoke(asset);

        protected void CreateAssetQuick(Type type)
        {
            if (type == null)
                throw new Exception("Cannot create asset from null type.");

            var path = $"Assets\\Scriptable Objects\\{type.Name}.asset";

            if (!AssetCreatorSettingsPanel.OverwriteExisting)
            {
                path = path.UniquePath(".asset");
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var instance = ScriptableObject.CreateInstance(type);

                AssetDatabase.CreateAsset(instance, path);
                AssetDatabase.SaveAssetIfDirty(instance);
                AssetDatabase.Refresh();

                CreatedAsset?.Invoke(instance);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected void CreateAsset(Type type, string path, bool multiple)
        {
            if (type == null)
                throw new Exception("Cannot create asset from null type.");

            if (string.IsNullOrEmpty(path))
            {
                path = $"Assets\\Scriptable Objects\\{type.Name}.asset";
            }

            if (multiple || !AssetCreatorSettingsPanel.OverwriteExisting)
            {
                path = path.UniquePath(".asset");
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var instance = ScriptableObject.CreateInstance(type);

                AssetDatabase.CreateAsset(instance, path);
                AssetDatabase.SaveAssetIfDirty(instance);
                AssetDatabase.Refresh();

                CreatedAsset?.Invoke(instance);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected void BeginScrollView(Rect mainRect, ref Vector2 scrollPosition, Rect viewRect)
        {
            // prevent scroll view from overflowing rect.
            mainRect.y += 2;
            mainRect.height -= 4;

            scrollPosition = GUI.BeginScrollView(mainRect, scrollPosition, viewRect, GUIStyle.none, GUIStyle.none);
        }

        #endregion

        #region Panel Implementations

        protected override void MakeBoxes()
        {
            MakeBox(PanelBoxRect);
            MakeBox(HeaderBoxRect, Color.black);
        }

        protected override void MakeRects(Vector2 size)
        {
            float offsetX = 10f;
            float offsetY = 10f;

            HeaderBoxRect = new Rect
            {
                x = offsetX + 2,
                y = offsetY + 2,
                width = size.x - offsetX - 4,
                height = (slh * 2) - 4
            };

            TitleBoxRect = new Rect
            {
                x = offsetX + 5,
                y = offsetY,
                width = PanelBoxRect.width - 15,
                height = slh * 2
            };

            PanelBoxRect = new Rect
            {
                x = offsetX,
                y = offsetY,
                width = size.x - offsetX,
                height = size.y - offsetY - 10
            };

            offsetY += 5;

            panelButtonRect = new Rect
            {
                x = PanelBoxRect.width - slh - 3,
                y = offsetY,
                width = 27,
                height = 27,
            };
        }

        #endregion
    }
}
