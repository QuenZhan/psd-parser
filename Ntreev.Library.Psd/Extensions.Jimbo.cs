using System.Collections.Generic;

namespace Ntreev.Library.Psd;

public static partial class Extensions
{
    public static IEnumerable<IPsdLayer> VisibleDescendants(this IPsdLayer layer)
    {
        return layer.Descendants(t => t is PsdLayer { IsVisible: true });
    }
    
}