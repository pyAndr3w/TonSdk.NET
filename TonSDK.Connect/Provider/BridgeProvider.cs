using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TonSdk.Connect
{
    public delegate void ProviderMessageHandler(string eventData);
    public delegate void ProviderErrorHandler(Exception args);
    public delegate void WalletEventListener(JObject walletEvent);

    public delegate void OnRequestSentHandler();

    internal class BridgeProvider : IHttpProvider
    {
        private readonly int DISCONNECT_TIMEOUT = 600;
        private readonly string STANDART_UNIVERSAL_URL = "tc://";

        private WalletConfig? _wallet;
        private BridgeSession _session;
        private BridgeGateway _gateway;

        internal RemoteStorage _storage;

        private Dictionary<string, TaskCompletionSource<JObject>> _pendingRequests;
        private List<WalletEventListener> _listeners;
        internal ListenEventsFunction _listenEventsFunction;
        internal SendGatewayMessage sendGatewayMessage;

        internal BridgeProvider(WalletConfig? wallet = null, RemoteStorage storage = null, ListenEventsFunction eventsFunction = null, SendGatewayMessage sendGatewayMessage = null)
        {
            _wallet = wallet;
            _session = new BridgeSession();
            _gateway = null;
            _storage = storage;
            _listenEventsFunction = eventsFunction;
            this.sendGatewayMessage = sendGatewayMessage;

            _pendingRequests = new Dictionary<string, TaskCompletionSource<JObject>>();
            _listeners = new List<WalletEventListener>();
        }

        public async Task<string> ConnectAsync(ConnectRequest connectRequest)
        {
            CloseGateways();
            CryptedSessionInfo sessionInfo = new CryptedSessionInfo();

            string bridgeUrl = _wallet?.BridgeUrl;
            string universalUrl = _wallet?.UniversalUrl ?? STANDART_UNIVERSAL_URL;

            _gateway = new BridgeGateway(bridgeUrl, sessionInfo.SesionId, new ProviderMessageHandler(GatewayMessageListener), new ProviderErrorHandler(GatewayErrorListener), _storage, _listenEventsFunction, sendGatewayMessage);
            await _gateway.RegisterSession().ConfigureAwait(false);

            _session.CryptedSessionInfo = sessionInfo;
            _session.BridgeUrl = bridgeUrl;

            return GenerateUniversalLink(universalUrl, connectRequest);
        }

        public async Task<bool> RestoreConnection()
        {
            CloseGateways();
            string connectionJsonString = _storage != null ? _storage.GetItem(RemoteStorage.KEY_CONNECTION, "{}") : await DefaultStorage.GetItem(DefaultStorage.KEY_CONNECTION, "{}").ConfigureAwait(false);
            if (connectionJsonString == null || connectionJsonString == "{}") return false;

            ConnectionInfo connection = JsonConvert.DeserializeObject<ConnectionInfo>(connectionJsonString);

            if (connection.Session == null) return false;
            _session = new BridgeSession(connection.Session);

            _gateway = new BridgeGateway(_session.BridgeUrl, _session.CryptedSessionInfo.SesionId, new ProviderMessageHandler(GatewayMessageListener), new ProviderErrorHandler(GatewayErrorListener), _storage, _listenEventsFunction, sendGatewayMessage);
            await _gateway.RegisterSession();

            foreach (var listener in _listeners)
            {
                listener(connection.ConnectEvent);
            }

            return true;
        }

        public async Task<JObject> SendRequest(IRpcRequest request, OnRequestSentHandler? onRequestSent = null)
        {
            if (_gateway == null || _session == null || _session.WalletPublicKey == null) throw new TonConnectError("Trying to send bridge request without session.");
            string connectionJsonString = _storage != null ? _storage.GetItem(RemoteStorage.KEY_CONNECTION, "{}") : await DefaultStorage.GetItem(DefaultStorage.KEY_CONNECTION, "{}").ConfigureAwait(false);
            ConnectionInfo connection = JsonConvert.DeserializeObject<ConnectionInfo>(connectionJsonString);
            
            int id = connection.NextRpcRequestId ?? 0;
            connection.NextRpcRequestId = id + 1;

            string jsonString = JsonConvert.SerializeObject(connection);
            
            if (_storage != null) 
                _storage.SetItem(RemoteStorage.KEY_CONNECTION, jsonString);
            else 
                await DefaultStorage.SetItem(DefaultStorage.KEY_CONNECTION, jsonString).ConfigureAwait(false);

            request.id = id.ToString();
            string encryptedRequest = _session.CryptedSessionInfo.Encrypt(JsonConvert.SerializeObject(request), _session.WalletPublicKey);

            await _gateway.Send(Encoding.UTF8.GetBytes(encryptedRequest), _session.WalletPublicKey, request.method);
            
            TaskCompletionSource<JObject> resolve = new TaskCompletionSource<JObject>();
            _pendingRequests.Add(id.ToString(), resolve);

            JObject result = await resolve.Task.ConfigureAwait(false);
            onRequestSent?.Invoke();
            return result;
        }

        public async Task Disconnect()
        {
            try
            {
                DisconnectRpcRequest request = new DisconnectRpcRequest()
                {
                    method = "disconnect",
                    @params = Array.Empty<string>(),
                };
                JObject result = await SendRequest(request, RemoveSession).ConfigureAwait(false);
                _gateway.Pause();
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message).ConfigureAwait(false);
                RemoveSession();
            }
        }

        

        private string GenerateUniversalLink(string universalLink, ConnectRequest connectRequest)
        {
            if (UrlUtils.IsTelegramUrl(universalLink))
                return GenerateTGUniversalLink(universalLink, connectRequest);
            return GenerateRegularUniversalLink(universalLink, connectRequest);
        }
        
        private string GenerateTGUniversalLink(string universalLink, ConnectRequest connectRequest)
        {
            var urlToWrap = GenerateRegularUniversalLink("about:blank", connectRequest);
            var linkParams = HttpUtility.ParseQueryString(new Uri(urlToWrap).Query);

            var startapp = "tonconnect-" + UrlUtils.EncodeTelegramUrlParameters(linkParams.ToString());

            // TODO: Remove this line after all dApps and the wallets-list.json have been updated
            var updatedUniversalLink = ConvertToDirectLink(universalLink);

            var url = new UriBuilder(updatedUniversalLink);
            var query = HttpUtility.ParseQueryString(url.Query);
            query["startapp"] = startapp;
            url.Query = query.ToString();

            return url.ToString();
        }
        
        private static string ConvertToDirectLink(string universalLink)
        {
            var uriBuilder = new UriBuilder(universalLink);
            var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (queryParams["attach"] != null)
            {
                queryParams.Remove("attach");
                uriBuilder.Path += "/start";
            }

            uriBuilder.Query = queryParams.ToString();
            return uriBuilder.ToString();
        }

        private string GenerateRegularUniversalLink(string universalLink, ConnectRequest connectRequest)
        {
            UriBuilder url = new UriBuilder(universalLink);
            url.Port = -1;
            url.Query += $"v={2}";
            url.Query += $"&id={_session?.CryptedSessionInfo?.SesionId}";
            url.Query += $"&r={JsonConvert.SerializeObject(connectRequest)}";
            return url.ToString();
        }

        public void CloseGateways() => _gateway?.Close();

        public void CloseConnection()
        {
            CloseGateways();
            _session = new BridgeSession();
            _gateway = null;
            _pendingRequests = new Dictionary<string, TaskCompletionSource<JObject>>();
            _listeners = new List<WalletEventListener>();
        }

        public void Pause() => _gateway?.Pause();

        public async Task UnPause() => await _gateway.UnPause().ConfigureAwait(false);

        public void Listen(WalletEventListener listener) => _listeners.Add(listener);

        private void RemoveSession()
        {
            if (_gateway != null)
            {
                CloseConnection();
                if (_storage != null)
                {
                    _storage.RemoveItem(RemoteStorage.KEY_CONNECTION);
                    _storage.RemoveItem(RemoteStorage.KEY_LAST_EVENT_ID);
                }
                else
                {
                    DefaultStorage.RemoveItem(DefaultStorage.KEY_CONNECTION);
                    DefaultStorage.RemoveItem(DefaultStorage.KEY_LAST_EVENT_ID);
                }
            }
        }

        private async void UpdateSession(JObject walletMessage, string walletPublicKey)
        {
            _session.WalletPublicKey = walletPublicKey;
            ConnectionInfo connection = new ConnectionInfo();
            connection.Type = "http";
            connection.Session = _session.GetSessionInfo();
            connection.LastWalletEventId = (long?)walletMessage["id"] ?? null;
            connection.ConnectEvent = walletMessage;
            connection.NextRpcRequestId = 0;

            string jsonString = JsonConvert.SerializeObject(connection);
            if (_storage != null)
                _storage.SetItem(RemoteStorage.KEY_CONNECTION, jsonString);
            else 
                await DefaultStorage.SetItem(DefaultStorage.KEY_CONNECTION, jsonString).ConfigureAwait(false);
        }

        private async Task ParseGatewayMessage(BridgeIncomingMessage message)
        {
            string json = _session!.CryptedSessionInfo!.Decrypt(Convert.FromBase64String(message.Message), message.From);
            if (json == null || json.Length == 0) return;


            JObject data = JsonConvert.DeserializeObject<JObject>(json);
            if (data["event"] == null)
            {
                if (data["id"] != null)
                {
                    string id = data["id"].ToString();
                    if (!_pendingRequests.ContainsKey(id))
                    {
                        await Console.Out.WriteLineAsync($"Response id {id} doesn't match any request's id");
                        return;
                    }

                    _pendingRequests[id].SetResult(data);
                    _pendingRequests.Remove(id);
                }
                return;
            }

            if (data["id"] != null)
            {
                long id = (long)data["id"];
                ConnectionInfo connection = JsonConvert.DeserializeObject<ConnectionInfo>(_storage != null 
                    ? _storage.GetItem(RemoteStorage.KEY_CONNECTION, "{}") 
                    : await DefaultStorage.GetItem(DefaultStorage.KEY_CONNECTION, "{}").ConfigureAwait(false));
                long? lastId = connection.LastWalletEventId;

                if (lastId != null && id <= lastId)
                {
                    await Console.Out.WriteLineAsync($"Received event id (={id}) must be greater than stored last wallet event id (={lastId})");
                    return;
                }

                if (data["event"] != null && (string)data["event"] != "connect")
                {
                    connection.LastWalletEventId = id;
                    string dumpedConnection = JsonConvert.SerializeObject(connection);
                    if (_storage != null)
                        _storage.SetItem(RemoteStorage.KEY_CONNECTION, dumpedConnection);
                    else
                        await DefaultStorage.SetItem(DefaultStorage.KEY_CONNECTION, dumpedConnection).ConfigureAwait(false);
                }
            }

            List<WalletEventListener> listenersTemp = _listeners;

            if (data["event"] != null && (string)data["event"] == "connect") UpdateSession(data, message.From);

            if (data["event"] != null && (string)data["event"] == "disconnect") RemoveSession();

            foreach (WalletEventListener listener in listenersTemp)
            {
                listener(data);
            }
            return;
        }

        private async void GatewayMessageListener(string eventData)
        {
            if (eventData.StartsWith("id:"))
                if (_storage != null)
                    _storage.SetItem(RemoteStorage.KEY_LAST_EVENT_ID, eventData.Substring(4));
                else
                    await DefaultStorage.SetItem(DefaultStorage.KEY_LAST_EVENT_ID, eventData.Substring(4)).ConfigureAwait(false);

            if (eventData.StartsWith("data:") && _gateway != null && !_gateway.isClosed)
            {
                string data = eventData.Substring(5);
                BridgeIncomingMessage dataMessage = JsonConvert.DeserializeObject<BridgeIncomingMessage>(data);
                await ParseGatewayMessage(dataMessage).ConfigureAwait(false);
            }
        }

        private void GatewayErrorListener(Exception e)
        {
            throw new TonConnectError(e.ToString());
        }
    }
}
