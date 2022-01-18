var btnConnect = document.querySelector("#btn-connect");
var symbol = document.querySelector("#symbol");
var dataElement = document.querySelector("#data");


var ws;

btnConnect.onclick = () => {
    ws = new WebSocket('wss://stream.binance.com/ws/' + symbol.value.toLowerCase() + '@aggTrade');
    setWebSocketClient(ws);
}

function setWebSocketClient(ws) {
    ws.onopen = (ev) => {
        console.log("Opened!");
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

        var trade = data.p;
        var amount = data.q;
        var type = (data.m) ? "sell" : "buy";;
        var tradeTime = data.T;
        dataElement.innerHTML = `
 ${new Date(tradeTime).toLocaleTimeString()} \t ${trade - 0} \t ${amount - 0} \t ${type}` + dataElement.innerHTML;
        
    }

    ws.onerror = (err) => {
        console.log(`ERROR`);
    }
}
