using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Serialization;
using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;
using HarmonyLib;
using ISurvived;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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

        Log.LogInfo("Patch Called!");
    }

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
}