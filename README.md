### Blackwood-Connector

***

This project contains a connector of Blackwood for TradeSharp (a C# based Algorithmic Trading Platform).

Find more about TradeSharp [here](https://www.tradesharp.se/).

### Getting Started
***

#### Tools
- Microsoft Visual Studio 2012 or higher
- .NET Framework 4.5.1


#### Prerequisites
- Working TradeSharp Application
- Blackwood Credentials

#### Setting up The Connector
1. Download the connector zip file or clone the repository.
2. Add the folowing params files for the Blackwood Connector.

- In **TradeHub.MarketDataProvider.Blackwood -> Config** add ***BlackwoodParams.xml*** file with following content.
```
    <?xml version="1.0" encoding="utf-8" ?>
    <Blackwood>
      <Username>USERNAME</Username>
      <Password>PASSWORD</Password>
      <Ip>IP_ADDRESS</Ip>
      <Port>1234</Port>
      <ClientPort>2345</ClientPort>
    </Blackwood>
```

- In **TradeHub.OrderExecutionProvider.Blackwood -> Config** add ***BlackwoodOrderParams.xml*** file with following content.
```
    <?xml version="1.0" encoding="utf-8" ?>
    <Blackwood>
      <Username>USERNAME</Username>
      <Password>PASSWORD</Password>
      <Ip>IP_ADDRESS</Ip>
      <Port>1234</Port>
      <ClientPort>2345</ClientPort>
    </Blackwood>
```

- In **TradeHub.MarketDataProvider.Blackwood.Tests -> Config** add ***BlackwoodTestParams.xml*** file with following content.
```
    <?xml version="1.0" encoding="utf-8" ?>
    <Blackwood>
      <Username>USERNAME</Username>
      <Password>PASSWORD</Password>
      <Ip>IP_ADDRESS</Ip>
      <Port>1234</Port>
      <ClientPort>2345</ClientPort>
    </Blackwood>
```

**Clean** and **Build** the code.

### Bugs

Please report bugs [here](https://github.com/trade-nexus/bugs)
