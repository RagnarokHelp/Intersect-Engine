﻿using Intersect.GameObjects;

namespace Intersect.Framework.Core.GameObjects.Crafting;

public partial class CraftIngredient
{
    public Guid ItemId { get; set; }

    public int Quantity { get; set; }

    public CraftIngredient(Guid itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }

    public ItemBase GetItem()
    {
        return ItemBase.Get(ItemId);
    }
}