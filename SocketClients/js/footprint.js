class Footprint {
    constructor(openPrice) {
        this.openPrice = openPrice;
        this.range = openPrice * 0.0003;
        this.above.push([0,0]);
        this.below.push([0,0]);
    }

    range = 0;
    openPrice = 0;
    above = new Array();
    below = new Array();
}
class Trade {
    constructor(amount, price, tradeTime) {
        this.amount = amount;
        this.price = price;
        this.tradeTime = new Date(tradeTime);
    }
    amount;
    price;
    tradeTime;
}



var btnConnect = document.querySelector("#btn-connect");
var connectionUrl = document.querySelector("#connection-url");
var sendButton = document.querySelector("#btn-send");
var symbol = document.querySelector("#txt-symbol");
var timeFrame = document.querySelector("#txt-timeFrame");
var dataElement = document.querySelector("#data");

var btnClose = document.querySelector("#btn-close");
btnClose.onclick = () => {
    try {
        ws.close();
    } catch (error) {
        console.error('first connect.');
    }
}
var ws;

btnConnect.onclick = () => {
    ws = new WebSocket(connectionUrl.value);
    setWebSocketClient(ws);
}

var candleKey = "";
var tradeKey = "";
var candleChanId = 0;
var tradeChanId = 0;

var candleOpenTime = 0;
var fp = null;

sendButton.onclick = () => {
    candleKey = `binance.${symbol.value}:${timeFrame.value}`;

    var msg = { "event": "subscribe", "key": candleKey, "channel": "candle" }

    // Send the msg object as a JSON-formatted string.
    ws.send(JSON.stringify(msg));
}

function setWebSocketClient(ws) {
    ws.onopen = (ev) => {
        console.log("Opened!");
        let auth = { "event": "auth", "account-Id": 4, "account-Token": "1c9b0aa7-ecfc-4435-8163-e13309b10b21" };
        ws.send(JSON.stringify(auth));
        tradeKey = `binance.${symbol.value}`;
        var subTrade = { "event": "subscribe", "key": tradeKey, "channel": "trade" }
        ws.send(JSON.stringify(subTrade));
    }

    ws.onclose = (ev) => {
        console.log("Closed!");
    }

    ws.onmessage = (message) => {
        var data = JSON.parse(message.data);
        if (data.status) {
            if (data.status == "confirmed") {
                console.log("Authorized!");
            }
            else {
                console.error("Authentication Failed!");
            }
        }
        else if (data.event && data.event == "subscribed") {
            console.log(`${data.event} ${data.channel} => ${data.key} | ${data.chanId}`);
            if (data.key == candleKey)
                candleChanId = data.chanId;
            else if (data.key == tradeKey)
                tradeChanId = data.chanId;
        }
        else if (data.event && data.event == "snapshot" && data.chanId == candleChanId) {
            console.log(data);
            var footprint = data.footprint;
            candleOpenTime = data.candle[0];
            fp = new Footprint(footprint[0]);
            fp.range = footprint[1];
            fp.above =  footprint[2];
            fp.below =  footprint[3];
            displayFooprint();
        }
        else if (data[0] == tradeChanId) {
            console.log("Trade:");
            updateFootprint(data);
            displayFooprint();
        }
        else if (data[0] == candleChanId){
            //console.log("Candle:");
            var candle = data[1];
            if (candle[0] > candleOpenTime)
            {
                candleOpenTime = candle[0];
                fp = new Footprint(candle[1])
            }

        }
    }

    ws.onerror = (err) => {
        console.log(`ERROR`);
    }
}

function updateFootprint(data) {
    if (fp != null) {
        var trade = new Trade(data[1][2], data[1][1], data[1][0]);

        let price = trade.price;
        let openPrice = fp.openPrice;
        let diff = Math.abs(openPrice - price);
        let step = fp.range;
        let index = Math.floor((diff / step));

        let type = (trade.amount < 0) ? 0 : 1;
        let amount = Math.abs(trade.amount);
        if (price >= openPrice) { // its above
            if (index <= fp.above.length - 1) {  // exist
                fp.above[index][type] += amount;
            }
            else {   // add new one
                for (let i = fp.above.length; i <= index; i++) {
                    var block = [0, 0];
                    if (i == index) {
                        block[type] += amount;
                    }
                    fp.above.push(block);
                }
            }
        }
        else { // its below
            if (index <= fp.below.length - 1) {  // exist
                fp.below[index][type] += amount;
            }
            else {    // add new one
                for (let i = fp.below.length; i <= index; i++) {
                    var block = [0, 0];
                    if (i == index) {
                        block[type] += amount;
                    }
                    fp.below.push(block);
                }
            }
        }

    }
}

function displayFooprint() {
    if (fp == null) return;

    dataElement.innerHTML = `
    OpenPrice: ${fp.openPrice.toString()}
    Range:     ${fp.range.toString()}
    ---------------------------------------
    `;
    var aboves = "";
    fp.above.forEach((val, index) => {
        aboves = `
        ${val[1]} \t ${val[0]}` + aboves;
    });
    dataElement.innerHTML += aboves;
    dataElement.innerHTML += `
        =========================`;
    fp.below.forEach((val, index) => {
        dataElement.innerHTML += `
        ${val[1]} \t ${val[0]}`;
    });
}