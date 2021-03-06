﻿#region Using
using HearthMirror.Objects;
using Hearthstone_Deck_Tracker.Enums;
using Spawn.HDT.DustUtility.CardManagement.Offline;
using Spawn.HDT.DustUtility.Logging;
using System;
using System.Collections.Generic;
#endregion

namespace Spawn.HDT.DustUtility.AccountManagement
{
    public class MockAccount : IAccount
    {
        #region Member Variables
        private readonly List<Card> m_lstCollection;
        private readonly List<Deck> m_lstDecks;
        private readonly List<CachedHistoryCard> m_lstHistory;
        #endregion

        #region Properties
        public BattleTag BattleTag { get; }

        public Region Region => Region.EU;

        public string DisplayString { get; }

        public string AccountString { get; }

        public bool IsEmpty => false;

        public bool IsValid => true;

        public AccountPreferences Preferences { get; }

        public bool HasFiles => false;
        #endregion

        #region Ctor
        public MockAccount()
        {
            m_lstCollection = new List<Card>
            {
                new Card("EX1_350", 1, true),
                new Card("EX1_055", 2, false),
                new Card("EX1_607", 2, true),
                new Card("EX1_339", 2, false),
                new Card("EX1_339", 2, true),
                new Card("EX1_145", 2, false),
                new Card("LOOT_542", 1, false),
                new Card("CFM_637", 1, false),
                new Card("CS2_146", 1, false),
                new Card("KAR_069", 2, false),
                new Card("EX1_012", 1, false),
                new Card("EX1_124", 2, false),
                new Card("EX1_613", 1, false),
                new Card("EX1_134", 2, false),
                new Card("LOOT_026", 2, false),
                new Card("GVG_022", 2, false),
                new Card("EX1_284", 2, false),
                new Card("UNG_064", 1, false),
                new Card("LOOT_149", 2, false),
                new Card("ICC_021", 2, true),
                new Card("BRM_024", 2, false),
                new Card("LOE_002", 2, true),
                new Card("FP1_001", 2, true),
                new Card("OG_006", 2, true),
                new Card("AT_037", 2, false),
                new Card("LOE_119", 2, false),
                new Card("ICC_836", 2, false),
                new Card("FP1_013", 2, true),
                new Card("OG_134", 2, true)
            };

            m_lstDecks = new List<Deck>
            {
                new Deck
                {
                    Id = 1427144022,
                    Name = "Oil Rogue",
                    Hero = "HERO_03",
                    IsWild = true,
                    Type = 1,
                    SeasonId = 0,
                    CardBackId = 99,
                    HeroPremium = 0,
                    Cards = new List<Card>
                    {
                        new Card("CS2_072", 2, false),
                        new Card("EX1_145", 2, false),
                        new Card("LOOT_542", 1, false),
                        new Card("CFM_637", 1, false),
                        new Card("CS2_146", 1, false),
                        new Card("KAR_069", 2, false),
                        new Card("EX1_012", 1, false),
                        new Card("EX1_124", 2, false),
                        new Card("EX1_581", 1, false),
                        new Card("EX1_613", 1, false),
                        new Card("EX1_129", 2, false),
                        new Card("EX1_134", 2, false),
                        new Card("LOOT_026", 2, false),
                        new Card("GVG_022", 2, false),
                        new Card("EX1_284", 2, false),
                        new Card("UNG_064", 1, false),
                        new Card("NEW1_004", 1, false),
                        new Card("LOOT_149", 2, false),
                        new Card("CS2_077", 2, false)
                    }
                }
            };

            m_lstHistory = new List<CachedHistoryCard>
            {
                new CachedHistoryCard { Id = "EX1_145", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id= "LOOT_542", Count = 1, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "CFM_637", Count = 1, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "CS2_146", Count = 1, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "KAR_069", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "EX1_012", Count = 1, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "EX1_124", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "EX1_613", Count = 1, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "EX1_134", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "LOOT_026", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "GVG_022", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "EX1_284", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "UNG_064", Count = 1, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "LOOT_149", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "ICC_021", Count = 2, IsGolden = true, Date = DateTime.Now },
                new CachedHistoryCard { Id = "BRM_024", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "LOE_002", Count = 2, IsGolden = false, Date = DateTime.Now },
                new CachedHistoryCard { Id = "FP1_013", Count = 1, IsGolden = true, Date = DateTime.Now },
                new CachedHistoryCard { Id = "OG_134", Count = 1, IsGolden = true, Date = DateTime.Now },
                new CachedHistoryCard { Id = "AT_037", Count = 2, IsGolden = true, Date = DateTime.Now },
            };

            BattleTag = new BattleTag { Name = "MockAccount", Number = 12345 };
            AccountString = $"{BattleTag.Name}_{BattleTag.Number}_{Region}";
            DisplayString = $"{BattleTag.Name}#{BattleTag.Number} ({Region})";
            Preferences = new AccountPreferences();

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Created new 'MockAccount' instance");
        }
        #endregion

        #region ExcludeDeckFromSearch
        public void ExcludeDeckFromSearch(long nDeckId)
        {
            if (!IsDeckExcludedFromSearch(nDeckId))
            {
                Preferences.ExcludedDecks.Add(nDeckId);

                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Excluded deck (Id={nDeckId})");
            }
            else
            {
                DustUtilityPlugin.Logger.Log(LogLevel.Warning, $"Deck already excluded (Id={nDeckId})");
            }
        }
        #endregion

        #region IncludeDeckInSearch
        public void IncludeDeckInSearch(long nDeckId)
        {
            if (IsDeckExcludedFromSearch(nDeckId))
            {
                Preferences.ExcludedDecks.Remove(nDeckId);

                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Included deck (Id={nDeckId})");
            }
            else
            {
                DustUtilityPlugin.Logger.Log(LogLevel.Warning, $"Deck already included (Id={nDeckId})");
            }
        }
        #endregion

        #region GetCollection
        public List<Card> GetCollection() => m_lstCollection;
        #endregion

        #region GetDecks
        public List<Deck> GetDecks() => m_lstDecks;
        #endregion

        #region GetHistory
        public List<CachedHistoryCard> GetHistory() => m_lstHistory;
        #endregion

        #region IsDeckExcludedFromSearch
        public bool IsDeckExcludedFromSearch(long nDeckId) => Preferences.ExcludedDecks.Contains(nDeckId);
        #endregion

        #region SavePreferences
        public void SavePreferences() => DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Called 'SavePreferences' in mock account");
        #endregion
    }
}