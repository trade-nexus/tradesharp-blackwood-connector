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
using NUnit.Framework;
using TradeHub.Common.Core.Constants;
using TradeHub.MarketDataProvider.Blackwood.Utility;
namespace TradeHub.MarketDataProvider.Blackwood.Tests.Unit
{
    [TestFixture]
    class BlackwoodBarTypeConvertorTests
    {
        [Test]
        [Category("Unit")]
        public void GetBlackwoodBarTypeTestCase()
        {
            Pacmid.Messages.DBARTYPE dbartypeIntraDay = BlackwoodBarTypeConvertor.GetBarType(BarType.INTRADAY);
            Pacmid.Messages.DBARTYPE dbartypeDaily = BlackwoodBarTypeConvertor.GetBarType(BarType.DAILY);
            Pacmid.Messages.DBARTYPE dbartypeWeekly = BlackwoodBarTypeConvertor.GetBarType(BarType.WEEKLY);
            Pacmid.Messages.DBARTYPE dbartypeMonthly = BlackwoodBarTypeConvertor.GetBarType(BarType.MONTHLY);
            Pacmid.Messages.DBARTYPE dbartypeTick = BlackwoodBarTypeConvertor.GetBarType(BarType.TICK);

            Assert.AreEqual(DBARTYPE.INTRADAY, dbartypeIntraDay, "Intra Bar");
            Assert.AreEqual(DBARTYPE.DAILY, dbartypeDaily, "Daily Bar");
            Assert.AreEqual(DBARTYPE.WEEKLY, dbartypeWeekly, "Weekly Bar");
            Assert.AreEqual(DBARTYPE.MONTHLY, dbartypeMonthly, "Monthly Bar");
            Assert.AreEqual(DBARTYPE.TICK, dbartypeTick, "Tick Bar");
        }

        [Test]
        [Category("Unit")]
        public void GetTradeBarTypeTestCase()
        {
            string bartypeIntraDay = BlackwoodBarTypeConvertor.GetBarType(DBARTYPE.INTRADAY);
            string bartypeDaily = BlackwoodBarTypeConvertor.GetBarType(DBARTYPE.DAILY);
            string bartypeWeekly = BlackwoodBarTypeConvertor.GetBarType(DBARTYPE.WEEKLY);
            string bartypeMonthly = BlackwoodBarTypeConvertor.GetBarType(DBARTYPE.MONTHLY);
            string bartypeTick = BlackwoodBarTypeConvertor.GetBarType(DBARTYPE.TICK);

            Assert.AreEqual(BarType.INTRADAY, bartypeIntraDay, "Intra Bar");
            Assert.AreEqual(BarType.DAILY, bartypeDaily, "Daily Bar");
            Assert.AreEqual(BarType.WEEKLY, bartypeWeekly, "Weekly Bar");
            Assert.AreEqual(BarType.MONTHLY, bartypeMonthly, "Monthly Bar");
            Assert.AreEqual(BarType.TICK, bartypeTick, "Tick Bar");
        }
    }
}
