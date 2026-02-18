using System;
using System.Collections.Generic;
using System.Linq;
using ImageMagick;
using Ntreev.Library.Psd;

namespace TRNTHPsd;

public static class Extenstion
{
    public static Dictionary<IPsdLayer, IMagickImage<ushort>> CreateLayerToImageDictionary(this PsdDocument document, MagickImageCollection imageCollection)
    {
        return document.Descendants().Where(t=>t.HasImage && t is PsdLayer).Distinct().Select((t, i)=>new {Layer=t,Image=imageCollection[i+1]}).ToDictionary(t=>t.Layer,t=>t.Image);
    }

    public static IEnumerable<IPsdLayer> Ancestors(this IPsdLayer psdLayer)
    {
        // yield return psdLayer;
        while (psdLayer is { Parent: PsdLayer parent})
        {
            yield return parent;
            psdLayer = parent;
        }
    }

    public static MagickImage CreateEmptyCanvas(this PsdDocument document)
    {
        Console.WriteLine("[New Empty]");
        return new MagickImage(new MagickColor(0,0,0,0),(uint)document.Width, (uint)document.Height);
    }
}