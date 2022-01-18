class OrderBook {
    bids = new Array();
    asks = new Array();
}
class Heatmap {
    constructor(openPrice) {
        this.range = openPrice / 100;

        this.openPrice = openPrice;

        for (let i = 0; i < 200; i++) {
            this.blocks.push(0);
        }
    }

    range;
    openPrice;
    blocks = new Array();
}


var btnConnect = document.querySelector("#btn-connect");
var btnClose = document.querySelector("#btn-close");
var connectionUrl = document.querySelector("#connection-url");
var sendButton = document.querySelector("#btn-send");
var showHeatmapButton = document.querySelector("#btn-heatmap");
var symbol = document.querySelector("#txt-symbol");
var limit = document.querySelector("#txt-limit");
var dataElement = document.querySelector("#data");
var ShowHeatmap = false;

var candleKey = "";
var orderbookKey = "";
var candleChanId = 0;
var orderbookChanId = 0;

var openPrice = null;

// wsc.onopen = (ev) => {
//     console.log("Opened!");
// }

// wsc.onmessage = (mes) => {
//     var data = JSON.parse(mes.data);
//     if (data[0] && data[1] && data[1].length == 6)

// }

var ws;
var snapshot = new OrderBook();


/// for order book

btnClose.onclick = () => {
    try {
        ws.close();
    } catch (error) {
    }
}

btnConnect.onclick = () => {
    ws = new WebSocket(connectionUrl.value);
    setWebSocketClient(ws);
}

sendButton.onclick = () => {
    orderbookKey = `binance.${symbol.value}`;
    var msg = { "event": "subscribe", "key": orderbookKey, "channel": "orderbook" }

    ws.send(JSON.stringify(msg));
}

function setWebSocketClient(ws) {
    ws.onopen = (ev) => {
        console.log("Opened!");
        let auth = { "event": "auth", "account-Id": 4, "account-Token": "1c9b0aa7-ecfc-4435-8163-e13309b10b21" };
        candleKey = `binance.${symbol.value}:1m`;
        var msg = { "event": "subscribe", "key": candleKey, "channel": "candle" }
        ws.send(JSON.stringify(auth));
        ws.send(JSON.stringify(msg));
    }

    ws.onclose = (ev) => {
        console.log("Closed!");
    }

    ws.onmessage = (message) => {
        var data = JSON.parse(message.data);
        if (data.event == "subscribed") {
            if (data.key = orderbookKey)
                orderbookChanId = data.chanId;
            else if (data.key = candleKey)
                candleChanId = data.chanId;
            return;
        }

        if (data.status) {
            if (data.status == "confirmed") {
                console.log("Authorized!");
            } else {
                console.error("Authentication Failed!");
            }
        } else if (data.event == "snapshot" && data.chanId == orderbookChanId) {
            snapshot.bids = data.data[0];
            snapshot.asks = data.data[1];
            displayHeatmap();
            console.log("Snapshot");
            console.log(snapshot);
        } else if (data.event && data.event == "snapshot" && data.chanId == candleChanId) {
            openPrice = data.candle[1][1];
        } else if (data[0] == candleChanId) {
            openPrice = data[1][1];
        } else if (data.event && data.event == "error") {
            console.error(`code: ${data.code} | ${data.msg}`);
        } else if (data.event) {
            console.log(data.event);
        } else {
            snapshot = updateOrderBook(data);
            displayHeatmap();
        }
    }

    ws.onerror = (err) => {
        console.log(`ERROR`);
    }
}

function updateOrderBook(data) {
    try {
        var bids = data[1][0];
        var asks = data[1][1];
        for (const item of asks) {
            var price = item[0];
            var quantity = item[1];

            if (quantity == 0) {  // remove the item in snapshot ask

                snapshot.asks.forEach((val, index) => {
                    if (val[0] == price) {
                        snapshot.asks.splice(index, 1);
                    }
                });
            }
            else {
                let index = -1;
                snapshot.asks.filter((ask, i) => {
                    let val = ask[0] == price;
                    if (val) {
                        index = i;
                    }
                    return val;
                });

                if (index == -1) {
                    snapshot.asks.push(item);
                } else {
                    snapshot.asks[index][1] = quantity;
                }
            }
        }

        for (const item of bids) {
            var price = item[0];
            var quantity = item[1];

            if (quantity == 0) {  // remove the item in snapshot ask

                snapshot.bids.forEach((val, index) => {
                    if (val[0] == price) {
                        snapshot.bids.splice(index, 1);
                    }
                });
            }
            else {
                let index = -1;
                snapshot.bids.filter((ask, i) => {
                    let val = ask[0] == price;
                    if (val) {
                        index = i;
                    }
                    return val;
                });

                if (index == -1) {
                    snapshot.bids.push(item);
                } else {
                    snapshot.bids[index][1] = quantity;
                }
            }
        }
    } catch (error) {
        console.log(error);
    }
    return snapshot;
}

function displayHeatmap() {
    dataElement.innerHTML = "";
    if (openPrice == null) {
        return;
    }
    heatmap = new Heatmap(openPrice);

    canculateHeatmap(heatmap);
    heatmap.blocks.forEach((val, index) => {
        dataElement.innerHTML = `
    ${val}   \t\t  ${heatmap.range * index} - ${(heatmap.range) * (index + 1)} ` + dataElement.innerHTML;
    });

    dataElement.innerHTML = `
         Range: ${heatmap.range}
    Open Price: ${heatmap.openPrice}
=================================================================
` + dataElement.innerHTML;
    function canculateHeatmap(heatmap) {
        var bids = snapshot.bids;

        var asks = snapshot.asks;

        const step = heatmap.range;
        var minRange = 0;
        var maxRange = step;

        for (let i = 0; i < heatmap.blocks.length; i++) {
            heatmap.blocks[i] = 0;
            for (var order of bids) {
                if (minRange <= order[0] && order[0] < maxRange) {
                    heatmap.blocks[i] += order[1];
                }
            }

            for (var order of asks) {
                if (minRange <= order[0] && order[0] < maxRange) {
                    heatmap.blocks[i] += order[1];
                }
            }

            minRange = maxRange;
            maxRange += step;
        }
    }
}













function updateHeatmap(data) {
    var bids = data[1][0];
    var asks = data[1][1];
    var blocks = heat.blocks;
    var step = heat.range;
    var minRange = 0;
    var maxRange = step;

    for (let i = 0; i < blocks.length; i++) {

        blocks[i] = 0;

        for (const a of bids) {

            if (minRange <= a[0] && a[0] < maxRange) {
                blocks[i] += a[1];
            }
        }

        for (const b of asks) {

            if (minRange <= b[0] && [0] < maxRange) {
                blocks[i] += b[1];
            }
        }
        minRange = maxRange;
        maxRange += step;
    }

    return heat
}
