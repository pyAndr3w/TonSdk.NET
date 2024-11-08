using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TonSdk.Client.Stack;
using TonSdk.Core;
using TonSdk.Core.Boc;

namespace TonSdk.Client
{
    public enum TonClientType
    {
        HTTP_TONCENTERAPIV2,
        HTTP_TONCENTERAPIV3,
        HTTP_TONWHALESAPI,
        LITECLIENT,
        
    }

    public enum SortDirection
    {
        ASC,
        DESC
    }
    
    public interface ITonClientOptions {}

    public class HttpParameters : ITonClientOptions
    {
        public string Endpoint { get; set; }
        public int? Timeout { get; set; }
        public string ApiKey { get; set; }
    }
    
    public class LiteClientParameters : ITonClientOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string PeerPublicKey { get; set; }

        public LiteClientParameters(string host, int port, string peerPublicKey)
        {
            Host = host;
            Port = port;
            PeerPublicKey = peerPublicKey;
        }
    }
}