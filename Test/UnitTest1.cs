using System.Diagnostics;
using ImageMagick;
using Ntreev.Library.Psd;
using TRNTHPsd;

namespace Test;

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
            
            Directory.Delete(midProductFolder, true);
            psd.MidProductFolder=midProductFolder;
            using var toMerge=psd.Merge("toMerge");
            const string path = "./outputShort.png";
            if (toMerge != null) await toMerge.WriteAsync(path, MagickFormat.Png);
            Assert.That(File.Exists(path), Is.True);
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

    [Test]
    public void Test2()
    {
        Directory.Delete("./midProduct",true);
        Directory.CreateDirectory("./midProduct");
        using var psd = new PsdController("/Users/jimbo/gitRepos/DungeonMunchies2DevContent/GUI/SettingUI.psd"){MidProductFolder = "./midProduct"};
        var theLayer=psd.Document.Childs.Single(t=>t.Name=="SettingUIAll").Childs.Single(t=>t.Name=="[merge]SettingBG");
        using var mergedImage=psd.Merge(theLayer.VisibleDescendants());
        Debug.Assert(mergedImage != null, nameof(mergedImage) + " != null");
        mergedImage.Write("./settingOutput.png",MagickFormat.Png);
        Assert.That(File.Exists("./settingOutput.png"),Is.True);

    }

    [Test]
    public void GroupByClipping()
    {
        const string filepath = "./example.psd";
        const string midProductFolder = "./midProduct";
        Directory.CreateDirectory(midProductFolder);
        using var psd = new PsdController(filepath){MidProductFolder =  midProductFolder};
        var folder=psd.Document.Childs.Single(t => t.Name == "Clipping");
        var groupByClipping = PsdController.GroupByClipping(folder.Childs).Reverse().ToArray();
        Assert.That(groupByClipping, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(groupByClipping[0].First().Name, Is.EqualTo("圖層 1"));
            Assert.That(groupByClipping[1].First().Name, Is.EqualTo("圖層 8"));
        });
    }
}