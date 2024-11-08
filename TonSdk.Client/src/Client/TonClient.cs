using System;
using System.Threading.Tasks;
using TonSdk.Client.Stack;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;

namespace TonSdk.Client
{
    public class TonClient : ITonClient, IDisposable
    {
        private readonly TonClientType _type;
        private readonly HttpApi _httpApi;
        private readonly HttpApiV3 _httpApiV3;
        private readonly HttpWhales _httpWhales;
        private readonly LiteClientApi _liteClientApi;
        
        public Jetton Jetton { get; }
        public Nft Nft { get; }
        public Wallet Wallet { get; }
        public Dns Dns { get; }
        
        public TonClient(TonClientType clientType, ITonClientOptions options)
        {
            Jetton = new Jetton(this);
            Nft = new Nft(this);
            Wallet = new Wallet(this);
            Dns = new Dns(this);
            
            switch (clientType)
            {
                case TonClientType.HTTP_TONCENTERAPIV2:
                {
                    if (!(options is HttpParameters)) 
                        throw new Exception("Wrong provided options for HTTP Client type, use HttpParameters instead.");
                    var opts = (HttpParameters)options;
                    _httpApi = new HttpApi(opts);
                    break;
                }
                case TonClientType.HTTP_TONCENTERAPIV3:
                {
                    if (!(options is HttpParameters)) 
                        throw new Exception("Wrong provided options for HTTP Client type, use HttpParameters instead.");
                    var opts = (HttpParameters)options;
                    _httpApiV3 = new HttpApiV3(opts);
                    break;
                }
                case TonClientType.HTTP_TONWHALESAPI:
                {
                    if (!(options is HttpParameters)) 
                        throw new Exception("Wrong provided options for HTTP Client type, use HttpParameters instead.");
                    var opts = (HttpParameters)options;
                    _httpWhales = new HttpWhales(opts);
                    break;
                }
                case TonClientType.LITECLIENT:
                {
                    if (!(options is LiteClientParameters)) 
                        throw new Exception("Wrong provided options for Lite Client type, use LiteClientParameters instead.");
                    
                    var opts = (LiteClientParameters)options;
                    _liteClientApi = new LiteClientApi(opts.Host, opts.Port, opts.PeerPublicKey);
                    break;
                }
            }

            _type = clientType;
        }

        public TonClientType GetClientType() => _type;
        
        /// <summary>
        /// Retrieves the balance of the specified address.
        /// </summary>
        /// <param name="address">The address to retrieve the balance for.</param>
        /// <param name="block">Can be placed to fetch in specific block, requires LiteClient (optional).</param>
        /// <returns>The task result contains the balance as a Coins instance.</returns>
        public async Task<Coins> GetBalance(Address address, BlockIdExtended? block = null)
        {
            return (await GetAddressInformation(address, block))?.Balance;
        }

        /// <summary>
        /// Checks if a contract is deployed at the specified address.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <param name="block">Can be placed to fetch in specific block, requires LiteClient (optional).</param>
        /// <returns>The task result indicates whether a contract is deployed (true) or not (false) at the specified address.</returns>
        public async Task<bool> IsContractDeployed(Address address, BlockIdExtended? block = null)
        {
            return (await GetAddressInformation(address, block))?.State == AccountState.Active;
        }

        /// <summary>
        /// Retrieves the address information for the specified address.
        /// </summary>
        /// <param name="address">The address object to retrieve information for.</param>
        /// <param name="block">Can be placed to fetch in specific block, requires LiteClient (optional).</param>
        /// <returns>An object containing the address information.</returns>
        public async Task<AddressInformationResult?> GetAddressInformation(Address address, BlockIdExtended? block = null)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.GetAddressInformation(address),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.GetAddressInformation(address),
                TonClientType.HTTP_TONWHALESAPI => await _httpWhales.GetAddressInformation(address),
                TonClientType.LITECLIENT => await _liteClientApi.GetAddressInformation(address, block),
                _ => null
            };
        }
        
        /// <summary>
        /// Retrieves the wallet information for the specified address.
        /// </summary>
        /// <param name="address">The address object to retrieve information for.</param>
        /// <param name="block">Can be placed to fetch in specific block, requires LiteClient (optional).</param>
        /// <returns>An object containing the wallet information.</returns>
        public async Task<WalletInformationResult?> GetWalletInformation(Address address, BlockIdExtended? block = null)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.GetWalletInformation(address),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.GetWalletInformation(address),
                TonClientType.HTTP_TONWHALESAPI => await _httpWhales.GetWalletInformation(address),
                TonClientType.LITECLIENT => await _liteClientApi.GetWalletInformation(address, block),
                _ => null
            };
        }
        
        /// <summary>
        /// Get up-to-date masterchain state.
        /// </summary>
        /// <returns>An object containing the masterchain information.</returns>
        public async Task<MasterchainInformationResult?> GetMasterchainInfo()
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.GetMasterchainInfo(),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.GetMasterchainInfo(),
                TonClientType.LITECLIENT => await _liteClientApi.GetMasterchainInfo(),
                TonClientType.HTTP_TONWHALESAPI => throw new Exception("Method not supported by HTTP_TONWHALESAPI client."),
                _ => null
            };
        }

        /// <summary>
        /// Look up block by either seqno, lt or unixTime.
        /// </summary>
        /// <returns>An object containing the block information.</returns>
        public async Task<BlockIdExtended?> LookUpBlock(int workchain, long shard, long? seqno = null, ulong? lt = null, ulong? unixTime = null)
        {
            if (seqno == null && lt == null && unixTime == null)
                throw new ArgumentException("Seqno, lt or unixTime should be defined");
            
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.LookUpBlock(workchain, shard, seqno, lt, unixTime),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.LookUpBlock(workchain, shard, seqno, lt, unixTime),
                TonClientType.LITECLIENT => await _liteClientApi.LookUpBlock(workchain, shard, seqno, lt, unixTime),
                TonClientType.HTTP_TONWHALESAPI => throw new Exception("Method not supported by HTTP_TONWHALESAPI client."),
                _ => null
            };
        }
        
        /// <summary>
        /// Get metadata of a given block.
        /// </summary>
        /// <returns>An object containing the block metadata.</returns>
        public async Task<BlockDataResult?> GetBlockData(int workchain,
            long shard,
            long seqno,
            string rootHash = null,
            string fileHash = null)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.GetBlockHeader(workchain, shard, seqno, rootHash, fileHash),
                TonClientType.HTTP_TONCENTERAPIV3 => throw new Exception("Method not supported by HTTP_TONCENTERAPIV3 client."),
                TonClientType.LITECLIENT => throw new Exception("Method not supported yet."),
                TonClientType.HTTP_TONWHALESAPI => throw new Exception("Method not supported by HTTP_TONWHALESAPI client."),
                _ => null
            };
        }

        /// <summary>
        /// Get transactions of the given block.
        /// </summary>
        /// <param name="workchain"></param>
        /// <param name="shard"></param>
        /// <param name="seqno"></param>
        /// <param name="rootHash"></param>
        /// <param name="fileHash"></param>
        /// <param name="afterLt"></param>
        /// <param name="afterHash"></param>
        /// <param name="count"></param>
        /// <returns>An object containing the shards information.</returns>
        public async Task<BlockTransactionsResult?> GetBlockTransactions(
            int workchain,
            long shard,
            long seqno,
            string rootHash = null,
            string fileHash = null,
            ulong? afterLt = null,
            string afterHash = null,
            uint count = 10)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.GetBlockTransactions(workchain, shard, seqno, rootHash, fileHash, afterLt, afterHash, count),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.GetBlockTransactions(workchain, shard, seqno, rootHash, fileHash, afterLt, afterHash, count),
                TonClientType.LITECLIENT => await _liteClientApi.GetBlockTransactions(workchain, shard, seqno, rootHash, fileHash, afterLt, afterHash, count),
                TonClientType.HTTP_TONWHALESAPI => throw new Exception("Method not supported by HTTP_TONWHALESAPI client."),
                _ => null
            };
        }
        
        /// <summary>
        /// Get transactions of the given block (extended data).
        /// </summary>
        /// <param name="workchain"></param>
        /// <param name="shard"></param>
        /// <param name="seqno"></param>
        /// <param name="rootHash"></param>
        /// <param name="fileHash"></param>
        /// <param name="afterLt"></param>
        /// <param name="afterHash"></param>
        /// <param name="count"></param>
        /// <returns>An object containing the shards information.</returns>
        public async Task<BlockTransactionsResultExtended?> GetBlockTransactionsExtended(
            int workchain,
            long shard,
            long seqno,
            string rootHash = null,
            string fileHash = null,
            ulong? afterLt = null,
            string afterHash = null,
            uint count = 10)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => throw new Exception("Method not supported by HTTP_TONCENTERAPIV2 client."),
                TonClientType.HTTP_TONCENTERAPIV3 => throw new Exception("Method not supported by HTTP_TONCENTERAPIV3 client."),
                TonClientType.LITECLIENT => await _liteClientApi.GetBlockTransactionsExtended(workchain, shard, seqno, rootHash, fileHash, afterLt, afterHash, count),
                TonClientType.HTTP_TONWHALESAPI => throw new Exception("Method not supported by HTTP_TONWHALESAPI client."),
                _ => null
            };
        }
        
        /// <summary>
        /// Retrieves transaction information for the specified address.
        /// </summary>
        /// <param name="address">The address object to retrieve transaction information for (optional for <c>HTTP_TONCENTERAPIV3</c>).</param>
        /// <param name="workchain">Block workchain (optional).</param>
        /// <param name="shard">Block shard id. Must be sent with workchain (optional).</param>
        /// <param name="seqno">Block block seqno. Must be sent with workchain and shard (optional).</param>
        /// <param name="limit">The maximum number of transactions to retrieve (default: 10).</param>
        /// <param name="lt">The logical time of the transaction to start retrieving from (optional).</param>
        /// <param name="hash">The hash of the transaction to start retrieving from (optional).</param>
        /// <param name="to_lt">The logical time of the transaction to retrieve up to (optional).</param>
        /// <param name="archival">Specifies whether to retrieve transactions from archival nodes (optional).</param>
        /// <returns>An array of transaction information results.</returns>
        public async Task<TransactionsInformationResult[]> GetTransactions(Address address = null,
            int? workchain = null, long? shard = null, long? seqno = null, 
            uint limit = 10, ulong? lt = null, string hash = null, ulong? to_lt = null, bool? archival = null)
        {
            if(_type == TonClientType.LITECLIENT &&  (lt == null || hash == null))
                throw new ArgumentException("From lt and hash, must be defined for LiteClient type.");
            
            if (_type != TonClientType.HTTP_TONCENTERAPIV3 && (workchain != null || shard != null || seqno != null))
                throw new ArgumentException($"Workchain, shard and seqno parameters are not supported for {_type} client type.");
            
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.GetTransactions(address, limit, lt, hash, to_lt, archival),
                TonClientType.HTTP_TONWHALESAPI => await _httpWhales.GetTransactions(address, limit, lt, hash, to_lt, archival),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.GetTransactions(address, workchain, shard, seqno, limit, lt, hash, to_lt, archival),
                TonClientType.LITECLIENT => await _liteClientApi.GetTransactions(address, limit, (long)lt, hash),
                _ => null
            };
        }

        /// <summary>
        /// Executes a specific method on the specified address.
        /// </summary>
        /// <param name="address">The address object to execute the method on.</param>
        /// <param name="method">The name of the method to execute.</param>
        /// <param name="stackItems">The stack parameters for the method (optional).</param>
        /// <param name="block">Can be placed to fetch in specific block, requeres LiteClient (optional).</param>
        /// <returns>The result of the executed method.</returns>
        public async Task<RunGetMethodResult?> RunGetMethod(Address address, string method, IStackItem[] stackItems,
            BlockIdExtended? block = null)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.RunGetMethod(address, method,
                    StackUtils.PackInString(stackItems)),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.RunGetMethod(address, method,
                    StackUtils.PackInStringV3(stackItems)),
                TonClientType.HTTP_TONWHALESAPI => await _httpWhales.RunGetMethod(address, method,
                    StackUtils.PackInString(stackItems)),
                _ => await _liteClientApi.RunGetMethod(address, method, stackItems, block)
            };
        }
        
        /// <summary>
        /// Executes a specific method on the specified address.
        /// </summary>
        /// <param name="address">The address object to execute the method on.</param>
        /// <param name="method">The name of the method to execute.</param>
        /// <param name="stack">The stack parameters for the method (optional).</param>
        /// <returns>The result of the executed method.</returns>
        public async Task<RunGetMethodResult?> RunGetMethod(Address address, string method, string[][] stack)
        {
            if (_type != TonClientType.HTTP_TONCENTERAPIV2 || _type != TonClientType.HTTP_TONWHALESAPI)
                throw new ArgumentException("string[][] stack, must be defined with HTTP_TONCENTERAPIV2 type.");
            return await _httpApi.RunGetMethod(address, method, stack);
        }
        
        /// <summary>
        /// Sends a Bag of Cells (BoC) to the network.
        /// </summary>
        /// <param name="boc">The Cell object representing the Bag of Cells.</param>
        /// <returns>The result of sending the Bag of Cells.</returns>
        public async Task<SendBocResult?> SendBoc(Cell boc)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.SendBoc(boc),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.SendBoc(boc),
                TonClientType.LITECLIENT => await _liteClientApi.SendBoc(boc),
                TonClientType.HTTP_TONWHALESAPI => await _httpWhales.SendBoc(boc),
                _ => null
            };
        } 
        
        /// <summary>
        /// Retrieves a configuration parameter by its ID.
        /// </summary>
        /// <param name="configId">The ID of the configuration parameter to retrieve.</param>
        /// <param name="seqno">The sequence number of the configuration parameter (optional).</param>
        /// <returns>The result of the configuration parameter retrieval.</returns>
        public async Task<ConfigParamResult?> GetConfigParam(int configId, int? seqno = null)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.GetConfigParam(configId, seqno),
                TonClientType.HTTP_TONWHALESAPI => await _httpWhales.GetConfigParam(configId, seqno),
                TonClientType.HTTP_TONCENTERAPIV3 => throw new Exception("Method not supported by HTTP_TONCENTERAPIV3 client."),
                TonClientType.LITECLIENT => await _liteClientApi.GetConfigParam(configId),
                _ => null
            };
        } 
        
        /// <summary>
        /// Get shards information.
        /// </summary>
        /// <param name="seqno">Masterchain seqno to fetch shards of.</param>
        /// <returns>An object containing the shards information.</returns>
        public async Task<ShardsInformationResult?> Shards(long seqno)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.Shards(seqno),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.Shards(seqno),
                TonClientType.LITECLIENT => await _liteClientApi.GetShards(seqno),
                TonClientType.HTTP_TONWHALESAPI => throw new Exception("Method not supported by HTTP_TONCENTERAPIV3 client."),
                _ => null
            };
        }

        /// <summary>
        /// Estimates fee for the message
        /// </summary>
        /// <param name="message">The message for which you need to calculate the fees</param>
        /// <returns>The result of estimation fees.</returns>
        public async Task<IEstimateFeeResult?> EstimateFee(MessageX message, bool ignoreChksig = true)
        {
            return _type switch
            {
                TonClientType.HTTP_TONCENTERAPIV2 => await _httpApi.EstimateFee(message, ignoreChksig),
                TonClientType.HTTP_TONCENTERAPIV3 => await _httpApiV3.EstimateFee(message, ignoreChksig),
                TonClientType.HTTP_TONWHALESAPI => await _httpWhales.EstimateFee(message, ignoreChksig),
                TonClientType.LITECLIENT => throw new Exception("Method cannot be called with LiteClient. Use other client type instead."),
                _ => null
            };
        }

        public void Dispose()
        {
            _httpApi?.Dispose();
            _httpApiV3?.Dispose();
            _httpWhales?.Dispose();
            _liteClientApi?.Dispose();
        }
    }
}