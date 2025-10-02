<%@ Page Language="C#" CodeBehind="WebSocketEcho.aspx.cs" Inherits="WebFormsApp.WebSocketEcho" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>WebSocket Echo Test</title>
</head>
<body>

<div id="websocket-messages"></div>
<button onclick="start()">Start WebSocket Test</button>

<script>
function start() {
    // Connect to the WebSocket server at /ws
    const socket = new WebSocket(`ws://${window.location.host}/ws/echo`);

    socket.onmessage = function(event) {
        const messagesDiv = document.getElementById('websocket-messages');
        const message = document.createElement('div');
        message.textContent = `Received: ${event.data}`;
        messagesDiv.appendChild(message);
    };

    socket.onopen = function(event) {
        socket.send('Hello, WebSocket server!');
    };
}
</script>

</body>
</html>