﻿#region Using
using HearthDb.Enums;
using HearthMirror.Objects;
#if DEBUG
using Hearthstone_Deck_Tracker.Utility.Extensions;
#endif
using Spawn.HDT.DustUtility.Hearthstone;
using Spawn.HDT.DustUtility.Properties;
using Spawn.HDT.DustUtility.UI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
#endregion

namespace Spawn.HDT.DustUtility.UI.ViewModels
{
    public class CollectionInfoFlyoutViewModel : ViewModelBase
    {
        #region Properties
        #region CanNotifyDirtyStatus
        public override bool CanNotifyDirtyStatus => false;
        #endregion

        #region CardSetItems
        public ObservableCollection<CardSetItemModel> CardSetItems { get; set; }
        #endregion
        #endregion

        #region Ctor
        public CollectionInfoFlyoutViewModel()
        {
            CardSetItems = new ObservableCollection<CardSetItemModel>();

#if DEBUG
            if (IsInDesignMode)
            {
                ReloadRequired = true;

                InitializeAsync().Forget();
            }
#endif
        }
        #endregion

        #region InitializeAsync
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (ReloadRequired)
            {
                CardSetItems.Clear();

                List<Card> lstCollection = DustUtilityPlugin.CurrentAccount.GetCollection();

                List<CardSet> lstCardSets = new List<CardSet>(CardSets.All.Keys);

                for (int i = 0; i < lstCardSets.Count; i++)
                {
                    CardSet cardSet = lstCardSets[i];

                    if (HasCardSet(lstCollection, cardSet))
                    {
                        CardSetItems.Add(CreateCardSetItem(lstCollection, cardSet));
                    }
                }

                ReloadRequired = false;
            }
        }
        #endregion

        #region HasCardSet
        private bool HasCardSet(List<Card> lstCollection, CardSet cardSet)
        {
            bool blnRet = false;

            if (lstCollection.Count > 0)
            {
                blnRet = lstCollection.Find(c =>
                {
                    if (HearthDb.Cards.Collectible.ContainsKey(c.Id))
                    {
                        return HearthDb.Cards.Collectible[c.Id].Set == cardSet;
                    }
                    else
                    {
                        return false;
                    }
                }) != null;
            }

            return blnRet;
        }
        #endregion

        #region CreateCardSetItem
        private CardSetItemModel CreateCardSetItem(List<Card> lstCollection, CardSet cardSet)
        {
            CardSets.Info.InfoItem cardSetInfo = CardSets.Info.Dictionary[cardSet];

            return new CardSetItemModel
            {
                Logo = GetLogo(cardSet),
                BackgroundImage = GetBanner(cardSet),
                Name = CardSets.AllFullName[cardSet],
                CommonsCount = GetCountForRarity(lstCollection, cardSet, Rarity.COMMON, false),
                RaresCount = GetCountForRarity(lstCollection, cardSet, Rarity.RARE, false),
                EpicsCount = GetCountForRarity(lstCollection, cardSet, Rarity.EPIC, false),
                LegendariesCount = GetCountForRarity(lstCollection, cardSet, Rarity.LEGENDARY, false),
                DustAmount = GetDustValue(lstCollection, cardSet, Rarity.INVALID, false),
                GoldenCommonsCount = GetCountForRarity(lstCollection, cardSet, Rarity.COMMON, true),
                GoldenRaresCount = GetCountForRarity(lstCollection, cardSet, Rarity.RARE, true),
                GoldenEpicsCount = GetCountForRarity(lstCollection, cardSet, Rarity.EPIC, true),
                GoldenLegendariesCount = GetCountForRarity(lstCollection, cardSet, Rarity.LEGENDARY, true),
                GoldenDustAmount = GetDustValue(lstCollection, cardSet, Rarity.INVALID, true),
                MaxCommonsCount = cardSetInfo.MaxCommonsCount,
                MaxRaresCount = cardSetInfo.MaxRaresCount,
                MaxEpicsCount = cardSetInfo.MaxEpicsCount,
                MaxLegendariesCount = cardSetInfo.MaxLegendariesCount
            };
        }
        #endregion

        #region GetLogo
        private BitmapImage GetLogo(CardSet cardSet)
        {
            string strSource = Settings.Default.ResourcesBasePath;

            switch (cardSet)
            {
                case CardSet.GVG:
                    strSource = $"{strSource}goblins_logo.png";
                    break;

                case CardSet.TGT:
                    strSource = $"{strSource}tournament_logo.png";
                    break;

                case CardSet.OG:
                    strSource = $"{strSource}oldgods_logo.png";
                    break;

                case CardSet.GANGS:
                    strSource = $"{strSource}gadgetzan_logo.png";
                    break;

                case CardSet.UNGORO:
                    strSource = $"{strSource}ungoro_logo.png";
                    break;

                case CardSet.ICECROWN:
                    strSource = $"{strSource}frozenthrone_logo.png";
                    break;

                case CardSet.LOOTAPALOOZA:
                    strSource = $"{strSource}kobolds_logo.png";
                    break;

                case CardSet.GILNEAS:
                    strSource = $"{strSource}witchwood_logo.png";
                    break;

                case CardSet.NAXX:
                    strSource = $"{strSource}naxx_logo.png";
                    break;

                case CardSet.BRM:
                    strSource = $"{strSource}mountain_logo.png";
                    break;

                case CardSet.LOE:
                    strSource = $"{strSource}league_logo.png";
                    break;

                case CardSet.KARA:
                    strSource = $"{strSource}karazhan_logo.png";
                    break;

                default:
                    strSource = $"{strSource}hearthstone_logo.png";
                    break;
            }

            return new BitmapImage(new Uri(strSource, UriKind.Relative));
        }
        #endregion

        #region GetBanner
        private BitmapImage GetBanner(CardSet cardSet)
        {
            string strSource = Settings.Default.ResourcesBasePath;

            switch (cardSet)
            {
                case CardSet.GVG:
                    strSource = $"{strSource}goblins_banner.jpg";
                    break;

                case CardSet.TGT:
                    strSource = $"{strSource}tournament_banner.jpg";
                    break;

                case CardSet.OG:
                    strSource = $"{strSource}oldgods_banner.jpg";
                    break;

                case CardSet.GANGS:
                    strSource = $"{strSource}gadgetzan_banner.jpg";
                    break;

                case CardSet.UNGORO:
                    strSource = $"{strSource}ungoro_banner.jpg";
                    break;

                case CardSet.ICECROWN:
                    strSource = $"{strSource}frozenthrone_banner.jpg";
                    break;

                case CardSet.LOOTAPALOOZA:
                    strSource = $"{strSource}kobolds_banner.jpg";
                    break;

                case CardSet.NAXX:
                    strSource = $"{strSource}naxx_banner.jpg";
                    break;

                case CardSet.BRM:
                    strSource = $"{strSource}mountain_banner.jpg";
                    break;

                case CardSet.LOE:
                    strSource = $"{strSource}league_banner.jpg";
                    break;

                case CardSet.KARA:
                    strSource = $"{strSource}karazhan_banner.jpg";
                    break;

                default:
                    strSource = $"{strSource}hearthstone_banner.jpg";
                    break;
            }

            return new BitmapImage(new Uri(strSource, UriKind.Relative));
        }
        #endregion

        #region GetCountForRarity
        private int GetCountForRarity(List<Card> lstCollection, CardSet cardSet, Rarity rarity, bool blnIsGolden)
        {
            int nRet = 0;

            List<Card> lstChunk = lstCollection.FindAll(c =>
            {
                if (HearthDb.Cards.Collectible.ContainsKey(c.Id))
                {
                    HearthDb.Card card = HearthDb.Cards.Collectible[c.Id];

                    return card.Set == cardSet && card.Rarity == rarity && c.Premium == blnIsGolden;
                }
                else
                {
                    return false;
                }
            });

            int nMaxCount = 2;

            if (rarity == Rarity.LEGENDARY)
            {
                nMaxCount = 1;
            }

            for (int i = 0; i < lstChunk.Count; i++)
            {
                nRet += Math.Min(lstChunk[i].Count, nMaxCount);
            }

            return nRet;
        }
        #endregion

        #region GetDustValue
        private int GetDustValue(List<Card> lstCards, CardSet cardSet, Rarity rarity, bool blnIsGolden)
        {
            int nRet = 0;

            List<Card> lstChunk = lstCards;

            if (cardSet != CardSet.INVALID || rarity != Rarity.INVALID)
            {
                if (cardSet != CardSet.INVALID && rarity != Rarity.INVALID)
                {
                    lstChunk = lstCards.FindAll(c =>
                    {
                        if (HearthDb.Cards.Collectible.ContainsKey(c.Id))
                        {
                            HearthDb.Card card = HearthDb.Cards.Collectible[c.Id];

                            return card.Set == cardSet && card.Rarity == rarity && c.Premium == blnIsGolden;
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                else if (cardSet != CardSet.INVALID)
                {
                    lstChunk = lstCards.FindAll(c =>
                    {
                        if (HearthDb.Cards.Collectible.ContainsKey(c.Id))
                        {
                            HearthDb.Card card = HearthDb.Cards.Collectible[c.Id];

                            return card.Set == cardSet && c.Premium == blnIsGolden;
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                else if (rarity != Rarity.INVALID)
                {
                    lstChunk = lstCards.FindAll(c =>
                    {
                        if (HearthDb.Cards.Collectible.ContainsKey(c.Id))
                        {
                            HearthDb.Card card = HearthDb.Cards.Collectible[c.Id];

                            return card.Rarity == rarity && c.Premium == blnIsGolden;
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
            }

            for (int i = 0; i < lstChunk.Count; i++)
            {
                nRet += lstChunk[i].GetDustValue() * lstChunk[i].Count;
            }

            return nRet;
        }
        #endregion
    }
}