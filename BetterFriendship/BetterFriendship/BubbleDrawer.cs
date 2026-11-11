using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = StardewValley.Object;

namespace BetterFriendship
{
    public class BubbleDrawer
    {
        private const float BaseOpacity = 0.75f;

        // The bubble sprite is 20x24 drawn at 4x. Scaling is anchored to its bottom-center
        // so a resized bubble stays put above the villager's head.
        private static readonly Vector2 BubbleAnchor = new(40, 96);

        private Vector2 BubbleOffset { get; set; }
        private readonly ModConfig _config;
        private readonly Dictionary<string, (int item, double time)> _lastCycled = new();
        private readonly Rectangle _bubbleSourceRectangle = new(141, 465, 20, 24);
        private readonly Rectangle _giftSourceRectangle = new(167, 175, 12, 11);
        private readonly Rectangle _talkSourceRectangle = new(181, 175, 12, 11);
        private readonly Texture2D _emojiTexture2D;
        private readonly PerScreen<ScreenState> _screenState = new(() => new ScreenState());

        private sealed class BubbleState
        {
            public Character Character;
            public List<(Object item, int taste)> BestItems;
            public bool DisplayGift;
            public bool DisplayTalk;
            public float Alpha;
            public int LastRequestedFrame;
        }

        private sealed class ScreenState
        {
            public readonly Dictionary<string, BubbleState> Bubbles = new();
            public int Frame;
        }

        internal BubbleDrawer(ModConfig config)
        {
            _config = config;
            BubbleOffset = new Vector2(-6, 120);
            _emojiTexture2D = Game1.content.Load<Texture2D>("LooseSprites\\emojis");
        }

        public void UpdateOffset(Vector2 offset)
        {
            BubbleOffset += offset;
        }

        /// <summary>Draws (and fades in) the bubble for a character that should currently display one.</summary>
        public void DrawBubble(SpriteBatch spriteBatch, Character character, List<(Object item, int taste)> bestItems,
            bool displayGift, bool displayTalk)
        {
            if (!displayGift && !displayTalk)
            {
                return;
            }

            var screen = _screenState.Value;

            if (!screen.Bubbles.TryGetValue(character.Name, out var bubble))
            {
                screen.Bubbles[character.Name] = bubble = new BubbleState();
            }

            bubble.Character = character;
            bubble.BestItems = bestItems;
            bubble.DisplayGift = displayGift;
            bubble.DisplayTalk = displayTalk;
            bubble.LastRequestedFrame = screen.Frame;
            bubble.Alpha = Math.Min(1f, bubble.Alpha + GetFadeStep());

            Draw(spriteBatch, bubble);
        }

        /// <summary>
        ///     Fades out the bubbles that were not requested via <see cref="DrawBubble" /> this frame.
        ///     Must be called once per frame after all bubbles have been drawn.
        /// </summary>
        public void DrawFadingBubbles(SpriteBatch spriteBatch)
        {
            var screen = _screenState.Value;
            var frame = screen.Frame++;
            var fadeStep = GetFadeStep();

            foreach (var (name, bubble) in screen.Bubbles.Where(x => x.Value.LastRequestedFrame != frame).ToList())
            {
                bubble.Alpha -= fadeStep;

                if (bubble.Alpha <= 0f || bubble.Character.currentLocation != Game1.currentLocation)
                {
                    screen.Bubbles.Remove(name);
                    continue;
                }

                Draw(spriteBatch, bubble);
            }
        }

        /// <summary>Forgets all fade progress, e.g. when bubbles are hidden outright.</summary>
        public void Reset()
        {
            _screenState.Value.Bubbles.Clear();
        }

        private float GetFadeStep()
        {
            if (_config.BubbleFadeDuration <= 0) return 1f;

            return (float)(Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds / _config.BubbleFadeDuration);
        }

        private void Draw(SpriteBatch spriteBatch, BubbleState bubble)
        {
            var character = bubble.Character;
            var bestItems = bubble.BestItems;
            var displayTalk = bubble.DisplayTalk;

            var selectedItem = bubble.DisplayGift ? SelectIem(character.Name, bestItems) : -1;

            var displayGift = bubble.DisplayGift && (selectedItem != -1 ||
                                                     _config.DisplayGenericGiftPrompts ||
                                                     character.ShouldOverrideForSpouse(_config));

            if (!displayGift && !displayTalk) return;

            var hoverVal = (float)(4.0 * Math.Round(Math.Sin(DateTime.Now.TimeOfDay.TotalMilliseconds / 250.0), 2)) -
                           (Game1.tileSize / 2);

            var scale = _config.BubbleScale;
            var color = Color.White * (BaseOpacity * bubble.Alpha);
            var origin = new Vector2(
                             character.Position.X + BubbleOffset.X,
                             character.Position.Y - BubbleOffset.Y + hoverVal)
                         + BubbleAnchor * (1f - scale);

            // x/y are offsets from the bubble's top-left corner at 100% bubble size
            void DrawSprite(Texture2D texture, Rectangle sourceRect, float x, float y, float spriteScale,
                float layerDepth)
            {
                spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, origin + new Vector2(x, y) * scale),
                    sourceRect, color, 0.0f, Vector2.Zero, spriteScale * scale, SpriteEffects.None, layerDepth);
            }

            DrawSprite(Game1.mouseCursors, _bubbleSourceRectangle, 0, 0, 4f, 0);

            if (displayGift)
            {
                if (selectedItem == -1)
                {
                    // Generic gift reminder
                    if (displayTalk)
                        DrawSprite(Game1.mouseCursors2, _giftSourceRectangle, 6, 10, 4f, 1);
                    else
                        DrawSprite(Game1.mouseCursors2, _giftSourceRectangle, 16, 16, 4f, 1);
                }
                else
                {
                    var (item, taste) = bestItems[selectedItem];
                    var itemData = ItemRegistry.GetData(item.QualifiedItemId);

                    if (displayTalk)
                    {
                        DrawSprite(itemData.GetTexture(), itemData.GetSourceRect(), 6, 10, 3f, 1);
                    }
                    else
                    {
                        DrawSprite(itemData.GetTexture(), itemData.GetSourceRect(), 8, 8, 4f, 1);
                        DrawSprite(_emojiTexture2D, GetSmileySourceRect(taste), 50, 60, 3f, 2);

                        if (item.Quality is not 0)
                        {
                            DrawSprite(Game1.mouseCursors, GetQualitySourceRect(item.Quality), 3, 61, 3f, 2);
                        }
                    }
                }
            }

            if (displayTalk)
            {
                if (displayGift)
                    DrawSprite(Game1.mouseCursors2, _talkSourceRectangle, 22, 30, 4f, 2);
                else
                    DrawSprite(Game1.mouseCursors2, _talkSourceRectangle, 16, 16, 4f, 1);
            }
        }

        private static Rectangle GetQualitySourceRect(int quality)
        {
            return quality < 4 ? new Rectangle(338 + (quality - 1) * 8, 400, 8, 8) : new Rectangle(346, 392, 8, 8);
        }

        private static Rectangle GetSmileySourceRect(int taste) =>
            taste switch
            {
                0 => new Rectangle(9, 27, 9, 9),
                2 => new Rectangle(0, 0, 9, 9),
                not 4 or 6 => new Rectangle(18, 9, 9, 9),
                _ => new Rectangle(0, 0, 0, 0)
            };

        private int SelectIem(string npcName, IReadOnlyCollection<(Object item, int taste)> bestItems)
        {
            if (bestItems == null || !bestItems.Any() || !_lastCycled.ContainsKey(npcName) ||
                _lastCycled[npcName].item >= bestItems.Count)
            {
                _lastCycled[npcName] = (0, 0);
                return -1;
            }

            if (!(Game1.currentGameTime.TotalGameTime.TotalMilliseconds >=
                  _lastCycled[npcName].time + _config.GiftCycleDelay))
                return _lastCycled[npcName].item;
            _lastCycled[npcName] = ((_lastCycled[npcName].item + 1) % bestItems.Count,
                Game1.currentGameTime.TotalGameTime.TotalMilliseconds);

            return _lastCycled[npcName].item;
        }
    }
}
