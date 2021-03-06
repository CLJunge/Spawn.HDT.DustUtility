﻿#region Using
using Spawn.HDT.DustUtility.Hearthstone;
using Spawn.HDT.DustUtility.Logging;
using Spawn.HDT.DustUtility.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
#endregion

namespace Spawn.HDT.DustUtility.UI.Controls
{
    public partial class CardImageContainer
    {
        #region Member Variables
        private CardWrapper m_wrapper;
        private ImageSource m_defaultImageSource;
        private Thickness m_defaultImageMargin;
        private Stream m_currentImageStream;
        #endregion

        #region Ctor
        public CardImageContainer()
        {
            InitializeComponent();

            m_defaultImageSource = image.Source;
            m_defaultImageMargin = image.Margin;

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Created new 'CardImageContainer' instance");
        }
        #endregion

        #region UpdateCardWrapperAsync
        public async Task UpdateCardWrapperAsync(CardWrapper wrapper)
        {
            if (wrapper != null && (wrapper.RawCard.Id != m_wrapper?.RawCard.Id || wrapper.RawCard.Premium != m_wrapper?.RawCard.Premium))
            {
                m_wrapper = wrapper;

                image.Source = m_defaultImageSource;
                image.Margin = m_defaultImageMargin;

                loadingLabel.Visibility = Visibility.Visible;

                if (m_currentImageStream != null)
                {
                    DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Disposing current image...");

                    m_currentImageStream.Dispose();
                    m_currentImageStream = null;
                }

                if (Visibility == Visibility.Visible)
                {
                    DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Loading image for {m_wrapper.RawCard.Id} (Premium={m_wrapper.RawCard.Premium})");

                    m_currentImageStream = (await CardImageProvider.GetStreamAsync(m_wrapper.RawCard.Id, m_wrapper.RawCard.Premium));

                    if (m_currentImageStream != null)
                    {
                        loadingLabel.Visibility = Visibility.Hidden;

                        if (m_wrapper.RawCard.Premium)
                        {
                            DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Setting current image as GIF");

                            image.SetValue(XamlAnimatedGif.AnimationBehavior.SourceStreamProperty, m_currentImageStream);
                        }
                        else
                        {
                            DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Setting current image as normal bitmap");

                            image.Source = (Image.FromStream(m_currentImageStream) as Bitmap).ToBitmapImage(ImageFormat.Png);
                        }

                        SetMargin();
                    }
                }
            }
        }
        #endregion

        #region SetMargin
        private void SetMargin() => image.Margin = m_wrapper.DbCard.Type == HearthDb.Enums.CardType.HERO
                ? new Thickness(-10, -35, 0, 25)
                : !m_wrapper.RawCard.Premium &&
                                    (m_wrapper.DbCard.Id.Equals("CFM_321")
                                    || m_wrapper.DbCard.Id.Equals("CFM_619")
                                    || m_wrapper.DbCard.Id.Equals("CFM_621")
                                    || m_wrapper.DbCard.Id.Equals("CFM_649")
                                    || m_wrapper.DbCard.Id.Equals("CFM_685")
                                    || m_wrapper.DbCard.Id.Equals("CFM_902"))
                    ? new Thickness(0, 0, 0, -25)
                    : m_wrapper.RawCard.Premium
                                    ? m_wrapper.DbCard.Type == HearthDb.Enums.CardType.ABILITY && m_wrapper.DbCard.Rarity == HearthDb.Enums.Rarity.LEGENDARY
                                                    ? new Thickness(-15, -30, 0, 0)
                                                    : m_wrapper.DbCard.Type == HearthDb.Enums.CardType.ABILITY || m_wrapper.DbCard.Type == HearthDb.Enums.CardType.WEAPON
                                                        ? new Thickness(0, -30, 0, 0)
                                                        : m_wrapper.DbCard.Type == HearthDb.Enums.CardType.MINION && m_wrapper.DbCard.Rarity != HearthDb.Enums.Rarity.LEGENDARY
                                                                            ? new Thickness(0, -20, -10, 0)
                                                                            : new Thickness()
                                    : !m_wrapper.RawCard.Premium ? new Thickness(0, -25, 0, 0) : new Thickness();
        #endregion
    }
}