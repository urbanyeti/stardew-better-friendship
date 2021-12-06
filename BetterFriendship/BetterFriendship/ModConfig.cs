namespace BetterFriendship
{
    internal class ModConfig
    {
        public bool DisplayTalkPrompts { get; set; } = true;
        public string GiftPreference { get; set; } = "like";
        public int GiftCycleCount { get; set; } = 3;
        public int GiftCycleDelay { get; set; } = 2000;
        public bool IgnoreMaxedFriendships { get; set; } = true;
        public bool OnlyHighestQuality { get; set; } = false;
        public bool DisplayBubbles { get; set; } = true;
    }
}
