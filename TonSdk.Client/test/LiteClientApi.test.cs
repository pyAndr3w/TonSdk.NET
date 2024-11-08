using Newtonsoft.Json;
using TonSdk.Core;
using TonSdk.Core.Crypto;

namespace TonSdk.Client.Tests;

public class DevelopmentTests
{
    private const string AdnlHost = "1592601963";
    private const int AdnlPort = 13833;
    private const string AdnlPubKey = "QpVqQiv1u3nCHuBR3cg3fT6NqaFLlnLGbEgtBRukDpU=";

    private TonClient _client; 
    
    [SetUp]
    public void SetUpTest()
    {
        _client = new TonClient(TonClientType.LITECLIENT, new LiteClientParameters("5.9.10.47", 19949, "n4VDnSCUuSpjnCyUk9e3QOOd6o0ItSWYbTnW3Wnn8wk="));
    }
    
    [TearDown]
    public void TearDownTest()
    {
        _client.Dispose();
    }

    [Test]
    public async Task Test_GetAddressInformation()
    {
        Address destination = new Address("EQAiqHfH96zGIC38oNRs1AWHRyn3rsjT1zOiAYfjQ4NKN64s");
        var result = (await _client.GetAddressInformation(destination))!.Value;
    }
    
    [Test]
    public async Task Test_GetWalletInformation()
    {
        Address destination = new Address("EQAiqHfH96zGIC38oNRs1AWHRyn3rsjT1zOiAYfjQ4NKN64s");
        var result = (await _client.GetWalletInformation(destination))!.Value;
        Console.WriteLine(JsonConvert.SerializeObject(result));
    }
    
    [Test]
    public async Task Test_GetMasterchainInfo()
    {
        var result = (await _client.GetMasterchainInfo())!.Value;
        Console.WriteLine(JsonConvert.SerializeObject(result));
    }
    
    [Test]
    public async Task Test_LookUpBlock()
    {
        var result = (await _client.LookUpBlock(-1, -9223372036854775808, lt:
            43737978000000));
        Console.WriteLine(JsonConvert.SerializeObject(result));
    }
    
    [Test]
    public async Task Test_GetBlockTransactions()
    {
        var result = (await _client.GetBlockTransactions(0, -9223372036854775808, 41103967, afterLt: 43754620000007, afterHash: "iu9374hp8/8ews3em7bvow7qhvgowkjzw/djhajef2y="))!.Value;
        Console.WriteLine(JsonConvert.SerializeObject(result));
    }

    [Test]
    public async Task Test_GetTransactions()
    {
        var result = await _client.GetTransactions(address: new Address("EQCwHyzOrKP1lBHbvMrFHChifc1TLgeJVpKgHpL9sluHU-gV"), limit: 10, lt: 20075201000003, hash: "KC2THpE1CsLKCUJQj/CVtzZ1gTPt/Ho36h8bwIlcyII=");
        Console.WriteLine(JsonConvert.SerializeObject(result));
    }
    
    [Test]
    public async Task Test_GetShards()
    {
        var ms = (await _client.GetMasterchainInfo())!.Value;
        var result = await _client.Shards(18244754);
        Console.WriteLine(JsonConvert.SerializeObject(result));
    }
    
    [Test]
    public async Task Test_GetConfigParams()
    {
        var result = await _client.GetConfigParam(4);
        Console.WriteLine(JsonConvert.SerializeObject(result));
    }
    
    // [Test]
    // public async Task Test_GetBlockData()
    // {
    //     // var result = (await _client.GetBlockData(-1, -9223372036854775808, 35247926))!.Value;
    //     // Console.WriteLine(JsonConvert.SerializeObject(result));
    // }
}