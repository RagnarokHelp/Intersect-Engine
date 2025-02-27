﻿

using Microsoft.Extensions.Logging;

namespace Intersect.Plugins;

/// <summary>
/// Convenience abstract class that defines commonly used properties for certain plugin helpers.
/// </summary>
public abstract partial class PluginHelper
{
    /// <summary>
    /// The <see cref="Logger"/> for this helper to use.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes this <see cref="PluginHelper"/>.
    /// </summary>
    /// <param name="logger">The <see cref="Logger"/> for this helper to use.</param>
    protected PluginHelper(ILogger logger)
    {
        Logger = logger;
    }
}
