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

    public void Start() { server.Start(); }

    /// <summary>
    /// Disposes the connection and stops the gesture server.
    /// </summary>
    public void Stop() {
      if (connection != null)
        connection.Dispose();
      server.Stop(); 
    }

    void OnClientConnected(object sender, ClientConnectedEventArgs e) {
      connection = e.Client;
    }
  }
}
