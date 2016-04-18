using System;
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
