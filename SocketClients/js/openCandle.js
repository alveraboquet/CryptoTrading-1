var btnConnect = document.querySelector("#btn-connect");
var connectionUrl = document.querySelector("#connection-url");
var sendButton = document.querySelector("#btn-send");
var symbol = document.querySelector("#txt-symbol");
var timeFrame = document.querySelector("#txt-timeFrame");
var dataElement = document.querySelector("#data");
var chanId = 0;
var btnClose = document.querySelector("#btn-close");
btnClose.onclick = () => {
    try {
        ws.close();
    } catch (error) {
    }
}
var ws;
var openPrice;
btnConnect.onclick = () => {
    ws =  new WebSocket(connectionUrl.value);
    setWebSocketClient(ws);
}

sendButton.onclick = () => {
    var msg = {"event":"subscribe", "key": `binance.${symbol.value}:${timeFrame.value}`, "channel":"candle"}
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
        } 
        else if (data.event && data.event == "subscribed") {
            console.log(data.event + ' | ' + data.chanId);
            chanId = data.chanId;
        } 
        else if (data.event && data.event == "snapshot") {
            var candle = data.candle;
            dispalyCandle(candle);
        }
        else if(data[0] == chanId) {
            var candle = data[1];
            dispalyCandle(candle);
        }
    }
    
    ws.onerror = (err) => {
        console.log(`ERROR`);
    }
}
function dispalyCandle(candle) {
    var openTime = new Date(candle[0]);
            dataElement.innerHTML = `
            OpenTime: ${openTime.toLocaleTimeString()}
            --------------------------------
            O: ${candle[1]}
            H: ${candle[2]}
            L: ${candle[3]}
            C: ${candle[4]}
            V: ${candle[5]}
            `;
}