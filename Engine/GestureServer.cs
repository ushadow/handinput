using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocket;

namespace HandInput.Engine {
  public class GestureServer {
    WebSocketServer server;
    WebSocketConnection connection;

    public GestureServer(String ipAddress, int port) {
      server = new WebSocketServer(ipAddress, port);
      server.ClientConnected += OnClientConnected;
    }

    public void Send(String message) {
      if (connection != null) {
        connection.Send(message);
      }
    }

    void OnClientConnected(object sender, ClientConnectedEventArgs e) {
      connection = e.Client;
    }
  }
}
