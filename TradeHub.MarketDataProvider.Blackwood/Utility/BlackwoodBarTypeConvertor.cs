/***************************************************************************** 
* Copyright 2016 Aurora Solutions 
* 
*    http://www.aurorasolutions.io 
* 
* Aurora Solutions is an innovative services and product company at 
* the forefront of the software industry, with processes and practices 
* involving Domain Driven Design(DDD), Agile methodologies to build 
* scalable, secure, reliable and high performance products.
* 
* TradeSharp is a C# based data feed and broker neutral Algorithmic 
* Trading Platform that lets trading firms or individuals automate 
* any rules based trading strategies in stocks, forex and ETFs. 
* TradeSharp allows users to connect to providers like Tradier Brokerage, 
* IQFeed, FXCM, Blackwood, Forexware, Integral, HotSpot, Currenex, 
* Interactive Brokers and more. 
* Key features: Place and Manage Orders, Risk Management, 
* Generate Customized Reports etc 
* 
* Licensed under the Apache License, Version 2.0 (the "License"); 
* you may not use this file except in compliance with the License. 
* You may obtain a copy of the License at 
* 
*    http://www.apache.org/licenses/LICENSE-2.0 
* 
* Unless required by applicable law or agreed to in writing, software 
* distributed under the License is distributed on an "AS IS" BASIS, 
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
* See the License for the specific language governing permissions and 
* limitations under the License. 
*****************************************************************************/


﻿using System;
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
