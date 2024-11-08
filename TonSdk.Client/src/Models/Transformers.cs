using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using TonSdk.Adnl.LiteClient;
using TonSdk.Client.Stack;
using TonSdk.Core;
using TonSdk.Core.Boc;
using static TonSdk.Client.Transformers;

namespace TonSdk.Client
{
    internal static class Transformers
    {
        internal static string[] PackRequestStack(object element)
        {
            if (element is Cell)
            {
                return new string[] { "tvm.Cell", ((Cell)element).ToString() };
            }

            if (element is BigInteger || element is uint || element is int || element is long || element is ulong)
            {
                return new string[] { "num", element.ToString()! };
            }

            if (element is Coins)
            {
                return new string[] { "num", ((Coins)element).ToNano() };
            }

            if (element is CellSlice)
            {
                return new string[] { "tvm.Slice", ((CellSlice)element).RestoreRemainder().ToString()! };
            }

            if (element is Address)
            {
                return new string[] { "tvm.Slice", ((Address)element).ToBOC() };
            }

            // TODO: Message Layout
            throw new Exception($"Unknown type of element: {element}");
        }

        // in
        internal struct EmptyBody : IRequestBody
        {
        }

        internal struct InShardsBody : IRequestBody
        {
            public long seqno { get; set; }

            internal InShardsBody(long seqno) => this.seqno = seqno;
        }

        internal struct InBlockTransactions : IRequestBody
        {
            public int workchain { get; set; }
            public long shard { get; set; }
            public long seqno { get; set; }
            public string root_hash { get; set; }
            public string file_hash { get; set; }
            public ulong? after_lt { get; set; }
            public string after_hash { get; set; }
            public uint? count { get; set; }

            internal InBlockTransactions(
                int workchain,
                long shard,
                long seqno,
                string root_hash = null,
                string file_hash = null,
                ulong? after_lt = null,
                string after_hash = null,
                uint? count = null)
            {
                this.workchain = workchain;
                this.shard = shard;
                this.seqno = seqno;
                this.root_hash = root_hash;
                this.file_hash = file_hash;
                this.after_lt = after_lt;
                this.after_hash = after_hash;
                this.count = count;
            }
        }
        
        internal struct InLookUpBlock : IRequestBody
        {
            public int workchain { get; set; }
            public long shard { get; set; }
            public long? seqno { get; set; }
            public ulong? lt { get; set; }
            public ulong? unixTime { get; set; }

            public InLookUpBlock(
                int workchain,
                long shard,
                long? seqno = null,
                ulong? lt = null,
                ulong? unixTime = null)
            {
                this.workchain = workchain;
                this.shard = shard;
                this.seqno = seqno;
                this.lt = lt;
                this.unixTime = unixTime;
            }
        }

        internal struct InBlockHeader : IRequestBody
        {
            public int workchain { get; set; }
            public long shard { get; set; }
            public long seqno { get; set; }
            public string root_hash { get; set; }
            public string file_hash { get; set; }

            public InBlockHeader(
                int workchain,
                long shard,
                long seqno,
                string root_hash = null,
                string file_hash = null)
            {
                this.workchain = workchain;
                this.shard = shard;
                this.seqno = seqno;
                this.root_hash = root_hash;
                this.file_hash = file_hash;
            }
        }

        internal struct InAdressInformationBody : IRequestBody
        {
            public string address { get; set; }

            internal InAdressInformationBody(string address) => this.address = address;
        }

        internal struct InTransactionsBody : IRequestBody
        {
            public string address;

            public uint limit;

            public ulong lt;

            public string hash;

            public ulong to_lt;

            public bool archival;
        }

        internal struct InRunGetMethodBody : IRequestBody
        {
            public string address;
            public string method;
            public string[][] stack;

            internal InRunGetMethodBody(string address, string method, string[][] stack)
            {
                this.address = address;
                this.method = method;
                this.stack = stack;
            }
        }

        internal struct InSendBocBody : IRequestBody
        {
            public string boc;
        }

        internal struct InEstimateFeeBody : IRequestBody
        {
            public string address;
            public string body;
            public string init_code;
            public string init_data;
            public bool ignore_chksig;
        }

        internal struct InGetConfigParamBody : IRequestBody
        {
            public int config_id;
            public int seqno;
        }

        internal interface OutResult
        {
        }

        internal struct RootAddressInformation
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public OutAddressInformationResult Result { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }
        
        internal struct RootWalletInformation
        {
            [JsonProperty("ok")] public bool Ok { get; set; }

            [JsonProperty("result")]
            public OutWalletInformationResult Result { get; set; }
        }

        internal struct RootMasterchainInformation
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public OutMasterchanInformationResult Result { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }

        internal struct RootShardsInformation
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public OutShardsInformationResult Result { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }

        internal struct RootBlockTransactions
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public OutBlockTransactionsResult Result { get; set; }
            [JsonProperty("transactions")] public OutV3ShortTransactionsResult[] Transactions { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }

        internal struct RootBlockHeader
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public OutBlockHeaderResult Result { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }
        
        internal struct RootLookUpBlock
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public BlockIdExtended Result { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }
        
        internal struct RootV3LookUpBlock
        {
            [JsonProperty("blocks")] public BlockIdExtended[] Blocks { get; set; }
        }

        internal struct RootTransactions
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public OutTransactionsResult[] Result { get; set; }
            [JsonProperty("transactions")] public OutV3TransactionsResult[] Transactions { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }

        internal struct RootRunGetMethod
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public OutRunGetMethod Result { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }

        internal struct RootSendBoc
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public SendBocResult Result { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
            [JsonProperty("message_hash")] public string MessageHash { get; set; }
        }

        internal struct RootEstimateFee
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public EstimateFeeResult Result { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }

        internal struct RootGetConfigParam
        {
            [JsonProperty("ok")] public bool Ok { get; set; }
            [JsonProperty("result")] public OutGetConfigParamResult Result { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("jsonrpc")] public string JsonRPC { get; set; }
        }

        internal struct OutGetConfigParamResult
        {
            [JsonProperty("config")] public OutConfigParamResult Config;
        }

        internal struct OutConfigParamResult
        {
            [JsonProperty("bytes")] public string Bytes;
        }

        internal struct OutAddressInformationResult
        {
            [JsonProperty("state")] public string State;
            [JsonProperty("balance")] public string Balance;
            [JsonProperty("code")] public string Code;
            [JsonProperty("data")] public string Data;
            [JsonProperty("last_transaction_id")] public TransactionId LastTransactionId;
            [JsonProperty("block_id")] public BlockIdExtended BlockId;
            [JsonProperty("frozen_hash")] public string FrozenHash;
            [JsonProperty("sync_utime")] public long SyncUtime;
        }
        
        internal struct OutV3AddressInformationResult
        {
            [JsonProperty("status")] public string Status;
            [JsonProperty("balance")] public string Balance;
            [JsonProperty("code")] public string Code;
            [JsonProperty("data")] public string Data;
            [JsonProperty("last_transaction_lt")] public string LastTransactionLt;
            [JsonProperty("last_transaction_hash")] public string LastTransactionHash;
            [JsonProperty("frozen_hash")] public string FrozenHash;
        }

        internal struct OutWalletInformationResult
        {
            [JsonProperty("wallet")] public string IsWallet;
            [JsonProperty("balance")] public string Balance;
            [JsonProperty("account_state")] public string State;
            [JsonProperty("wallet_type")] public string WalletType;
            [JsonProperty("seqno")] public string Seqno;
            [JsonProperty("last_transaction_id")] public TransactionId LastTransactionId;
            [JsonProperty("wallet_id")] public string WalletId;
        }
        
        internal struct OutV3WalletInformationResult
        {
            [JsonProperty("balance")] public string Balance;
            [JsonProperty("status")] public string Status;
            [JsonProperty("wallet_type")] public string WalletType;
            [JsonProperty("seqno")] public string Seqno;
            [JsonProperty("last_transaction_lt")] public string LastTransactionLt;
            [JsonProperty("last_transaction_hash")] public string LastTransactionHash;
            [JsonProperty("wallet_id")] public string WalletId;
        }

        internal struct OutMasterchanInformationResult
        {
            [JsonProperty("last")] public BlockIdExtended LastBlock;
            [JsonProperty("init")] public BlockIdExtended InitBlock;
            [JsonProperty("state_root_hash")] public string StateRootHash;
        }
        
        internal struct OutV3MasterchainInformationResult
        {
            [JsonProperty("last")] public BlockIdExtended LastBlock;
            [JsonProperty("first")] public BlockIdExtended InitBlock;
        }

        internal struct OutShardsInformationResult
        {
            [JsonProperty("shards")] public BlockIdExtended[] Shards;
        }
        
        internal struct OutV3ShardsInformationResult
        {
            [JsonProperty("blocks")] public OutBlockIdExtended[] Blocks;
        }
        
        internal struct OutBlockIdExtended
        {
            [JsonProperty("workchain")] public int Workchain;
            [JsonProperty("shard")] public string Shard;
            [JsonProperty("seqno")] public string Seqno;
            [JsonProperty("hash")] public string Hash;
            [JsonProperty("root_hash")] public string RootHash;
            [JsonProperty("file_hash")] public string FileHash;
        }

        internal struct OutBlockTransactionsResult
        {
            [JsonProperty("id")] public BlockIdExtended Id;
            [JsonProperty("req_count")] public int ReqCount;
            [JsonProperty("incomplete")] public bool Incomplete;
            [JsonProperty("transactions")] public ShortTransactionsResult[] Transactions;
        }
        
        internal struct OutBlockHeaderResult
        {
            [JsonProperty("id")] public BlockIdExtended Id;
            [JsonProperty("global_id")] public long GlobalId;
            [JsonProperty("version")] public uint Version;
            [JsonProperty("flags")] public int Flags;
            [JsonProperty("after_merge")] public bool AfterMerge;
            [JsonProperty("after_split")] public bool AfterSplit;
            [JsonProperty("before_split")] public bool BeforeSplit;
            [JsonProperty("want_merge")] public bool WantMerge;
            [JsonProperty("want_split")] public bool WantSplit;
            [JsonProperty("validator_list_hash_short")] public long ValidatorListHashShort;
            [JsonProperty("catchain_seqno")] public long CatchainSeqno;
            [JsonProperty("min_ref_mc_seqno")] public long MinRefMcSeqno;
            [JsonProperty("is_key_block")] public bool IsKeyBlock;
            [JsonProperty("prev_key_block_seqno")] public long PrevKeyBlockSeqno;
            [JsonProperty("start_lt")] public ulong StartLt;
            [JsonProperty("end_lt")] public ulong EndLt;
            [JsonProperty("gen_utime")] public long RgenUtime;
            [JsonProperty("prev_blocks")] public BlockIdExtended[] PrevBlocks;
        }
        
        internal struct OutTransactionsResult
        {
            [JsonProperty("address")] public OutTxAddress Address;
            [JsonProperty("utime")] public long Utime;
            [JsonProperty("data")] public string Data;
            [JsonProperty("transaction_id")] public TransactionId TransactionId;
            [JsonProperty("fee")] public string Fee;
            [JsonProperty("storage_fee")] public string StorageFee;
            [JsonProperty("other_fee")] public string OtherFee;
            [JsonProperty("in_msg")] public OutRawMessage InMsg;
            [JsonProperty("out_msgs")] public OutRawMessage[] OutMsgs;
        }
        
        internal struct OutTxAddress
        {
            [JsonProperty("account_address")] public string AccountAddress;
        }
        
        internal struct OutV3TransactionsResult
        {
            [JsonProperty("account")] public string Account;
            [JsonProperty("now")] public long Now;
            [JsonProperty("lt")] public ulong Lt;
            [JsonProperty("hash")] public string Hash;
            [JsonProperty("total_fees")] public string Fee;
            [JsonProperty("prev_trans_hash")] public string PrevTransHash;
            [JsonProperty("prev_trans_lt")] public string PrevTransLt;
            [JsonProperty("in_msg")] public OutV3RawMessage InMsg;
            [JsonProperty("out_msgs")] public OutV3RawMessage[] OutMsgs;
        }

        internal struct OutRawMessage
        {
            [JsonProperty("source")] public string Source;
            [JsonProperty("destination")] public string Destination;
            [JsonProperty("value")] public string Value;
            [JsonProperty("fwd_fee")] public string FwdFee;
            [JsonProperty("ihr_fee")] public string IhrFee;
            [JsonProperty("created_lt")] public ulong? CreaterLt;
            [JsonProperty("body_hash")] public string BodyHash;
            [JsonProperty("msg_data")] public OutRawMessageData MsgData;
            [JsonProperty("message")] public string Message;
        }
        
        internal struct OutV3RawMessage
        {
            [JsonProperty("hash")] public string Hash;
            [JsonProperty("source")] public string Source;
            [JsonProperty("destination")] public string Destination;
            [JsonProperty("value")] public string Value;
            [JsonProperty("opcode")] public string OpCode;
            [JsonProperty("fwd_fee")] public string FwdFee;
            [JsonProperty("ihr_fee")] public string IhrFee;
            [JsonProperty("created_lt")] public ulong? CreatedLt;
            [JsonProperty("message_content")] public OutV3RawMessageData MsgData;
        }
        
        internal struct OutV3RawMessageData
        {
            [JsonProperty("hash")] public string BodyHash;
            [JsonProperty("body")] public string Body;
            [JsonProperty("decoded")] public OutV3MessageDataDecoded? Decoded;
        }
        
        internal struct OutV3MessageDataDecoded
        {
            [JsonProperty("comment")] public string Comment;
        }

        internal struct OutRawMessageData
        {
            [JsonProperty("text")] public string Text;
            [JsonProperty("body")] public string Body;
            [JsonProperty("init_state")] public string InitState;
        }
        
        public struct OutV3ShortTransactionsResult
        {
            [JsonProperty("description")] public OutV3ShortTransactionsDescription Description;
            [JsonProperty("account")] public string Account;
            [JsonProperty("lt")] public ulong Lt;
            [JsonProperty("hash")] public string Hash;
        }

        public struct OutV3ShortTransactionsDescription
        {
            [JsonProperty("compute_ph")] public OutV3ShortTransactionsDescriptionComputePh ComputePh;
        }
    
        public struct OutV3ShortTransactionsDescriptionComputePh
        {
            [JsonProperty("mode")] public int Mode;
        }
    }

    public struct TransactionId
    {
        [JsonProperty("lt")] public ulong Lt;
        [JsonProperty("hash")] public string Hash;
    }

    public class BlockIdExtended
    {
        [JsonProperty("workchain")] public int Workchain;
        [JsonProperty("shard")] public long Shard;
        [JsonProperty("seqno")] public long Seqno;
        [JsonProperty("hash")] public string Hash;
        [JsonProperty("root_hash")] public string RootHash;
        [JsonProperty("file_hash")] public string FileHash;

        public BlockIdExtended()
        {
            
        }
        
        public BlockIdExtended(
            int workchain,
            string rootHash,
            string fileHash,
            long shard,
            int seqno)
        {
            Workchain = workchain;
            RootHash = rootHash;
            FileHash = fileHash;
            Shard = shard;
            Seqno = seqno;
        }
        
        public BlockIdExtended(TonSdk.Adnl.LiteClient.BlockIdExtended blockIdExtended)
        {
            FileHash = Convert.ToBase64String(blockIdExtended.FileHash);
            RootHash = Convert.ToBase64String(blockIdExtended.RootHash);
            Seqno = blockIdExtended.Seqno;
            Shard = blockIdExtended.Shard;
            Workchain = blockIdExtended.Workchain;
        }
    }

    public struct ShortTransactionsResult
    {
        [JsonProperty("mode")] public int Mode;
        [JsonProperty("account")] public string Account;
        [JsonProperty("lt")] public ulong Lt;
        [JsonProperty("hash")] public string Hash;
    }
    
    public struct BlockDataResult
    {
        public BlockIdExtended BlockIdExtended;
        public long GlobalId;
        public uint Version;
        public int Flags;
        public bool AfterMerge;
        public bool AfterSplit;
        public bool BeforeSplit;
        public bool WantMerge;
        public bool WantSplit;
        public long ValidatorListHashShort;
        public long CatchainSeqno;
        public long MinRefMcSeqno;
        public bool IsKeyBlock;
        public long PrevKeyBlockSeqno;
        public ulong StartLt;
        public ulong EndLt;
        public long RgenUtime;
        public BlockIdExtended[] PrevBlocks;
        
        internal BlockDataResult(OutBlockHeaderResult outBlockHeaderResult)
        {
            BlockIdExtended = outBlockHeaderResult.Id;
            GlobalId = outBlockHeaderResult.GlobalId;
            Version = outBlockHeaderResult.Version;
            Flags = outBlockHeaderResult.Flags;
            AfterMerge = outBlockHeaderResult.AfterMerge;
            AfterSplit = outBlockHeaderResult.AfterSplit;
            BeforeSplit = outBlockHeaderResult.BeforeSplit;
            WantMerge = outBlockHeaderResult.WantMerge;
            WantSplit = outBlockHeaderResult.WantSplit;
            ValidatorListHashShort = outBlockHeaderResult.ValidatorListHashShort;
            CatchainSeqno = outBlockHeaderResult.CatchainSeqno;
            MinRefMcSeqno = outBlockHeaderResult.MinRefMcSeqno;
            IsKeyBlock = outBlockHeaderResult.IsKeyBlock;
            PrevKeyBlockSeqno = outBlockHeaderResult.PrevKeyBlockSeqno;
            StartLt = outBlockHeaderResult.StartLt;
            EndLt = outBlockHeaderResult.EndLt;
            RgenUtime = outBlockHeaderResult.RgenUtime;
            PrevBlocks = outBlockHeaderResult.PrevBlocks;
        }
    }

    public struct AddressInformationResult
    {
        public AccountState State;
        public Coins Balance;
        public Cell Code;
        public Cell Data;
        public TransactionId LastTransactionId;
        public string FrozenHash;

        internal AddressInformationResult(OutV3AddressInformationResult outAddressInformationResult)
        {
            switch (outAddressInformationResult.Status)
            {
                case "active":
                {
                    State = AccountState.Active;
                    break;
                }
                case "frozen":
                {
                    State = AccountState.Frozen;
                    break;
                }
                case "uninitialized":
                {
                    State = AccountState.Uninit;
                    break;
                }
                default:
                {
                    State = AccountState.NonExist;
                    break;
                }
            }

            Balance = new Coins(outAddressInformationResult.Balance, new CoinsOptions(true, 9));
            Code = string.IsNullOrEmpty(outAddressInformationResult.Code) ? null : Cell.From(outAddressInformationResult.Code);
            Data = string.IsNullOrEmpty(outAddressInformationResult.Data) ? null : Cell.From(outAddressInformationResult.Data);
            LastTransactionId = new TransactionId()
            {
                Hash = outAddressInformationResult.LastTransactionHash,
                Lt = ulong.Parse(string.IsNullOrEmpty(outAddressInformationResult.LastTransactionLt) ? "0" : outAddressInformationResult.LastTransactionLt)
            };
            // BlockId = outAddressInformationResult.BlockId;
            FrozenHash = outAddressInformationResult.FrozenHash;
            // SyncUtime = outAddressInformationResult.SyncUtime;
        }
        
        internal AddressInformationResult(OutAddressInformationResult outAddressInformationResult)
        {
            switch (outAddressInformationResult.State)
            {
                case "active":
                    {
                        State = AccountState.Active;
                        break;
                    }
                case "frozen":
                    {
                        State = AccountState.Frozen;
                        break;
                    }
                case "uninitialized":
                    {
                        State = AccountState.Uninit;
                        break;
                    }
                default:
                    {
                        State = AccountState.NonExist;
                        break;
                    }
            }

            Balance = new Coins(outAddressInformationResult.Balance, new CoinsOptions(true, 9));
            Code = outAddressInformationResult.Code == "" ? null : Cell.From(outAddressInformationResult.Code);
            Data = outAddressInformationResult.Data == "" ? null : Cell.From(outAddressInformationResult.Data);
            LastTransactionId = outAddressInformationResult.LastTransactionId;
            // BlockId = outAddressInformationResult.BlockId;
            FrozenHash = outAddressInformationResult.FrozenHash;
            // SyncUtime = outAddressInformationResult.SyncUtime;
        }
    }

    public struct MasterchainInformationResult
    {
        public BlockIdExtended LastBlock;
        public BlockIdExtended InitBlock;
        public string? StateRootHash;

        internal MasterchainInformationResult(OutMasterchanInformationResult outAddressInformationResult)
        {
            LastBlock = outAddressInformationResult.LastBlock;
            InitBlock = outAddressInformationResult.InitBlock;
            StateRootHash = outAddressInformationResult.StateRootHash;
        }
        
        internal MasterchainInformationResult(OutV3MasterchainInformationResult outAddressInformationResult)
        {
            LastBlock = outAddressInformationResult.LastBlock;
            InitBlock = outAddressInformationResult.InitBlock;
            StateRootHash = null;
        }
        
        internal MasterchainInformationResult(MasterChainInfo masterChainInfo)
        {
            LastBlock = new BlockIdExtended(masterChainInfo.LastBlockId);
            InitBlock = new BlockIdExtended(masterChainInfo.InitBlockId);
            StateRootHash = Convert.ToBase64String(masterChainInfo.StateRootHash.ToByteArray());
        }
    }

    public struct ShardsInformationResult
    {
        public BlockIdExtended[] Shards;

        internal ShardsInformationResult(OutShardsInformationResult outShardsInformationResult)
        {
            Shards = outShardsInformationResult.Shards;
        }
        
        internal ShardsInformationResult(OutV3ShardsInformationResult outShardsInformationResult)
        {
            Shards = outShardsInformationResult.Blocks
                .Where(shard => shard.Workchain != -1)
                .Select(shard => new BlockIdExtended(shard.Workchain, shard.RootHash, shard.FileHash, Convert.ToInt64(shard.Shard, 16), Convert.ToInt32(shard.Seqno)))
                .ToArray();
        }
    }

    public struct BlockTransactionsResult
    {
        public BlockIdExtended Id;
        public int ReqCount;
        public bool Incomplete;
        public ShortTransactionsResult[] Transactions;

        internal BlockTransactionsResult(OutBlockTransactionsResult outBlockTransactionsResult)
        {
            Id = outBlockTransactionsResult.Id;
            ReqCount = outBlockTransactionsResult.ReqCount;
            Incomplete = outBlockTransactionsResult.Incomplete;
            Transactions = outBlockTransactionsResult.Transactions;
        }
    }
    
    public struct BlockTransactionsResultExtended
    {
        public BlockIdExtended Id;
        public int ReqCount;
        public bool Incomplete;
        public TransactionsInformationResult[] Transactions;
    }

    public struct TransactionsInformationResult
    {
        public Address Address;
        public uint UTime;
        public int OutMsgCount;
        public Cell Data;
        public TransactionId TransactionId;
        public TransactionId PrevTransactionId;
        public Coins Fee;
        public Coins StorageFee;
        public Coins OtherFee;
        public AccountState OrigAccountStatus;
        public AccountState EndAccountStatus;
        public RawMessage InMsg;
        public RawMessage[] OutMsgs;

        internal TransactionsInformationResult(OutTransactionsResult outTransactionsResult)
        {
            Address = new Address(outTransactionsResult.Address.AccountAddress);
            UTime = (uint)outTransactionsResult.Utime;
            Data = Cell.From(outTransactionsResult.Data);
            TransactionId = outTransactionsResult.TransactionId;
            Fee = new Coins(outTransactionsResult.Fee, new CoinsOptions(true, 9));
            StorageFee = new Coins(outTransactionsResult.StorageFee, new CoinsOptions(true, 9));
            OtherFee = new Coins(outTransactionsResult.OtherFee, new CoinsOptions(true, 9));
            InMsg = new RawMessage(outTransactionsResult.InMsg);

            OutMsgs = new RawMessage[outTransactionsResult.OutMsgs.Length];
            for (int i = 0; i < outTransactionsResult.OutMsgs.Length; i++)
            {
                OutMsgs[i] = new RawMessage(outTransactionsResult.OutMsgs[i]);
            }

            OrigAccountStatus = AccountState.Active;
            EndAccountStatus = AccountState.Active;
            OutMsgCount = OutMsgs.Length;
            PrevTransactionId = new TransactionId();
        }
        
        internal TransactionsInformationResult(OutV3TransactionsResult outTransactionsResult)
        {
            Address = new Address(outTransactionsResult.Account);
            UTime = (uint)outTransactionsResult.Now;
            Data = null;
            TransactionId = new TransactionId()
            {
                Hash = outTransactionsResult.Hash,
                Lt = outTransactionsResult.Lt
            };
            PrevTransactionId = new TransactionId()
            {
                Hash = outTransactionsResult.PrevTransHash,
                Lt = ulong.Parse(outTransactionsResult.PrevTransLt)
            };
            Fee = new Coins(outTransactionsResult.Fee, new CoinsOptions(true, 9));
            StorageFee = null;
            OtherFee = null;
            InMsg = new RawMessage(outTransactionsResult.InMsg);

            OutMsgs = new RawMessage[outTransactionsResult.OutMsgs.Length];
            for (int i = 0; i < outTransactionsResult.OutMsgs.Length; i++)
            {
                OutMsgs[i] = new RawMessage(outTransactionsResult.OutMsgs[i]);
            }
            
            OrigAccountStatus = AccountState.Active;
            EndAccountStatus = AccountState.Active;
            OutMsgCount = OutMsgs.Length;
        }
    }

    public struct ConfigParamResult
    {
        public Cell Bytes;

        internal ConfigParamResult(OutConfigParamResult outConfigParamResult)
        {
            Bytes = Cell.From(outConfigParamResult.Bytes);
        }
    }

    public struct RawMessage
    {
        public string Hash;
        public Address Source;
        public Address Destination;
        public Coins Value;
        public Coins FwdFee;
        public Coins IhrFee;
        public ulong? CreatedLt;
        public string OpCode;
        public string BodyHash;
        public RawMessageData MsgData;
        public string Message;

        internal RawMessage(OutRawMessage outRawMessage)
        {
            Source = outRawMessage.Source != null && outRawMessage.Source.Length != 0
                ? new Address(outRawMessage.Source)
                : null;
            Destination = outRawMessage.Destination != null && outRawMessage.Destination.Length != 0
                ? new Address(outRawMessage.Destination)
                : null;
            Value = new Coins(outRawMessage.Value, new CoinsOptions(true, 9));
            FwdFee = new Coins(outRawMessage.FwdFee, new CoinsOptions(true, 9));
            IhrFee = new Coins(outRawMessage.IhrFee, new CoinsOptions(true, 9));
            CreatedLt = outRawMessage.CreaterLt;
            BodyHash = outRawMessage.BodyHash;
            MsgData = new RawMessageData(outRawMessage.MsgData);
            Message = outRawMessage.Message;
            Hash = "";
            OpCode = MsgData.Body != null && MsgData.Body.BitsCount >= 32
                ? $"0x{MsgData.Body.Parse().LoadUInt(32).ToString("X")}"
                : "";
        }
        
        internal RawMessage(OutV3RawMessage outRawMessage)
        {
            Source = !string.IsNullOrEmpty(outRawMessage.Source)
                ? new Address(outRawMessage.Source)
                : null;
            Destination = new Address(outRawMessage.Destination);
            Value = new Coins(outRawMessage.Value, new CoinsOptions(true, 9));
            FwdFee = new Coins(outRawMessage.FwdFee, new CoinsOptions(true, 9));
            IhrFee = new Coins(outRawMessage.IhrFee, new CoinsOptions(true, 9));
            CreatedLt = outRawMessage.CreatedLt;
            BodyHash = outRawMessage.MsgData.BodyHash;
            MsgData = new RawMessageData(outRawMessage.MsgData);
            Message = null;
            Hash = outRawMessage.Hash;
            OpCode = outRawMessage.OpCode ?? "";
        }
    }

    public struct RawMessageData
    {
        public string Text;
        public Cell Body;
        public string InitState;

        internal RawMessageData(OutRawMessageData outRawMessageData)
        {
            Text = outRawMessageData.Text ?? null;
            Body = outRawMessageData.Body != null ? Cell.From(outRawMessageData.Body) : null;
            InitState = outRawMessageData.InitState ?? null;
        }
        internal RawMessageData(OutV3RawMessageData outRawMessageData)
        {
            Text = outRawMessageData.Decoded?.Comment;
            Body = outRawMessageData.Body != null ? Cell.From(outRawMessageData.Body) : null;
            InitState = null;
        }
    }

    internal struct OutRunGetMethod
    {
        [JsonProperty("gas_used")] public int GasUsed;
        [JsonProperty("stack")] public object[][] Stack;
        [JsonProperty("exit_code")] public int ExitCode;
    }
    
    internal struct OutV3RunGetMethod
    {
        [JsonProperty("gas_used")] public int GasUsed;
        [JsonProperty("stack")] public JObject[] Stack;
        [JsonProperty("exit_code")] public int ExitCode;
    }

    public struct RunGetMethodResult
    {
        public int GasUsed;
        public object[] Stack;
        public int ExitCode;
        public IStackItem[] StackItems;

        internal RunGetMethodResult(OutRunGetMethod outRunGetMethod)
        {
            GasUsed = outRunGetMethod.GasUsed;
            ExitCode = outRunGetMethod.ExitCode;
            Stack = new object[outRunGetMethod.Stack.Length];
            for (int i = 0; i < outRunGetMethod.Stack.Length; i++)
            {
                Stack[i] = ParseStackItem(outRunGetMethod.Stack[i]);
            }

            StackItems = new IStackItem[] { };
        }
        
        internal RunGetMethodResult(OutV3RunGetMethod outRunGetMethod)
        {
            GasUsed = outRunGetMethod.GasUsed;
            ExitCode = outRunGetMethod.ExitCode;
            Stack = new object[outRunGetMethod.Stack.Length];
            for (int i = 0; i < outRunGetMethod.Stack.Length; i++)
            {
                Stack[i] = ParseStackItem(outRunGetMethod.Stack[i]);
            }

            StackItems = new IStackItem[] { };
        }

        internal static object ParseObject(JObject x)
        {
            string typeName = x["@type"].ToString();
            switch (typeName)
            {
                case "tvm.list":
                case "tvm.tuple":
                    object[] list = new object[x["elements"].Count()];
                    for (int c = 0; c < x["elements"].Count(); c++)
                    {
                        list[c] = ParseObject((JObject)x["elements"][c]);
                    }

                    return list;
                case "tvm.cell":
                    return Cell.From(x["bytes"].ToString()); // Cell.From should be defined elsewhere in your code.
                case "tvm.stackEntryCell":
                    return ParseObject((JObject)x["cell"]);
                case "tvm.stackEntryTuple":
                    return ParseObject((JObject)x["tuple"]);
                case "tvm.stackEntryNumber":
                    return ParseObject((JObject)x["number"]);
                case "tvm.numberDecimal":
                    string number = x["number"].ToString();
                    return BigInteger.Parse(number);
                default:
                    throw new Exception($"Unknown type {typeName}");
            }
        }
        
        internal static object ParseStackItem(JObject item)
        {
            string type = item["type"].ToString();

            switch (type)
            {
                case "num":
                {
                    string valueStr = item["value"].ToString();
                    if (valueStr == null)
                        throw new Exception("Expected a string value for 'num' type.");

                    bool isNegative = valueStr[0] == '-';
                    string slice = isNegative ? valueStr.Substring(3) : valueStr.Substring(2);
                    BitsSlice bitsSlice = new Bits(slice).Parse();
                    BigInteger x = bitsSlice.LoadUInt(bitsSlice.RemainderBits);

                    return isNegative ? 0 - x : x;
                }
                case "cell":
                {
                    return Cell.From(item["value"].ToString());
                }
                case "list":
                case "tuple":
                {
                    if (item["value"] is JObject jObject)
                    {
                        return ParseObject(jObject);
                    }
                    else
                    {
                        throw new Exception("Expected a JObject value for 'list' or 'tuple' type.");
                    }
                }
                default:
                {
                    throw new Exception("Unknown type " + type);
                }
            }
        }

        internal static object ParseStackItem(object[] item)
        {
            string type = item[0].ToString();
            object value = item[1];

            switch (type)
            {
                case "num":
                    {
                        string valueStr = value as string;
                        if (valueStr == null)
                            throw new Exception("Expected a string value for 'num' type.");
                        
                        bool isNegative = valueStr[0] == '-';
                        string slice = isNegative ? valueStr.Substring(3) : valueStr.Substring(2);
                        
                        if (slice.Length % 2 != 0)
                        {
                            slice = "0" + slice;
                        }

                        int length = slice.Length;
                        byte[] bytes = new byte[length / 2];
                        for (int i = 0; i < length; i += 2)
                        {
                            bytes[i / 2] = Convert.ToByte(slice.Substring(i, 2), 16);
                        }
                        
                        if (bytes[0] >= 0x80)
                        {
                            byte[] temp = new byte[bytes.Length + 1];
                            Array.Copy(bytes, 0, temp, 1, bytes.Length);
                            bytes = temp;
                        }

                        Array.Reverse(bytes);
                        var bigInt = new BigInteger(bytes);
                        
                        return isNegative ? 0 - bigInt : bigInt;
                    }
                case "cell":
                    {
                        if (value is JObject jObject && jObject["bytes"] is JValue jValue)
                        {
                            return Cell.From((string)jValue.Value);
                        }
                        else
                        {
                            throw new Exception("Expected a JObject value for 'cell' type.");
                        }
                    }
                case "list":
                case "tuple":
                    {
                        if (value is JObject jObject)
                        {
                            return ParseObject(jObject);
                        }
                        else
                        {
                            throw new Exception("Expected a JObject value for 'list' or 'tuple' type.");
                        }
                    }
                default:
                    {
                        throw new Exception("Unknown type " + type);
                    }
            }
        }
    }
    
    public struct WalletInformationResult
    {
        public bool IsWallet;
        public Coins Balance;
        public AccountState State;
        public string? WalletType;
        public long? Seqno;
        public TransactionId LastTransactionId;
        public long? WalletId;

        internal WalletInformationResult(AddressInformationResult addressInformationResult)
        {
            WalletType = null;
            Seqno = null;
            WalletId = null;
            IsWallet = false;

            Balance = addressInformationResult.Balance;
            LastTransactionId = addressInformationResult.LastTransactionId;
            State = addressInformationResult.State;
        }
        
        internal WalletInformationResult(OutV3WalletInformationResult walletInformationResult)
        {
            Seqno = long.Parse(walletInformationResult.Seqno);
            WalletType = walletInformationResult.WalletType;
            WalletId = long.Parse(walletInformationResult.WalletId);
            IsWallet = true;
            
            Balance = new Coins(walletInformationResult.Balance, new CoinsOptions(true, 9));
            LastTransactionId = new TransactionId()
            {
                Hash =  walletInformationResult.LastTransactionHash,
                Lt =  ulong.Parse(walletInformationResult.LastTransactionLt)
            };
            switch (walletInformationResult.Status)
            {
                case "active":
                {
                    State = AccountState.Active;
                    break;
                }
                case "frozen":
                {
                    State = AccountState.Frozen;
                    break;
                }
                case "uninitialized":
                {
                    State = AccountState.Uninit;
                    break;
                }
                default:
                {
                    State = AccountState.NonExist;
                    break;
                }
            }
        }

        internal WalletInformationResult(OutWalletInformationResult walletInformationResult)
        {
            IsWallet = bool.Parse(walletInformationResult.IsWallet);
            if (IsWallet)
            {
                WalletType = walletInformationResult.WalletType;
                Seqno = long.Parse(walletInformationResult.Seqno);
                WalletId = long.Parse(walletInformationResult.WalletId);
            }
            else
            {
                WalletType = null;
                Seqno = null;
                WalletId = null;
            }

            Balance = new Coins(walletInformationResult.Balance, new CoinsOptions(true, 9));
            LastTransactionId = walletInformationResult.LastTransactionId;
            switch (walletInformationResult.State)
            {
                case "active":
                {
                    State = AccountState.Active;
                    break;
                }
                case "frozen":
                {
                    State = AccountState.Frozen;
                    break;
                }
                case "uninitialized":
                {
                    State = AccountState.Uninit;
                    break;
                }
                default:
                {
                    State = AccountState.NonExist;
                    break;
                }
            }
        }
    }


    public struct SendBocResult
    {
        [JsonProperty("@type")] public string Type;
        [JsonProperty("hash")] public string Hash;
    }

    public interface IEstimateFeeResult
    {
        public SourceFees SourceFees { get; set; }
    }

    public struct EstimateFeeResult : IEstimateFeeResult
    {
        [JsonProperty("@type")] public string Type;
        [JsonProperty("source_fees")] public SourceFees SourceFees { get; set; }
    }
    
    public struct EstimateFeeResultExtended : IEstimateFeeResult
    {
        [JsonProperty("source_fees")] public SourceFees SourceFees { get; set; }
        [JsonProperty("destination_fees")] public SourceFees[] DestinationFees { get; set; }
    }

    public struct SourceFees
    {
        [JsonProperty("@type")] public string Type;
        [JsonProperty("in_fwd_fee")] public long InFwdFee;
        [JsonProperty("storage_fee")] public long StorageFee;
        [JsonProperty("gas_fee")] public long GasFee;
        [JsonProperty("fwd_fee")] public long FwdFee;
    }

    public enum AccountState
    {
        Active,
        Frozen,
        Uninit,
        NonExist
    }
}
