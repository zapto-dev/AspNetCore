using System.Net.WebSockets;

namespace Zapto.AspNetCore.NetFx.Tests;

public class WebSocketTest(AspNetFixture fixture)
{
   [Fact]
   public async Task Echo_WebSocket()
   {
      using var client = new ClientWebSocket();
      var uri = new UriBuilder(fixture.BaseAddress)
      {
         Scheme = "ws",
         Path = "/ws/echo"
      }.Uri;

      await client.ConnectAsync(uri, TestContext.Current.CancellationToken);

      var sendBuffer = new byte[1024];
      var receiveBuffer = new byte[1024];
      var message = "Hello, WebSocket!";
      var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
      Array.Copy(messageBytes, sendBuffer, messageBytes.Length);

      await client.SendAsync(new ArraySegment<byte>(sendBuffer, 0, messageBytes.Length), WebSocketMessageType.Text, true, TestContext.Current.CancellationToken);

      var result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), TestContext.Current.CancellationToken);
      var receivedMessage = System.Text.Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

      Assert.Equal(message, receivedMessage);

      await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", TestContext.Current.CancellationToken);

      Assert.Equal(WebSocketState.Closed, client.State);
   }
}
