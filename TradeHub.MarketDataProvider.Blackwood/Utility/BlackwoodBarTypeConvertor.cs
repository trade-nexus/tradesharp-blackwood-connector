using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackwood.CBWMessages;
using TradeHub.Common.Core.Constants;

namespace TradeHub.MarketDataProvider.Blackwood.Utility
{
    /// <summary>
    /// Converts Local BarType to Blackwood BarTypes and Vice Versa
    /// </summary>
    public static class BlackwoodBarTypeConvertor
    {
        /// <summary>
        /// Converts Blackwood BarType to TradeHub BarType
        /// </summary>
        /// <param name="dbartype">Blackwood BarType</param>
        /// <returns></returns>
        public static string GetBarType(DBARTYPE dbartype)
        {
            switch (dbartype)
            {
                case DBARTYPE.DAILY:
                    return BarType.DAILY;
                case DBARTYPE.WEEKLY:
                    return BarType.WEEKLY;
                case DBARTYPE.MONTHLY:
                    return BarType.MONTHLY;
                case DBARTYPE.TICK:
                    return BarType.TICK;
                default:
                    return BarType.INTRADAY;
            }
        }

        /// <summary>
        /// Converts TradeHub BarType to Blackwood BarType
        /// </summary>
        /// <param name="barType">TradeHub BarType</param>
        /// <returns></returns>
        public static Pacmid.Messages.DBARTYPE GetBarType(string barType)
        {
            switch (barType)
            {
                case BarType.DAILY:
                    return Pacmid.Messages.DBARTYPE.DAILY;
                case BarType.WEEKLY:
                    return Pacmid.Messages.DBARTYPE.WEEKLY;
                case BarType.MONTHLY:
                    return Pacmid.Messages.DBARTYPE.MONTHLY;
                case BarType.TICK:
                    return Pacmid.Messages.DBARTYPE.TICK;
                default:
                    return Pacmid.Messages.DBARTYPE.INTRADAY;
            }
        }
    }
}
