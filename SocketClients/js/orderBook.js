var btnConnect = document.querySelector("#btn-connect");
var btnClose = document.querySelector("#btn-close");
var connectionUrl = document.querySelector("#connection-url");
var sendButton = document.querySelector("#btn-send");
var symbol = document.querySelector("#txt-symbol");
var limit = document.querySelector("#txt-limit");
var dataElement = document.querySelector("#data");
var dataHeatmapElement = document.querySelector("#dataheat");
class OrderBook {
    bids = new Array();
    asks = new Array();
}


var ws;
var snapshot = new OrderBook();


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
    var msg = { "event": "subscribe", "key": `binance.${symbol.value}`, "channel": "orderbook" }
    // Send the msg object as a JSON-formatted string.
    ws.send(JSON.stringify(msg));
}

function setWebSocketClient(ws) {
    ws.onopen = (ev) => {
        console.log("Opened!");
        let auth = { "event": "auth", "account-Id": 4, "account-Token": "1c9b0aa7-ecfc-4435-8163-e13309b10b21" };
        ws.send(JSON.stringify(auth));
    }

    ws.onclose = (ev) => {
        console.log("Closed!");
    }

    ws.onmessage = (message) => {
        var data = JSON.parse(message.data);
        if (data.status) {
            if (data.status == "confirmed") {
                console.log("Authorized!");
            } else {
                console.error("Authentication Failed!");
            }
        } else if (data.event && data.event == "snapshot") {
            snapshot.bids = data.data[0];
            snapshot.asks = data.data[1];
            displayOrderBook();

        } else if (data.event && data.event == "error") {
            console.error(`code: ${data.code} | ${data.msg}`);
        } else if (data.event) {
            console.log(data.event);
        } else {
            snapshot = updateOrderBook(data);
            displayOrderBook();
        }
    }

    ws.onerror = (err) => {
        console.log(`ERROR`);
    }
}

function displayOrderBook() {
    sortSnapshot();
    var bids = getFrom(snapshot.bids, snapshot.bids.length - 10);
    var asks = getRange(snapshot.asks, 0, 10);
    dataElement.innerHTML = "";

    bids.forEach(bid => {
        dataElement.innerHTML = `
        ${bid[0]}     \t|\t ${bid[1]}` + dataElement.innerHTML;
    });

    dataElement.innerHTML = `

#############################################################
    ` + dataElement.innerHTML;

    asks.forEach(ask => {
        dataElement.innerHTML = `
        ${ask[0]}     \t|\t ${ask[1]}` + dataElement.innerHTML;
    });

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

function sortSnapshot() {
    snapshot.asks.sort((a, b) =>
    {
        return (a[0] > b[0]) ? 1 : -1;
    });

    snapshot.bids.sort((a, b) =>
    {
        return (a[0] > b[0]) ? 1 : -1;
    });
}

function getRange(array, start, count) {
    var newArray = new Array();
    for (let i = 0; i < count; i++) {
        const el = array[start + i];
        newArray.push(el);
    }
    return newArray;
}

function getFrom(array, start) {
    var newArray = new Array();
    for (let i = start; i < array.length; i++) {
        const el = array[i];
        newArray.push(el);
    }
    return newArray;
}

