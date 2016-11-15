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
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Blackwood.CBWMessages;
using Blackwood.Framework;
using Pacmid.Messages;
using TraceSourceLogger;
using TradeHub.Common.Core.DomainModels;
using TradeHub.Common.Core.MarketDataProvider;
using TradeHub.Common.Core.ValueObjects.MarketData;
using TradeHub.MarketDataProvider.Blackwood.Utility;
using TradeHub.MarketDataProvider.Blackwood.ValueObjects;
using Constants = TradeHub.Common.Core.Constants;

namespace TradeHub.MarketDataProvider.Blackwood.Provider
{
    public class BlackwoodMarketDataProvider :  ILiveTickDataProvider, IHistoricBarDataProvider
    {
        private readonly Type _type = typeof (BlackwoodMarketDataProvider);

        private BWSession _session;
        private readonly ConnectionParameters _parameters;
        private readonly string _marketDataProviderName = Constants.MarketDataProvider.Blackwood;

        // Field to indicate User Logout request
        private bool _logoutRequest = false;

        /// <summary>
        /// Argument Constructor
        /// </summary>
        /// <param name="session">Blackwood Session</param>
        /// <param name="parameters">Parameters required for maintaining connection</param>
        public BlackwoodMarketDataProvider(BWSession session, ConnectionParameters parameters)
        {
            _session = session;
            _parameters = parameters;
        }

        #region Implementation of IMarketDataProvider

        #region Events

        public event Action<string> LogonArrived;
        public event Action<string> LogoutArrived;
        public event Action<MarketDataEvent> MarketDataRejectionArrived;

        #endregion

        #region Methods

        /// <summary>
        /// Start the Blackwood Data Session
        /// </summary>
        public bool Start()
        {
            try
            {
                // Toggle Field Value
                _logoutRequest = false;

                // Hook Blackwood events
                RegisterBlackWoodEvents(true);

                // Get IP address for establishing the connection
                IPAddress bwIp = IPAddress.Parse(_parameters.Ip);

                // Connect to Market Data
                _session.ConnectToMarketData(_parameters.UserName, _parameters.Password, bwIp,
                                                  _parameters.Port, true);
                // Connect to Historic Data
                _session.ConnectToHistoricData(_parameters.UserName, _parameters.Password, bwIp,
                                                    _parameters.ClientPort);

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Session connection calls successfully sent.", _type.FullName, "Start");
                }

                return true;
            }
            catch (ClientPortalConnectionException exception)
            {
                Logger.Error(exception, _type.FullName, "Start");
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "Start");
            }
            return false;
        }

        /// <summary>
        /// Stops the running Blackwood Data Session 
        /// </summary>
        public bool Stop()
        {
            try
            {
                if (_session != null)
                {
                    // Toggle Field Value
                    _logoutRequest = true;

                    // Disconnect from Live Market Data Feed
                    _session.DisconnectFromMarketData();

                    // Disconnect from Historic Data Feed
                    _session.DisconnectFromHistoricData();
                    
                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Session disconnect calls successfully sent.", _type.FullName, "Stop");
                    }
                }
                else
                {
                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Session no longer exists.", _type.FullName, "Stop");
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "Stop");
            }
            return false;
        }

        /// <summary>
        /// Indicates whether the Provider is Connected/Disconnected
        /// </summary>
        public bool IsConnected()
        {
            try
            {
                if (_session != null)
                {
                    // Check whether the Market Data session is connected or not
                    if (_session.IsConnectedToMarketData)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "IsConnected");
                return false;
            }
        }

        #endregion

        #endregion

        #region Implementation of ILiveTickDataProvider

        /// <summary>
        /// Fired when new Tick arrives
        /// </summary>
        public event Action<Tick> TickArrived;

        /// <summary>
        /// Sends Market Data request to the provider
        /// </summary>
        public bool SubscribeTickData(Subscribe request)
        {
            try
            {
                // Get BW Stock Instance from the requested Symbol
                BWStock bwStock = _session.GetStock(request.Security.Symbol);

                // Register Quote Data
                //bwStock.OnLevel1Update2 += OnLevelOneUpdate;
                bwStock.OnLevel1Update3 += OnLevelOneUpdate;
                // Register Trade Data
                //bwStock.OnTrade2 += OnTradeUpdate;
                bwStock.OnTrade3 += OnTradeUpdate;
                // Send Subscription Request
                bwStock.Subscribe();

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Sending market data request for: " + request.Security.Symbol, _type.FullName,
                                "SubscribeTickData");
                }

                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "SubscribeTickData");
                return false;
            }
        }

        /// <summary>
        /// Unsubscribe Market Data
        /// </summary>
        public bool UnsubscribeTickData(Unsubscribe request)
        {
            try
            {
                BWStock bwStock = _session.GetStock(request.Security.Symbol);

                //bwStock.OnLevel1Update2 -= OnLevelOneUpdate;
                bwStock.OnLevel1Update3 -= OnLevelOneUpdate;
                //bwStock.OnTrade2 -= OnTradeUpdate;
                bwStock.OnTrade3 -= OnTradeUpdate;
                bwStock.Unsubscribe();

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Unsubscribing market data request for: " + request.Security.Symbol, _type.FullName,
                                "UnsubscribeTickData");
                }

                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "UnsubscribeTickData");
                return false;
            }
        }

        #endregion

        #region Implementation of IHistoricBarDataProvider

        /// <summary>
        /// Fired when requested Historic Bar Data arrives
        /// </summary>
        public event Action<HistoricBarData> HistoricBarDataArrived;

        /// <summary>
        /// Historic Bar Data Request Message
        /// </summary>
        public bool HistoricBarDataRequest(HistoricDataRequest historicDataRequest)
        {
            try
            {
                // Get Blackwood BarType
                Pacmid.Messages.DBARTYPE bwBarType = BlackwoodBarTypeConvertor.GetBarType(historicDataRequest.BarType);

                // Send Historic Data Request
                _session.RequestHistoricData(historicDataRequest.Security.Symbol, bwBarType,
                                             new DateTime(historicDataRequest.StartTime.Ticks, DateTimeKind.Local),
                                             new DateTime(historicDataRequest.EndTime.Ticks, DateTimeKind.Local),
                                             historicDataRequest.Interval,
                                             Convert.ToUInt32(historicDataRequest.Id));

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Historic Data Request Sent: " + historicDataRequest, _type.FullName, "RequestHistoricBarData");
                }
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "RequestHistoricBarData");
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Hook/unhook blackwood events for martket data
        /// </summary>
        public void RegisterBlackWoodEvents(bool connect)
        {
            try
            {
                // Hook Events if the connected is opened
                if (connect)
                {
                    // Unhook events
                    RemoveBlackWoodEvents();

                    // Hook Events
                    AddBlackwoodEvents();
                }
                // Unhook Events if the connection is closed
                else
                {
                    // Unhook events
                    RemoveBlackWoodEvents();
                }
            }

            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "RegisterBlackWoodEvents");
            }
        }

        /// <summary>
        /// Adds Blackwood Events
        /// </summary>
        private void AddBlackwoodEvents()
        {
            try
            {
                // Register historic data event
                //_session.OnHistMessage2 += OnHistoricDataUpdate;
                _session.OnHistMessage3 += OnHistoricDataUpdate;

                // Register Logon/Logout status event
                //_session.OnMarketDataClientPortalConnectionChange += OnConnectionStatusChanged;
                _session.OnMarketDataClientPortalConnectionChange2 += OnConnectionStatusChanged;

                // Register Network status event
                NetworkChange.NetworkAvailabilityChanged += InternetConnectionStatusChanged;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "AddBlackwoodEvents");
            }
        }

        /// <summary>
        /// Remove Blackwood Events
        /// </summary>
        private void RemoveBlackWoodEvents()
        {
            try
            {
                // Unregister historic data event
                //_session.OnHistMessage2 -= OnHistoricDataUpdate;
                _session.OnHistMessage3 -= OnHistoricDataUpdate;
                
                // Unregister Logon/Logout status event
                //_session.OnMarketDataClientPortalConnectionChange -= OnConnectionStatusChanged;
                _session.OnMarketDataClientPortalConnectionChange2 -= OnConnectionStatusChanged;

                // Unregister Network status event
                NetworkChange.NetworkAvailabilityChanged -= InternetConnectionStatusChanged;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "RemoveBlackWoodEvents");
            }
        }
        
        /// <summary>
        /// Called when Quote update arrives
        /// </summary>
        private void OnLevelOneUpdate(object sender, MsgLevel1 quote)
        {
            try
            {
                lock (quote)
                {
                    Security security = new Security();
                    security.Symbol = quote.Symbol.Value;
                    Tick tick = new Tick(security, Constants.MarketDataProvider.Blackwood, DateTime.Now)
                        {
                            AskPrice = Convert.ToDecimal(quote.Ask.Value),
                            AskSize = Convert.ToDecimal(quote.AskSize.Value),
                            BidPrice = Convert.ToDecimal(quote.Bid.Value),
                            BidSize = Convert.ToDecimal(quote.BidSize.Value),
                        };

                    // Raise Tick Arrived Event
                    if (TickArrived != null)
                    {
                       TickArrived(tick);
                    }

                    // Log incoming price event
                    if (Logger.IsDebugEnabled)
                    {
                        Logger.Debug(String.Format("Quote Arrived for: {0} | Ask: {1} | AskSize: {2} " +
                                                   "| Bid: {3} | BidSize: {4} | Time: {5}",
                                                   tick.Security, tick.AskPrice, tick.AskSize,
                                                   tick.BidPrice, tick.BidSize, tick.DateTime),
                                     _type.FullName, "OnLevelOneUpdate");
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnLevelOneUpdate");
            }
        }

        /// <summary>
        /// Called when Blackwood Trade arrives (contains price info)
        /// </summary>
        private void OnTradeUpdate(object sender, MsgTrade trade)
        {
            try
            {
                lock (trade)
                {
                    Security security = new Security();
                    security.Symbol = trade.Symbol.Value;
                    Tick tick = new Tick(security, Constants.MarketDataProvider.Blackwood, trade.Time.Value)
                        {
                            LastPrice = Convert.ToDecimal(trade.Price.Value),
                            LastSize = Convert.ToDecimal(trade.TradeSize.Value),
                        };

                    // Raise Tick Arrived Event
                    if (TickArrived != null)
                    {
                        TickArrived(tick);
                    }
                }
                // Log incoming price event
                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug(String.Format("Trade Arrived for: {0} | Price: {1} | Time: {2}",
                                               trade.Symbol.Value, Convert.ToDecimal(trade.Price.Value),
                                               trade.Time.Value),
                                 _type.FullName, "OnTradeUpdate");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnTradeUpdate");
            }
        }

        /// <summary>
        /// Handles historic data event.
        /// </summary>
        private void OnHistoricDataUpdate(object sender, MsgHistResponse histMsg)
        {
            try
            {
                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug("Historic Data recieved for: " + histMsg.Symbol.Value + " containing: " + histMsg.BarCount + " bars.", _type.FullName, "OnHistoricDataUpdate");
                }

                Bar[] bars = new Bar[histMsg.BarCount];
                int count = 0;
                foreach (MsgHistResponse.BarData barData in histMsg.Bars)
                {
                    // Create new TradeHub Bar
                    Bar bar = new Bar(new Security() { Symbol = histMsg.Symbol.Value }, _marketDataProviderName,
                                                        histMsg.ReqID.Value.ToString(CultureInfo.InvariantCulture), barData.Time)
                        {
                            Close = Convert.ToDecimal(barData.Close),
                            Open = Convert.ToDecimal(barData.Open),
                            High = Convert.ToDecimal(barData.High),
                            Low = Convert.ToDecimal(barData.Low),
                            Volume = barData.Volume
                        };

                    // Add to the Array
                    bars[count] = bar;
                    count++;
                }

                // Create Historical Bar Data Message to be sent
                HistoricBarData historicBarData = new HistoricBarData(new Security() { Symbol = histMsg.Symbol.Value }, _marketDataProviderName, DateTime.Now)
                    {
                        Bars = bars,
                        ReqId = histMsg.ReqID.Value.ToString(CultureInfo.InvariantCulture)
                    };

                // Raise Event
                if (HistoricBarDataArrived != null)
                {
                    HistoricBarDataArrived(historicBarData);
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnHistoricDataUpdate");
            }
        }

        /// <summary>
        /// Invoked when a change in connection occurs
        /// </summary>
        private void OnConnectionStatusChanged(object sender, bool connected, string message)
        {
            try
            {
                if (connected == false)
                {
                    // Raise Logout Event
                    if (LogoutArrived != null)
                    {
                        LogoutArrived(_marketDataProviderName);   
                    }

                    // Unhook Blackwood events
                    RegisterBlackWoodEvents(false);

                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Session diconnected", _type.FullName, "OnConnectionStatusChanged");
                    }

                    // Attempt Logon if the Logout was not requested by the user
                    if (!_logoutRequest)
                    {
                        // Send Logon request
                        Start();
                    }
                }
                else
                {
                    // Raise Logon event
                    if (LogonArrived != null)
                    {
                        LogonArrived(_marketDataProviderName);   
                    }

                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Session Connected", _type.FullName, "OnConnectionStatusChanged");
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnConnectionStatusChanged");
            }
        }

        /// <summary>
        /// Invoked when a change in connection occurs
        /// </summary>
        private void InternetConnectionStatusChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            try
            {
                if (!e.IsAvailable)
                {
                    // Raise Logout Event
                    if (LogoutArrived != null)
                    {
                        LogoutArrived(_marketDataProviderName);
                    }

                    // Unhook Blackwood events
                    RegisterBlackWoodEvents(false);

                    // Notify connection loss
                    ConnectionErrorNotification();
                }

                else
                {
                    // End Blackwood Session
                    _session = null;

                    // Restart Blackwood Session
                    Start();
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "InternetConnectionStatusChanged");
            }
        }

        /// <summary>
        /// Displays the Error notification stating connection loss
        /// </summary>
        private void ConnectionErrorNotification()
        {
            try
            {
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Session disconnected due to connection loss.", _type.FullName, "ConnectionErrorNotification");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "ConnectionErrorNotification");
            }
        }
    }
}
