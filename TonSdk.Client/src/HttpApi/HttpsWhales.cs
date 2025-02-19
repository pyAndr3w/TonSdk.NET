using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using static TonSdk.Client.Transformers;

namespace TonSdk.Client
{

    public class HttpWhales : IDisposable
    {
        private readonly HttpClient _httpClient;
        
        internal HttpWhales(HttpParameters httpApiParameters)
        {
            if (string.IsNullOrEmpty(httpApiParameters.Endpoint))
            {
                throw new ArgumentNullException("Endpoint field in Http options cannot be null.");
            }

            if (httpApiParameters.Proxy != null)
            {
                WebProxy webProxy = new WebProxy
                {
                    Address = new Uri( httpApiParameters.Proxy.ProxyType switch
                    {
                        ProxyType.HTTP => "http://" + httpApiParameters.Proxy.Ip + ":" + httpApiParameters.Proxy.Port,
                        ProxyType.HTTPS => "https://" + httpApiParameters.Proxy.Ip + ":" + httpApiParameters.Proxy.Port,
                        ProxyType.Socks4 => "socks4://" + httpApiParameters.Proxy.Ip + ":" + httpApiParameters.Proxy.Port,
                        ProxyType.Socks5 => "socks5://" + httpApiParameters.Proxy.Ip + ":" + httpApiParameters.Proxy.Port,
                        _ => throw new ArgumentOutOfRangeException()
                    }),
                    Credentials = new NetworkCredential(
                        userName: httpApiParameters.Proxy.UserName,
                        password: httpApiParameters.Proxy.Password
                    )
                };
                HttpClientHandler httpClientHandler = new HttpClientHandler
                {
                    Proxy = webProxy
                };

                _httpClient = new HttpClient(httpClientHandler);
                
            }
            else
            {
                _httpClient = new HttpClient();
            }
            _httpClient.Timeout = TimeSpan.FromMilliseconds(Convert.ToDouble(httpApiParameters.Timeout ?? 30000));
            //httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(httpApiParameters.ApiKey))
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", httpApiParameters.ApiKey);

            _httpClient.BaseAddress = new Uri(httpApiParameters.Endpoint);
        }
        
        internal async Task<AddressInformationResult> GetAddressInformation(Address address)
        {
            InAdressInformationBody requestBody =
                new InAdressInformationBody(address.ToString());
            var result = await new TonRequest(new RequestParameters("getAddressInformation", requestBody), _httpClient)
                .Call();
            RootAddressInformation resultAddressInformation =
                JsonConvert.DeserializeObject<RootAddressInformation>(result);
            AddressInformationResult addressInformationResult =
                new AddressInformationResult(resultAddressInformation.Result);
            return addressInformationResult;
        }
        
        
        internal async Task<WalletInformationResult> GetWalletInformation(Address address)
        {
            InAdressInformationBody requestBody =
                new InAdressInformationBody(address.ToString());
            var result = await new TonRequest(new RequestParameters("getWalletInformation", requestBody), _httpClient)
                .Call();
            RootWalletInformation resultWalletInformation =
                JsonConvert.DeserializeObject<RootWalletInformation>(result);
            if (!resultWalletInformation.Ok) throw new Exception("An error occured when requesting a method.");
            WalletInformationResult walletInformationResult =
                new WalletInformationResult(resultWalletInformation.Result);
            return walletInformationResult;
        }

        
        internal async Task<MasterchainInformationResult> GetMasterchainInfo()
        {
            
            var result = await new TonRequest(new RequestParameters("getMasterchainInfo", new EmptyBody()), _httpClient)
                .Call();
            RootMasterchainInformation rootMasterchainInformation =
                JsonConvert.DeserializeObject<RootMasterchainInformation>(result);
            MasterchainInformationResult masterchainInformationResult =
                new MasterchainInformationResult(rootMasterchainInformation.Result);
            return masterchainInformationResult;
        }

        
        internal async Task<ShardsInformationResult> Shards(long seqno)
        {
            var requestBody = new InShardsBody(seqno);
            var result = await new TonRequest(new RequestParameters("shards", requestBody), _httpClient).Call();
            RootShardsInformation rootShardsInformation = JsonConvert.DeserializeObject<RootShardsInformation>(result);
            ShardsInformationResult shardsInformationResult = new ShardsInformationResult(rootShardsInformation.Result);
            return shardsInformationResult;
        }

        
        internal async Task<BlockDataResult> GetBlockHeader(
            int workchain,
            long shard,
            long seqno,
            string rootHash = null,
            string fileHash = null)
        {
            var requestBody = new InBlockHeader(workchain, shard, seqno, rootHash, fileHash);
            var result = await new TonRequest(new RequestParameters("getBlockHeader", requestBody), _httpClient)
                .Call();

            RootBlockHeader rootBlockHeader = JsonConvert.DeserializeObject<RootBlockHeader>(result);
            BlockDataResult blockDataResult = new BlockDataResult(rootBlockHeader.Result);
            return blockDataResult;
        }

        
        internal async Task<BlockIdExtended> LookUpBlock(int workchain, long shard, long? seqno = null, ulong? lt = null, ulong? unixTime = null)
        {
            var requestBody = new InLookUpBlock(workchain, shard, seqno, lt, unixTime);
            var result = await new TonRequest(new RequestParameters("lookupBlock", requestBody), _httpClient)
                .Call();
            BlockIdExtended rootBlockIdExtended = JsonConvert.DeserializeObject<RootLookUpBlock>(result).Result;
            return rootBlockIdExtended;
        }
        
        internal async Task<BlockTransactionsResult> GetBlockTransactions(
            int workchain,
            long shard,
            long seqno,
            string rootHash = null,
            string fileHash = null,
            ulong? afterLt = null,
            string afterHash = null,
            uint? count = null)
        {
            var requestBody =
                new InBlockTransactions(workchain, shard, seqno, rootHash, fileHash, afterLt, afterHash, count);
            var result = await new TonRequest(new RequestParameters("getBlockTransactions", requestBody), _httpClient)
                .Call();
            RootBlockTransactions rootBlockTransactions = JsonConvert.DeserializeObject<RootBlockTransactions>(result);
            BlockTransactionsResult blockTransactionsResult = new BlockTransactionsResult(rootBlockTransactions.Result);
            return blockTransactionsResult;
        }
        
        internal async Task<TransactionsInformationResult[]> GetTransactions(Address address, uint limit = 10,
            ulong? lt = null, string hash = null, ulong? to_lt = null, bool? archival = null)
        {
            InTransactionsBody requestBody = new InTransactionsBody()
            {
                address = address.ToString(AddressType.Base64, new AddressStringifyOptions(true, false, false)),
                limit = limit
            };
            if (lt != null) requestBody.lt = (ulong)lt;
            if (hash != null) requestBody.hash = hash;
            if (to_lt != null) requestBody.to_lt = (ulong)to_lt;
            if (archival != null) requestBody.archival = (bool)archival;

            var result = await new TonRequest(new RequestParameters("getTransactions", requestBody), _httpClient)
                .Call();
            RootTransactions resultRoot = JsonConvert.DeserializeObject<RootTransactions>(result);

            TransactionsInformationResult[] transactionsInformationResult =
                new TransactionsInformationResult[resultRoot.Result.Length];
            for (int i = 0; i < resultRoot.Result.Length; i++)
            {
                transactionsInformationResult[i] = new TransactionsInformationResult(resultRoot.Result[i]);
            }

            return transactionsInformationResult;
        }
        
        internal async Task<RunGetMethodResult> RunGetMethod(Address address, string method, string[][] stack = null)
        {
            InRunGetMethodBody requestBody = new InRunGetMethodBody()
            {
                address = address.ToString(),
                method = method,
                stack = stack ?? Array.Empty<string[]>()
            };
            var result = await new TonRequest(new RequestParameters("runGetMethod", requestBody), _httpClient).Call();
            RootRunGetMethod resultRoot = JsonConvert.DeserializeObject<RootRunGetMethod>(result);
            RunGetMethodResult outRunGetMethod = new RunGetMethodResult(resultRoot.Result);
            return outRunGetMethod;
        }
        
        internal async Task<SendBocResult> SendBoc(Cell boc)
        {
            InSendBocBody requestBody = new InSendBocBody()
            {
                boc = boc.ToString("base64")
            };
            var result = await new TonRequest(new RequestParameters("sendBoc", requestBody), _httpClient).Call();
            RootSendBoc resultRoot = JsonConvert.DeserializeObject<RootSendBoc>(result);
            SendBocResult outSendBoc = resultRoot.Result;
            outSendBoc.Hash = boc.Hash.ToString();
            return outSendBoc;
        }
        
        internal async Task<EstimateFeeResult> EstimateFee(MessageX message, bool ignoreChksig = true)
        {
            var dataMsg = message.Data;

            Address address = (message.Data.Info.Data is IntMsgInfoOptions info) ? info.Dest :
                      (message.Data.Info.Data is ExtInMsgInfoOptions info2) ? info2.Dest : null;

            Cell body = dataMsg.Body;
            Cell init_code = dataMsg.StateInit?.Data.Code;
            Cell init_data = dataMsg.StateInit?.Data.Data;

            InEstimateFeeBody requestBody = new InEstimateFeeBody()
            {
                address = address.ToString(),
                body = body?.ToString("base64") ?? string.Empty,
                init_code = init_code?.ToString("base64") ?? string.Empty,
                init_data = init_data?.ToString("base64") ?? string.Empty,
                ignore_chksig = ignoreChksig,
            };

            var result = await new TonRequest(new RequestParameters("estimateFee", requestBody), _httpClient).Call();
            RootEstimateFee resultRoot = JsonConvert.DeserializeObject<RootEstimateFee>(result);
            EstimateFeeResult outEstimateFee = resultRoot.Result;
            return outEstimateFee;
        }
        
        internal async Task<ConfigParamResult> GetConfigParam(int configId, int? seqno = null)
        {
            InGetConfigParamBody requestBody = new InGetConfigParamBody()
            {
                config_id = configId,
            };
            if (seqno != null)
            {
                requestBody.seqno = (int)seqno;
            }

            var result = await new TonRequest(new RequestParameters("getConfigParam", requestBody), _httpClient).Call();
            RootGetConfigParam resultRoot = JsonConvert.DeserializeObject<RootGetConfigParam>(result);
            ConfigParamResult outConfigParam = new ConfigParamResult(resultRoot.Result.Config);
            return outConfigParam;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}