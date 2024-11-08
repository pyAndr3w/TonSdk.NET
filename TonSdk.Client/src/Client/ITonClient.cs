using System.Threading.Tasks;
using TonSdk.Client.Stack;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;

namespace TonSdk.Client
{
    public interface ITonClient
    {
        Jetton Jetton { get; }
        Nft Nft { get; }
        Wallet Wallet { get; }
        Dns Dns { get; }
        TonClientType GetClientType();

        /// <summary>
        /// Retrieves the balance of the specified address.
        /// </summary>
        /// <param name="address">The address to retrieve the balance for.</param>
        /// <param name="block">Can be provided to fetch in specific block, requires LiteClient (optional).</param>
        /// <returns>The task result contains the balance as a Coins instance.</returns>
        Task<Coins> GetBalance(Address address, BlockIdExtended? block = null);

        /// <summary>
        /// Checks if a contract is deployed at the specified address.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <param name="block">Can be provided to fetch in specific block, requires LiteClient (optional).</param>
        /// <returns>The task result indicates whether a contract is deployed (true) or not (false) at the specified address.</returns>
        Task<bool> IsContractDeployed(Address address, BlockIdExtended? block = null);

        /// <summary>
        /// Retrieves the address information for the specified address.
        /// </summary>
        /// <param name="address">The address object to retrieve information for.</param>
        /// <param name="block">Can be provided to fetch in specific block, requires LiteClient (optional).</param>
        /// <returns>An object containing the address information.</returns>
        Task<AddressInformationResult?> GetAddressInformation(Address address, BlockIdExtended? block = null);

        /// <summary>
        /// Retrieves the wallet information for the specified address.
        /// </summary>
        /// <param name="address">The address object to retrieve information for.</param>
        /// <param name="block">Can be provided to fetch in specific block, requires LiteClient (optional).</param>
        /// <returns>An object containing the wallet information.</returns>
        Task<WalletInformationResult?> GetWalletInformation(Address address, BlockIdExtended? block = null);

        /// <summary>
        /// Get up-to-date masterchain state.
        /// </summary>
        /// <returns>An object containing the masterchain information.</returns>
        Task<MasterchainInformationResult?> GetMasterchainInfo();

        /// <summary>
        /// Look up block by either seqno, lt or unixTime.
        /// </summary>
        /// <returns>An object containing the block information.</returns>
        Task<BlockIdExtended> LookUpBlock(int workchain, long shard, long? seqno = null, ulong? lt = null, ulong? unixTime = null);

        /// <summary>
        /// Get metadata of a given block.
        /// </summary>
        /// <returns>An object containing the block metadata.</returns>
        Task<BlockDataResult?> GetBlockData(int workchain,
            long shard,
            long seqno,
            string rootHash = null,
            string fileHash = null);

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
        Task<BlockTransactionsResult?> GetBlockTransactions(
            int workchain,
            long shard,
            long seqno,
            string rootHash = null,
            string fileHash = null,
            ulong? afterLt = null,
            string afterHash = null,
            uint count = 10);

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
        Task<TransactionsInformationResult[]> GetTransactions(
            Address address = null,
            int? workchain = null,
            long? shard = null,
            long? seqno = null,
            uint limit = 10,
            ulong? lt = null,
            string hash = null,
            ulong? to_lt = null,
            bool? archival = null);

        /// <summary>
        /// Executes a specific method on the specified address.
        /// </summary>
        /// <param name="address">The address object to execute the method on.</param>
        /// <param name="method">The name of the method to execute.</param>
        /// <param name="stackItems">The stack parameters for the method (optional).</param>
        /// <param name="block">Can be provided to fetch in specific block, requires LiteClient (optional).</param>
        /// <returns>The result of the executed method.</returns>
        Task<RunGetMethodResult?> RunGetMethod(Address address, string method, IStackItem[] stackItems, BlockIdExtended? block = null);

        /// <summary>
        /// Executes a specific method on the specified address.
        /// </summary>
        /// <param name="address">The address object to execute the method on.</param>
        /// <param name="method">The name of the method to execute.</param>
        /// <param name="stack">The stack parameters for the method (optional).</param>
        /// <returns>The result of the executed method.</returns>
        Task<RunGetMethodResult?> RunGetMethod(Address address, string method, string[][] stack);

        /// <summary>
        /// Sends a Bag of Cells (BoC) to the network.
        /// </summary>
        /// <param name="boc">The Cell object representing the Bag of Cells.</param>
        /// <returns>The result of sending the Bag of Cells.</returns>
        Task<SendBocResult?> SendBoc(Cell boc);

        /// <summary>
        /// Retrieves a configuration parameter by its ID.
        /// </summary>
        /// <param name="configId">The ID of the configuration parameter to retrieve.</param>
        /// <param name="seqno">The sequence number of the configuration parameter (optional).</param>
        /// <returns>The result of the configuration parameter retrieval.</returns>
        Task<ConfigParamResult?> GetConfigParam(int configId, int? seqno = null);

        /// <summary>
        /// Get shards information.
        /// </summary>
        /// <param name="seqno">Masterchain seqno to fetch shards of.</param>
        /// <returns>An object containing the shards information.</returns>
        Task<ShardsInformationResult?> Shards(long seqno);

        /// <summary>
        /// Estimates fee for the message
        /// </summary>
        /// <param name="message">The message for which you need to calculate the fees</param>
        /// <returns>The result of estimation fees.</returns>
        Task<IEstimateFeeResult> EstimateFee(MessageX message, bool ignoreChksig = true);
    }
}