using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Characters;

namespace BetterFriendship
{
    internal static class GameExtensions
    {
        public static bool IsTownsfolk(this NPC npc)
        {
            return npc is not Pet
                   && npc is not Horse
                   && npc is not Junimo
                   && npc is not JunimoHarvester
                   && npc is not TrashBear;
        }

        public static IEnumerable<(Object, int)> TakeTopPrioritized(this IEnumerable<(Object item, int taste)> items,
            ModConfig config)
        {
            if (config.OnlyHighestQuality)
            {
                items = items.GroupBy(x => x.item.name)
                    .Select(x => x.OrderByDescending(y => y.item.Quality).First());
            }

            return items
                .OrderBy(x => x.taste)
                .ThenByDescending(x => x.item.Quality)
                .ThenBy(x => x.item.salePrice())
                .Take(config.GiftCycleCount);
        }
    }
}