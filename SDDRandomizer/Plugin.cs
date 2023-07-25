using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;
using HarmonyLib;
using ISurvived;
using Microsoft.Xna.Framework;

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
            original: AccessTools.Method(AccessTools.TypeByName("GettingQuestOne"), "Play"),
            prefix: new HarmonyMethod(typeof(Plugin), nameof(Play_OverwriteChapterOneSkillShop_Prefix))
        );
        Log.LogInfo("Patch Called!");
    }


    private static Type typeCutscene = typeof(Cutscene);

    private static MethodInfo methodPlay = typeCutscene.GetMethod("Play", BindingFlags.Instance | BindingFlags.Public);

    private static FieldInfo gameFieldInfo =
        typeCutscene.GetField("game", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo playerFieldInfo =
        typeCutscene.GetField("player", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo stateFieldInfo =
        typeCutscene.GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo firstFrameFieldInfo =
        typeCutscene.GetField("firstFrameOfTheState", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo timerFieldInfo =
        typeCutscene.GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo cameraFieldInfo =
        typeCutscene.GetField("camera", BindingFlags.Instance | BindingFlags.NonPublic);


    private static Type typeGettingQuestOne = AccessTools.TypeByName("GettingQuestOne");

    private static FieldInfo alanFieldInfo =
        typeGettingQuestOne.GetField("alan", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo paulFieldInfo =
        typeGettingQuestOne.GetField("paul", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo givenBookFieldInfo =
        typeGettingQuestOne.GetField("givenBook", BindingFlags.Instance | BindingFlags.NonPublic);
    
    
    private static Type typeChapter = typeof(Chapter);
    
    private static FieldInfo cutsceneFieldInfo =
        typeChapter.GetField("currentScene", BindingFlags.Instance | BindingFlags.Public);
    
    private static Type typeGame = typeof(Game1);

    private static FieldInfo chapterFieldInfo =
        typeGame.GetField("currentChapter", BindingFlags.Instance | BindingFlags.Public);


    //public override void Play()
    public static bool Play_OverwriteChapterOneSkillShop_Prefix(Cutscene __instance)
    {
        try
        {
            //_log.LogInfo("Overwrite Method is Running!");
            
            var player = (Player)playerFieldInfo.GetValue(__instance);
            var state = (int)stateFieldInfo.GetValue(__instance);
            var firstFrameOfTheState = (bool)firstFrameFieldInfo.GetValue(__instance);
            var alan = (NPC)alanFieldInfo.GetValue(__instance);
            var paul = (NPC)paulFieldInfo.GetValue(__instance);
            var givenBook = (bool)givenBookFieldInfo.GetValue(__instance);
            var timer = (int)timerFieldInfo.GetValue(__instance);
            var camera = (Camera)cameraFieldInfo.GetValue(__instance);
            var cutscene = (Cutscene)cutsceneFieldInfo.GetValue(__instance);
            var chapter = (Chapter)chapterFieldInfo.GetValue(cutscene);
            var game = (Game1)gameFieldInfo.GetValue(chapter);
            
            



            methodPlay.Invoke(cutscene, null);
            switch (state)
            {
                case -5:
                    if (!givenBook)
                    {
                        Sound.PlayGlobalSoundEvent("object_pickup_textbook");
                        Chapter.effectsManager.AddFoundItem("Textbook", Game1.equipmentTextures["Textbook"]);
                        player.playerState = Player.PlayerState.relaxedStanding;
                        givenBook = true;
                    }

                    __instance.FadeOut(60f);
                    Chapter.effectsManager.Update();
                    // return true;
                    break;
                case -4:
                    if (firstFrameOfTheState)
                    {
                        game.SideQuestManager.nPCs["Skill Sorceress"].RecX = 1011;
                        game.SideQuestManager.nPCs["Skill Sorceress"].RecY = 287;
                        game.SideQuestManager.nPCs["Skill Sorceress"].PositionX = 1011f;
                        game.SideQuestManager.nPCs["Skill Sorceress"].PositionY = 287f;
                        game.SideQuestManager.nPCs["Skill Sorceress"].MapName = "Dwarves & Druids Club";
                        game.SideQuestManager.nPCs["Skill Sorceress"].FacingRight = false;
                        alan = game.CurrentChapter.NPCs["Alan"];
                        paul = game.CurrentChapter.NPCs["Paul"];
                        alan.RemoveQuest((Quest)game.ChapterOne.ReturningKeys);
                        alan.ClearDialogue();
                        paul.ClearDialogue();
                        paul.MapName = "North Hall";
                        alan.MapName = "North Hall";
                        player.playerState = Player.PlayerState.relaxedStanding;
                        player.FacingRight = false;
                        player.PositionX = 3000f;
                        player.UpdatePosition();
                    }

                    camera.StartUpdate((GameObject)player, game, game.CurrentChapter.CurrentMap);
                    Chapter.effectsManager.Update();
                    if (timer <= 60)
                        break;
                    ++state;
                    timer = 0;
                    // return true;
                    break;
                case -3:
                    Chapter.effectsManager.Update();
                    __instance.FadeIn(60f);
                    camera.StartUpdate((GameObject)player, game, game.CurrentChapter.CurrentMap);
                    Chapter.effectsManager.fButtonRecs.Clear();
                    Chapter.effectsManager.foregroundFButtonRecs.Clear();
                    // return true;
                    break;
                case -2:
                    if (firstFrameOfTheState)
                    {
                        alan.Dialogue.Add("Oh, hey, it's Daniel!");
                        alan.Talking = true;
                    }

                    Sound.ChangeBackgroundMusicWithFade(Sound.MusicNames.paulandalantheme, 25f);
                    camera.StartUpdate((GameObject)player, game, game.CurrentChapter.CurrentMap);
                    if (alan.Talking)
                        alan.UpdateInteraction();
                    if (alan.RecX < 2760)
                    {
                        paul.Move(new Vector2(3f, 0.0f));
                        alan.Move(new Vector2(3f, 0.0f));
                    }
                    else
                    {
                        paul.moveState = NPC.MoveState.standing;
                        alan.moveState = NPC.MoveState.standing;
                    }

                    if (alan.Talking || alan.RecX < 2760)
                    {
                        // return true;
                        break;
                    }

                    paul.moveState = NPC.MoveState.standing;
                    alan.moveState = NPC.MoveState.standing;
                    ++state;
                    timer = 0;
                    // return true;
                    break;
                case -1:
                    if (firstFrameOfTheState)
                    {
                        alan.ClearDialogue();
                        alan.Dialogue.Add(
                            "There you are. We've already started making the rounds! A tad bit late to your second day of work, aren't you? And wearing the same clothes as yesterday, too... ");
                        alan.Dialogue.Add(
                            "Wait, is that a textbook? Well, look at that, Paul! Our Product Manager found us some more product.");
                        alan.Talking = true;
                        player.playerState = Player.PlayerState.relaxedStanding;
                    }

                    if (alan.DialogueState == 1)
                        alan.CurrentDialogueFace = "Arrogant";
                    alan.UpdateInteraction();
                    camera.StartUpdate((GameObject)player, game, game.CurrentChapter.CurrentMap);
                    if (alan.Talking)
                    {
                        // return true;
                        break;
                    }

                    state = 1;
                    timer = 0;
                    // return true;
                    break;
                case 0:
                    state = -5;
                    timer = 0;
                    // return true;
                    break;
                case 1:
                    if (firstFrameOfTheState)
                    {
                        alan.Dialogue.Clear();
                        paul.Dialogue.Clear();
                        paul.AddQuest((Quest)game.ChapterOne.theProductManager);
                        paul.Talking = true;
                    }

                    paul.CurrentDialogueFace = "Arrogant";
                    paul.Choice = 0;
                    camera.StartUpdate((GameObject)player, game, game.CurrentChapter.CurrentMap);
                    paul.UpdateInteraction();
                    if (paul.Talking)
                    {
                        // return true;
                        break;
                    }

                    alan.Dialogue.Clear();
                    alan.Dialogue.Add(
                        "We'll talk about your mistakes after we do business. Business always comes first, Derek.");
                    state = 0;
                    timer = 0;
                    player.playerState = Player.PlayerState.standing;
                    game.CurrentChapter.state = Chapter.GameState.Game;
                    game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Mopping Up"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Shocking Statements CH.1"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Combustible Confutation CH.1"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Fowl Mouth"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Sharp Comments"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Crushing Realization"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Combustible Confutation CH.2"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Shocking Statements CH.2"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Shocking Statements CH.3"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Combustible Confutation CH.3"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Catching Lies"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Healthy Sacrifices"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Debate Dissimilarities"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["What's Yours is Mine"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Keep Healthy"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Ride the Tide"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Cutting Corners"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Faux Pow"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Sharpen The Saw"]);
                    // game.YourLocker.SkillsOnSale.Add(SkillManager.AllSkills["Self Destruct"]);
                    Chapter.effectsManager.notificationQueue.Enqueue((Notification)new NewSkillsUnlockedNotification());
                    Chapter.effectsManager.fButtonRecs.Clear();
                    Chapter.effectsManager.foregroundFButtonRecs.Clear();
                    // return true;
                    break;
            }

            return false;
        }
        catch (Exception ex)
        {
            _log.LogError($"Failed in {nameof(Play_OverwriteChapterOneSkillShop_Prefix)}:\n\t{ex}");
            return true;
        }
    }
}