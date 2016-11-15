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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Blackwood.CBWMessages;
using Blackwood.Framework;
using Pacmid.Messages;
using Pacmid.Types;
using TraceSourceLogger;
using TradeHub.Common.Core.DomainModels;
using TradeHub.Common.Core.DomainModels.OrderDomain;
using TradeHub.Common.Core.FactoryMethods;
using TradeHub.Common.Core.OrderExecutionProvider;
using TradeHub.OrderExecutionProvider.Blackwood.Utility;
using TradeHub.OrderExecutionProvider.Blackwood.ValueObjects;
using Constants = TradeHub.Common.Core.Constants;
using TIMEOUT = Blackwood.CBWMessages.TIMEOUT;

namespace TradeHub.OrderExecutionProvider.Blackwood.Provider
{
    public class BlackwoodOrderExecutionProvider : IOrderExecutionProvider, IMarketOrderProvider, ILimitOrderProvider
    {
        private readonly Type _type = typeof(BlackwoodOrderExecutionProvider);

        private BWSession _session;
        private readonly ConnectionParameters _parameters;
        private readonly string _orderExecutionProviderName = Constants.MarketDataProvider.Blackwood;

        // Field to indicate User Logout request
        private bool _logoutRequest = false;

        /// <summary>
        /// Contains Local IDs to Blackwood IDs Map
        /// KEY = Local ID
        /// VALUE = Blackwood ID
        /// </summary>
        private ConcurrentDictionary<string, string> _localToBlackwoodIdsMap; 

        /// <summary>
        /// Contains all the Active Blackwood Orders
        /// KEY = BW Order ID
        /// VALUE = <see cref="BWOrder"/>
        /// </summary>
        private ConcurrentDictionary<string, BWOrder> _bwOrders;

        /// <summary>
        /// Contains all Locate Order waiting for a response
        /// KEY = BW Client Order ID
        /// Value = <see cref="BWOrder"/>
        /// </summary>
        private ConcurrentDictionary<string, BWOrder> _locateOrders;

        /// <summary>
        /// Contains Blackwood Locate Order Symbols to Local IDs Map
        /// KEY = Blackwood Symbol
        /// VALUE = Local ID (Strategy + "|" + OrderID)
        /// </summary>
        private ConcurrentDictionary<string, string> _locateOrdersToLocalIdsMap; 

        /// <summary>
        /// Arguement Constructor
        /// </summary>
        /// <param name="session">Blackwood Session</param>
        /// <param name="parameters">Parameters required for maintaining connection</param>
        public BlackwoodOrderExecutionProvider(BWSession session, ConnectionParameters parameters)
        {
            _session = session;
            _parameters = parameters;

            // Initialize
            _bwOrders = new ConcurrentDictionary<string, BWOrder>();
            _locateOrders = new ConcurrentDictionary<string, BWOrder>();
            _localToBlackwoodIdsMap = new ConcurrentDictionary<string, string>();
            _locateOrdersToLocalIdsMap = new ConcurrentDictionary<string, string>();
        }

        #region Implementation of IOrderExecutionProvider

        private event Action<Position> _onPositionMessage;

        public event Action<string> LogonArrived;
        public event Action<string> LogoutArrived;
        public event Action<Rejection> OrderRejectionArrived;
        public event Action<LimitOrder> OnLocateMessage;
        public event Action<Position> OnPositionMessage
        {
            add
            {
                if (_onPositionMessage == null)
                    _onPositionMessage += value;

            }
            remove
            {
                _onPositionMessage -= value;
            }
            
        }

        /// <summary>
        /// Connects/Starts a client
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

                // Connect to Order Server
                _session.ConnectToOrderRouting(_parameters.UserName, _parameters.Password, bwIp,
                                                  _parameters.Port, true, false, false, false);

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
        /// Disconnects/Stops a client
        /// </summary>
        public bool Stop()
        {
            try
            {
                if (_session != null)
                {
                    // Toggle Field Value
                    _logoutRequest = true;

                    // Disconnect from Orders Server
                    _session.DisconnectFromOrders();

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
        /// Is Order Execution client connected
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            try
            {
                if (_session != null)
                {
                    // Check whether the Order Execution session is connected or not
                    if (_session.IsConnectedToOrderRouting)
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

        /// <summary>
        /// Sends Locate message Accepted/Rejected response to Broker
        /// </summary>
        /// <param name="locateResponse">TradeHub LocateResponse containing Acceptence/Rejection Info</param>
        /// <returns></returns>
        public bool LocateMessageResponse(LocateResponse locateResponse)
        {
            try
            {
                // Check whether the Order Execution session is connected or not
                if (IsConnected())
                {
                    BWOrder locateMsg;
                    if (_locateOrders.TryRemove(locateResponse.OrderId, out locateMsg))
                    {
                        if (locateResponse.Accepted)
                        {
                            _locateOrdersToLocalIdsMap.AddOrUpdate(locateMsg.Symbol, locateResponse.StrategyId,
                                                                   (key, value) => locateResponse.StrategyId);

                            // Accept Locate Message
                            locateMsg.AcceptLocate();

                            if (Logger.IsInfoEnabled)
                            {
                                Logger.Info("Accepted locate message: " + locateMsg.ToString(), _type.FullName, "LocateMessageResponse");
                            }
                            return true;
                        }
                        else
                        {
                            if (Logger.IsInfoEnabled)
                            {
                                Logger.Info("Rejected locate message: " + locateMsg.ToString(), _type.FullName, "LocateMessageResponse");
                            }
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "LocateMessageResponse");
                return false;
            }
        }

        #endregion

        #region Implementation of IXOrderProvider

        public event Action<Order> NewArrived;
        public event Action<Execution> ExecutionArrived;
        public event Action<Order> CancellationArrived;
        public event Action<Rejection> RejectionArrived;

        /// <summary>
        /// Sends Limit Order on the given Order Execution Provider
        /// </summary>
        /// <param name="limitOrder">TradeHub LimitOrder</param>
        public void SendLimitOrder(LimitOrder limitOrder)
        {
            try
            {
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Sending Limit Order on Blackwood." + limitOrder, _type.FullName, "SendLimitOrder");
                }

                OrderSide tempSide = BlackwoodTypeConvertor.OrderSideConvertor.GetBlackwoodOrderSide(limitOrder.OrderSide);
                TIMEOUT tempTimeout = BlackwoodTypeConvertor.OrderTifConvertor.GetBlackwoodOrderTif(limitOrder.OrderTif);

                if (tempSide.Equals(ORDER_SIDE.NONE))
                {
                    Logger.Info("Invalid Order Side", _type.FullName, "SendLimitOrder");
                    return;
                }

                // NOTE: FFED_ID is fixed to ARCA according to StockTrader code
                // Create Blackwood Order
                //BWOrder bwOrder = new BWOrder(_session, limitOrder.Security.Symbol, tempSide,
                //                              (uint)limitOrder.OrderSize, (double)limitOrder.LimitPrice, 0,
                //                              ORDER_TYPE.LIMIT, (int)tempTimeout, FEED_ID.ARCA, false,
                //                              (uint)limitOrder.OrderSize);
                BWOrder bwOrder = new BWOrder(_session, limitOrder.Security.Symbol, tempSide,
                    (uint) limitOrder.OrderSize, (double) limitOrder.LimitPrice, 0, OrderType.LIMIT, (int) tempTimeout,
                    FeedId.ARCA, false, (uint) limitOrder.OrderSize);
                
                // Send Order to gateway
                bwOrder.Send();

                // Update Local IDs Map
                _localToBlackwoodIdsMap.TryAdd(limitOrder.OrderID, bwOrder.ClientOrderID.ToString());

                // Update BW-Orders Map
                _bwOrders.TryAdd(bwOrder.ClientOrderID.ToString(), bwOrder);

                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug("BW-Order ID: " + bwOrder.OrderID + " | BW-ClientOrder ID: " + bwOrder.ClientOrderID,
                                 _type.FullName, "SendLimitOrder");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "SendLimitOrder");
            }
        }

        /// <summary>
        /// Sends Limit Order Cancallation on the given Order Execution Provider
        /// </summary>
        /// <param name="order">TradeHub Order</param>
        public void CancelLimitOrder(Order order)
        {
            try
            {
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Sending Limit Order cancellation on Blackwood." + order, _type.FullName, "CancelLimitOrder");
                }

                string bwOrderId;
                BWOrder bwOrder;
                if (_localToBlackwoodIdsMap.TryGetValue(order.OrderID, out bwOrderId))
                {
                    if (_bwOrders.TryGetValue(bwOrderId, out bwOrder))
                    {
                        // Send Cancellation to gateway
                        bwOrder.Cancel();
                        return;
                    }
                }

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("BW-Order not found for the given ID: " + order.OrderID, _type.FullName, "CancelLimitOrder");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "CancelLimitOrder");
            }
        }

        /// <summary>
        /// Sends Market Order on the given Order Execution Provider
        /// </summary>
        /// <param name="marketOrder">TradeHub MarketOrder</param>
        public void SendMarketOrder(MarketOrder marketOrder)
        {
            try
            {
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Sending Market Order on Blackwood." + marketOrder, _type.FullName, "SendMarketOrder");
                }

                OrderSide tempSide = BlackwoodTypeConvertor.OrderSideConvertor.GetBlackwoodOrderSide(marketOrder.OrderSide);
                TIMEOUT tempTimeout = BlackwoodTypeConvertor.OrderTifConvertor.GetBlackwoodOrderTif(marketOrder.OrderTif);

                if (tempSide.Equals(ORDER_SIDE.NONE))
                {
                    Logger.Info("Invalid Order Side", _type.FullName, "SendMarketOrder");
                    return;
                }

                // Create Blackwood Order
                //BWOrder bwOrder = new BWOrder(_session, marketOrder.Security.Symbol, tempSide,
                //                              (uint)marketOrder.OrderSize, 0, 0,
                //                              ORDER_TYPE.MARKET, (int)tempTimeout, FEED_ID.NONE, false,
                //                              (uint)marketOrder.OrderSize);
                BWOrder bwOrder = new BWOrder(_session, marketOrder.Security.Symbol, tempSide,
                    (uint) marketOrder.OrderSize, 0, OrderType.MARKET, (int) tempTimeout, FeedId.NONE, false,
                    (uint) marketOrder.OrderSize);
                // Set OPG Venue
                DetermineAndSetTraderDestinationsMarket(bwOrder, marketOrder.Exchange);

                // Send Order to gateway
                bwOrder.Send();

                // Update Local IDs Map
                _localToBlackwoodIdsMap.TryAdd(marketOrder.OrderID, bwOrder.ClientOrderID.ToString());

                // Update BW-Orders Map
                _bwOrders.TryAdd(bwOrder.ClientOrderID.ToString(), bwOrder);

                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug("BW-Order ID: " + bwOrder.OrderID + " | BW-ClientOrder ID: " + bwOrder.ClientOrderID,
                                 _type.FullName, "SendMarketOrder");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "SendMarketOrder");
            }
        }

        #endregion

        /// <summary>
        /// Hook/unhook blackwood events for Order Execution
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
                //This message is for update to order
                _session.OnOrderMessage += OnOrderUpdate;
                //This messsage is for order execution
                //_session.OnExecutionMessage2 += OnOrderFill;
                _session.OnExecutionMessage3 += OnOrderFill;
                //This message is for order cancel
                //_session.OnCancelMessage2 += OnOrderCancel;
                _session.OnCancelMessage3 += OnOrderCancel;
                //This message is for order reject
                //_session.OnRejectMessage2 += OnOrderReject;
                _session.OnRejectMessage3 += OnOrderReject;
                //This message is for a position update
                //_session.OnPositionMessage2 += OnPositionUpdate;
                _session.OnPositionMessage3 += OnPositionUpdate;
                //This message is for HTB locate
                _session.OnLocateMessage += OnLocate;

                // Register Logon/Logout status event
                //_session.OnOrdersClientPortalConnectionChange += OnConnectionStatusChanged;
                _session.OnOrdersClientPortalConnectionChange2 += OnConnectionStatusChanged;
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
                //This message is for update to order
                _session.OnOrderMessage -= OnOrderUpdate;
                //This messsage is for order execution
                _session.OnExecutionMessage3 -= OnOrderFill;
                //This message is for order cancel
                _session.OnCancelMessage3 -= OnOrderCancel;
                //This message is for order reject
                _session.OnRejectMessage3 -= OnOrderReject;
                //This message is for a position update
                _session.OnPositionMessage3 -= OnPositionUpdate;
                //This message is for HTB locate
                _session.OnLocateMessage -= OnLocate;

                // Unregister Logon/Logout status event
                _session.OnOrdersClientPortalConnectionChange2 -= OnConnectionStatusChanged;

                // Unregister Network status event
                NetworkChange.NetworkAvailabilityChanged -= InternetConnectionStatusChanged;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "RemoveBlackWoodEvents");
            }
        }

        /// <summary>
        /// Called when Order Fill is receieved from Blackwood
        /// </summary>
        private void OnOrderFill(object sender, MsgExecution executionmsg)
        {
            try
            {
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Order execution receieved from Blackwood: " + executionmsg.ToString(), _type.FullName, "OnOrderFill");
                }

                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug("BW-Order ID: " + executionmsg.OrderID + " | BW-ClientOrder ID: " + executionmsg.ClOrdID + " | BW-Execution ID: " + executionmsg.ExecutionID,
                                 _type.FullName, "OnOrderFill");
                }

                lock (executionmsg)
                {
                    // Get corresponding Local Order ID in general Order IDs Map
                    string localId = (from id in _localToBlackwoodIdsMap
                                      where id.Value.Equals(executionmsg.OrderID.Value.ToString())
                                      select id.Key).FirstOrDefault();

                    // Check Locate Order ID map 
                    if (localId == null)
                    {
                        //localId = (from id in _locateOrdersToLocalIdsMap
                        //           where id.Key.Equals(executionmsg.Symbol.Value)
                        //           select id.Value).FirstOrDefault();

                        // Return if the ID is not found
                        if (!_locateOrdersToLocalIdsMap.TryRemove(executionmsg.Symbol.Value, out localId))
                        {
                            return;
                        }
                    }

                    // If Full Fill Remove from Local Map
                    if (executionmsg.Left.Value == 0)
                    {
                        BWOrder executedOrder;

                        string tempId;
                        // Remove from Ids Map
                        _localToBlackwoodIdsMap.TryRemove(localId, out tempId);

                        // Remove Executed Order from Orders Map
                        _bwOrders.TryRemove(executionmsg.OrderID.Value.ToString(), out executedOrder);
                    }

                    // Create TradeHub Execution Message
                    Fill execution = new Fill(new Security {Symbol = executionmsg.Symbol.Value},
                                              _orderExecutionProviderName, localId)
                        {
                            ExecutionDateTime = DateTime.Now,
                            ExecutionType =
                                executionmsg.Left.Value == 0
                                    ? Constants.ExecutionType.Fill
                                    : Constants.ExecutionType.Partial,
                            ExecutionId = executionmsg.ExecutionID.Value.ToString(),
                            ExecutionPrice = Convert.ToDecimal(executionmsg.Price.Value),
                            ExecutionSize = executionmsg.ExecSize.Value,
                            ExecutionSide =
                                BlackwoodTypeConvertor.OrderSideConvertor.GetTradeHubOrderSide(executionmsg.Side),
                            AverageExecutionPrice = Convert.ToDecimal(executionmsg.AvgPx.Value),
                            LeavesQuantity = executionmsg.Left.Value,
                            CummalativeQuantity = executionmsg.OrderSize.Value - executionmsg.Left.Value
                        };

                    Order order = new Order(_orderExecutionProviderName)
                        {
                            OrderID = localId,
                            BrokerOrderID = executionmsg.OrderID.Value.ToString(),
                            OrderSide =
                                BlackwoodTypeConvertor.OrderSideConvertor.GetTradeHubOrderSide(executionmsg.Side.Value),
                            Security = new Security { Symbol = executionmsg.Symbol.Value },
                            OrderSize = executionmsg.OrderSize.Value
                        };

                    // Raise Order Execution Event
                    if (ExecutionArrived != null)
                    {
                        ExecutionArrived(new Execution(execution, order)
                            {
                                OrderExecutionProvider = _orderExecutionProviderName
                            });
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnOrderFill");
            }
        }

        /// <summary>
        /// Called when Order Cancellation is received from Blackwood
        /// </summary>
        private void OnOrderCancel(object sender, MsgCancel cancelmsg)
        {
            try
            {
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Order cancellation received from Blackwood: " + cancelmsg.ToString(), _type.FullName, "OnOrderCancel");
                }

                BWOrder cancelledOrder;
                
                // Get corresponding Local Order ID
                string localId = (from id in _localToBlackwoodIdsMap
                                  where id.Value.Equals(cancelmsg.OrderID.Value.ToString())
                                  select id.Key).FirstOrDefault();

                if (localId == null)
                {
                    return;
                }

                // Remove from Ids Map
                string tempId;
                _localToBlackwoodIdsMap.TryRemove(localId, out tempId);
                

                // Remove Cancelled Order from the local Map
                _bwOrders.TryRemove(cancelmsg.OrderID.Value.ToString(), out cancelledOrder);

                Order order = new Order (_orderExecutionProviderName)
                {
                    OrderID = localId,
                    BrokerOrderID = cancelmsg.OrderID.ToString(),
                    OrderSide = BlackwoodTypeConvertor.OrderSideConvertor.GetTradeHubOrderSide(cancelmsg.Side),
                    Security = new Security { Symbol = cancelmsg.Symbol.Value }
                };

                // Raise Cancellation event
                if (CancellationArrived != null)
                {
                    CancellationArrived(order);
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnOrderCancel");
            }
        }

        /// <summary>
        /// Called  when Order Reject is received from Blackwood
        /// </summary>
        private void OnOrderReject(object sender, MsgReject rejectmsg)
        {
            try
            {
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Order rejected by Blackwood: " + rejectmsg.ToString(), _type.FullName, "OnOrderReject");
                }

                BWOrder rejectedOrder;

                // Get corresponding Local Order ID
                string localId = (from id in _localToBlackwoodIdsMap
                                  where id.Value.Equals(rejectmsg.ClientOrderID.Value.ToString())
                                  select id.Key).FirstOrDefault();

                if (localId == null)
                {
                    return;
                }

                // Remove from Ids Map
                string tempId;
                _localToBlackwoodIdsMap.TryRemove(localId, out tempId);

                // Remove Rejected Order from the List
                _bwOrders.TryRemove(rejectmsg.ClientOrderID.Value.ToString(), out rejectedOrder);

                Rejection rejection = new Rejection(new Security {Symbol = rejectmsg.Symbol.Value}, _orderExecutionProviderName)
                    {
                        OrderId = localId,
                        RejectioReason = rejectmsg.Reason.Value,
                    };

                // Raise Rejection reason
                if (OrderRejectionArrived != null)
                {
                    OrderRejectionArrived(rejection);
                }

                if (rejectmsg.Reason.Value.Equals("Not Shortable - Hard To Borrow"))
                {
                    // Get BW Stock Instance from the requested Symbol
                    BWStock bwStock = _session.GetStock(rejectmsg.Symbol.Value);

                    // Locate Stock
                    bwStock.LocateStock(rejectedOrder.Size);

                    if (Logger.IsDebugEnabled)
                    {
                        Logger.Debug(
                            "Locating " + rejectmsg.Symbol.Value + " sharess of" + rejectmsg.Symbol.Value,
                            _type.FullName, "OnOrderReject");
                    }
                    //_htbRejectID = orderReject.ClientOrderID;
                }

            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnOrderReject");
            }
        }

        /// <summary>
        /// Called when Position Update is received from Blackwood
        /// </summary>
        private void OnPositionUpdate(object sender, MsgPosition positionmsg)
        {
            try
            {
                
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Position update received from Blackwood: " + positionmsg.ToString(), _type.FullName, "OnPositionUpdate");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnPositionUpdate");
            }
            Position position=new Position();
            position.Price =(decimal) positionmsg.Price.Value;
            position.Security = new Security() {Symbol = positionmsg.Symbol};
            position.Quantity = positionmsg.PosSize.Value;
            position.AvgBuyPrice = (decimal)positionmsg.AvgBuyPrice.Value;
            position.AvgSellPrice = (decimal)positionmsg.AvgSellPrice.Value;
            position.Provider = "Blackwood";
            if (position.Quantity > 0)
                position.Type = PositionType.Long;
            else if(position.Quantity<0)
                position.Type=PositionType.Short;
            else if(position.Quantity==0)
                position.Type=PositionType.Flat;
            position.IsOpen = true;
            
            if (_onPositionMessage != null)
            {
                _onPositionMessage(position);
            }


        }

        /// <summary>
        /// Called when Order Update is received from Blackwood
        /// </summary>
        private void OnOrderUpdate(object sender, BWOrder ordermsg)
        {
            try
            {
                if (ordermsg.OrderID != 0 && ordermsg.OrderStatus == OrderStatus.Market)
                {
                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Order update received from Blackwood: " + ordermsg.ToString(), _type.FullName, "OnOrderUpdate");
                    }

                    if (Logger.IsDebugEnabled)
                    {
                        Logger.Debug("BW-Order ID: " + ordermsg.OrderID + " | BW-ClientOrder ID: " + ordermsg.ClientOrderID,
                                     _type.FullName, "OnOrderUpdate");
                    }

                    // Get corresponding Local Order ID
                    string localId = (from id in _localToBlackwoodIdsMap
                                      where id.Value.Equals(ordermsg.ClientOrderID.ToString())
                                      select id.Key).FirstOrDefault();

                    if (localId == null)
                    {
                        return;
                    }

                    Order order= new Order (_orderExecutionProviderName)
                        {
                            OrderID = localId,
                            OrderSide = BlackwoodTypeConvertor.OrderSideConvertor.GetTradeHubOrderSide(ordermsg.OrderSide2),
                            OrderSize = Convert.ToInt32(ordermsg.Size),
                            Security = new Security { Symbol = ordermsg.Symbol },
                            OrderDateTime = ordermsg.OrderTime
                        };

                    // Update IDs Map
                    _localToBlackwoodIdsMap[localId] = ordermsg.OrderID.ToString();

                    BWOrder bwOrder;
                    if (_bwOrders.TryRemove(ordermsg.ClientOrderID.ToString(), out bwOrder))
                    {
                        // Update Orders Map
                        _bwOrders.TryAdd(ordermsg.OrderID.ToString(), bwOrder);
                    }

                    // Raise Order Acceptance Event
                    if (NewArrived != null)
                    {
                        NewArrived(order);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnOrderUpdate");
            }
        }

        /// <summary>
        /// Called when HTB Locate event is Raised by Blackwood
        /// </summary>
        private void OnLocate(object sender, BWOrder locatemsg)
        {
            try
            {
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("HTB Locate event raised by Blackwood: " + locatemsg.ToString(), _type.FullName, "OnLocate");
                }

                //Create limit order containing locate parameters
                LimitOrder locateOrder = OrderMessage.GenerateLimitOrder(locatemsg.ClientOrderID.ToString(),
                                                                         new Security() {Symbol = locatemsg.Symbol},
                                                                         locatemsg.OrderSide2.ToString(),
                                                                         (int) locatemsg.Size,
                                                                         (decimal) locatemsg.LimitPrice,
                                                                         _orderExecutionProviderName);

                // Raise event to notify listeners
                if (OnLocateMessage != null)
                {
                    // Update BW Locate Orders Map
                    _locateOrders.AddOrUpdate(locatemsg.ClientOrderID.ToString(), locatemsg, (key, value) => locatemsg);

                    // Fire Event
                    OnLocateMessage(locateOrder);
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "OnLocate");
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
                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Session diconnected", _type.FullName, "OnConnectionStatusChanged");
                    }

                    // Raise Logout Event
                    if (LogoutArrived != null)
                    {
                        LogoutArrived(_orderExecutionProviderName);
                    }

                    // Clear Locate Orders Map
                    _locateOrders.Clear();

                    // Clear BW Order Map
                    _bwOrders.Clear();

                    // Clear Order IDs Map
                    _localToBlackwoodIdsMap.Clear();

                    // Unhook Blackwood events
                    RegisterBlackWoodEvents(false);

                    // Attempt Logon if the Logout was not requested by the user
                    if (!_logoutRequest)
                    {
                        // Send Logon request
                        Start();
                    }
                }
                else
                {
                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info("Session Connected", _type.FullName, "OnConnectionStatusChanged");
                    }
                 
                    // Raise Logon event
                    if (LogonArrived != null)
                    {
                        LogonArrived(_orderExecutionProviderName);
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
                        LogoutArrived(_orderExecutionProviderName);
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

        /// <summary>
        /// Determine and Set trader Destinations for Market orders
        /// </summary>
        private bool DetermineAndSetTraderDestinationsMarket(BWOrder bwOrder, string opgVenue)
        {
            try
            {
                foreach (Pacmid.Messages.TraderDestination traderDestination in _session.GetTraderDestinations().Values)
                {
                    if (bwOrder.Timeout == (int)TIMEOUT.TIMEOUT_OPG)
                    {
                        if (traderDestination.FeedID.FeedName == opgVenue
                        && traderDestination.OrderType.BWOrderType == bwOrder.OrderType
                        && (int)traderDestination.TIF.BWTIF == bwOrder.Timeout)
                        {
                            bwOrder.SetTraderDestination(traderDestination);
                            if (Logger.IsDebugEnabled)
                            {
                                Logger.Debug("Venue on the Order is: " + bwOrder.FeedId2.ToString(), _type.FullName, "DetermineAndSetTraderDestinationsOPG");
                                Logger.Debug("Venue on the Trader Destination is: " + traderDestination.FeedID.ToString(), _type.FullName, "DetermineAndSetTraderDestinationsOPG");
                            }

                            return true;
                        }
                    }
                    else if (traderDestination.FeedID.FeedName == "SMARTEDGEP"
                        && traderDestination.OrderType.BWOrderType == bwOrder.OrderType
                        && (int)traderDestination.TIF.BWTIF == bwOrder.Timeout)
                    {
                        bwOrder.SetTraderDestination(traderDestination);

                        if (Logger.IsDebugEnabled)
                        {
                            Logger.Debug("Venue on the Order is: " + bwOrder.FeedId2.ToString(), _type.FullName, "DetermineAndSetTraderDestinationsOPG");
                            Logger.Debug("Venue on the Trader Destination is: " + traderDestination.FeedID.FeedName, _type.FullName, "DetermineAndSetTraderDestinationsOPG");
                        }

                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, _type.FullName, "DetermineAndSetTraderDestinationsOPG");
            }
            return false;
        }
    }
}
