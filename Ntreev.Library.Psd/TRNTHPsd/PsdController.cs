using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using Ntreev.Library.Psd;
using Console = System.Console;
using GC = System.GC;
using IDisposable = System.IDisposable;

// ReSharper disable once CheckNamespace
namespace TRNTHPsd;

public class PsdController:IDisposable
{
    private readonly MagickImageCollection _magickImageCollection;

    public PsdController(string psdFilePath)
    {
        Document = PsdDocument.Create(psdFilePath);
        _magickImageCollection = new MagickImageCollection(psdFilePath);
        Dictionary= Document.CreateLayerToImageDictionary(_magickImageCollection);
    }

    public IReadOnlyDictionary<IPsdLayer, IMagickImage<ushort>> Dictionary { get;  }
    public PsdDocument Document { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Document.Dispose();
        _magickImageCollection.Dispose();
    }

    public IMagickImage<ushort>? Merge(IEnumerable<IPsdLayer> layers)
    {
        return MergeLayers(Dictionary, layers.SelectMany(t=>t.VisibleDescendants()), MidProductFolder);
    }

    public string? MidProductFolder { get; set; }

    private static void Composite(IMagickImage<ushort> image, IPsdLayer layer,
        IMagickImage<ushort> canvas, int composingIndex, string? midProductFolder, int hashCode=0)
    {
        image.Evaluate(Channels.RGB
            , EvaluateOperator.Multiply, layer.Opacity); // pre multiply alpha
        var compositeOperator = layer.IsClipping ? CompositeOperator.Atop : layer.BlendMode==BlendMode.ColorDodge?CompositeOperator.ColorDodge: CompositeOperator.Over;
        if(layer.HasImage)canvas.Composite(image, layer.Left, layer.Top, compositeOperator);
        else canvas.Composite(image,  compositeOperator);
        Console.WriteLine($"[Composite\t]\t{layer.Name} {compositeOperator}");
        if (string.IsNullOrEmpty(midProductFolder)) return;
        image.WriteAsync($"{midProductFolder}/{hashCode}_{composingIndex}_image.png", MagickFormat.Png);
        canvas.WriteAsync($"{midProductFolder}/{hashCode}_{composingIndex}_{layer.Name}_{layer.Opacity*100:000}_{compositeOperator}.png", MagickFormat.Png);
    }

    private static IEnumerable<IEnumerable<IPsdLayer>> GroupByClipping(IPsdLayer[] array)
    {
        var list = new List<IPsdLayer>(){array[^1]};
        for (var index = array.Length - 2; index >= 0; index--)
        {
            var current = array[index];
            if (list.Count>0 && current.IsClipping != list[^1].IsClipping)
            {
                if(!current.IsClipping)list.Add(current);
                yield return list.ToArray().Reverse(); 
                list.Clear();
            }
            list.Add(current);
        }

        yield return list.ToArray().Reverse();
    }

    public static IMagickImage<ushort>? MergeLayers(IReadOnlyDictionary<IPsdLayer, IMagickImage<ushort>> allImages, IEnumerable<IPsdLayer> psdLayers, string? midProductFolder)
    {
        var layersToMerge = psdLayers as IPsdLayer[] ?? psdLayers.ToArray();
        if (layersToMerge.Length <= 0)
        {
            return null;
        }
        var document = layersToMerge[0].Document;
        var imagesClone = allImages.ToDictionary(t => t.Key, t => t.Value);
        var hashCode = layersToMerge.GetHashCode();
        var composingIndex = 0;
        var folders = layersToMerge.GroupBy(t => t.Parent ?? document)
                .Where(t => layersToMerge.Contains(t.Key))
                .OrderByDescending(t => t.Key.Ancestors().Count());
        foreach (var folder in folders)
        {
            Console.WriteLine($"Merge Folder :{folder.Key.Name}");
            var canvas1 = document.CreateEmptyCanvas();
            var reverse = GroupByClipping(folder.ToArray()).Reverse().ToArray();
            foreach (var layer in reverse.First())
            {
                if (imagesClone.TryGetValue(layer, out var image1)) Composite(image1, layer, canvas1, composingIndex++, midProductFolder, hashCode);
            }

            foreach (var clippingGroup in reverse.Skip(1))
            {
                var layers1 = clippingGroup.ToArray();
                using var empty = document.CreateEmptyCanvas();
                foreach (var layer in layers1)
                {
                    if (imagesClone.TryGetValue(layer, out var image2)) Composite(image2, layer, canvas1, composingIndex++, midProductFolder, hashCode);
                }

                Composite(empty, layers1[0], canvas1, composingIndex++, midProductFolder, hashCode);
            }

            imagesClone[folder.Key] = canvas1;
        }

        var canvas = document.CreateEmptyCanvas();
        foreach (var e in layersToMerge.GroupBy(t => t.Ancestors().Count()).OrderBy(t => t.Key).First().ToArray())
        {
            if (imagesClone.TryGetValue(e, out var image)) Composite(image, e, canvas, composingIndex++, midProductFolder, hashCode);
        }

        foreach (var e in imagesClone.Values.Except(allImages.Values.Append(canvas)))
        {
            e.Dispose();
        }
        return canvas;
    }

    public IMagickImage<ushort>? Merge(string layerName)
    {
        return Merge(Document.VisibleDescendants().Where(t => t.Name == layerName));
    }

    public static IMagickImage<ushort>? MergeLayers(IReadOnlyDictionary<IPsdLayer, IMagickImage<ushort>> allImages,
        IEnumerable<IPsdLayer> psdLayers, IPsdLayer? toCrop = null)
    {
        // var keepMidProduct = _texturePackerController.KeepMidProduct;

        const string usersJimboDownloadsTest = "/Users/jimbo/Downloads/test";
        var keepMidProduct = Directory.Exists(usersJimboDownloadsTest);
        var canvas = PsdController.MergeLayers(allImages, psdLayers, keepMidProduct?usersJimboDownloadsTest:null);
        if (toCrop is null) return canvas;
        if (canvas == null) return null;
        canvas.Crop(new MagickGeometry(toCrop.Left, toCrop.Top, (uint)toCrop.Width, (uint)toCrop.Height));
        canvas.ResetPage();
        return canvas;

    }
}