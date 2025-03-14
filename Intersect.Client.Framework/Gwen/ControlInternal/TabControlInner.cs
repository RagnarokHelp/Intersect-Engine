﻿using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Framework.Gwen.ControlInternal;


/// <summary>
///     Inner panel of tab control.
/// </summary>
public partial class TabControlInner : Base
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TabControlInner" /> class.
    /// </summary>
    /// <param name="parent">Parent control.</param>
    /// <param name="name"></param>
    internal TabControlInner(Base parent, string? name = default) : base(parent, name: name)
    {
    }

    /// <summary>
    ///     Renders the control using specified skin.
    /// </summary>
    /// <param name="skin">Skin to use.</param>
    protected override void Render(Skin.Base skin)
    {
        skin.DrawTabControl(this);
    }

}
