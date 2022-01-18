var btnConnect = document.querySelector("#btn-connect");
var connectionUrl = document.querySelector("#connection-url");
var sendButton = document.querySelector("#btn-send");
var symbol = document.querySelector("#txt-symbol");
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
    ws =  new WebSocket(connectionUrl.value);
    setWebSocketClient(ws);
}

sendButton.onclick = () => {
    var msg = {"event":"subscribe", "key": `binance.${symbol.value}`, "channel":"trade"}
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
        var lines = dataElement.innerHTML.split(/\r\n|\r|\n/);

        if (lines.length > 99) {
            dataElement.innerHTML = '';
            for (var i = 0; i < 99; i++) {
                dataElement.innerHTML += lines[i] + '\n';
            }
        }

        if (data.status) {
            if (data.status == "confirmed") {
                console.log("Authorized!");
            } else {
                console.error("Authentication Failed!");
            }
        } else if (data.event && data.event == "subscribed") {
            console.log(data.event);
        } else {
            var trade = data[1];
            var amount = Math.abs(trade[2]);
            var type = (trade[2] > 0) ? "sell" : "buy";
            var tradeTime = new Date(trade[0])
            dataElement.innerHTML = `
 ${tradeTime.toLocaleTimeString()} \t ${trade[1]} \t ${amount} \t ${type}` + dataElement.innerHTML;
        }
    }
    
    ws.onerror = (err) => {
        console.log(`ERROR`);
    }
}
