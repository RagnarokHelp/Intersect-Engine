﻿using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public partial class TilesetBase : DatabaseObject<TilesetBase>
{
    [JsonConstructor]
    public TilesetBase(Guid id) : base(id)
    {
        Name = string.Empty;
    }

    //Ef Parameterless Constructor
    public TilesetBase()
    {
        Name = string.Empty;
    }

    public new string Name
    {
        get => base.Name;
        set => base.Name = value?.Trim().ToLower();
    }
}
