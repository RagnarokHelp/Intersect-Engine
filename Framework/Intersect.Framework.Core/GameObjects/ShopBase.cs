﻿using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public partial class ShopBase : DatabaseObject<ShopBase>, IFolderable
{
    [NotMapped]
    public List<ShopItem> BuyingItems { get; set; } = [];

    [NotMapped]
    public List<ShopItem> SellingItems { get; set; } = [];

    [JsonConstructor]
    public ShopBase(Guid id) : base(id)
    {
        Name = "New Shop";
    }

    //EF is so damn picky about its parameters
    public ShopBase()
    {
        Name = "New Shop";
    }

    public bool BuyingWhitelist { get; set; } = true;

    //Spawn Info
    [Column("DefaultCurrency")]
    [JsonProperty]
    public Guid DefaultCurrencyId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public ItemDescriptor DefaultCurrency
    {
        get => ItemDescriptor.Get(DefaultCurrencyId);
        set => DefaultCurrencyId = value?.Id ?? Guid.Empty;
    }

    [Column("BuyingItems")]
    [JsonIgnore]
    public string JsonBuyingItems
    {
        get => JsonConvert.SerializeObject(BuyingItems);
        set => BuyingItems = JsonConvert.DeserializeObject<List<ShopItem>>(value);
    }

    [Column("SellingItems")]
    [JsonIgnore]
    public string JsonSellingItems
    {
        get => JsonConvert.SerializeObject(SellingItems);
        set => SellingItems = JsonConvert.DeserializeObject<List<ShopItem>>(value);
    }

    public string BuySound { get; set; } = null;

    public string SellSound { get; set; } = null;

    /// <inheritdoc />
    public string Folder { get; set; } = string.Empty;
}

public partial class ShopItem
{
    public Guid CostItemId;

    public int CostItemQuantity;

    public Guid ItemId;

    [JsonConstructor]
    public ShopItem(Guid itemId, Guid costItemId, int costVal)
    {
        ItemId = itemId;
        CostItemId = costItemId;
        CostItemQuantity = costVal;
    }

    [NotMapped]
    [JsonIgnore]
    public ItemDescriptor Item => ItemDescriptor.Get(ItemId);
}
