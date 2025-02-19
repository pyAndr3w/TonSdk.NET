using System;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using TonSdk.Core;

namespace TonSdk.Client.Tests;
public class ClientTest
{
    
    TonClient client = new(TonClientType.HTTP_TONCENTERAPIV2, 
        new HttpParameters() { Endpoint = "https://toncenter.com/api/v2/jsonRPC", ApiKey = "841a5532a6549cc38f465973eab609c9acb34bb8a15608c85468af38a2842cc9" });
    TonClient clientv3 = new(TonClientType.HTTP_TONCENTERAPIV3, 
        new HttpParameters() { Endpoint = "https://toncenter.com/api/v3/", ApiKey = "841a5532a6549cc38f465973eab609c9acb34bb8a15608c85468af38a2842cc9" });
    TonClient client_lite = new TonClient(TonClientType.LITECLIENT, new LiteClientParameters("5.9.10.47", 19949, "n4VDnSCUuSpjnCyUk9e3QOOd6o0ItSWYbTnW3Wnn8wk="));

    [OneTimeTearDown]
    public void TearDown()
    {
        client.Dispose();
        client_lite.Dispose();
        clientv3.Dispose();
    }
    
    private BigInteger ParseStackItemNum(string valueStr)
    { 
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
    
    [Test]
    public void Test_ParseStackItemNum()
    {
        Assert.That(ParseStackItemNum("0xf") == 15, Is.EqualTo(true));
        Assert.That(ParseStackItemNum("0x0f") == 15, Is.EqualTo(true));
        Assert.That(ParseStackItemNum("0x10") == 16, Is.EqualTo(true));
        Assert.That(ParseStackItemNum("0x159ecd52") == 362728786, Is.EqualTo(true));
        Assert.That(ParseStackItemNum("0x9a") == 154, Is.EqualTo(true));
    }
    
    [Test]
    public async Task Test_AddressBalance()
    {
        Assert.That((await client.GetBalance(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N"))) is Coins , Is.EqualTo(true));
        Assert.That((await clientv3.GetBalance(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N"))) is Coins , Is.EqualTo(true));
        Assert.That((await client_lite.GetBalance(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N"))) is Coins , Is.EqualTo(true));
    }

    [Test]
    public void Test_GetMasterchainInfo()
    {
        Assert.DoesNotThrowAsync(() => client.GetMasterchainInfo());
        Assert.DoesNotThrowAsync(() => client_lite.GetMasterchainInfo());
        Assert.DoesNotThrowAsync(() => clientv3.GetMasterchainInfo());
    }

    [Test]
    public async Task Test_Shards()
    {
        var masterchainInfo = await client.GetMasterchainInfo();
        Assert.DoesNotThrowAsync(() => client.Shards(masterchainInfo.Value.LastBlock.Seqno));
        var masterchainInfo2 = await client_lite.GetMasterchainInfo();
        Assert.DoesNotThrow(() => client_lite.Shards(masterchainInfo2.Value.LastBlock.Seqno));
        var masterchainInfo3 = await clientv3.GetMasterchainInfo();
        Assert.DoesNotThrow(() => clientv3.Shards(masterchainInfo3.Value.LastBlock.Seqno));
    }

    [Test]
    public void Test_GetBlockTransactions()
    {
        Assert.DoesNotThrowAsync(() => client.GetBlockTransactions(0, -9223372036854775808, 14255576, count: 40));
        
        var last = client_lite.GetMasterchainInfo().Result.Value.LastBlock;
        
        Assert.DoesNotThrowAsync(() => client_lite.GetBlockTransactions(last.Workchain, last.Shard, last.Seqno, count: 10));
    }

    [Test]
    public async Task Test_IsContractDeployed()
    {
        Assert.That(await client.IsContractDeployed(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")), Is.EqualTo(true));
        Assert.That(await client.IsContractDeployed(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), Is.EqualTo(false));
        Assert.That(await clientv3.IsContractDeployed(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")), Is.EqualTo(true));
        Assert.That(await clientv3.IsContractDeployed(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), Is.EqualTo(false));
        Assert.That(await client_lite.IsContractDeployed(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")), Is.EqualTo(true));
        Assert.That(await client_lite.IsContractDeployed(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), Is.EqualTo(false));
    }

    [Test]
    public async Task Test_WalletGetSeqno()
    {
        Assert.That(await client.Wallet.GetSeqno(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")) > 0, Is.EqualTo(true));
        Assert.That(await client.Wallet.GetSeqno(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), Is.EqualTo(null));
        Assert.That(await clientv3.Wallet.GetSeqno(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")) > 0, Is.EqualTo(true));
        Assert.That(await clientv3.Wallet.GetSeqno(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), Is.EqualTo(null));
        Assert.That(await client_lite.Wallet.GetSeqno(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")) > 0, Is.EqualTo(true));
        Assert.That(await client_lite.Wallet.GetSeqno(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), Is.EqualTo(null));
    }

    [Test]
    public void Test_WalletGetSubwalletAndPluginList()
    {
        Assert.DoesNotThrowAsync(async Task() => await client.Wallet.GetSubwalletId(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")));
        Assert.DoesNotThrowAsync(async Task() => await client.Wallet.GetPluginList(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")));
        Assert.DoesNotThrowAsync(async Task() => await client_lite.Wallet.GetSubwalletId(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")));
        Assert.DoesNotThrowAsync(async Task() => await client_lite.Wallet.GetPluginList(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")));
        Assert.DoesNotThrowAsync(async Task() => await clientv3.Wallet.GetSubwalletId(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")));
        Assert.DoesNotThrowAsync(async Task() => await clientv3.Wallet.GetPluginList(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqB2N")));
    }

    [Test]
    public async Task Test_DnsGetWalletAddress()
    {
        Assert.That((await client_lite.Dns.GetWalletAddress("foundation.ton")).Equals(new Address("UQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqEBI")), Is.EqualTo(true));
        Assert.That((await clientv3.Dns.GetWalletAddress("foundation.ton")).Equals(new Address("UQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqEBI")), Is.EqualTo(true));
        Assert.That((await client.Dns.GetWalletAddress("foundation.ton")).Equals(new Address("UQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HUn4bpAOg8xqEBI")), Is.EqualTo(true));
    }

    [Test]
    public async Task Test_NftGetItemAddress()
    {
        Assert.That((await clientv3.Nft.GetItemAddress(new Address("EQBiX_Sxuy5htTLg_BzUk1kw7FuI3zWvmnotiuvmnwqZpmUj"), 0)).Equals(new Address("EQCD4WUntH5862PfOQc1vsGAPZsjnm49Mxrj-23O01B_YaJ2")), Is.EqualTo(true));
        Assert.That((await client.Nft.GetItemAddress(new Address("EQBiX_Sxuy5htTLg_BzUk1kw7FuI3zWvmnotiuvmnwqZpmUj"), 0)).Equals(new Address("EQCD4WUntH5862PfOQc1vsGAPZsjnm49Mxrj-23O01B_YaJ2")), Is.EqualTo(true));
        Assert.ThrowsAsync<Exception>(async Task() => await client.Nft.GetItemAddress(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto"), 2), "Cannot retrieve nft address.");
        Assert.That((await client_lite.Nft.GetItemAddress(new Address("EQBiX_Sxuy5htTLg_BzUk1kw7FuI3zWvmnotiuvmnwqZpmUj"), 0)).Equals(new Address("EQCD4WUntH5862PfOQc1vsGAPZsjnm49Mxrj-23O01B_YaJ2")), Is.EqualTo(true));
    }

    [Test]
    public async Task Test_NftGetRoyaltyParams()
    {
        Assert.That((await client.Nft.GetRoyaltyParams(new Address("EQBiX_Sxuy5htTLg_BzUk1kw7FuI3zWvmnotiuvmnwqZpmUj"))).RoyaltyAddress.Equals(new Address("0:580637c2c42ca612ccc0a641b40f9dedfd08e3882dcdf72cc4bd0467194ea014")), Is.EqualTo(true));
        Assert.That((await clientv3.Nft.GetRoyaltyParams(new Address("EQBiX_Sxuy5htTLg_BzUk1kw7FuI3zWvmnotiuvmnwqZpmUj"))).RoyaltyAddress.Equals(new Address("0:580637c2c42ca612ccc0a641b40f9dedfd08e3882dcdf72cc4bd0467194ea014")), Is.EqualTo(true));
        Assert.ThrowsAsync<Exception>(async Task () => await client.Nft.GetRoyaltyParams(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), "Cannot retrieve nft collection royalty params.");
        Assert.That((await client_lite.Nft.GetRoyaltyParams(new Address("EQBiX_Sxuy5htTLg_BzUk1kw7FuI3zWvmnotiuvmnwqZpmUj"))).RoyaltyAddress.Equals(new Address("0:580637c2c42ca612ccc0a641b40f9dedfd08e3882dcdf72cc4bd0467194ea014")), Is.EqualTo(true));
    }

    [Test]
    public async Task Test_NftGetCollectionData()
    {
        
        Assert.ThrowsAsync<Exception>(async Task () => await client.Nft.GetCollectionData(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), "Cannot retrieve nft collection data.");
        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await client.Nft.GetCollectionData(new Address("EQBiX_Sxuy5htTLg_BzUk1kw7FuI3zWvmnotiuvmnwqZpmUj"))).OwnerAddress.Equals(new Address("EQAkL6uxUwDxqI_4r7GwdzbpzoxiVRrT92WZ2ri84qYW8xPz")), Is.EqualTo(true));
            Assert.That((await client_lite.Nft.GetCollectionData(new Address("EQBiX_Sxuy5htTLg_BzUk1kw7FuI3zWvmnotiuvmnwqZpmUj"))).OwnerAddress.Equals(new Address("EQAkL6uxUwDxqI_4r7GwdzbpzoxiVRrT92WZ2ri84qYW8xPz")), Is.EqualTo(true));
            Assert.That((await clientv3.Nft.GetCollectionData(new Address("EQBiX_Sxuy5htTLg_BzUk1kw7FuI3zWvmnotiuvmnwqZpmUj"))).OwnerAddress.Equals(new Address("EQAkL6uxUwDxqI_4r7GwdzbpzoxiVRrT92WZ2ri84qYW8xPz")), Is.EqualTo(true));
        });
    }

    [Test]
    public async Task Test_NftGetNftItemData()
    {
        Assert.That((await client.Nft.GetNftItemData(new Address("EQBcPvGBUexUD5R0u5QAponoKDxmBPx0MsDa1nuoMPOqMx_n"))).OwnerAddress.Equals(new Address("EQAzkwiDLGRqr766B6ZPaBBHLOYiVZCZpgrHTL3cT8TAcALA")), Is.EqualTo(true));
        Assert.That((await clientv3.Nft.GetNftItemData(new Address("EQBcPvGBUexUD5R0u5QAponoKDxmBPx0MsDa1nuoMPOqMx_n"))).OwnerAddress.Equals(new Address("EQAzkwiDLGRqr766B6ZPaBBHLOYiVZCZpgrHTL3cT8TAcALA")), Is.EqualTo(true));
        Assert.ThrowsAsync<Exception>(async Task () => await client.Nft.GetNftItemData(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), "Cannot retrieve nft item data.");
        Assert.That((await client_lite.Nft.GetNftItemData(new Address("EQBcPvGBUexUD5R0u5QAponoKDxmBPx0MsDa1nuoMPOqMx_n"))).OwnerAddress.Equals(new Address("EQAzkwiDLGRqr766B6ZPaBBHLOYiVZCZpgrHTL3cT8TAcALA")), Is.EqualTo(true));
    }

    [Test]
    public async Task Test_JettonGetJettonData()
    {
        Assert.That((await client.Jetton.GetData(new Address("EQAvDfWFG0oYX19jwNDNBBL1rKNT9XfaGP9HyTb5nb2Eml6y"))).AdminAddress.Equals(new Address("0:bd871909f584158689cf86520c54c8efa23559973ddc4b0820cbd12c514832c2")), Is.EqualTo(true));
        //Assert.That((await client.Jetton.GetData(new Address("EQBlU_tKISgpepeMFT9t3xTDeiVmo25dW_4vUOl6jId_BNIj"))).Content.Symbol.Equals("KOTE"), Is.EqualTo(true));
        Assert.That((await client.Jetton.GetData(new Address("EQDCJL0iQHofcBBvFBHdVG233Ri2V4kCNFgfRT-gqAd3Oc86"))).Content.Symbol, Is.EqualTo("FNZ"));
        Assert.ThrowsAsync<Exception>(async Task () => await client.Jetton.GetData(new Address("EQCD39VS5jcptHL8vMjEXrzGaRcCVYto7HAn4bpAOg8xofto")), "Cannot retrieve jetton data.");
        Assert.That((await client_lite.Jetton.GetData(new Address("EQAvDfWFG0oYX19jwNDNBBL1rKNT9XfaGP9HyTb5nb2Eml6y"))).AdminAddress.Equals(new Address("0:bd871909f584158689cf86520c54c8efa23559973ddc4b0820cbd12c514832c2")), Is.EqualTo(true));
        Assert.That((await client_lite.Jetton.GetData(new Address("EQDCJL0iQHofcBBvFBHdVG233Ri2V4kCNFgfRT-gqAd3Oc86"))).Content.Symbol, Is.EqualTo("FNZ"));
        Assert.That((await clientv3.Jetton.GetData(new Address("EQDCJL0iQHofcBBvFBHdVG233Ri2V4kCNFgfRT-gqAd3Oc86"))).Content.Symbol, Is.EqualTo("FNZ"));
        Assert.That((await clientv3.Jetton.GetData(new Address("EQAvDfWFG0oYX19jwNDNBBL1rKNT9XfaGP9HyTb5nb2Eml6y"))).AdminAddress.Equals(new Address("0:bd871909f584158689cf86520c54c8efa23559973ddc4b0820cbd12c514832c2")), Is.EqualTo(true));

    }

    [Test]
    public void Test_JettonGetBalance()
    {
        Assert.DoesNotThrowAsync(async Task () => await client.Jetton.GetBalance(new Address("EQARULUYsmJq1RiZ-YiH-IJLcAZUVkVff-KBPwEmmaQGH6aC")));
        Assert.DoesNotThrowAsync(async Task () => await client_lite.Jetton.GetBalance(new Address("EQARULUYsmJq1RiZ-YiH-IJLcAZUVkVff-KBPwEmmaQGH6aC")));
        Assert.DoesNotThrowAsync(async Task () => await clientv3.Jetton.GetBalance(new Address("EQARULUYsmJq1RiZ-YiH-IJLcAZUVkVff-KBPwEmmaQGH6aC")));
        Assert.ThrowsAsync<Exception>(async Task () => await client.Jetton.GetBalance(new Address("EQAvDfWFG0oYX19jwNDNBBL1rKNT9XfaGP9HyTb5nb2Eml6y")), "Cannot retrieve jetton wallet data.");
    }

    [Test]
    public void Test_JettonGetTransactions()
    {
        Assert.DoesNotThrowAsync(async Task () => await client.Jetton.GetTransactions(new Address("EQARULUYsmJq1RiZ-YiH-IJLcAZUVkVff-KBPwEmmaQGH6aC")));
        Assert.DoesNotThrowAsync(async Task () => await clientv3.Jetton.GetTransactions(new Address("EQARULUYsmJq1RiZ-YiH-IJLcAZUVkVff-KBPwEmmaQGH6aC")));
        Assert.ThrowsAsync<Exception>(async Task () => await client.Jetton.GetTransactions(new Address("EQAvDfWFG0oYX19jwNDNBBL1rKNT9XfaGP9HyTb5nb2Eml6y")), "Cannot retrieve jetton wallet data.");
    }

    [Test]
    public async Task Test_JettonGetWalletAddress()
    {
        Assert.That((await clientv3.Jetton.GetWalletAddress(new Address("EQBlqsm144Dq6SjbPI4jjZvA1hqTIP3CvHovbIfW_t-SCALE"), new Address("EQAEnqomwC3dg323OcdgUsvk3T38VvYawX8q6x38ulfnCn7b"))).Equals(new Address("EQA_d9IqxSQCSuwZIvH0RRSUMvWK4qrvl5ZH_nOHFH7Gxifq")), Is.EqualTo(true));
        Assert.That((await client.Jetton.GetWalletAddress(new Address("EQBlqsm144Dq6SjbPI4jjZvA1hqTIP3CvHovbIfW_t-SCALE"), new Address("EQAEnqomwC3dg323OcdgUsvk3T38VvYawX8q6x38ulfnCn7b"))).Equals(new Address("EQA_d9IqxSQCSuwZIvH0RRSUMvWK4qrvl5ZH_nOHFH7Gxifq")), Is.EqualTo(true));
        Assert.That((await client.Jetton.GetWalletAddress(new Address("EQBlqsm144Dq6SjbPI4jjZvA1hqTIP3CvHovbIfW_t-SCALE"), new Address("EQAEnqomwC3dg323OcdgUsvk3T38VvYawX8q6x38ulfnCn7b"))).Equals(new Address("EQAEnqomwC3dg323OcdgUsvk3T38VvYawX8q6x38ulfnCn7b")), Is.EqualTo(false));
        Assert.That((await client_lite.Jetton.GetWalletAddress(new Address("EQBlqsm144Dq6SjbPI4jjZvA1hqTIP3CvHovbIfW_t-SCALE"), new Address("EQAEnqomwC3dg323OcdgUsvk3T38VvYawX8q6x38ulfnCn7b"))).Equals(new Address("EQA_d9IqxSQCSuwZIvH0RRSUMvWK4qrvl5ZH_nOHFH7Gxifq")), Is.EqualTo(true));
        Assert.That((await client_lite.Jetton.GetWalletAddress(new Address("EQBlqsm144Dq6SjbPI4jjZvA1hqTIP3CvHovbIfW_t-SCALE"), new Address("EQAEnqomwC3dg323OcdgUsvk3T38VvYawX8q6x38ulfnCn7b"))).Equals(new Address("EQAEnqomwC3dg323OcdgUsvk3T38VvYawX8q6x38ulfnCn7b")), Is.EqualTo(false));
    }

    [Test]
    public void Test_ProxyClass()
    {
        Proxy proxy1 = new Proxy
        {
            Ip = IPAddress.Parse("1.1.1.1"),
            Port = 22222,
            UserName = "WhoIsIt?",
            Password = "54321"
        };
        
        Proxy proxy2 = new Proxy
        {
            Ip = IPAddress.Parse("1.1.1.1"),
            Port = 22222,
            UserName = "WhoIsIt?",
            Password = "54321"
        };
        
        Proxy proxy3 = new Proxy
        {
            Ip = IPAddress.Parse("123.123.123.123"),
            Port = 33333,
            UserName = "ItsMe,Mario",
            Password = "51423"
        };
        
        Assert.That(proxy1.GetHashCode(), Is.EqualTo(-1512725739));
        Assert.That(proxy2.GetHashCode(), Is.EqualTo(-1512725739));
        Assert.That(proxy3.GetHashCode(), Is.EqualTo(168523034));
        
        Assert.That(proxy1.Equals(proxy2), Is.True);
        Assert.That(proxy1.Equals(proxy3), Is.False);
    }
}
