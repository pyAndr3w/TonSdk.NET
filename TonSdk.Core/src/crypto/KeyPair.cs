﻿using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using System.Text;
using TonSdk.Core.Boc;

namespace TonSdk.Core.Crypto;

public class KeyPair
{
    public byte[]? PrivateKey { get; set; }
    public byte[]? PublicKey { get; set; }

    private static byte[] SignDetached(byte[] hash, byte[] privateKey)
    {
        Ed25519PrivateKeyParameters privateKeyParams = new(privateKey, 0);

        ISigner signer = new Ed25519Signer();
        signer.Init(true, privateKeyParams);

        signer.BlockUpdate(hash, 0, hash.Length);
        byte[] signature = signer.GenerateSignature();

        return signature;
    }

    public static byte[] Sign(Cell data, byte[] key)
    {
        byte[] hash = data.Hash.ToBytes();
        byte[] signature = SignDetached(hash, key);

        return signature;
    }
}