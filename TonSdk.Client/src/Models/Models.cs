using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection.Metadata;
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
    
    public enum ProxyType
    {
        Socks5,
        Socks4,
        HTTP,
        HTTPS
    }
    
    public interface ITonClientOptions {}

    public class HttpParameters : ITonClientOptions
    {
        public string Endpoint { get; set; }
        public int? Timeout { get; set; }
        public string ApiKey { get; set; }
        public Proxy? Proxy { get; set; }
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
    
    /// <summary>
    /// Credentials for connecting to the proxy
    /// </summary>
    public partial class Proxy : ITonClientOptions
    {
        /// <summary>
        /// Ip address of the proxy server
        /// </summary>
        public IPAddress Ip { get; set; }

        private int _port;
        
        /// <summary>
        /// Port number of the proxy server
        /// </summary>
        /// <exception cref="Exception">The proxy port number goes beyond 1-65536</exception>
        public int Port
        {
            set
            {
                if (value < 1 || value > 65536)
                    throw new Exception("The proxy port number goes beyond 1-65536");
                _port = value;
            }
            get => _port;
        }

        /// <summary>
        /// The user's name of the credentials
        /// </summary>
        public string UserName { get; set; }
        
        /// <summary>
        /// The password's of the credentials
        /// </summary>
        public string Password { get; set; }
        
        /// <summary>
        /// Type of proxy server protocol
        /// </summary>
        public ProxyType ProxyType { get; set; }
    }
}