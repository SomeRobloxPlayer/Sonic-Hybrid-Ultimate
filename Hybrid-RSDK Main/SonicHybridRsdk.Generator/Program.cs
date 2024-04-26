﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static SonicHybridRsdk.Generator.Global;
using static SonicHybridRsdk.Generator.RsdkGenericImporter;
using static SonicHybridRsdk.Generator.RsdkSonicCdImporter;

namespace SonicHybridRsdk.Generator
{
    enum StageType
    {
        StagesPresentation,
        StagesRegular,
        StagesSpecial,
    }

    class Context
    {
        public string SrcPath { get; init; }
        public string DstPath { get; init; }
        public IGameConfig SrcConfig { get; init; }
        public IGameConfig DstConfig { get; init; }
        public Dictionary<int, GameObject> SrcObjects { get; init; }
        public Dictionary<string, int> DstObjects { get; init; }
        public Dictionary<string, string> Replacements { get; init; }
    }

    public class Program
    {
        static void Main(string[] args) => Generate(args[0], args[1]);

        public static void CopyResources(string sourceDataRsdk, string destinationDataRsdk)
        {
            var sonic1Path = Path.Combine(sourceDataRsdk, "sonic1/Data");
            var sonicCdPath = Path.Combine(sourceDataRsdk, "soniccd/Data");
            var sonic2Path = Path.Combine(sourceDataRsdk, "sonic2/Data");
            var sonicHybridPath = Path.Combine(destinationDataRsdk, "Data");
            var sonicHybridCustomPath = Path.Combine(destinationDataRsdk, "Data-Custom");

            foreach (var folder in new string[]
            {
                "Animations",
                "Game",
                "Music",
                "Palettes",
                "SoundFX",
                "Sprites",
            })
            {
                Copy(Path.Combine(sonic1Path, folder), Path.Combine(sonicHybridPath, folder));
                Copy(Path.Combine(sonicCdPath, folder), Path.Combine(sonicHybridPath, folder));
                Copy(Path.Combine(sonic2Path, folder), Path.Combine(sonicHybridPath, folder));
            }

            foreach (var (SourcePath, DestinationPath) in new (string, string)[]
            {
                ("Sprites/Global/Items2.gif", "Sprites/Global/Items4.gif"),
                ("Sprites/Global/Display.gif", "Sprites/Global/Display2.gif"),
            })
                File.Copy(
                    Path.Combine(sonic1Path, SourcePath),
                    Path.Combine(sonicHybridPath, DestinationPath),
                    true);

            foreach (var (SourcePath, DestinationPath) in new (string, string)[]
            {
                ("Sprites/Global/Items.gif", "Sprites/Global/ItemsCD.gif"),
                ("Animations/MetalSonic.Ani", "Animations/MetalSonicBoss.Ani"),
            })
                File.Copy(
                    Path.Combine(sonicCdPath, SourcePath),
                    Path.Combine(sonicHybridPath, DestinationPath),
                    true);

            foreach (var folderPath in Directory.GetDirectories(sonicHybridCustomPath))
            {
                var folderName = Path.GetFileName(folderPath);
                Copy(
                    Path.Combine(sonicHybridCustomPath, folderName),
                    Path.Combine(sonicHybridPath, Path.GetFileName(folderPath)));
            }
        }

        public static void Generate(string sourceDataRsdk, string destinationDataRsdk)
        {
            CopyResources(sourceDataRsdk, destinationDataRsdk);

            var sonic1Path = Path.Combine(sourceDataRsdk, "sonic1/Data");
            var sonicCdPath = Path.Combine(sourceDataRsdk, "soniccd/Data");
            var sonic2Path = Path.Combine(sourceDataRsdk, "sonic2/Data");
            var sonicHybridPath = Path.Combine(destinationDataRsdk, "Data");

            var sonic1Config = OpenRead(Path.Combine(sonic1Path, "Game/GameConfig.bin"), GameConfig.Read);
            var sonicCdConfig = OpenRead(Path.Combine(sonicCdPath, "Game/GameConfig.bin"), GameConfigV3.Read);
            var sonic2Config = OpenRead(Path.Combine(sonic2Path, "Game/GameConfig.bin"), GameConfig.Read);

            var sonicHybridConfig = new GameConfig
            {
                Name = "Sonic Hybrid Ultimate",
                Description = $"Hack by Xeeynamo\n\n{sonic1Config.Description}",
                PaletteData = sonic2Config.PaletteData,
                StagesPresentation = new List<Stage>(),
                StagesRegular = new List<Stage>(),
                StagesBonus = new List<Stage>(),
                StagesSpecial = new List<Stage>(),
            };

            var hybridObjects = new Dictionary<string, GameObject>();
            foreach (var obj in sonic2Config.GameObjects)
                hybridObjects.Add(obj.Name, obj);
            foreach (var obj in sonic1Config.GameObjects)
            {
                if (!hybridObjects.ContainsKey(obj.Name))
                    hybridObjects.Add(obj.Name, obj);
            }
            sonicHybridConfig.GameObjects = hybridObjects.Values.ToList();

            var dicHybridObjects = sonicHybridConfig.GameObjects.Select((x, i) => (Id: i, Obj: x)).ToDictionary(x => x.Obj.Name, x => x.Id);
            dicHybridObjects["Lamp Post"] = dicHybridObjects["Star Post"]; // Sonic 1
            dicHybridObjects["LampPost"] = dicHybridObjects["Star Post"]; // Sonic CD
            dicHybridObjects["SignPost"] = dicHybridObjects["Sign Post"]; // Sonic CD
            dicHybridObjects["Flower Pod"] = dicHybridObjects["Animal Prison"]; // Sonic CD
            dicHybridObjects["Future Post"] = dicHybridObjects["Star Post"]; // TODO HACK
            dicHybridObjects["Past Post"] = dicHybridObjects["Star Post"]; // TODO HACK
            dicHybridObjects["Transporter"] = dicHybridObjects["Ring"]; // TODO HACK
            dicHybridObjects["Goal Post"] = dicHybridObjects["Ring"]; // TODO HACK
            dicHybridObjects["MSProjector"] = dicHybridObjects["Ring"]; // TODO HACK

            var context1 = new Context
            {
                SrcPath = sonic1Path,
                DstPath = sonicHybridPath,
                SrcConfig = sonic1Config,
                DstConfig = sonicHybridConfig,
                SrcObjects = sonic1Config.GameObjects.Select((x, i) => (Id: i, Obj: x)).ToDictionary(x => x.Id, x => x.Obj),
                DstObjects = dicHybridObjects,
                Replacements = new()
                {
                    ["Special/PlayerObject.txt"] = "Special/PlayerObject1.txt",
                    ["Special/SpecialSetup.txt"] = "Special/SpecialSetup1.txt",
                    ["Special/SpecialFinish.txt"] = "Special/SpecialFinish1.txt",
                    ["Special/ChaosEmerald.txt"] = "Special/ChaosEmerald1.txt",
                }
            };

            var contextCd = new Context
            {
                SrcPath = sonicCdPath,
                DstPath = sonicHybridPath,
                SrcConfig = sonicCdConfig,
                DstConfig = sonicHybridConfig,
                SrcObjects = sonicCdConfig.GameObjects.Select((x, i) => (Id: i, Obj: x)).ToDictionary(x => x.Id, x => x.Obj),
                DstObjects = dicHybridObjects,
                Replacements = new()
                {
                }
            };

            var context2 = new Context
            {
                SrcPath = sonic2Path,
                DstPath = sonicHybridPath,
                SrcConfig = sonic2Config,
                DstConfig = sonicHybridConfig,
                SrcObjects = sonic2Config.GameObjects.Select((x, i) => (Id: i, Obj: x)).ToDictionary(x => x.Id, x => x.Obj),
                DstObjects = dicHybridObjects,
                Replacements = new()
                {
                    ["Special/PlayerObject.txt"] = "Special/PlayerObject2.txt",
                    ["Special/SpecialSetup.txt"] = "Special/SpecialSetup2.txt",
                    ["Special/SpecialFinish.txt"] = "Special/SpecialFinish2.txt",
                    ["Special/ChaosEmerald.txt"] = "Special/ChaosEmerald2.txt",
                }
            };

            var variables = new Dictionary<string, int>();
            foreach (var item in sonic1Config.Variables)
                variables[item.Name] = item.Value;
            foreach (var item in sonicCdConfig.Variables)
                variables[item.Name] = item.Value;
            foreach (var item in sonic2Config.Variables)
                variables[item.Name] = item.Value;
            variables["stage.gameid"] = 0;
            sonicHybridConfig.Variables = variables.Select(x => new Variable { Name = x.Key, Value = x.Value }).ToList();

            sonicHybridConfig.Players = sonic2Config.Players;
            sonicHybridConfig.Players.Add("METAL SONIC");

            sonicHybridConfig.SoundEffects = sonic2Config.SoundEffects;

            UseStageV4(context2, StageType.StagesPresentation, "TITLE SCREEN SONIC 2", 1, "Title", "TitleS2");
            UseStageV4(context2, StageType.StagesPresentation, "ENDING SONIC 2", 1, "Ending", "EndingS2");
            UseStageV4(context2, StageType.StagesPresentation, "STAFF CREDITS SONIC 2", 1, "Credits", "CreditsS2");
            UseStageV4(context2, StageType.StagesPresentation, "LEVEL SELECT SONIC 2", 1, "LSelect", "LSelectS2");
            UseStageV4(context2, StageType.StagesPresentation, "LEVEL SELECT 2P", 2, "LSelect", "LSelectS2");
            UseStageV4(context2, StageType.StagesPresentation, "CONTINUE SCREEN SONIC 1", 1, "Continue", "ContinueS1");

            UseStageV4(context1, StageType.StagesPresentation, "TITLE SCREEN SONIC 1", 1, "Title", "TitleS1");
            UseStageV4(context1, StageType.StagesPresentation, "ENDING SONIC 1", 1, "Ending", "EndingS1");
            UseStageV4(context1, StageType.StagesPresentation, "STAFF CREDITS SONIC 1", 1, "Credits", "CreditsS1");
            UseStageV4(context1, StageType.StagesPresentation, "UNLOCK ALL ACHIEVEMENTS", 2, "Credits", "CreditsS1");
            UseStageV4(context1, StageType.StagesPresentation, "CONTINUE SCREEN SONIC 1", 1, "Continue", "ContinueS1");
            UseStageV4(context1, StageType.StagesPresentation, "LEVEL SELECT SONIC 1", 1, "LSelect", "LSelectS1");

            UseStageV4(context1, StageType.StagesRegular, "GREEN HILL ZONE", 1, "Zone01", "ZoneGHZ");
            UseStageV4(context1, StageType.StagesRegular, "GREEN HILL ZONE", 2, "Zone01", "ZoneGHZ");
            UseStageV4(context1, StageType.StagesRegular, "GREEN HILL ZONE", 3, "Zone01", "ZoneGHZ");
            UseStageV4(context1, StageType.StagesRegular, "MARBLE ZONE", 1, "Zone02", "ZoneMZ");
            UseStageV4(context1, StageType.StagesRegular, "MARBLE ZONE", 2, "Zone02", "ZoneMZ");
            UseStageV4(context1, StageType.StagesRegular, "MARBLE ZONE", 3, "Zone02", "ZoneMZ");
            UseStageV4(context1, StageType.StagesRegular, "SPRING YARD ZONE", 1, "Zone03", "ZoneSYZ");
            UseStageV4(context1, StageType.StagesRegular, "SPRING YARD ZONE", 2, "Zone03", "ZoneSYZ");
            UseStageV4(context1, StageType.StagesRegular, "SPRING YARD ZONE", 3, "Zone03", "ZoneSYZ");
            UseStageV4(context1, StageType.StagesRegular, "LABYRINTH ZONE", 1, "Zone04", "ZoneLZ");
            UseStageV4(context1, StageType.StagesRegular, "LABYRINTH ZONE", 2, "Zone04", "ZoneLZ");
            UseStageV4(context1, StageType.StagesRegular, "LABYRINTH ZONE", 3, "Zone04", "ZoneLZ");
            UseStageV4(context1, StageType.StagesRegular, "STARLIGHT ZONE", 1, "Zone05", "ZoneSZ");
            UseStageV4(context1, StageType.StagesRegular, "STARLIGHT ZONE", 2, "Zone05", "ZoneSZ");
            UseStageV4(context1, StageType.StagesRegular, "STARLIGHT ZONE", 3, "Zone05", "ZoneSZ");
            UseStageV4(context1, StageType.StagesRegular, "SCRAP BRAIN ZONE", 1, "Zone06", "ZoneSBZ");
            UseStageV4(context1, StageType.StagesRegular, "SCRAP BRAIN ZONE", 2, "Zone06", "ZoneSBZ");
            UseStageV4(context1, StageType.StagesRegular, "SCRAP BRAIN ZONE", 4, "Zone04", "ZoneLZ", visualActNumber: 3);
            UseStageV4(context1, StageType.StagesRegular, "FINAL ZONE", 5, "Zone06", "ZoneSBZ", visualActNumber: 0);

            var SonicCDStageNames = new[]
            {
                "PALMTREE PANIC",
                "DESERT DAZZLE",
                "COLLISION CHAOS",
                "TIDAL TEMPEST",
                "QUARTZ QUADRANT",
                "WACKY WORKBENCH",
                "STARDUST SPEEDWAY",
                "METALLIC MADNESS",
            };
            var SonicCDTimeZones = new[]
            {
                "PRESENT",
                "PAST",
                "GOOD FUTURE",
                "BAD FUTURE",
            };
            for (var zone = 1; zone <= SonicCDStageNames.Length; zone++)
            {
                if (zone == 2) // Ignore R2
                    continue;

                var stageName = $"{SonicCDStageNames[zone - 1]} ZONE";
                var stageShortName = new string(stageName.Split(' ').Select(x => x.First()).ToArray());
                for (var act = 1; act <= 3; act++)
                {
                    for (var timeZoneId = 0; timeZoneId < SonicCDTimeZones.Length; timeZoneId++)
                    {
                        if (act == 3 && timeZoneId < 2) // Act 3 does not contain PRESENT or PAST
                            continue;

                        var timeZone = (char)('A' + timeZoneId);
                        var srcFolder = $"R{zone}{act}{timeZone}";
                        var dstFolder = $"Zone{stageShortName}{act}{timeZone}";
                        UseStageV3(contextCd, StageType.StagesRegular, stageName, act, srcFolder, dstFolder, SonicCDTimeZones[timeZoneId]);
                    }
                }
            }

            UseStageV4(context2, StageType.StagesRegular, "EMERALD HILL ZONE", 1, "Zone01", "ZoneEHZ");
            UseStageV4(context2, StageType.StagesRegular, "EMERALD HILL ZONE", 2, "Zone01", "ZoneEHZ");
            UseStageV4(context2, StageType.StagesRegular, "CHEMICAL PLANT ZONE", 1, "Zone02", "ZoneCPZ");
            UseStageV4(context2, StageType.StagesRegular, "CHEMICAL PLANT ZONE", 2, "Zone02", "ZoneCPZ");
            UseStageV4(context2, StageType.StagesRegular, "AQUATIC RUIN ZONE", 1, "Zone03", "ZoneARZ");
            UseStageV4(context2, StageType.StagesRegular, "AQUATIC RUIN ZONE", 2, "Zone03", "ZoneARZ");
            UseStageV4(context2, StageType.StagesRegular, "CASINO NIGHT ZONE", 1, "Zone04", "ZoneCNZ");
            UseStageV4(context2, StageType.StagesRegular, "CASINO NIGHT ZONE", 2, "Zone04", "ZoneCNZ");
            UseStageV4(context2, StageType.StagesRegular, "HILL TOP ZONE", 1, "Zone05", "ZoneHTZ");
            UseStageV4(context2, StageType.StagesRegular, "HILL TOP ZONE", 2, "Zone05", "ZoneHTZ");
            UseStageV4(context2, StageType.StagesRegular, "MYSTIC CAVE ZONE", 1, "Zone06", "ZoneMCZ");
            UseStageV4(context2, StageType.StagesRegular, "MYSTIC CAVE ZONE", 2, "Zone06", "ZoneMCZ");
            UseStageV4(context2, StageType.StagesRegular, "OIL OCEAN ZONE", 1, "Zone07", "ZoneOOZ");
            UseStageV4(context2, StageType.StagesRegular, "OIL OCEAN ZONE", 2, "Zone07", "ZoneOOZ");
            UseStageV4(context2, StageType.StagesRegular, "HIDDEN PALACE ZONE", 1, "Zone08", "ZoneHPZ");
            UseStageV4(context2, StageType.StagesRegular, "METROPOLIS ZONE", 1, "Zone09", "ZoneMPZ");
            UseStageV4(context2, StageType.StagesRegular, "METROPOLIS ZONE", 2, "Zone09", "ZoneMPZ");
            UseStageV4(context2, StageType.StagesRegular, "METROPOLIS ZONE", 3, "Zone09", "ZoneMPZ");
            UseStageV4(context2, StageType.StagesRegular, "SKY CHASE ZONE", 1, "Zone10", "ZoneSCZ", visualActNumber: 0);
            UseStageV4(context2, StageType.StagesRegular, "WING FORTRESS ZONE", 1, "Zone11", "ZoneWFZ", visualActNumber: 0);
            UseStageV4(context2, StageType.StagesRegular, "DEATH EGG ZONE", 1, "Zone12", "ZoneDEZ", visualActNumber: 0);

            for (var i = 1; i <= 8; i++)
                UseStageV4(context2, StageType.StagesSpecial, "SPECIAL STAGE", i, "Special", "Special2");
            for (var i = 1; i <= 6; i++)
                UseStageV4(context1, StageType.StagesSpecial, "SPECIAL STAGE", i, "Special", "Special1");

            Create(Path.Combine(destinationDataRsdk, "Data/Game/GameConfig.bin"), sonicHybridConfig.Write);
        }

        private static void UseStageV4(
            Context context,
            StageType stageType,
            string name,
            int actNumber,
            string srcFolder,
            string dstFolder,
            int visualActNumber = -1)
        {
            var stages = context.SrcConfig.GetStages(stageType);
            if (visualActNumber < 0)
                visualActNumber = actNumber;

            var srcStage = stages.First(x => x.Act == actNumber.ToString() && x.Path == srcFolder);
            var dstStage = new Stage
            {
                Name = visualActNumber > 0 ? $"{name} {visualActNumber}" : name,
                Act = actNumber.ToString(),
                Mode = srcStage.Mode,
                Path = dstFolder,
            };

            var srcPath = Path.Combine(context.SrcPath, "Stages", srcFolder);
            var dstPath = Path.Combine(context.DstPath, "Stages", dstFolder);
            Directory.CreateDirectory(dstPath);

            File.Copy(Path.Combine(srcPath, "16x16Tiles.gif"), Path.Combine(dstPath, "16x16Tiles.gif"), true);
            File.Copy(Path.Combine(srcPath, "128x128Tiles.bin"), Path.Combine(dstPath, "128x128Tiles.bin"), true);
            File.Copy(Path.Combine(srcPath, "Backgrounds.bin"), Path.Combine(dstPath, "Backgrounds.bin"), true);
            File.Copy(Path.Combine(srcPath, "CollisionMasks.bin"), Path.Combine(dstPath, "CollisionMasks.bin"), true);
            PatchStageConfig(context,
                StageConfig.Read,
                Path.Combine(srcPath, "StageConfig.bin"),
                Path.Combine(dstPath, "StageConfig.bin"));
            PatchStage(context,
                StageAct.Read,
                Path.Combine(srcPath, $"Act{actNumber}.bin"),
                Path.Combine(dstPath, $"Act{actNumber}.bin"),
                (context, entity, name) =>
                {
                    switch (name)
                    {
                        case "Title Card":
                            entity.PropertyValue = (byte)(visualActNumber > 0 ? visualActNumber : 4);
                            break;
                        default:
                            return false;
                    }

                    return true;
                });

            var background = OpenRead(Path.Combine(srcPath, "Backgrounds.bin"), StageBackgroundV4.Read);
            Create(Path.Combine(dstPath, "Backgrounds.bin"), background.Write);

            context.DstConfig.GetStages(stageType).Add(dstStage);
        }
    }
}
