using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Ikonoclast.AssetCreator.Editor
{
    using Ikonoclast.Common.Editor;

    using static Ikonoclast.Common.Editor.PanelUtils;
    using static Ikonoclast.Common.Editor.CustomEditorHelper;
    using System.IO;

    internal sealed class AssetCreatorCartPanel : AssetCreatorPanel
    {
        private sealed class CartItem
        {
            private string _path = null;

            public Type Type
            {
                get;
            }

            public int Amount
            {
                get;
                set;
            }

            public string Path
            {
                get => _path;
                set
                {
                    _path = value;

                    if (_path != null)
                    {
                        int index = _path.IndexOf("Assets/");

                        if (index != -1)
                        {
                            _path = _path.Substring(index);
                        }

                        DisplayPath = _path.Remove(0, _path.LastIndexOf('/') + 1);
                    }
                }
            }

            public string DisplayPath
            {
                get;
                private set;
            }

            public CartItem(Type type, int amount)
            {
                Type = type ?? throw new NotSupportedException($"{nameof(CartItem)} type cannot be null.");

                Amount = amount;

                if (Amount <= 0)
                    throw new NotSupportedException($"{nameof(CartItem)} amount cannot be lower than 1");
            }
        }

        #region Fields

        private Rect
            emptyButtonRect,
            createButtonRect,
            cartItemAmountRect;

        private string lastDirectory = null;

        private readonly List<CartItem>
            cartItems = new List<CartItem>();

        private readonly GUIContent
            cartItemTooltip = new GUIContent
            {
                text = string.Empty,
                tooltip = string.Empty
            };

        private readonly GUIStyle
            cartItemAmountStyle = new GUIStyle(BaseStyle)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };

        #endregion

        #region Properties

        public int Count =>
            cartItems.Count;

        #endregion

        #region Constructors

        ~AssetCreatorCartPanel()
        {
            Unsubscribe();
        }

        public AssetCreatorCartPanel()
        {
            Subscribe();
        }

        #endregion

        #region Event Listeners

        private void OnRequestAddAssetToCart(Type type)
        {
            var found = cartItems.Find(item => item.Type == type);

            if (found != null)
            {
                found.Amount++;

                return;
            }

            cartItems.Add(new CartItem(type, 1));
        }

        private void OnRequestRemoveAssetFromCart(Type type)
        {
            var found = cartItems.Find(item => item.Type == type);

            if (found != null)
            {
                cartItems.Remove(found);
            }
        }

        #endregion

        #region Methods

        private void Subscribe()
        {
            AddAssetToCart += OnRequestAddAssetToCart;
            RemoveAssetFromCart += OnRequestRemoveAssetFromCart;
        }

        private void Unsubscribe()
        {
            AddAssetToCart -= OnRequestAddAssetToCart;
            RemoveAssetFromCart -= OnRequestRemoveAssetFromCart;
        }

        public bool IsTypeInCart(Type type) =>
            cartItems.Exists(item => item.Type == type);

        #endregion

        #region AssetCreatorPanel Implementations

        protected override string Title =>
            "Asset Cart";

        protected override void MakeBoxes()
        {
            base.MakeBoxes();

            MakeBox(mainRect);
        }

        public override void OnPanelGUI(Vector2 size)
        {
            if (Count == 0)
            {
                AssetCreatorEditorWindow.OpenExplorerPanel();
            }

            MakeRects(size);
            MakeBoxes();

            GUI.Label(TitleBoxRect, Title, TitleStyle);

            EditorGUIUtility.SetIconSize(panelButtonIconSize);

            IconButton
            (
                panelButtonRect,
                () => AssetCreatorEditorWindow.OpenExplorerPanel(),
                UnityIcons.Search,
                "Open the asset explorer."
            );

            bool removeItem = false;

            BeginScrollView(mainRect, ref scrollViewPosition, scrollViewRect);

            for (int i = 0; i < cartItems.Count; ++i)
            {
                MakeBox(itemRect, Color.gray);
                MakeBox(cartItemAmountRect);

                var item = cartItems[i];

                cartItemTooltip.tooltip = item.DisplayPath;

                GUI.Label(itemRect, cartItemTooltip);

                EditorGUIUtility.SetIconSize(mediumIconSize);

                GUI.Label(itemIconRect, UnityIcons.ScriptableObject);

                GUI.Label(itemNameRect, item.Type.Name, EditorStyles.boldLabel);

                GUI.Label(cartItemAmountRect, item.Amount.ToString(), cartItemAmountStyle);

                EditorGUIUtility.SetIconSize(smallIconSize);

                IconButton
                (
                    itemButtonRect,
                    () =>
                    {
                        item.Path =
                            EditorUtility.SaveFilePanel(
                                "Select",
                                lastDirectory ?? "Assets/Scriptable Objects",
                                item.Type.Name,
                                "asset")
                            ?? item.Path;

                        lastDirectory = string.IsNullOrEmpty(item.Path)
                            ? "Assets/Scriptable Objects"
                            : Path.GetDirectoryName(item.Path);
                    },
                    string.IsNullOrEmpty(item.Path)
                        ? UnityIcons.FolderEmpty
                        : UnityIcons.Folder,
                    "Select the path to create the asset at."
                );

                EditorGUI.BeginDisabledGroup(item.Amount == 1);

                itemButtonRect.x += 25;

                IconButton
                (
                    itemButtonRect,
                    () =>
                    {
                        item.Amount--;

                        if (item.Amount <= 0)
                        {
                            removeItem = true;
                        }
                    },
                    UnityIcons.PrevTab
                );

                EditorGUI.EndDisabledGroup();

                itemButtonRect.x += 50;

                IconButton
                (
                    itemButtonRect,
                    () => item.Amount++,
                    UnityIcons.NextTab
                );

                itemButtonRect.x += 35;

                IconButton
                (
                    itemButtonRect,
                    () => removeItem = true,
                    UnityIcons.Close,
                    "Remove the asset from the cart."
                );

                itemButtonRect.x -= 110;

                if (removeItem)
                {
                    cartItems.RemoveAt(i);

                    return;
                }

                itemRect.y += slh * 2 + 5;

                itemIconRect.y =
                itemNameRect.y =
                itemButtonRect.y =
                cartItemAmountRect.y = itemRect.y + 4;
            }

            GUI.EndScrollView();

            if (GUI.Button(emptyButtonRect, "Empty"))
            {
                cartItems.Clear();
            }
            else if (GUI.Button(createButtonRect, "Create"))
            {
                RequestCreateAssetsInCart();
            }

            var evt = Event.current;

            if (evt == null)
                return;

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.C)
            {
                RequestCreateAssetsInCart();
            }

            void RequestCreateAssetsInCart()
            {
                foreach (var item in cartItems)
                {
                    if (item.Amount == 1)
                    {
                        CreateAsset(item.Type, item.Path, false);
                    }
                    else
                    {
                        for (int i = 0; i < item.Amount; ++i)
                        {
                            CreateAsset(item.Type, item.Path, true);
                        }
                    }
                }

                cartItems.Clear();
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
                height = (PanelBoxRect.height - offsetY) - slh
            };

            scrollViewRect = new Rect
            {
                width = mainRect.width - offsetX - 10,
                height = ((slh * 2 + 5) * cartItems.Count) + 14
            };

            offsetX = 10;
            offsetY = 10;

            itemRect = new Rect
            {
                x = offsetX,
                y = offsetY,
                width = scrollViewRect.width,
                height = slh * 2
            };

            offsetX += 5;
            offsetY += 4;

            itemIconRect = new Rect
            {
                x = offsetX,
                y = offsetY,
                width = slh * 1.6f,
                height = slh * 1.6f
            };

            offsetX += slh * 1.6f + 10;

            itemNameRect = new Rect
            {
                x = offsetX,
                y = offsetY,
                width = itemRect.width - 100,
                height = slh * 1.6f
            };

            cartItemAmountRect = new Rect
            {
                x = itemRect.width - 80,
                y = itemRect.y + 6,
                width = slh * 1.3f,
                height = slh * 1.3f
            };

            itemButtonRect = new Rect(cartItemAmountRect)
            {
                x = cartItemAmountRect.x - 50,
            };

            emptyButtonRect = new Rect
            {
                x = mainRect.x,
                y = PanelBoxRect.height - slh - 4.75f,
                width = mainRect.width * 0.5f + 1,
                height = itemRect.height - 5
            };
            createButtonRect = new Rect
            {
                x = emptyButtonRect.x + emptyButtonRect.width + 1,
                y = PanelBoxRect.height - slh - 4.75f,
                width = emptyButtonRect.width - 2,
                height = emptyButtonRect.height
            };
        }

        #endregion
    }
}
