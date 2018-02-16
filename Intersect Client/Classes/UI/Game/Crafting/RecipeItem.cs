﻿using System;
using Intersect.Enums;
using Intersect.GameObjects;
using IntersectClientExtras.File_Management;
using IntersectClientExtras.Graphics;
using IntersectClientExtras.Gwen.Control;
using IntersectClientExtras.Input;
using Intersect_Client.Classes.General;
using Intersect_Client.Classes.UI.Game;

namespace Intersect.Client.Classes.UI.Game.Crafting
{
    public class RecipeItem
    {
        //References
        private CraftingWindow mCraftingBenchWindow;

        public ItemDescWindow DescWindow;

        //Slot info
        CraftIngredient mIngredient;

        //Dragging
        private bool mCanDrag;

        public ImagePanel Container;
        private Draggable mDragIcon;
        public bool IsDragging;

        //Mouse Event Variables
        private bool mMouseOver;

        private int mMouseX = -1;
        private int mMouseY = -1;
        public ImagePanel Pnl;

        public RecipeItem(CraftingWindow craftingBenchWindow, CraftIngredient ingredient)
        {
            mCraftingBenchWindow = craftingBenchWindow;
            mIngredient = ingredient;
        }

        public void Setup(string name)
        {
            Pnl = new ImagePanel(Container, name);
            Pnl.HoverEnter += pnl_HoverEnter;
            Pnl.HoverLeave += pnl_HoverLeave;

           
        }

        public void LoadItem()
        {
            var item = ItemBase.Lookup.Get<ItemBase>(mIngredient.Item);

            if (item != null)
            {
                GameTexture itemTex = Globals.ContentManager.GetTexture(GameContentManager.TextureType.Item, item.Pic);
                if (itemTex != null)
                {
                    Pnl.Texture = itemTex;
                }
                else
                {
                    if (Pnl.Texture != null)
                    {
                        Pnl.Texture = null;
                    }
                }
            }
            else
            {
                if (Pnl.Texture != null)
                {
                    Pnl.Texture = null;
                }
            }
        }

        void pnl_HoverLeave(Base sender, EventArgs arguments)
        {
            mMouseOver = false;
            mMouseX = -1;
            mMouseY = -1;
            if (DescWindow != null)
            {
                DescWindow.Dispose();
                DescWindow = null;
            }
        }

        void pnl_HoverEnter(Base sender, EventArgs arguments)
        {
            mMouseOver = true;
            mCanDrag = true;
            if (Globals.InputManager.MouseButtonDown(GameInput.MouseButtons.Left))
            {
                mCanDrag = false;
                return;
            }
            if (DescWindow != null)
            {
                DescWindow.Dispose();
                DescWindow = null;
            }
            if (mIngredient != null)
            {
                DescWindow = new ItemDescWindow(mIngredient.Item, mIngredient.Quantity, mCraftingBenchWindow.X - 255,
                    mCraftingBenchWindow.Y, new int[(int) Stats.StatCount]);
            }
        }
    }
}