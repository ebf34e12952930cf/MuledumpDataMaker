using Accord.Imaging.Filters;
using MuledumpDataMaker.Imaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AImage = Accord.Imaging.Image;

namespace MuledumpDataMaker
{
    internal static class Program
    {
        private static readonly List<ItemData> items = new List<ItemData>();
        private static readonly List<ClassData> classes = new List<ClassData>();
        private static readonly List<SkinData> skins = new List<SkinData>();
        private static readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private static void Log(string text)
        {
            if (text.StartsWith("ERROR"))
                Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{Math.Round(stopwatch.Elapsed.TotalSeconds, 1):##.0}] {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            /*  new epicHiveObjectsCXML(), new lostHallsObjectsCXML(), new oryxScribeObjectsCXML(),
            new cnidarianReefObjectsCXML(), new gemLordObjectCXML(), new testAtrapperObjectCXML(), new stSorcAndHuntObjectsCXML(),
            new bearCaveObjectCXML(), new goblinLairObjectCXML(), new KrathTestObjectsCXML(), new KrathMuTestObjectsCXML()];*/
            var objectXmlPaths = new[]
            {
                "projectiles.xml", "equip.xml", "skins.xml", "dungeons/testAtrapper/testAtrapperSkins.xml",
                "dungeons/stSorcAndHunt/stSorcAndHuntSkins.xml", "dyes.xml", "textiles.xml", "permapets.xml",
                "token.xml", "testing/willemTesting.xml", "testing/ttesting.xml", "testing/btesting.xml",
                "testing/stesting.xml", "testing/mtesting.xml", "testing/ktesting.xml", "players.xml", "containers.xml",
                "objects.xml", "portals.xml", "testingObjects.xml", "staticobjects.xml", "tutorial/tutorialObjects.xml",
                "tutorial/tutorialMonsters.xml", "allies.xml", "heroes.xml", "playersZombies.xml", "pets.xml",
                "npc.xml", "realm/shore.xml", "realm/low.xml", "realm/mid.xml", "realm/high.xml", "realm/mountains.xml",
                "encounters.xml", "arena.xml", "dungeons/oryxCastle.xml", "dungeons/tombOfTheAncients.xml",
                "dungeons/spriteWorld.xml", "dungeons/undeadLair.xml", "dungeons/oceanTrench.xml",
                "dungeons/forbiddenJungle.xml", "dungeons/oryxChamber.xml", "dungeons/oryxChickenChamber.xml",
                "dungeons/oryxWineCellar.xml", "dungeons/manorOfTheImmortals.xml", "dungeons/pirateCave.xml",
                "dungeons/snakePit.xml", "dungeons/spiderDen.xml", "dungeons/abyssOfDemons.xml",
                "dungeons/ghostShip.xml", "dungeons/madLab.xml", "dungeons/caveOfAThousandTreasures.xml",
                "dungeons/candyLand.xml", "dungeons/hauntedCemetery.xml", "dungeons/forestMaze.xml",
                "dungeons/epicForestMaze.xml", "dungeons/epicPirateCave.xml", "dungeons/epicSpiderDen.xml",
                "dungeons/nexusDestroyed.xml", "dungeons/miniDungeonHub.xml", "dungeons/lairOfDraconis.xml",
                "dungeons/lairOfShaitan.xml", "dungeons/shatters.xml", "dungeons/belladonna.xml",
                "dungeons/puppetMaster.xml", "dungeons/iceCave.xml", "dungeons/theHive.xml", "dungeons/toxicSewers.xml",
                "dungeons/puppetMasterEncore.xml", "dungeons/iceTomb.xml",
                "dungeons/parasiteDen/parasiteDenObjects.xml", "dungeons/stPatricks/stPatricksObjects.xml",
                "dungeons/buffedBunny/buffedBunnyObjects.xml", "dungeons/hanamiNexus/hanamiNexusObjects.xml",
                "dungeons/mountainTemple/mountainTempleObjects.xml", "dungeons/oryxHorde/oryxHordeObjects.xml",
                "dungeons/summerNexus/summerNexusObjects.xml", "dungeons/autumnNexus/autumnNexusObjects.xml",
                "dungeons/epicHive/epicHiveObjects.xml", "dungeons/lostHalls/lostHallsObjects.xml",
                "dungeons/oryxScribe/oryxScribeObjects.xml", "dungeons/cnidarianReef/cnidarianReefObjects.xml",
                "dungeons/magicWoods/magicWoodsObjects.xml", "dungeons/santaWorkshop/santaWorkshopObjects.xml",
                "dungeons/gemLord/gemLordObjects.xml", "dungeons/testAtrapper/testAtrapperObjects.xml",
                "dungeons/stSorcAndHunt/stSorcAndHuntObjects.xml", "dungeons/bearCave/bearCaveObjects.xml",
                "dungeons/goblinLair/goblinLairObjects.xml", "dungeons/krathTest/krathTestObjects.xml",
                "dungeons/krathTest/krathMuTestObjects.xml"
            };

            foreach (var file in objectXmlPaths.Select(o => new FileInfo($"xml/{o}")))
            {
                if (!file.Exists)
                {
                    Log($"ERROR: {file.FullName} doesn't exist");
                    continue;
                }
                load(file);
            }

            Log($"{items.Count} items");

            var rendersBitmap = new Bitmap(40 * 25, Math.Max(items.Count / 25, 1) * 40);
            Log(
                $"Output bitmap: {rendersBitmap.Width} * {rendersBitmap.Height} ({rendersBitmap.Width * rendersBitmap.Height}px)");

            var dict = new Dictionary<int, List<object>>
            {
                {-1, new List<object> {"empty slot", 0, -1, 0, 0, 0, 0}}
            };

            var bitmapSheetByName = new Dictionary<string, BitmapSheet>();
            var addedSprites = new Dictionary<string, Dictionary<ushort, int>>();
            var index = 1;
            foreach (var item in items)
            {
                var increment = true;

                var sheet = new FileInfo($"sheets/{item.Sheet}.png");
                if (!sheet.Exists)
                {
                    Log($"ERROR: sheet {sheet.Name} doesn't exist");
                    break;
                }

                var sheetBitmap = AImage.FromFile(sheet.FullName);
                if (!bitmapSheetByName.ContainsKey(item.Sheet))
                {
                    Log(
                        $"{item.Sheet} bitmap: {sheetBitmap.Width} * {sheetBitmap.Height} ({sheetBitmap.Width * sheetBitmap.Height}px)");

                    int size;
                    bool animated;
                    switch (sheetBitmap.Width)
                    {
                        case 8 * 16:
                            size = 8;
                            animated = false;
                            break;

                        case 8 * 7:
                            size = 8;
                            animated = true;
                            break;

                        case 16 * 16:
                            size = 16;
                            animated = false;
                            break;

                        case 16 * 7:
                            size = 16;
                            animated = true;
                            break;

                        default:
                            Log($"ERROR: unknown width: {sheetBitmap.Width}");
                            continue;
                    }
                    bitmapSheetByName.Add(item.Sheet,
                        BitmapSheet.FromImage(sheet.FullName, 5, 4, size, false, false, false));
                    Log(
                        $"{item.Sheet} - size {size} animated {animated} indexes {bitmapSheetByName[item.Sheet].Bitmaps.Keys.Max()}");
                }

                // Log($"{item.Id}: {item.Sheet}.{item.Index}");

                if (!addedSprites.ContainsKey(item.Sheet))
                    addedSprites.Add(item.Sheet, new Dictionary<ushort, int>());
                int x, y;
                if (!addedSprites[item.Sheet].ContainsKey(item.Index))
                {
                    addedSprites[item.Sheet].Add(item.Index, index);
                    indexToCoords(index, rendersBitmap.Width, out x, out y);
                    rendersBitmap.Add(
                        bitmapSheetByName[item.Sheet].Bitmaps[
                            item.Animated
                                ? (ushort)(item.Index * (item.Sheet.Contains("player") ? 21 : 7))
                                : item.Index], x, y);
                }
                else
                {
                    indexToCoords(addedSprites[item.Sheet][item.Index], rendersBitmap.Width, out x, out y);
                    increment = false;
                }

                dict.Add(item.Type, new List<object>
                {
                    item.Id,
                    item.SlotType,
                    item.Tier,
                    x,
                    y,
                    item.FameBonus,
                    item.FeedPower
                });
                if (increment)
                    index++;
            }
            rendersBitmap.SavePng("renders.png");
            rendersBitmap.Dispose();

            var classesDict = classes.ToDictionary<ClassData, int, List<object>>(classData => classData.Type,
                classData => new List<object>
                {
                    classData.Id ?? "",
                    classData.Starts ?? new ushort[0],
                    classData.Averages ?? new ushort[0],
                    classData.Maxes ?? new ushort[0],
                    classData.Slots ?? new ushort[0]
                });
            File.WriteAllText("constants.js",
                $@"items = {
                        JsonConvert.SerializeObject(dict, new JsonSerializerSettings {Formatting = Formatting.Indented})
                    }

classes = {JsonConvert.SerializeObject(classesDict, new JsonSerializerSettings {Formatting = Formatting.Indented})}

skins = {{
{string.Join("\n", skins.Select(skin => $"  0x{skin.Type:x}: {skin.Index}, // {skin.Id}"))}
}}");

            var skinBitmap = new Bitmap(56, (skins.Count + 14) * 24);
            var maskBitmap = new Bitmap(56, (skins.Count + 14) * 24);
            addedSprites.Clear();
            index = 0;

            foreach (var classData in classes)
            {
                var sheet = new FileInfo($"sheets/{classData.Sheet}.png");
                if (!sheet.Exists)
                {
                    Log($"ERROR: sheet {sheet.Name} doesn't exist");
                    break;
                }
                var mask = new FileInfo($"sheets/{classData.Sheet}Mask.png");
                var maskSheet = AImage.FromFile(mask.FullName);

                var sheetBitmap = AImage.FromFile(sheet.FullName);
                var crop = new Crop(new Rectangle(0, classData.SheetIndex * 24, 56, 24));
                var piece = crop.Apply(sheetBitmap);

                skinBitmap.Add(piece, 0, index * 24);

                crop = new Crop(new Rectangle(0, classData.SheetIndex * 24, 56, 24));
                piece = crop.Apply(maskSheet);
                maskBitmap.Add(piece, 0, index * 24);

                index++;
            }
            index = 0;
            foreach (var skinData in skins.OrderBy(skin => skin.Index))
            {
                var sheet = new FileInfo($"sheets/{skinData.Sheet}.png");
                if (!sheet.Exists)
                {
                    Log($"ERROR: sheet {sheet.Name} doesn't exist");
                    break;
                }
                var mask = new FileInfo($"sheets/{skinData.Sheet}Mask.png");
                var maskSheet = AImage.FromFile(mask.FullName);

                var sheetBitmap = AImage.FromFile(sheet.FullName);
                var crop = new Crop(new Rectangle(0, skinData.SheetIndex * 24, 56, 24));
                var piece = crop.Apply(sheetBitmap);

                skinBitmap.Add(piece, 0, (index + 14) * 24);

                crop = new Crop(new Rectangle(0, skinData.SheetIndex * 24, 56, 24));
                piece = crop.Apply(maskSheet);
                maskBitmap.Add(piece, 0, (index + 14) * 24);

                index++;
            }
            using (var ms = new MemoryStream())
            {
                skinBitmap.Save(ms, ImageFormat.Png);
                File.WriteAllText("sheets-skin.js", $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}");
            }
            using (var ms = new MemoryStream())
            {
                maskBitmap.Save(ms, ImageFormat.Png);
                File.WriteAllText("sheets-skinmask.js",
                    $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}");
            }
            skinBitmap.SavePng("skins.png");
            maskBitmap.SavePng("skinsmask.png");
        }

        private static void indexToCoords(int index, int width, out int x, out int y)
        {
            x = 40;
            y = 0;

            while (index > 0)
            {
                x += 40;
                if (x >= width)
                {
                    x = 0;
                    y += 40;
                }
                --index;
            }
        }

        private static void load(FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                var xml = XElement.Load(stream);

                Log($"{file.Name}: {xml.Name.LocalName} ({xml.Elements().Count()})");

                foreach (var obj in xml.Elements("Object"))
                {
                    loadObject(obj);
                }
            }
        }

        private static readonly string[] stats =
            {"MaxHitPoints", "MaxMagicPoints", "Attack", "Defense", "Speed", "Dexterity", "HpRegen", "MpRegen"};

        private static void loadObject(XElement obj)
        {
            var type = ushort.Parse(obj.Attribute("type").Value.Substring(2), NumberStyles.HexNumber);
            var id = obj.Attribute("id").Value;
            if (obj.Element("Item") != null)
            {
                var animated = obj.Element("AnimatedTexture") != null;
                var texture = animated ? obj.Element("AnimatedTexture") : obj.Element("Texture");
                var data = new ItemData
                {
                    Type = type,
                    Id = id,

                    SlotType = byte.Parse(obj.Element("SlotType")?.Value ?? byte.MaxValue.ToString()),
                    Tier = sbyte.Parse(obj.Element("Tier")?.Value ?? "-1"),
                    FameBonus = byte.Parse(obj.Element("FameBonus")?.Value ?? "0"),
                    FeedPower = uint.Parse(obj.Element("feedPower")?.Value ?? "0"),

                    Animated = animated,
                    Sheet = texture.Element("File").Value,
                    Index =
                        texture.Element("Index").Value.StartsWith("0x")
                            ? ushort.Parse(texture.Element("Index").Value.Substring(2), NumberStyles.HexNumber)
                            : ushort.Parse(texture.Element("Index").Value)
                };
                var sheet = new FileInfo($"sheets/{data.Sheet}.png");
                if (!sheet.Exists)
                {
                    Log($"ERROR: sheet {sheet.Name} doesn't exist");
                    return;
                }
                items.Add(data);
            }
            if (obj.Element("Player") != null)
            {
                var animated = obj.Element("AnimatedTexture") != null;
                var texture = animated ? obj.Element("AnimatedTexture") : obj.Element("Texture");
                var data = new ClassData
                {
                    Type = type,
                    Id = id,

                    Starts = stats.Select(stat => ushort.Parse(obj.Element(stat).Value)).ToArray(),
                    Averages = obj.Elements("LevelIncrease").Select(li =>
                    {
                        var min = double.Parse(li.Attribute("min").Value);
                        var max = double.Parse(li.Attribute("max").Value);
                        var start = double.Parse(obj.Element(li.Value).Value);
                        //Log($"{li.Value}: min {min} max {max} start {start}");
                        return (ushort)(start + 19 * ((min + max) / 2));
                    }).ToArray(),
                    Maxes = stats.Select(stat => ushort.Parse(obj.Element(stat).Attribute("max").Value)).ToArray(),

                    Slots = obj.Element("SlotTypes").Value.Split(new[] {", "}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(ushort.Parse).Take(4).ToArray(),

                    Animated = animated,
                    Sheet = texture.Element("File").Value,
                    SheetIndex =
                        texture.Element("Index").Value.StartsWith("0x")
                            ? ushort.Parse(texture.Element("Index").Value.Substring(2), NumberStyles.HexNumber)
                            : ushort.Parse(texture.Element("Index").Value)
                };
                var sheet = new FileInfo($"sheets/{data.Sheet}.png");
                if (!sheet.Exists)
                {
                    Log($"ERROR: sheet {sheet.Name} doesn't exist");
                    return;
                }
                // Log($"new class {data.Id} ({data.Type:x})");
                classes.Add(data);
            }
            if (obj.Element("Skin") != null)
            {
                var animated = obj.Element("AnimatedTexture") != null;
                var texture = animated ? obj.Element("AnimatedTexture") : obj.Element("Texture");
                if (texture.Element("File").Value.Contains("16"))
                    return;
                var data = new SkinData
                {
                    Type = type,
                    Id = id,
                    Index = SkinData.Counter++,

                    Sheet = texture.Element("File").Value,
                    SheetIndex =
                        texture.Element("Index").Value.StartsWith("0x")
                            ? ushort.Parse(texture.Element("Index").Value.Substring(2), NumberStyles.HexNumber)
                            : ushort.Parse(texture.Element("Index").Value),
                    Animated = animated
                };
                var sheet = new FileInfo($"sheets/{data.Sheet}.png");
                if (!sheet.Exists)
                {
                    Log($"ERROR: sheet {sheet.Name} doesn't exist");
                    return;
                }
                skins.Add(data);
                // Log($"new skin {data.Id} ({data.Type:x})");
            }
        }
    }

    /* type: [id, slot, tier, x, y, famebonus, feedpower]*/

    internal class ItemData
    {
        public ushort Type { get; set; }
        public string Id { get; set; }

        public byte SlotType { get; set; }
        public sbyte Tier { get; set; }
        public byte FameBonus { get; set; }
        public uint FeedPower { get; set; }

        public string Sheet { get; set; }
        public ushort Index { get; set; }
        public bool Animated { get; set; }
    }

    /* type: [id, starts, averages, maxes, slots] */

    internal class ClassData
    {
        public ushort Type { get; set; }
        public string Id { get; set; }

        public ushort[] Starts { get; set; }
        public ushort[] Averages { get; set; }
        public ushort[] Maxes { get; set; }
        public ushort[] Slots { get; set; }

        public string Sheet { get; set; }
        public ushort SheetIndex { get; set; }
        public bool Animated { get; set; }
    }

    internal class SkinData
    {
        public ushort Type { get; set; }
        public string Id { get; set; }
        public ushort Index { get; set; }

        public string Sheet { get; set; }
        public ushort SheetIndex { get; set; }
        public bool Animated { get; set; }

        public static ushort Counter = 0;
    }
}