using DunGen.Graph;
using LethalLevelLoader.Extras;
using LethalLevelLoader.Modules;
using static LethalLevelLoader.Modules.Dungeon;
using System;
using System.Collections.Generic;
using System.Text;
using DunGen;
using UnityEngine;

namespace LethalLevelLoader.Modules
{
    public class DungeonUtils
    {
        /// <summary>
        /// Registers a custom archetype to a level.
        /// </summary>
        public static void AddArchetype(DungeonArchetype archetype, Levels.LevelTypes levelFlags, int lineIndex = -1)
        {
            var customArchetype = new CustomDungeonArchetype();
            customArchetype.archeType = archetype;
            customArchetype.LevelTypes = levelFlags;
            customArchetype.lineIndex = lineIndex;
            customDungeonArchetypes.Add(customArchetype);
        }

        /// <summary>
        /// Registers a dungeon graphline to a level.
        /// </summary>
        public static void AddLine(GraphLine line, Levels.LevelTypes levelFlags)
        {
            var customLine = new CustomGraphLine();
            customLine.graphLine = line;
            customLine.LevelTypes = levelFlags;
            customGraphLines.Add(customLine);
        }

        /// <summary>
        /// Registers a dungeon graphline to a level.
        /// </summary>
        public static void AddLine(DungeonGraphLineDef line, Levels.LevelTypes levelFlags)
        {
            AddLine(line.graphLine, levelFlags);
        }

        /// <summary>
        /// Adds a tileset to a dungeon archetype
        /// </summary>
        public static void AddTileSet(TileSet set, string archetypeName)
        {
            extraTileSets.Add(archetypeName, set);
        }

        /// <summary>
        /// Adds a room to a tileset with the given name.
        /// </summary>
        public static void AddRoom(GameObjectChance room, string tileSetName)
        {
            extraRooms.Add(tileSetName, room);
        }

        /// <summary>
        /// Adds a room to a tileset with the given name.
        /// </summary>
        public static void AddRoom(GameObjectChanceDef room, string tileSetName)
        {
            AddRoom(room.gameObjectChance, tileSetName);
        }

        /// <summary>
        /// Adds a dungeon to the given levels.
        /// </summary>
        public static void AddDungeon(DungeonDef dungeon, Levels.LevelTypes levelFlags)
        {
            AddDungeon(dungeon.dungeonFlow, dungeon.rarity, levelFlags, dungeon.firstTimeDungeonAudio);
        }

        /// <summary>
        /// Adds a dungeon to the given levels.
        /// </summary>
        public static void AddDungeon(DungeonDef dungeon, Levels.LevelTypes levelFlags, string[] levelOverrides)
        {
            AddDungeon(dungeon.dungeonFlow, dungeon.rarity, levelFlags, levelOverrides, dungeon.firstTimeDungeonAudio);
        }

        /// <summary>
        /// Adds a dungeon to the given levels.
        /// </summary>
        public static void AddDungeon(DungeonFlow dungeon, int rarity, Levels.LevelTypes levelFlags, AudioClip firstTimeDungeonAudio = null)
        {
            ExtendedDungeonFlow extendedDungeonFlow = ScriptableObject.CreateInstance<ExtendedDungeonFlow>();
            extendedDungeonFlow.Initialize(dungeon, firstTimeDungeonAudio, LevelType.Custom, "DEFAULTMODDED", newDungeonRarity: 300);
            Dungeon.AddExtendedDungeonFlow(extendedDungeonFlow);
        }
        /*public static void AddDungeon(DungeonFlow dungeon, int rarity, Levels.LevelTypes levelFlags, AudioClip firstTimeDungeonAudio = null)
        {
            customDungeons.Add(new CustomDungeon
            {
                dungeonFlow = dungeon,
                rarity = 300,
                LevelTypes = Levels.LevelTypes.All,
                //rarity = rarity,
                //LevelTypes = levelFlags,
                firstTimeDungeonAudio = firstTimeDungeonAudio
            });
        }*/

        /// <summary>
        /// Adds a dungeon to the given levels.
        /// </summary>
        public static void AddDungeon(DungeonFlow dungeon, int rarity, Levels.LevelTypes levelFlags, string[] levelOverrides = null, AudioClip firstTimeDungeonAudio = null)
        {
            customDungeons.Add(new CustomDungeon
            {
                dungeonFlow = dungeon,
                rarity = rarity,
                LevelTypes = levelFlags,
                firstTimeDungeonAudio = firstTimeDungeonAudio,
                levelOverrides = levelOverrides
            });
        }
    }
}
