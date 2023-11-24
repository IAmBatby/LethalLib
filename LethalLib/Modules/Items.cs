﻿using BepInEx.Logging;
using LethalLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LethalLib.Modules.Enemies;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LethalLib.Modules
{
    public class Items
    {
        public static void Init()
        {
            On.StartOfRound.Awake += RegisterLevelScrap;
            On.Terminal.Awake += Terminal_Awake;
        }


        private static void Terminal_Awake(On.Terminal.orig_Awake orig, Terminal self)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            var itemList = terminal.buyableItemsList.ToList();

            var buyKeyword = self.terminalNodes.allKeywords.First(keyword => keyword.word == "buy");
            var cancelPurchaseNode = buyKeyword.compatibleNouns[0].result.terminalOptions[1].result;

            Plugin.logger.LogInfo($"Adding {shopItems.Count} items to terminal");
            foreach (ShopItem item in shopItems)
            {
                itemList.Add(item.item);

                var itemName = item.item.itemName;
                var lastChar = itemName[itemName.Length - 1];
                var itemNamePlural = itemName;

                var buyNode2 = ScriptableObject.CreateInstance<TerminalNode>();

                buyNode2.name = $"{itemName.Replace(" ", "-")}BuyNode2";
                buyNode2.displayText = $"Ordered [variableAmount] {itemNamePlural}. Your new balance is [playerCredits].\n\nOur contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.\r\n\r\n";
                buyNode2.clearPreviousText = true;
                buyNode2.maxCharactersToType = 15;
                buyNode2.buyItemIndex = itemList.Count - 1;
                buyNode2.isConfirmationNode = false;
                buyNode2.itemCost = item.price;
               

                var buyNode1 = ScriptableObject.CreateInstance<TerminalNode>();
                buyNode1.name = $"{itemName.Replace(" ", "-")}BuyNode1";
                buyNode1.displayText = $"You have requested to order {itemNamePlural}. Amount: [variableAmount].\nTotal cost of items: [totalCost].\n\nPlease CONFIRM or DENY.\r\n\r\n";
                buyNode1.clearPreviousText = true;
                buyNode1.maxCharactersToType = 35;
                buyNode1.buyItemIndex = itemList.Count - 1;
                buyNode1.isConfirmationNode = true;
                buyNode1.overrideOptions = true;
                buyNode1.itemCost = item.price;
                buyNode1.terminalOptions = new CompatibleNoun[2]
                {
                    new CompatibleNoun()
                    {
                        noun = self.terminalNodes.allKeywords.First(mykeyword => mykeyword.word == "confirm"),
                        result = buyNode2
                    },
                    new CompatibleNoun()
                    {
                        noun = self.terminalNodes.allKeywords.First(mykeyword => mykeyword.word == "deny"),
                        result = cancelPurchaseNode
                    }
                };
                TerminalKeyword keyword = TerminalUtils.CreateTerminalKeyword(itemName.ToLowerInvariant().Replace(" ", "-"), defaultVerb: buyKeyword);

                //self.terminalNodes.allKeywords.AddItem(keyword);
                var allKeywords = self.terminalNodes.allKeywords.ToList();
                allKeywords.Add(keyword);
                self.terminalNodes.allKeywords = allKeywords.ToArray();

                var nouns = buyKeyword.compatibleNouns.ToList();
                nouns.Add(new CompatibleNoun()
                {
                    noun = keyword,
                    result = buyNode1
                });
                buyKeyword.compatibleNouns = nouns.ToArray();



                Plugin.logger.LogInfo($"Added {item.item.name} to terminal");
            }

            terminal.buyableItemsList = itemList.ToArray();

            orig(self);
        }

        public static List<ScrapItem> scrapItems = new List<ScrapItem>();
        public static List<ShopItem> shopItems = new List<ShopItem>();

        private static void RegisterLevelScrap(On.StartOfRound.orig_Awake orig, StartOfRound self)
        {
            orig(self);

            foreach (SelectableLevel level in self.levels)
            {
                var name = level.name;

                if (Enum.IsDefined(typeof(Levels.LevelTypes), name))
                {
                    var levelEnum = (Levels.LevelTypes)Enum.Parse(typeof(Levels.LevelTypes), name);
                    foreach (ScrapItem scrapItem in scrapItems)
                    {
                        if (scrapItem.spawnLevels.HasFlag(levelEnum))
                        {
                            var spawnableÍtemWithRarity = new SpawnableItemWithRarity()
                            {
                                spawnableItem = scrapItem.item,
                                rarity = scrapItem.rarity
                            };

                            // make sure spawnableScrap does not already contain item
                            Plugin.logger.LogInfo($"Checking if {scrapItem.item.name} is already in {name}");

                            if (!level.spawnableScrap.Any(x => x.spawnableItem == scrapItem.item))
                            {
                                level.spawnableScrap.Add(spawnableÍtemWithRarity);
                                Plugin.logger.LogInfo($"Added {scrapItem.item.name} to {name}");
                            }
                        }
                    }

                }
            }



            foreach (ScrapItem scrapItem in scrapItems)
            {
                if (!self.allItemsList.itemsList.Contains(scrapItem.item))
                {
                    Plugin.logger.LogInfo($"Item registered: {scrapItem.item.name}");
                    self.allItemsList.itemsList.Add(scrapItem.item);
                }
            }

        }

        public class ScrapItem
        {
            public Item item;
            public int rarity;
            public Levels.LevelTypes spawnLevels;

            public ScrapItem(Item item, int rarity, Levels.LevelTypes spawnLevels)
            {
                this.item = item;
                this.rarity = rarity;
                this.spawnLevels = spawnLevels;
            }
        }

        public class ShopItem
        {
            public Item item;
            public int price;
            public ShopItem(Item item, int price)
            {
                this.item = item;
                this.price = price;
            }
        }

        public static void RegisterScrap(Item spawnableItem, int rarity, Levels.LevelTypes levelFlags)
        {
            var scrapItem = new ScrapItem(spawnableItem, rarity, levelFlags);

            scrapItems.Add(scrapItem);
        }

        public static void RegisterShopItem(Item shopItem, int price)
        {
            shopItems.Add(new ShopItem(shopItem, price));
        }
    }
}
