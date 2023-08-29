﻿using Newtonsoft.Json;
using System.Collections.Generic;
using TonSdk.Core;

namespace TonSdk.Connect
{
    public struct BridgeIncomingMessage
    {
        [JsonProperty("from")] public string? From { get; set; }
        [JsonProperty("message")] public string? Message { get; set; }
    }

    public interface IRpcRequest
    {
        public string method { get; set; }
        public string id { get; set; }
    };

    public class DisconnectRpcRequest : IRpcRequest
    {
        public string method { get; set; } = "disconnect";

        public string[] @params;
        public string id { get; set; }
    }

    public class SendTransactionRpcRequest : IRpcRequest
    {
        public string method { get; set; } = "sendTransaction";

        public string[] @params;
        public string id { get; set; }
    }

    public class ConnectAdditionalRequest
    {
        public string? TonProof { get; set; }
    }

    public interface IConnectItem
    {
        public string? name { get; set; }
    }

    public class ConnectRequest
    {
        public string? manifestUrl { get; set; }
        public IConnectItem[] items { get; set; }
    }

    public class ConnectAddressItem : IConnectItem
    {
        public string? name { get; set; }
    }

    public class ConnectProofItem : IConnectItem
    {
        public string? name { get; set; }
        public string? payload { get; set; }
    }

    public struct ConnectionInfo
    {
        public string? Type { get; set; }
        public SessionInfo? Session { get; set; }
        public int? LastWalletEventId { get; set; }
        public int? NextRpcRequestId { get; set; }
        public dynamic ConnectEvent { get; set; }
    }

    public enum CHAIN
    {
        MAINNET = -239,
        TESTNET = -3
    }

    public enum CONNECT_EVENT_ERROR_CODE
    {
        UNKNOWN_ERROR = 0,
        BAD_REQUEST_ERROR = 1,
        MANIFEST_NOT_FOUND_ERROR = 2,
        MANIFEST_CONTENT_ERROR = 3,
        UNKNOWN_APP_ERROR = 100,
        USER_REJECTS_ERROR = 300,
        METHOD_NOT_SUPPORTED = 400
    }

    public struct ConnectErrorData
    {
        public CONNECT_EVENT_ERROR_CODE Code { get; set; }
        public string Message { get; set; }
    }

    public class ConnectEventParser
    {
        public static Wallet ParseResponse(dynamic payload)
        {
            if (payload.items == null) throw new TonConnectError("items not contains in payload");

            Wallet wallet = new Wallet();

            foreach (var item in payload.items)
            {
                if (item.name != null)
                {
                    if ((string)item.name == "ton_addr") wallet.Account = Account.Parse(item);
                    else if ((string)item.name == "ton_proof") wallet.TonProof = TonProof.Parse(item);
                }
            }

            if (wallet.Account == null) throw new TonConnectError("ton_addr not contains in items");
            wallet.Device = DeviceInfo.Parse(payload.device);

            return wallet;
        }

        public static ConnectErrorData ParseError(dynamic payload)
        {
            ConnectErrorData data = new ConnectErrorData()
            {
                Code = (CONNECT_EVENT_ERROR_CODE)payload.code,
                Message = payload.message.ToString()
            };
            return data;
        }
    }

    public class ProviderModels
    {
        public class SendTransactionRequestSerialized
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public long? valid_until { get; set; }
            public string network { get; set; }
            public string from { get; set; }
            public SendTransactionMessageSerialized[] messages { get; set; }

            public SendTransactionRequestSerialized(SendTransactionRequest request)
            {
                valid_until = request.ValidUntil;
                network = ((int)request.Network!).ToString();
                from = request.From!.ToString(AddressType.Raw);

                List<SendTransactionMessageSerialized> messagesList = new List<SendTransactionMessageSerialized>();
                foreach (Message message in request.Messages)
                {
                    messagesList.Add(new SendTransactionMessageSerialized(message));
                }
                messages = messagesList.ToArray();
            }
        }

        public class SendTransactionMessageSerialized
        {
            public string address { get; set; }
            public string amount { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string? stateInit { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string? payload { get; set; }

            public SendTransactionMessageSerialized(Message message)
            {
                address = message.Address.ToString();
                amount = message.Amount.ToNano();
                stateInit = message.StateInit?.ToString();
                payload = message.Payload?.ToString();
            }
        }
    }
}