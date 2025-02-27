using Intersect.Framework.Annotations;

namespace Intersect.Config;

/// <summary>
/// Contains configurable options pertaining to stat/metrics collecting
/// </summary>
public partial class MetricsOptions
{
    /// <summary>
    /// Track game performance metrics
    /// </summary>
    [RequiresRestart]
    public bool Enable { get; set; } = false;
}
