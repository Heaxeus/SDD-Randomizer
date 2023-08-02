using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;
using HarmonyLib;
using ISurvived;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace SDDRandomizer;

[BepInPlugin("heaxeus.plugins.sddrandomizer", "SDD Randomizer", "1.0.0")]
public class Plugin : BasePlugin
{
    private static ManualLogSource _log;

    public override void Load()
    {
        _log = Log;
        // Plugin startup logic
        Log.LogInfo("SDDRandomizer is Loaded!");

        var harmony = new Harmony("SDD Randomizer");

        /*
         * Skill Randomizer and Tutorial Shortener
         */
        harmony.Patch(
            original: AccessTools.Method(AccessTools.TypeByName("ChapterThree"), "UnlockPartTwoSkills"),
            prefix: new HarmonyMethod(typeof(Plugin), nameof(UnlockPartTwoSkills_PopulateRandomSkills_Prefix))
        );

        harmony.Patch(
            original: AccessTools.Method(AccessTools.TypeByName("Game1"), "LoadGameContent"),
            postfix: new HarmonyMethod(typeof(Plugin), nameof(LoadGameContent_CreateDictionary_Postfix))
        );


        harmony.Patch(
            original: AccessTools.Method(AccessTools.TypeByName("GettingQuestOne"), "Play"),
            postfix: new HarmonyMethod(typeof(Plugin), nameof(Play_ModifyShopInventory_Postfix))
        );

        harmony.Patch(
            original: AccessTools.Method(AccessTools.TypeByName("MadSkills"), "LoadPassive"),
            postfix: new HarmonyMethod(typeof(Plugin), nameof(LoadPassive_RandomizeMadSkills_Postfix))
        );

        harmony.Patch(
            original: AccessTools.Method(AccessTools.TypeByName("Prologue"), "Update"),
            prefix: new HarmonyMethod(typeof(Plugin), nameof(SetVariablesPrologue_SkipDaydream_Prefix))
        );

        harmony.Patch(
            original: AccessTools.Method(AccessTools.TypeByName("DarkAlleyway"), "SetDestinationPortals"),
            postfix: new HarmonyMethod(typeof(Plugin), nameof(SetDestinationPortals_SkipDaydream_Postfix))
        );

        harmony.Patch(
            original: AccessTools.Method(AccessTools.TypeByName("DepositoryOfDoom"), "Update"),
            postfix: new HarmonyMethod(typeof(Plugin), nameof(Update_KillLeader_Postfix))
        );


        /*
         * Door Randomizer
         */

        harmony.Patch(
            original: AccessTools.Method(AccessTools.TypeByName("MapManager"), "CreateMaps"),
            postfix: new HarmonyMethod(typeof(Plugin), nameof(CreateMaps_OverwritePortalLocations_Postfix))
        );

        


        Log.LogInfo("Patch Called!");
    }

    /*
     * Skill Randomizer *****************************************************************
     */
    private static Type skillManagerType = typeof(SkillManager);

    private static FieldInfo allSkillsFieldInfo =
        skillManagerType.GetField("allSkills", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

    private static bool firstPass = true;

    private static Dictionary<string, Skill> randomizedSkills;

    private static Random rand = new Random();

    private static void Update_KillLeader_Postfix(DepositoryOfDoom __instance)
    {
        try
        {
            __instance.leader.Health = 0;
        }
        catch (Exception ex)
        {
            _log.LogError($"Failed in {nameof(Update_KillLeader_Postfix)}:\n\t{ex}");
        }
    }

    private static bool SetVariablesPrologue_SkipDaydream_Prefix(Prologue __instance)
    {
        try
        {
            __instance.ChapterBooleans["octopaulScenePlayed"] = true;
            __instance.ChapterBooleans["docksSceneOnePlayed"] = true;
            __instance.ChapterBooleans["docksSceneTwoPlayed"] = true;
            __instance.ChapterBooleans["depositoryColorAdded"] = true;
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError($"Failed in {nameof(SetVariablesPrologue_SkipDaydream_Prefix)}:\n\t{ex}");
            return true;
        }
    }

    private static void SetDestinationPortals_SkipDaydream_Postfix(MapClass __instance)
    {
        try
        {
            __instance.Portals.Remove(DarkAlleyway.toOldWarehouse);
            __instance.Portals.Add(DarkAlleyway.toOldWarehouse, DepositoryOfDoom.toDocks);
            DarkAlleyway.toOldWarehouse.IsUseable = true;
        }
        catch (Exception ex)
        {
            _log.LogError($"Failed in {nameof(SetDestinationPortals_SkipDaydream_Postfix)}:\n\t{ex}");
        }
    }

    private static void LoadGameContent_CreateDictionary_Postfix()
    {
        try
        {
            randomizedSkills = (Dictionary<string, Skill>)allSkillsFieldInfo.GetValue(Game1.g.SkillManager);
        }
        catch (Exception ex)
        {
            _log.LogError($"Failed in {nameof(LoadGameContent_CreateDictionary_Postfix)}:\n\t{ex}");
        }
    }

    public static void LoadPassive_RandomizeMadSkills_Postfix(MadSkills __instance)
    {
        try
        {
            Game1.g.YourLocker.SkillsOnSale.Remove(SkillManager.AllSkills["Multifaceted Approach"]);
            Game1.g.YourLocker.SkillsOnSale.Remove(SkillManager.AllSkills["Chaotic Confutation"]);
            Game1.g.YourLocker.SkillsOnSale.Remove(SkillManager.AllSkills["Startling Statements"]);
            for (var i = 0; i < 3; i++)
            {
                var randomSkill = randomizedSkills.ElementAt(rand.Next(0, randomizedSkills.Count)).Key;
                Game1.g.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills[randomSkill]);
                randomizedSkills.Remove(randomSkill);
            }
        }
        catch (Exception ex)
        {
            _log.LogError($"Failed in {nameof(LoadPassive_RandomizeMadSkills_Postfix)}:\n\t{ex}");
        }
    }

    public static bool UnlockPartTwoSkills_PopulateRandomSkills_Prefix(ChapterThree __instance)
    {
        try
        {
            for (var i = 0; i < 6; i++)
            {
                var randomSkill = randomizedSkills.ElementAt(rand.Next(0, randomizedSkills.Count)).Key;
                Game1.g.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills[randomSkill]);
                randomizedSkills.Remove(randomSkill);
            }

            return false;
        }
        catch (Exception ex)
        {
            _log.LogError($"Failed in {nameof(Play_ModifyShopInventory_Postfix)}:\n\t{ex}");
            return true;
        }
    }

    public static void Play_ModifyShopInventory_Postfix(Cutscene __instance)
    {
        try
        {
            if (__instance.State == 0 && !firstPass)
            {
                Game1.g.YourLocker.SkillsOnSale.Clear();

                for (var i = 0; i < 20; i++)
                {
                    var randomSkill = randomizedSkills.ElementAt(rand.Next(0, randomizedSkills.Count)).Key;
                    Game1.g.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills[randomSkill]);
                    randomizedSkills.Remove(randomSkill);
                }
            }

            firstPass = false;
        }
        catch (Exception ex)
        {
            _log.LogError($"Failed in {nameof(Play_ModifyShopInventory_Postfix)}:\n\t{ex}");
        }
    }


    /*
     * Door Randomizer *****************************************************************************
     */

    private static List<Portal> allSecondaryPortals = new();
    private static Random randomPortal = new();
    private static Dictionary<string, MapClass> copyOfMaps = new();


    private static string saveGameFileName = "";


    private static void SaveDoorsToFile()
    {
        saveGameFileName = Game1.g.SaveLoadManager.selectedSaveFile.fileName.Replace(".sav", "");

        if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\doors"))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\doors");
        }

        var counter = 0;
        if (File.Exists(Directory.GetCurrentDirectory() + "\\doors\\doors_" + saveGameFileName + ".xml")) return;


        var doc = new XDocument();
        doc.Add(new XElement("portals"));
        GetAllPortals();

        foreach (var map in copyOfMaps)
        {
            foreach (var portal in map.Value.Portals)
            {
                var storagePortal1 = CopySinglePortal(portal.Key);

                var storagePortal2 = CopySinglePortal(portal.Value);

                var jsonPortal1 = JsonConvert.SerializeObject(storagePortal1);
                var jsonPortal2 = JsonConvert.SerializeObject(storagePortal2);

                XDocument xNodePortal1 = JsonConvert.DeserializeXNode(jsonPortal1, "portal1");
                XDocument xNodePortal2 = JsonConvert.DeserializeXNode(jsonPortal2, "portal2");


                doc.Root.Add(new XElement("index" + counter,
                    new XElement(xNodePortal1.Root),
                    new XElement(xNodePortal2.Root)));
                counter++;
            }
        }


        doc.Save(Directory.GetCurrentDirectory() + "\\doors\\doors_" + saveGameFileName + ".xml");
    }


    private static void LoadDoorsFromFile()
    {
        try
        {
            GetAllPortals();
            var counter = 0;
            saveGameFileName = Game1.g.SaveLoadManager.selectedSaveFile.fileName.Replace(".sav", "");


            var doc = XDocument.Load(Directory.GetCurrentDirectory() + "\\doors\\doors_" + saveGameFileName + ".xml");


            while (counter < 281)
            {
                var portalElements = from portalEntry in doc.Descendants("index" + counter)
                    select new
                    {
                        portal1 = portalEntry.Element("portal1"),
                        portal2 = portalEntry.Element("portal2"),
                    };




                foreach (var portals in portalElements)
                {
                    Portal.DoorType.TryParse(portals.portal1.Element("doorType").Value, true,
                        out Portal.DoorType doorTypePortal1);
                    
                    Portal.DoorType.TryParse(portals.portal2.Element("doorType").Value, true,
                        out Portal.DoorType doorTypePortal2);

                    var portal1 = new Portal(0, 0, portals.portal1.Element("MapName").Value, doorTypePortal1)
                    {
                        doorLock = new DoorLock(portals.portal1.Element("doorLock").Value),
                        doorType = doorTypePortal1,
                        hideLock = Convert.ToBoolean(portals.portal1.Element("hideLock").Value),
                        lockTexture = null,
                        FButtonYOffset = Convert.ToInt32(portals.portal1.Element("FButtonYOffset").Value),
                        PortalNameYOffset = Convert.ToInt32(portals.portal1.Element("PortalNameYOffset").Value),
                        openingLock = Convert.ToBoolean(portals.portal1.Element("openingLock").Value),
                        lockFinished = Convert.ToBoolean(portals.portal1.Element("lockFinished").Value),
                        startedToOpen = Convert.ToBoolean(portals.portal1.Element("startedToOpen").Value),
                        PortalRec = new Rectangle(
                            Convert.ToInt32(portals.portal1.Element("PortalRec").Element("X").Value),
                            Convert.ToInt32(portals.portal1.Element("PortalRec").Element("Y").Value),
                            Convert.ToInt32(portals.portal1.Element("PortalRec").Element("Width").Value),
                            Convert.ToInt32(portals.portal1.Element("PortalRec").Element("Height").Value)),
                        PortalRecX = Convert.ToInt32(portals.portal1.Element("PortalRecX").Value),
                        PortalRecY = Convert.ToInt32(portals.portal1.Element("PortalRecY").Value),
                        MapName = portals.portal1.Element("MapName").Value,
                        IsUseable = Convert.ToBoolean(portals.portal1.Element("IsUseable").Value)
                    };

                    var portal2 = new Portal(0, 0, portals.portal2.Element("MapName").Value, doorTypePortal2)
                    {
                        doorLock = new DoorLock(portals.portal2.Element("doorLock").Value),
                        doorType = doorTypePortal2,
                        hideLock = Convert.ToBoolean(portals.portal2.Element("hideLock").Value),
                        lockTexture = null,
                        FButtonYOffset = Convert.ToInt32(portals.portal2.Element("FButtonYOffset").Value),
                        PortalNameYOffset = Convert.ToInt32(portals.portal2.Element("PortalNameYOffset").Value),
                        openingLock = Convert.ToBoolean(portals.portal2.Element("openingLock").Value),
                        lockFinished = Convert.ToBoolean(portals.portal2.Element("lockFinished").Value),
                        startedToOpen = Convert.ToBoolean(portals.portal2.Element("startedToOpen").Value),
                        PortalRec = new Rectangle(
                            Convert.ToInt32(portals.portal2.Element("PortalRec").Element("X").Value),
                            Convert.ToInt32(portals.portal2.Element("PortalRec").Element("Y").Value),
                            Convert.ToInt32(portals.portal2.Element("PortalRec").Element("Width").Value),
                            Convert.ToInt32(portals.portal2.Element("PortalRec").Element("Height").Value)),
                        PortalRecX = Convert.ToInt32(portals.portal2.Element("PortalRecX").Value),
                        PortalRecY = Convert.ToInt32(portals.portal2.Element("PortalRecY").Value),
                        MapName = portals.portal2.Element("MapName").Value,
                        IsUseable = Convert.ToBoolean(portals.portal2.Element("IsUseable").Value)
                    };


                    Game1.mapManager.maps[portals.portal1.Element("MapName").Value].Portals[portal1] = portal2;
                    Game1.mapManager.maps[portals.portal2.Element("MapName").Value].Portals[portal2] = portal1;
                    
                    break;
                }

                counter++;
            }

            
        }
        catch (Exception e)
        {
            _log.LogMessage("Loading File!" + e);
        }
    }

    private static Type mapClassType = typeof(MapClass);

    private static FieldInfo backgroundFieldInfo =
        mapClassType.GetField("background", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Portal CopySinglePortal(Portal portal)
    {
        return new Portal(portal.PortalRecX, (int)(portal.PortalRecY + Game1.g.portalSize.Y), portal.MapName,
            ref portal.doorLock)
        {
            doorLock = portal.doorLock,
            doorType = portal.doorType,
            hideLock = portal.hideLock,
            lockTexture = portal.lockTexture,
            FButtonYOffset = portal.FButtonYOffset,
            PortalNameYOffset = portal.PortalNameYOffset,
            openingLock = portal.openingLock,
            lockFinished = portal.lockFinished,
            startedToOpen = portal.startedToOpen,
            PortalRec = portal.PortalRec,
            PortalRecX = portal.PortalRecX,
            PortalRecY = portal.PortalRecY,
            MapName = portal.MapName,
            IsUseable = portal.IsUseable
        };
    }

    private static Dictionary<Portal, Portal> CopyDictPortals(Dictionary<Portal, Portal> portals)
    {
        Dictionary<Portal, Portal> newPortals = new();

        foreach (var kvPortals in portals)
        {
            var portal1 = CopySinglePortal(kvPortals.Key);
            var portal2 = CopySinglePortal(kvPortals.Value);
            newPortals[portal1] = portal2;
        }

        return newPortals;
    }


    private static void GetAllPortals()
    {
        var player = Game1.g.Player;
        try
        {
            copyOfMaps.Clear();
            allSecondaryPortals.Clear();
            foreach (var map in Game1.mapManager.maps)
            {
                if (map.Key is "Dark Alleyway" or "Old Warehouse" or "Detective Docks" or "Depository of Doom" or "Bathroom")
                    continue;
                copyOfMaps.Add(map.Key.Clone().ToString(),
                    new MapClass((List<Texture2D>)backgroundFieldInfo.GetValue(map.Value), Game1.g, ref player)
                    {
                        Portals = CopyDictPortals(map.Value.Portals),
                        EnemiesInMap = map.Value.EnemiesInMap,
                        cameraOffset = map.Value.cameraOffset,
                        currentBackgroundMusic = map.Value.currentBackgroundMusic,
                        Player = map.Value.Player,
                        Platforms = map.Value.Platforms,
                        Drops = map.Value.Drops,
                        StoryItems = map.Value.StoryItems,
                        Lockers = map.Value.Lockers,
                        TreasureChests = map.Value.TreasureChests,
                        yScroll = map.Value.yScroll,
                        MapNameTimer = map.Value.MapNameTimer,
                        mapName = map.Value.MapName,
                        soundEmitters = map.Value.soundEmitters,
                        HealthDrops = map.Value.HealthDrops,
                        Switches = map.Value.Switches,
                        MapQuestSigns = map.Value.MapQuestSigns,
                        mapWithMapQuest = map.Value.mapWithMapQuest,
                        mapHazards = map.Value.mapHazards,
                        Collectibles = map.Value.Collectibles,
                        InteractiveObjects = map.Value.InteractiveObjects,
                        Projectiles = map.Value.Projectiles,
                        MoneyDrops = map.Value.MoneyDrops,
                        EnemyNamesAndNumberInMap = map.Value.EnemyNamesAndNumberInMap,
                        doorLocks = map.Value.doorLocks,
                        ZoomLevel = map.Value.ZoomLevel,
                        displayName = map.Value.displayName,
                        Discovered = map.Value.Discovered,
                        CurrentPickedItem = map.Value.CurrentPickedItem,
                        mapRec = map.Value.mapRec,
                        OverlayType = map.Value.OverlayType
                    });

                foreach (var portals in map.Value.Portals.ToList())
                {
                    allSecondaryPortals.Add(CopySinglePortal(portals.Value));
                }
            }

            foreach (var map in copyOfMaps.ToList())
            {
                Game1.mapManager.maps[map.Key].Portals.Clear();
            }
        }
        catch (Exception e)
        {
            _log.LogMessage("GetAllPortals" + e);
            
        }
    }

    private static int testCounter = 0;


    private static void CreateMaps_OverwritePortalLocations_Postfix(MapManager __instance)
    {
        try
        {
            
            saveGameFileName = Game1.g.SaveLoadManager.selectedSaveFile.fileName.Replace(".sav", "");
            if (File.Exists(Directory.GetCurrentDirectory() + "\\doors\\doors_" + saveGameFileName + ".xml"))
            {
                _log.LogMessage("LOADED!");
                LoadDoorsFromFile();
                return;
            }

            GetAllPortals();
            foreach (var map in copyOfMaps.ToList())
            {
                foreach (var portalPair in map.Value.Portals.ToList())
                {
                    var portal1 = portalPair.Key;

                    try
                    {
                        var portal2 =
                            allSecondaryPortals.ElementAt(randomPortal.Next(0, allSecondaryPortals.Count));
                        allSecondaryPortals.Remove(portal2);
                        foreach (var portal in allSecondaryPortals.ToList())
                        {
                            if (portal.MapName == portal1.MapName)
                            {
                                allSecondaryPortals.Remove(portal);
                                break;
                            }
                        }
                        __instance.maps[portal1.MapName].Portals[portal1] = portal2;
                        __instance.maps[portal2.MapName].Portals[portal2] = portal1;
                        copyOfMaps[portal1.MapName].Portals.Remove(portal1);
                        copyOfMaps[portal2.MapName].Portals.Remove(portal2);
                        _log.LogMessage("Portal1: " + portal1.MapName + " Portal2: " + portal2.MapName + "\n");
                        // testCounter++;
                        // if (testCounter == 40) break;
                        
                    }
                    catch (Exception e)
                    {
                        _log.LogError("Creating maps!" + e);
                    }
                }

                //if (testCounter == 40) break;
            }
            
            SaveDoorsToFile();
            LoadDoorsFromFile();
        }
        catch (Exception ex)
        {
            _log.LogError($"Failed in {nameof(CreateMaps_OverwritePortalLocations_Postfix)}:\n\t{ex}");
        }
    }
}