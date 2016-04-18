using NUnit.Framework;
using Spring.Context.Support;
using TradeHub.MarketDataProvider.Blackwood.Utility;
using TradeHub.MarketDataProvider.Blackwood.ValueObjects;

namespace TradeHub.MarketDataProvider.Blackwood.Tests.Integration
{
    [TestFixture]
    public class ConnectionParametersTestCases
    {
        private ConnectionParametersLoader _parametersLoader;
        private ConnectionParameters _parameters;

        [SetUp]
        public void SetUp()
        {
            _parametersLoader = ContextRegistry.GetContext()["BWConnectionParametersLoader"] as ConnectionParametersLoader;
            if (_parametersLoader != null) _parameters = _parametersLoader.Parameters;
        }

        [Test]
        [Category("Integration")]
        public void ReadParametersTestCase()
        {
            Assert.AreEqual("35PAIR", _parameters.UserName);
            Assert.AreEqual("123456", _parameters.Password);
            Assert.AreEqual("72.5.42.156", _parameters.Ip);
            Assert.AreEqual(5200, _parameters.Port);
            Assert.AreEqual(5300, _parameters.ClientPort);
        }
    }
}
