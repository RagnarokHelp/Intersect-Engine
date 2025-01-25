using Intersect.Client.Framework.Core.Sounds;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.Items;
using Intersect.Enums;

namespace Intersect.Client.Framework.Maps;

public interface IMapInstance
{
    Guid Id { get; }
    string Name { get; }
    IReadOnlyList<IActionMessage> ActionMessages { get; }
    IReadOnlyList<IMapSound> AttributeSounds { get; }
    IMapSound BackgroundSound { get; }
    IReadOnlyList<IMapItemInstance> Items { get; }
    IReadOnlyList<IEntity> Entities { get; }
    IReadOnlyList<IMapAnimation> Animations { get; }
    IReadOnlyList<IEntity> Critters { get; }

    MapZone ZoneType { get; }

    int X { get; }
    int Y { get; }
    int GridX { get; set; }
    int GridY { get; set; }
    bool IsDisposed { get; }
    bool IsLoaded { get; }

    void AddTileAnimation(Guid animId, int tileX, int tileY, Direction dir = Direction.None, IEntity? owner = null);
    void CompareEffects(IMapInstance oldMap);
    bool InView();
    void Load(string json);
    void LoadTileData(byte[] packet);
    void Update(bool isLocal);
}
