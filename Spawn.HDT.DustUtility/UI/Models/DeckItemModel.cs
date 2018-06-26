﻿#region Using
using GalaSoft.MvvmLight;
using HearthMirror.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Imaging;
#endregion

namespace Spawn.HDT.DustUtility.UI.Models
{
    [DebuggerDisplay("{Name}: {CardCount} Card(s) ({CraftingCost} Dust)")]
    public class DeckItemModel : ObservableObject
    {
        #region Static Fields
        private static Dictionary<string, BitmapImage> s_dImageCache = new Dictionary<string, BitmapImage>();
        #endregion

        #region Member Variables
        private Deck m_deck;
        private double m_dblOpacity;
        #endregion

        #region Properties
        #region Deck
        public Deck Deck
        {
            get => m_deck;
            set => Set(ref m_deck, value);
        }
        #endregion

        #region DeckId
        public long DeckId => (Deck?.Id ?? 0);
        #endregion

        #region Name
        public string Name => Deck?.Name;
        #endregion

        #region CardCount
        public int CardCount => (Deck?.Cards.Sum(c => c.Count) ?? 0);
        #endregion

        #region CraftingCost
        public int CraftingCost => (Deck?.GetCraftingCost() ?? 0);
        #endregion

        #region HeroImage
        public BitmapImage HeroImage => GetHeroImage();
        #endregion

        #region Opacity
        public double Opacity
        {
            get => m_dblOpacity;
            set => Set(ref m_dblOpacity, value);
        }
        #endregion
        #endregion

        #region Ctor
        public DeckItemModel()
        {
            Opacity = 1;

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(Deck)))
                {
                    RaisePropertyChanged(nameof(DeckId));
                    RaisePropertyChanged(nameof(Name));
                    RaisePropertyChanged(nameof(CardCount));
                    RaisePropertyChanged(nameof(CraftingCost));
                    RaisePropertyChanged(nameof(HeroImage));
                }
            };
#if DEBUG
            if (ViewModelBase.IsInDesignModeStatic)
            {
                m_deck = new Deck() { Id = 4342323, Hero = "HERO_01", Name = "Test Deck", Cards = new List<Card>() { new Card("", 0, false) } };
            }
#endif

            DustUtilityPlugin.Logger.Log(Logging.LogLevel.Debug, "Initialized new 'DeckItemModel' instance");
        }

        public DeckItemModel(Deck deck)
            : this()
        {
            m_deck = deck;
        }
        #endregion

        #region GetHeroImage
        private BitmapImage GetHeroImage()
        {
            string strHero = Deck?.Hero.Substring(0, 7);

            if (!s_dImageCache.TryGetValue(strHero, out BitmapImage retVal))
            {
                string strSource = string.Empty;

                switch (strHero)
                {
                    case "HERO_01": //Warrior
                        strSource = "/Spawn.HDT.DustUtility;component/Resources/Images/hero_garrosh.png";
                        break;

                    case "HERO_02": //Shaman
                        strSource = "/Spawn.HDT.DustUtility;component/Resources/Images/hero_thrall.png";
                        break;

                    case "HERO_03": //Rogue
                        strSource = "/Spawn.HDT.DustUtility;component/Resources/Images/hero_valeera.png";
                        break;

                    case "HERO_04": //Paladin
                        strSource = "/Spawn.HDT.DustUtility;component/Resources/Images/hero_uther.png";
                        break;

                    case "HERO_05": //Hunter
                        strSource = "/Spawn.HDT.DustUtility;component/Resources/Images/hero_rexxar.png";
                        break;

                    case "HERO_06": //Druid
                        strSource = "/Spawn.HDT.DustUtility;component/Resources/Images/hero_malfurion.png";
                        break;

                    case "HERO_07": //Warlock
                        strSource = "/Spawn.HDT.DustUtility;component/Resources/Images/hero_guldan.png";
                        break;

                    case "HERO_08": //Mage
                        strSource = "/Spawn.HDT.DustUtility;component/Resources/Images/hero_jaina.png";
                        break;

                    case "HERO_09": //Priest
                        strSource = "/Spawn.HDT.DustUtility;component/Resources/Images/hero_anduin.png";
                        break;
                }

                if (!string.IsNullOrEmpty(strSource))
                {
                    retVal = new BitmapImage(new Uri(strSource, UriKind.Relative));

                    s_dImageCache.Add(strHero, retVal);
                }
            }

            DustUtilityPlugin.Logger.Log(Logging.LogLevel.Debug, strHero);

            return retVal;
        }
        #endregion
    }
}