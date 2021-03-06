using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using Object = StardewValley.Object;
using System.Runtime.Caching;
using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace BetterFriendship
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig Config { get; set; }
        private BubbleDrawer BubbleDrawer { get; set; }
        private readonly ObjectCache _cache = MemoryCache.Default;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            BubbleDrawer = new BubbleDrawer(Config);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Player.InventoryChanged += OnInventoryChanged;
            Config.PropertyChanged += OnConfigChanged;
        }

        /// <summary>
        ///     Raised after the game is launched, right before the first update tick. This happens once per game session
        ///     (unrelated to loading saves). All mods are loaded and initialised at this point, so this is a good time to set up
        ///     mod integrations.
        /// </summary>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            SetupConfigMenu(configMenu);
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
        }

        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.eventUp) return;

            var currentLocation = Game1.currentLocation;

            if (!Config.DisplayBubbles || (Config.GiftPreference == "none" && !Config.DisplayTalkPrompts &&
                                           !Config.SpousePromptsOverride)) return;

            foreach (var npc in currentLocation.characters.Where(npc =>
                         npc.IsTownsfolk() || (npc is Child child && child.daysOld.Value > 14)))
            {
                if (!Game1.player.friendshipData.TryGetValue(npc.Name, out var friendship)) continue;

                if (Config.IgnoreMaxedFriendships && !FriendshipCanDecay(npc, friendship)) continue;

                if ((Config.GiftPreference == "none" && !npc.ShouldOverrideForSpouse(Config)) ||
                    npc is Child ||
                    friendship.GiftsToday != 0 ||
                    (friendship.GiftsThisWeek >= 2 && Game1.player.spouse != npc.Name &&
                     !npc.isBirthday(Game1.Date.Season, Game1.Date.DayOfMonth))
                   )
                {
                    if ((!Config.DisplayTalkPrompts && !npc.ShouldOverrideForSpouse(Config)) ||
                        friendship.TalkedToToday || npc.IsOutOfDialog()) continue;

                    DrawBubbleSafely(npc, null, false, true);

                    continue;
                }

                List<(Object, int)> bestItems;

                if (_cache.Contains(GetCacheKey(npc)))
                {
                    bestItems = _cache.Get(GetCacheKey(npc)) as List<(Object, int)>;
                }
                else
                {
                    bestItems = GetGiftSuggestionsSafely(npc);
                    _cache.Add(new CacheItem(GetCacheKey(npc), bestItems),
                        new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) });
                }

                var displayTalk = (Config.DisplayTalkPrompts || npc.ShouldOverrideForSpouse(Config)) &&
                              !friendship.TalkedToToday &&
                              !npc.IsOutOfDialog();

                DrawBubbleSafely(npc, bestItems, true, displayTalk);
            }
        }

        private static bool FriendshipCanDecay(NPC npc, Friendship friendship)
        {
            if (Game1.player.spouse == npc.Name) return true;
            if (friendship.IsDating() && friendship.Points < 2500) return true;

            var isPreBouquet = npc.datable.Value && !friendship.IsDating() && !npc.isMarried();
            return (!isPreBouquet && friendship.Points < 2500) || (isPreBouquet && friendship.Points < 2000);
        }

        private static string GetCacheKey(Character character) => $"{Game1.player.Name}:{character.Name}";

        private List<(Object, int)> GetGiftSuggestionsSafely(NPC npc)
        {
            try
            {
                return npc.GetGiftSuggestions(Config);
            }
            catch (NullReferenceException ex)
            {
                Monitor.VerboseLog(
                    $"Error with GetTopGiftSuggestion for {npc.Name}. Exception: {ex.Message}");
                return new List<(Object, int)>();
            }
        }

        private void DrawBubbleSafely(Character character, List<(Object, int)> bestItems, bool displayGift, bool displayTalk)
        {
            try
            {
                BubbleDrawer.DrawBubble(Game1.spriteBatch, character, bestItems,
                    displayGift,
                    displayTalk
                );
            }
            catch (Exception ex)
            {
                Monitor.VerboseLog(
                    $"Error with DrawBubble for {character.Name} | bestItems: {bestItems} | displayGift: {displayGift} | displayTalk: {displayTalk}. Exception: {ex.Message}");
            }
        }

        private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not (nameof(ModConfig.GiftPreference) or nameof(ModConfig.GiftCycleCount)
                or nameof(ModConfig.OnlyHighestQuality))) return;

            foreach (var key in _cache.Select(x => x.Key)) _cache.Remove(key);
        }

        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            foreach (var key in _cache.Select(x => x.Key)) _cache.Remove(key);
        }

        private void SetupConfigMenu(IGenericModConfigMenuApi configMenu)
        {
            configMenu.Register(
                ModManifest,
                () => Config = new ModConfig(),
                () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                ModManifest,
                name: () => "Prompt to Speak w/ Villagers",
                tooltip: () => "Displays an indicator if a villager has not been talked to today.",
                getValue: () => Config.DisplayTalkPrompts,
                setValue: value => Config.DisplayTalkPrompts = value
            );

            configMenu.AddTextOption(
                ModManifest,
                name: () => "Gift Suggestion Preference",
                tooltip: () =>
                    "The lowest level of matching you want for gift suggestions. Gift suggestions come from items currently in your inventory ordered by receiver's gift preference, quality of item, and cheapest price.",
                getValue: () => Config.GiftPreference,
                setValue: value => Config.GiftPreference = value,
                allowedValues: new[] { "love", "like", "neutral", "none" },
                formatAllowedValue: value => value switch
                {
                    "love" => "Show only loved gifts",
                    "like" => "Show liked gifts & above",
                    "neutral" => "Show neutral gifts & above",
                    "none" => "Hide all suggestions",
                    _ => "UNKNOWN"
                }
            );

            configMenu.AddNumberOption(
                ModManifest,
                name: () => "Max Gifts to Show",
                tooltip: () => "The maximum number of gift suggestions to cycle through.",
                interval: 1,
                min: 1,
                max: 10,
                getValue: () => Config.GiftCycleCount,
                setValue: value => Config.GiftCycleCount = value
            );

            configMenu.AddNumberOption(
                ModManifest,
                name: () => "Gift Display Time (ms)",
                tooltip: () => "The time to display each suggested gift in milliseconds.",
                interval: 500,
                min: 500,
                max: 5000,
                getValue: () => Config.GiftCycleDelay,
                setValue: value => Config.GiftCycleDelay = value
            );

            configMenu.AddBoolOption(
                ModManifest,
                name: () => "Display Generic Gift Prompts",
                tooltip: () =>
                    "Displays a generic gift indicator if a villager can receive a gift, but nothing in your inventory matches their tastes.",
                getValue: () => Config.DisplayGenericGiftPrompts,
                setValue: value => Config.DisplayGenericGiftPrompts = value
            );

            configMenu.AddBoolOption(
                ModManifest,
                name: () => "Ignore Maxed Friendships",
                tooltip: () =>
                    "Hides suggestions and prompts for relationships that won't decay.",
                getValue: () => Config.IgnoreMaxedFriendships,
                setValue: value => Config.IgnoreMaxedFriendships = value
            );

            configMenu.AddBoolOption(
                ModManifest,
                name: () => "Always Display Spouse Prompts",
                tooltip: () =>
                    "Overrides other settings to always display gift and talk prompts for your spouse.",
                getValue: () => Config.SpousePromptsOverride,
                setValue: value => Config.SpousePromptsOverride = value
            );

            configMenu.AddBoolOption(
                ModManifest,
                name: () => "Only Show Highest Quality",
                tooltip: () =>
                    "Display only the highest quality version of items available. E.g. if you have both a gold and silver quality Hot Pepper, only the gold quality Hot Pepper will be suggested.",
                getValue: () => Config.OnlyHighestQuality,
                setValue: value => Config.OnlyHighestQuality = value
            );

            configMenu.AddBoolOption(
                ModManifest,
                name: () => "[!] Enable Suggestion Bubbles",
                tooltip: () =>
                    "Allows floating bubbles to be displayed over villagers. Warning: Turning this off will hide ALL floating bubbles enabled by this mod (talk prompts, gift suggestions, etc.)",
                getValue: () => Config.DisplayBubbles,
                setValue: value => Config.DisplayBubbles = value
            );
        }
    }
}