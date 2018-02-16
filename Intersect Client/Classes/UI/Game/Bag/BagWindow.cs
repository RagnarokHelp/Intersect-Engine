﻿using System.Collections.Generic;
using Intersect.Client.Classes.UI.Game.Bag;
using Intersect.GameObjects;
using Intersect.Client.Classes.Localization;
using IntersectClientExtras.File_Management;
using IntersectClientExtras.GenericClasses;
using IntersectClientExtras.Gwen.Control;
using Intersect_Client.Classes.General;

namespace Intersect_Client.Classes.UI.Game
{
    public class BagWindow
    {
        private static int sItemXPadding = 4;

        private static int sItemYPadding = 4;

        //Controls
        private WindowControl mBagWindow;

        private ScrollControl mItemContainer;
        private List<Label> mValues = new List<Label>();

        public List<BagItem> Items = new List<BagItem>();

        //Init
        public BagWindow(Canvas gameCanvas)
        {
            mBagWindow = new WindowControl(gameCanvas, Strings.Bags.title, false, "BagWindow");
            mBagWindow.DisableResizing();
            Gui.InputBlockingElements.Add(mBagWindow);

            mItemContainer = new ScrollControl(mBagWindow, "ItemContainer");
            mItemContainer.EnableScroll(false, true);

            mBagWindow.LoadJsonUi(GameContentManager.UI.InGame);

            InitItemContainer();
        }

        //Location
        //Location
        public int X
        {
            get { return mBagWindow.X; }
        }

        public int Y
        {
            get { return mBagWindow.Y; }
        }

        public void Close()
        {
            mBagWindow.Close();
        }

        public bool IsVisible()
        {
            return !mBagWindow.IsHidden;
        }

        public void Hide()
        {
            mBagWindow.IsHidden = true;
        }

        public void Update()
        {
            if (mBagWindow.IsHidden == true || Globals.Bag == null)
            {
                return;
            }
            for (int i = 0; i < Globals.Bag.Length; i++)
            {
                if (Globals.Bag[i] != null && Globals.Bag[i].ItemNum > -1)
                {
                    var item = ItemBase.Lookup.Get<ItemBase>(Globals.Bag[i].ItemNum);
                    if (item != null)
                    {
                        Items[i].Pnl.IsHidden = false;

                        if (item.IsStackable())
                        {
                            mValues[i].IsHidden = false;
                            mValues[i].Text = Globals.Bag[i].ItemVal.ToString();
                        }
                        else
                        {
                            mValues[i].IsHidden = true;
                        }

                        if (Items[i].IsDragging)
                        {
                            Items[i].Pnl.IsHidden = true;
                            mValues[i].IsHidden = true;
                        }
                        Items[i].Update();
                    }
                }
                else
                {
                    Items[i].Pnl.IsHidden = true;
                    mValues[i].IsHidden = true;
                }
            }
        }

        private void InitItemContainer()
        {
            for (int i = 0; i < Globals.Bag.Length; i++)
            {
                Items.Add(new BagItem(this, i));
                Items[i].Container = new ImagePanel(mItemContainer, "BagItem");
                Items[i].Setup();

                mValues.Add(new Label(Items[i].Container, "BagItemValue"));
                mValues[i].Text = "";
                Items[i].Container.LoadJsonUi(GameContentManager.UI.InGame);

                var xPadding = Items[i].Container.Padding.Left + Items[i].Container.Padding.Right;
                var yPadding = Items[i].Container.Padding.Top + Items[i].Container.Padding.Bottom;
                Items[i].Container.SetPosition(
                    (i % (mItemContainer.Width / (Items[i].Container.Width + xPadding))) *
                    (Items[i].Container.Width + xPadding) + xPadding,
                    (i / (mItemContainer.Width / (Items[i].Container.Width + xPadding))) *
                    (Items[i].Container.Height + yPadding) + yPadding);
            }
        }

        public FloatRect RenderBounds()
        {
            FloatRect rect = new FloatRect()
            {
                X = mBagWindow.LocalPosToCanvas(new Point(0, 0)).X - sItemXPadding / 2,
                Y = mBagWindow.LocalPosToCanvas(new Point(0, 0)).Y - sItemYPadding / 2,
                Width = mBagWindow.Width + sItemXPadding,
                Height = mBagWindow.Height + sItemYPadding
            };
            return rect;
        }
    }
}