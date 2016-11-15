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
using System.Text;

namespace TradeHub.OrderExecutionProvider.Blackwood.ValueObjects
{
    /// <summary>
    /// Contains Properties required for Blackwood Connection
    /// </summary>
    public class ConnectionParameters
    {
        private string _userName;
        private int _port;
        private int _clientPort;
        private string _ip;
        private string _connectionStatus;
        private DateTime _historicDate;
        private String _password;

        #region Properties

        /// <summary>
        /// Gets/Sets the Password required to establish connection
        /// </summary>
        public String Password
        {
            set { _password = value; }
            get { return _password; }
        }

        /// <summary>
        /// Gets/Sets the Username used for establishing connection
        /// </summary>
        public string UserName
        {
            set { _userName = value.ToUpper(); }
            get { return _userName.ToUpper(); }
        }

        /// <summary>
        /// Gets/Sets the Port on which the Market Data is available
        /// </summary>
        public int Port
        {
            set { _port = value; }
            get { return _port; }
        }

        /// <summary>
        /// Gets/Sets Client Port
        /// </summary>
        public int ClientPort
        {
            set { _clientPort = value; }
            get { return _clientPort; }
        }

        /// <summary>
        /// Gets/Sets the IP Address on which to establish the connection
        /// </summary>
        public string Ip
        {
            set { _ip = value; }
            get { return _ip; }
        }

        /// <summary>
        /// Gets/Sets the Connection Status of the Blackwood session
        /// </summary>
        public string ConnectionStatus
        {
            set { _connectionStatus = value; }
            get { return _connectionStatus; }
        }

        /// <summary>
        /// Gets/Sets Historic Date
        /// </summary>
        public DateTime HistoricDate
        {
            get { return this._historicDate; }
            set { this._historicDate = value; }
        }

        #endregion

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ConnectionParameters()
        {
            _userName = string.Empty;
            _port = default(int);
            _clientPort = default(int);
            _ip = string.Empty;
            _connectionStatus = "Disconnected";
            _historicDate = DateTime.Now.AddDays(-1);
            _password = string.Empty;
        }

        /// <summary>
        /// Argument Constructor
        /// </summary>
        public ConnectionParameters(string userName, string password, string ip, int dataPort, int clientPort)
        {
            _userName = userName;
            _password = password;
            _ip = ip;
            _port = dataPort;
            _clientPort = clientPort;
            _connectionStatus = "Disconnected";
            _historicDate = DateTime.Now.AddDays(-1);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("Attributes :: ");
            stringBuilder.Append("Connection Status:" + _connectionStatus);
            stringBuilder.Append(" | Username:" + _userName);
            stringBuilder.Append(" | Password:" + _password);
            stringBuilder.Append(" | IP:" + _ip);
            stringBuilder.Append(" | Port:" +_port);
            stringBuilder.Append(" | Client Port:" + _clientPort);
            stringBuilder.Append(" | Historic Data:" + _historicDate);

            return stringBuilder.ToString();
        }
    }
}
