using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using WebSocket;

using HandInput.Engine;

namespace HandInput.Server {
  class HandInputServer {
    private WebSocketServer server;
    private List<WebSocketConnection> connections = new List<WebSocketConnection>();
    private HandInputEngine handInputEngine;

    public HandInputServer(String ipAddress, Int32 port) {
      server = new WebSocketServer(ipAddress, port);
      server.ClientConnected += new ClientConnectedEventHandler(OnClientConnected);
      handInputEngine = new HandInputEngine();
      handInputEngine.HandInputEvent += new HandInputEventHandler(OnHandInputEvent);
    }

    public void Start() {
      handInputEngine.Start();
      server.Start();
    }

    public void Stop() {
      handInputEngine.Stop();
      server.Stop();
    }

    private void OnClientConnected(WebSocketServer sender, ClientConnectedEventArgs e) {
      connections.Add(e.Client);
    }

    private void OnHandInputEvent(HandInputEngine sender, HandInputEvent e) {
      foreach (var connection in connections) {
        connection.Send(JsonConvert.SerializeObject(e));
      }
    }
  }
}
