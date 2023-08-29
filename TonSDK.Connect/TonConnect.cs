﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TonSdk.Core.Boc;

namespace TonSdk.Connect
{

    public delegate void StatusChangeCallback(Wallet wallet);
    public delegate void StatusChangeErrorsHandler(string error);

    public class TonConnectOptions
    {
        /// <summary>
        /// Url to the [manifest]{@link https://github.com/ton-connect/docs/blob/main/requests-responses.md#app-manifest} with the Dapp metadata that will be displayed in the user's wallet.
        /// </summary>
        public string ManifestUrl { get; set; }

        /// <summary>
        /// Redefine wallets list source URL.Must be a link to a json file with[following structure]{@link https://github.com/ton-connect/wallets-list}
        /// default https://raw.githubusercontent.com/ton-connect/wallets-list/main/wallets.json
        /// </summary>
        public string WalletsListSource { get; set; } = "";

        /// <summary>
        /// Wallets list cache time to live
        /// default Infinity
        /// </summary>
        public int WalletsListCacheTTLMs { get; set; } = 0;

        /// <summary>
        /// Allows to disable auto pause/unpause SSE connection on 'document.visibilitychange' event. It is not recommended to change default behaviour.
        /// </summary>
        public bool DisableAutoPauseConnection { get; set; }
    }

    public class TonConnect
    {
        private BridgeProvider? _provider;
        private string _manifestUrl;
        private Wallet? _wallet;
        private WalletsListManager? _walletsList = new WalletsListManager();

        private RemoteStorage _storage;
        private ListenEventsFunction _listenEventsFunction;

        private List<StatusChangeCallback> _statusChangeCallbacksSubscriptions;
        private List<StatusChangeErrorsHandler> _statusChangeErrorSubscriptions;

        /// <summary>
        /// Shows if the wallet is connected right now.
        /// </summary>
        public bool IsConnected { get => _wallet != null; }

        /// <summary>
        /// Current connected account or None if no account is connected.
        /// </summary>
        public Account Account { get => _wallet?.Account ?? null; }

        /// <summary>
        /// Current connected wallet or None if no account is connected.
        /// </summary>
        public Wallet Wallet { get => (Wallet)_wallet; }

        public TonConnect(TonConnectOptions options, RemoteStorage storage = null, ListenEventsFunction eventsFunction = null)
        {
            _walletsList = new WalletsListManager(options.WalletsListSource, options.WalletsListCacheTTLMs);

            _provider = null;
            _manifestUrl = options.ManifestUrl;
            _storage = storage;

            _wallet = null;

            _listenEventsFunction = eventsFunction;

            _statusChangeCallbacksSubscriptions = new List<StatusChangeCallback>();
            _statusChangeErrorSubscriptions = new List<StatusChangeErrorsHandler>();
        }

        /// <summary>
        /// Return available wallets list.
        /// </summary>
        /// <param name="current">Set false, if you want to get dafault wallet list, true - current TonConnect instance</param>
        /// <returns>WalletConfig array</returns>
        public WalletConfig[] GetWallets(bool current = true) => current ? _walletsList.GetWallets().ToArray() : new WalletsListManager().GetWallets().ToArray();

        /// <summary>
        /// Allows to subscribe to connection status changes and handle connection errors.
        /// </summary>
        /// <param name="callback">Callback will be called after connections status changes with actual wallet or null.</param>
        /// <param name="errorsHandler">ErrorsHandler (optional) will be called with some instance of TonConnectError when connect error is received.</param>
        /// <returns>Unsubscribe callback.</returns>
        public Action OnStatusChange(StatusChangeCallback callback, StatusChangeErrorsHandler? errorsHandler = null)
        {
            _statusChangeCallbacksSubscriptions.Add(callback);
            if (errorsHandler != null) _statusChangeErrorSubscriptions.Add(errorsHandler);

            void unsubscribe()
            {
                if (_statusChangeCallbacksSubscriptions.Contains(callback)) _statusChangeCallbacksSubscriptions.Remove(callback);
                if (errorsHandler != null && _statusChangeErrorSubscriptions.Contains(errorsHandler)) _statusChangeErrorSubscriptions.Remove(errorsHandler);
            }

            return unsubscribe;
        }

        /// <summary>
        /// Generates universal link for an external wallet and subscribes to the wallet's bridge, or sends connect request to the injected wallet.
        /// </summary>
        /// <param name="walletConfig">Wallet wallet's bridge url and universal link for an external wallet or jsBridge key for the injected wallet.</param>
        /// <param name="connectAdditionalRequest">Request (optional) additional request to pass to the wallet while connect (currently only ton_proof is available).</param>
        /// <returns>Universal link if external wallet was passed or void for the injected wallet.</returns>
        /// <exception cref="TonConnectError">Wallet already connected</exception>
        public async Task<string> Connect(WalletConfig walletConfig, ConnectAdditionalRequest? connectAdditionalRequest = null)
        {
            if (IsConnected) throw new WalletAlreadyConnectedError();
            _provider?.CloseConnection();
            _provider = CreateProvider(walletConfig);
            return await _provider.ConnectAsync(CreateConnectRequest(connectAdditionalRequest));
        }

        /// <summary>
        /// Try to restore existing session and reconnect to the corresponding wallet. Call it immediately when your app is loaded.
        /// </summary>
        /// <returns>True if connection is restored</returns>
        public async Task<bool> RestoreConnection()
        {
            _provider = new BridgeProvider()
            {
                _storage = _storage,
                _listenEventsFunction = _listenEventsFunction
            };
            _provider.Listen(new WalletEventListener(WalletEventsListener));
            bool isRestored = await _provider.RestoreConnection();

            if (!isRestored)
            {
                _provider = null;
                if (_storage != null)
                    _storage.RemoveItem(RemoteStorage.KEY_CONNECTION);
                else
                    DefaultStorage.RemoveItem(DefaultStorage.KEY_CONNECTION);
            }
            return isRestored;
        }

        /// <summary>
        /// Asks connected wallet to sign and send the transaction.
        /// </summary>
        /// <param name="request">Transaction to send</param>
        /// <returns>Signed transaction boc that allows you to find the transaction in the blockchain.</returns>
        public async Task<SendTransactionResult?> SendTransaction(SendTransactionRequest request)
        {
            if (!IsConnected) throw new WalletNotConnectedError();
            ValidateTransactionSupport(_wallet?.Device.Features, request.Messages.Length);

            SendTransactionRequest transactionRequest =
                new SendTransactionRequest(request.Messages ?? Array.Empty<Message>(), request.ValidUntil ?? null,
                request.Network ?? _wallet?.Account.Chain, request.From ?? _wallet?.Account.Address);

            ProviderModels.SendTransactionRequestSerialized serializedRequest = new ProviderModels.SendTransactionRequestSerialized(transactionRequest);

            dynamic response = await _provider.SendRequest(
            new SendTransactionRpcRequest()
            {
                method = "sendTransaction",
                @params = new string[] { JsonConvert.SerializeObject(serializedRequest) },
            });

            return ParseSendTransactionResponse(response);
        }

        /// <summary>
        /// Disconnect from wallet and drop current session.
        /// </summary>
        /// <exception cref="TonConnectError">Wallet not connected.</exception>
        public async Task Disconnect()
        {
            if (!IsConnected) throw new TonConnectError("Wallet not connected.");
            await _provider?.Disconnect();
            OnWalletDisconnected();
        }

        /// <summary>
        /// Pause bridge HTTP connection. Might be helpful, if you use SDK on backend and want to save server resources.
        /// </summary>
        public void PauseConnection() => _provider?.Pause();

        /// <summary>
        /// Unpause bridge HTTP connection if it is paused.
        /// </summary>
        public async Task UnPauseConnection() => await _provider?.UnPause();

        private SendTransactionResult? ParseSendTransactionResponse(dynamic response)
        {
            if (response.error != null)
            {
                switch((int)response.error.code)
                {
                    case 0: throw new UnknownError();
                    case 1: throw new BadRequestError();
                    case 100: throw new UnknownAppError();
                    case 300: throw new UserRejectsError();
                    case 400: throw new WalletNotSupportFeatureError();
                }
            }

            if (response.result != null) return new SendTransactionResult() { Boc = Cell.From(response.result.ToString()) };
            return null;
        }

        private void ValidateTransactionSupport(object[] walletFeatures, int messagesCount)
        {
            bool supportDepricatedSend = walletFeatures.Contains("SendTransaction");
            dynamic sendFeature = null;

            foreach (object feature in walletFeatures)
            {
                if (feature is JObject featureItem)
                {
                    dynamic currentFeature = featureItem.ToObject<dynamic>();
                    if (currentFeature != null && currentFeature.name != null && currentFeature.name == "SendTransaction") sendFeature = currentFeature;
                }
            }

            if (!supportDepricatedSend && sendFeature == null) throw new TonConnectError("Wallet doesn't support SendTransaction feature.");
            if (sendFeature != null)
            {
                int maxMessages = (int)sendFeature.maxMessages;
                if (maxMessages < messagesCount)
                    throw new TonConnectError($"Wallet is not able to handle such SendTransaction request. Max support messages number is {maxMessages}, but {messagesCount} is required.");
            }
            else Console.WriteLine("Connected wallet didn't provide information about max allowed messages in the SendTransaction request. Request may be rejected by the wallet.");
        } 

        private BridgeProvider CreateProvider(WalletConfig walletConfig)
        {
            BridgeProvider provider = new BridgeProvider(walletConfig, _storage, _listenEventsFunction);
            provider.Listen(new WalletEventListener(WalletEventsListener));
            return provider;
        }

        private void WalletEventsListener(dynamic eventData)
        {

            switch ((string)eventData.@event)
            {
                case "connect":
                    {
                        OnWalletConnected(eventData.payload);
                        break;
                    }
                case "connect_error":
                    {
                        OnWalletConnectError(eventData.payload);
                        break;
                    }
                case "disconnect":
                    {
                        OnWalletDisconnected();
                        break;
                    }
            }
        }

        private void OnWalletConnected(dynamic payload)
        {
            _wallet = ConnectEventParser.ParseResponse(payload);
            foreach (StatusChangeCallback listener in _statusChangeCallbacksSubscriptions)
            {
                listener((Wallet)_wallet);
            }
        }

        private void OnWalletConnectError(dynamic payload)
        {
            ConnectErrorData errorData = ConnectEventParser.ParseError(payload);
            foreach (StatusChangeErrorsHandler listener in _statusChangeErrorSubscriptions)
            {
                listener(errorData.Message);
            }

            switch ((int)errorData.Code)
            {
                case 0: throw new UnknownError();
                case 1: throw new BadRequestError();
                case 2: throw new ManifestNotFoundError();
                case 3: throw new ManifestContentError();
                case 100: throw new UnknownAppError();
                case 300: throw new UserRejectsError();
            }
        }

        private void OnWalletDisconnected() => _wallet = null;

        private ConnectRequest CreateConnectRequest(ConnectAdditionalRequest? connectAdditionalRequest = null)
        {
            ConnectRequest connectRequest = new ConnectRequest();
            connectRequest.manifestUrl = _manifestUrl;
            List<IConnectItem> connectItems = new List<IConnectItem>()
        {
            new ConnectAddressItem() { name = "ton_addr" }
        };
            if (connectAdditionalRequest != null && connectAdditionalRequest?.TonProof != null && connectAdditionalRequest?.TonProof != "")
            {
                connectItems.Add(new ConnectProofItem() { name = "ton_proof", payload = connectAdditionalRequest?.TonProof });
            }
            connectRequest.items = connectItems.ToArray();
            return connectRequest;
        }
    }
}