using ImageMagick;
using Ntreev.Library.Psd;
using TRNTHPsd;

namespace PsdTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public Task Test1()
    {
        const string filepath = "./example.psd";
        const string midProductFolder = "./midProduct";
        Directory.CreateDirectory(midProductFolder);
        using var psd = new PsdController(filepath);
        Assert.That(psd.Document.VisibleDescendants().ToArray(),Has.Length.LessThan(psd.Document.Descendants().Count()));

       
        var psdFolderLayer=psd.Document.VisibleDescendants().First(t => t.Name == "toMerge");

        Assert.Multiple(async () =>
        {
            Directory.Delete(midProductFolder, true);
            Directory.CreateDirectory(midProductFolder);
            psd.MidProductFolder = null;
            using var noMidProduct=psd.Merge(psdFolderLayer.VisibleDescendants());
            Assert.That(psd.Merge(Array.Empty<IPsdLayer>()) , Is.Null);
            Assert.That(Directory.GetFiles(midProductFolder), Has.Length.EqualTo(0));
            
            psd.MidProductFolder=midProductFolder;
            using var mergedImage=psd.Merge(psdFolderLayer.VisibleDescendants());
            const string fileName = "./testOutput.png";
            if (mergedImage != null)
            {
                await mergedImage.WriteAsync(fileName,MagickFormat.Png);
            }
            Assert.That(File.Exists(fileName), Is.True);
            Assert.That(Directory.GetFiles(midProductFolder), Has.Length.GreaterThan(3));

            var psdLayer = psd.Document.VisibleDescendants().First(t=>t.Name=="toCrop");
            using var cropped=psd.Merge(psdFolderLayer.VisibleDescendants(),psdLayer);
            Assert.That(cropped?.Width,Is.EqualTo(psdLayer.Width));
        });
        return Task.CompletedTask;
    }

    [Test]
    public void BasicUsage()
    {
        using var psd = new PsdController("./example.psd");
        using var mergedImage=psd.Merge("toMerge");
        mergedImage?.Write("./output.png",MagickFormat.Png);
        
        Assert.That(File.Exists("./output.png"),Is.True);
    }
}