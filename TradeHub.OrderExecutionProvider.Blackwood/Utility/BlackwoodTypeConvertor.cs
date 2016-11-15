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
using Pacmid.Types;
using TradeHub.Common.Core.DomainModels.OrderDomain;
using Constants = TradeHub.Common.Core.Constants;

namespace TradeHub.OrderExecutionProvider.Blackwood.Utility
{
    /// <summary>
    /// Converts TradeHub types to Blackwood Type and Vice Versa
    /// </summary>
    public static class BlackwoodTypeConvertor
    {
        /// <summary>
        /// Converts Order Side
        /// </summary>
        public static class OrderSideConvertor
        {
            /// <summary>
            /// Converts TradeHub Order Side to Blackwood Order Side
            /// </summary>
            /// <param name="orderSide">TradeHub Order Side</param>
            /// <returns></returns>
            public static OrderSide GetBlackwoodOrderSide(string orderSide)
            {
                switch (orderSide)
                {
                    case Constants.OrderSide.BUY:
                        return OrderSide.Buy;
                    case Constants.OrderSide.SELL:
                        return OrderSide.Sell;
                    case Constants.OrderSide.COVER:
                        return OrderSide.Cover;
                    case Constants.OrderSide.SHORT:
                        return OrderSide.Short;
                    default:
                        return OrderSide.None;
                }
            }

            /// <summary>
            /// Converts Blackwood Order Side to TradeHub Order Side
            /// </summary>
            /// <param name="orderSide"></param>
            /// <returns></returns>
            public static string GetTradeHubOrderSide(OrderSide orderSide)
            {
                switch (orderSide)
                {
                    case OrderSide.Buy:
                        return Constants.OrderSide.BUY;
                    case OrderSide.Sell:
                        return Constants.OrderSide.SELL;
                    case OrderSide.Cover:
                        return Constants.OrderSide.COVER;
                    case OrderSide.Short:
                        return Constants.OrderSide.SHORT;
                    default:
                        return Constants.OrderSide.NONE;
                }
            }
        }

        /// <summary>
        /// Converts Order TIF value
        /// </summary>
        public static class OrderTifConvertor
        {
            /// <summary>
            /// Converts TradeHub TIF value to Blackwood TIMEOUT value
            /// </summary>
            /// <param name="tif">TradeHub TIF</param>
            /// <returns></returns>
            public static TIMEOUT GetBlackwoodOrderTif(string tif)
            {
                switch (tif)
                {
                    case Constants.OrderTif.DAY:
                        return TIMEOUT.TIMEOUT_DAY;
                    case Constants.OrderTif.GTC:
                        return TIMEOUT.TIMEOUT_GTC;
                    case Constants.OrderTif.GTX:
                        return TIMEOUT.TIMEOUT_GTX;
                    case Constants.OrderTif.IOC:
                        return TIMEOUT.TIMEOUT_IOC;
                    case Constants.OrderTif.OPG:
                        return TIMEOUT.TIMEOUT_OPG;
                    default :
                        return TIMEOUT.TIMEOUT_MARKET_CLOSE;
                }
            }

            /// <summary>
            /// Converts Blackwood TIMEOUT value to TradeHub TIF value
            /// </summary>
            /// <param name="timeout">Blackwood TIMEOUT</param>
            /// <returns></returns>
            public static string GetTradeHubOrderTif(TIMEOUT timeout)
            {
                switch (timeout)
                {
                    case TIMEOUT.TIMEOUT_DAY:
                        return Constants.OrderTif.DAY;
                    case TIMEOUT.TIMEOUT_GTC:
                        return Constants.OrderTif.GTC;
                    case TIMEOUT.TIMEOUT_GTX:
                        return Constants.OrderTif.GTX;
                    case TIMEOUT.TIMEOUT_IOC:
                        return Constants.OrderTif.IOC;
                    case TIMEOUT.TIMEOUT_OPG:
                        return Constants.OrderTif.OPG;
                    default:
                        return Constants.OrderTif.NONE;
                }
            }
        }
    }
}
