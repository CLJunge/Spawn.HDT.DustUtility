﻿#region Using
using HearthDb.Enums;
using HearthMirror.Objects;
using MahApps.Metro.Controls.Dialogs;
using Spawn.HDT.DustUtility.AccountManagement;
using Spawn.HDT.DustUtility.CardManagement.AutoDisenchant;
using Spawn.HDT.DustUtility.Hearthstone;
using Spawn.HDT.DustUtility.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion

namespace Spawn.HDT.DustUtility.CardManagement
{
    public static class CardsManager
    {
        #region Member Variables
        private static List<CardWrapper> s_lstUnusedCards;
        #endregion

        #region Static Ctor
        static CardsManager()
        {
            s_lstUnusedCards = new List<CardWrapper>();

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Initialized 'CardsManager'");
        }
        #endregion

        #region GetCardsAsync
        public static async Task<SearchResult> GetCardsAsync(IAccount account)
        {
            SearchResult retVal = null;

            SearchParameters parameters = account.Preferences.SearchParameters;

            if (parameters != null)
            {
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Collecting cards...");
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Parameters:");
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"QueryString={parameters.QueryString}");
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"IncludeGoldenCards={parameters.IncludeGoldenCards}");
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"UnusedCardsOnly={parameters.UnusedCardsOnly}");
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Rarities={string.Join(",", parameters.Rarities)}");
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Classes={string.Join(",", parameters.Classes)}");
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Sets={string.Join(",", parameters.Sets)}");

                List<Card> lstCollection = account.GetCollection();

                if (lstCollection.Count > 0)
                {
                    if (parameters.UnusedCardsOnly)
                        await CheckCollectionForUnusedCardsAsync(account);
                    else
                        s_lstUnusedCards = lstCollection.ConvertAll(c => new CardWrapper(c));

                    List<CardWrapper> lstCards = new List<CardWrapper>();

                    bool blnDustMode = DustUtilityPlugin.NumericRegex.IsMatch(parameters.QueryString);

                    if (blnDustMode)
                        GetCardsForDustAmount(parameters, lstCards);
                    else
                        GetCardsByQueryString(parameters, lstCards);

                    DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Found {lstCards.Count} cards");

                    retVal = SearchResult.Create(lstCards);
                }
            }

            return retVal;
        }
        #endregion

        #region GetCardsForDustAmount
        private static void GetCardsForDustAmount(SearchParameters parameters, List<CardWrapper> lstCards)
        {
            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Getting cards for {parameters.QueryString} dust...");

            //Check for invalid value
            if (!Int32.TryParse(parameters.QueryString, out int nDustAmount))
                nDustAmount = Int32.MaxValue;

            int nTotalAmount = 0;

            bool blnDone = false;

            for (int i = 0; i < parameters.Rarities.Count && !blnDone; i++)
            {
                List<CardWrapper> lstChunk = FilterByRarity(parameters.Rarities[i], parameters);

                lstChunk = FilterByClasses(lstChunk, parameters.Classes.ToList());

                lstChunk = FilterBySets(lstChunk, parameters.Sets.ToList());

                lstChunk = new List<CardWrapper>(lstChunk.OrderBy(c => c.DustValue));

                for (int j = 0; j < lstChunk.Count && !blnDone; j++)
                {
                    CardWrapper cardWrapper = lstChunk[j];

                    nTotalAmount += cardWrapper.DustValue;

                    lstCards.Add(cardWrapper);

                    blnDone = nTotalAmount >= nDustAmount;
                }
            }

            //Post processing
            //Remove low rarity cards if the total amount is over the targeted amount
            if (nTotalAmount > nDustAmount)
                RemoveRedundantCards(lstCards, parameters, nDustAmount, nTotalAmount);
        }
        #endregion

        #region RemoveRedundantCards
        private static void RemoveRedundantCards(List<CardWrapper> lstCards, SearchParameters parameters, int nDustAmount, int nTotalAmount)
        {
            if (lstCards.Count > 0 && parameters.Rarities.Count > 0)
            {
                int nDifference = nTotalAmount - nDustAmount;

                if (nDifference > 0)
                {
                    bool blnDone = false;

                    int nCurrentAmount = 0;

                    for (int i = 0; i < parameters.Rarities.Count && !blnDone; i++)
                    {
                        List<CardWrapper> lstChunk = lstCards.FindAll(c => c.DbCard.Rarity == parameters.Rarities[i]);

                        for (int j = 0; j < lstChunk.Count && !blnDone; j++)
                        {
                            CardWrapper cardWrapper = lstChunk[j];

                            nCurrentAmount += cardWrapper.DustValue;

                            int nCurrentDifference = nDifference - nCurrentAmount;

                            blnDone = nCurrentDifference == 0;

                            if (!blnDone)
                            {
                                if (nCurrentDifference < 0)
                                    blnDone = true;
                                else
                                    lstCards.Remove(cardWrapper);
                            }
                            else
                            {
                                lstCards.Remove(cardWrapper);
                            }
                        }
                    }
                }
            }

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Removed redundant cards");
        }
        #endregion

        #region GetCardsByQueryString
        private static void GetCardsByQueryString(SearchParameters parameters, List<CardWrapper> lstCards)
        {
            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Getting cards by query string: {parameters.QueryString}...");

            string strQueryString = parameters.QueryString.ToLowerInvariant();

            for (int i = 0; i < parameters.Rarities.Count; i++)
            {
                List<CardWrapper> lstChunk = FilterByRarity(parameters.Rarities[i], parameters);

                lstChunk = FilterByClasses(lstChunk, parameters.Classes.ToList());

                lstChunk = FilterBySets(lstChunk, parameters.Sets.ToList());

                for (int j = 0; j < lstChunk.Count; j++)
                {
                    CardWrapper cardWrapper = lstChunk[j];

                    if (IsCardMatch(cardWrapper, strQueryString))
                        lstCards.Add(cardWrapper);
                }
            }
        }
        #endregion

        #region IsCardMatch
        private static bool IsCardMatch(CardWrapper cardWrapper, string strKeyString)
        {
            bool blnRet = false;

            blnRet |= cardWrapper.DbCard.Name.ToLowerInvariant().Contains(strKeyString);

            blnRet |= cardWrapper.DbCard.Race.ToString().Equals(strKeyString.ToUpperInvariant());

            blnRet |= cardWrapper.DbCard.Mechanics.Any(s => s.ToLowerInvariant().Equals(strKeyString));

            blnRet |= CardSets.AllFullName[cardWrapper.DbCard.Set].ToLowerInvariant().Contains(strKeyString);

            blnRet |= cardWrapper.DbCard.Type.ToString().ToLowerInvariant().Equals(strKeyString) || (strKeyString.Equals("spell") && cardWrapper.DbCard.Type.ToString().ToLowerInvariant().Equals("ability"));

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"IsCardMatch: Name={cardWrapper.Card.Name} Key={strKeyString} > Result={blnRet}");

            return blnRet;
        }
        #endregion

        #region CheckCollectionForUnusedCardsAsync
        private static async Task CheckCollectionForUnusedCardsAsync(IAccount account)
        {
            s_lstUnusedCards.Clear();

            List<Deck> lstDecks = account.GetDecks();

            if (lstDecks.Count > 0 && lstDecks[0].Cards.Count > 0)
            {
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Checking for unused cards...");

                List<Card> lstCollection = account.GetCollection();

                for (int i = 0; i < lstCollection.Count; i++)
                {
                    Card card = lstCollection[i];
                    CardWrapper cardWrapper = new CardWrapper(card);

                    for (int j = 0; j < lstDecks.Count; j++)
                    {
                        if (!account.IsDeckExcludedFromSearch(lstDecks[j].Id) && lstDecks[j].ContainsCard(card.Id, card.Premium))
                        {
                            Card cardInDeck = lstDecks[j].GetCard(card.Id, card.Premium);

                            if (cardInDeck.Count > cardWrapper.MaxCountInDecks)
                                cardWrapper.MaxCountInDecks = cardInDeck.Count;
                        }
                    }

                    if (cardWrapper.MaxCountInDecks < 2 && cardWrapper.RawCard.Count > cardWrapper.MaxCountInDecks && !(cardWrapper.DbCard.Rarity == Rarity.LEGENDARY && cardWrapper.MaxCountInDecks == 1))
                        s_lstUnusedCards.Add(cardWrapper);
                }

                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Found {s_lstUnusedCards.Count} unused cards");
            }
            else if (!DustUtilityPlugin.IsOffline)
            {
                await DustUtilityPlugin.MainWindow.ShowMessageAsync("No decks available", "Navigate to the 'Play' page first!");
            }
        }
        #endregion

        #region FilterByClasses
        private static List<CardWrapper> FilterByClasses(List<CardWrapper> lstCards, List<CardClass> lstClasses)
        {
            List<CardWrapper> lstRet = new List<CardWrapper>();

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Filtering cards by classes... [{string.Join(",", lstClasses)}]");

            for (int i = 0; i < lstClasses.Count; i++)
                lstRet.AddRange(lstCards.FindAll(c => c.DbCard.Class == lstClasses[i]));

            return lstRet;
        }
        #endregion

        #region FilterBySets
        private static List<CardWrapper> FilterBySets(List<CardWrapper> lstCards, List<CardSet> lstSets)
        {
            List<CardWrapper> lstRet = new List<CardWrapper>();

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Filtering cards by sets... [{string.Join(",", lstSets)}]");

            for (int i = 0; i < lstSets.Count; i++)
                lstRet.AddRange(lstCards.FindAll(c => c.DbCard.Set == lstSets[i]));

            return lstRet;
        }
        #endregion

        #region FilterByRarity
        private static List<CardWrapper> FilterByRarity(Rarity rarity, SearchParameters parameters)
        {
            List<CardWrapper> lstRet = null;

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Filtering cards by rarity... ({rarity})");

            lstRet = parameters.IncludeGoldenCards
                ? (parameters.GoldenCardsOnly
                    ? s_lstUnusedCards.FindAll(c => c.DbCard.Rarity == rarity && c.RawCard.Premium)
                    : s_lstUnusedCards.FindAll(c => c.DbCard.Rarity == rarity))
                : s_lstUnusedCards.FindAll(c => c.DbCard.Rarity == rarity && !c.RawCard.Premium);

            return lstRet;
        }
        #endregion

        #region GetTotalCollectionValue
        public static int GetTotalCollectionValue(IAccount account)
        {
            int nRet = 0;

            List<Card> lstCards = account.GetCollection();

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Calculating total collection value... ({account.DisplayString})");

            for (int i = 0; i < lstCards?.Count; i++)
                nRet += lstCards[i].GetDustValue() * lstCards[i].Count;

            return nRet;
        }
        #endregion

        #region AutoDisenchant
        public static async Task<bool> AutoDisenchant(IAccount account, List<CardWrapper> lstCards)
        {
            bool blnRet = false;

            Account loggedInAcc = await Account.GetLoggedInAccountAsync();

            if (!DustUtilityPlugin.IsOffline && (account?.Equals(loggedInAcc) ?? false))
            {
                DisenchantConfig.Instance.ForceClear = true;

                blnRet = await AutoDisenchanter.Disenchant(account, lstCards, null);
            }
            else
            {
                DustUtilityPlugin.Logger.Log(LogLevel.Warning, "Invalid function call! Plugin not in offline mode or Hearthstone isn't running!");
            }

            return blnRet;
        }
        #endregion
    }
}