﻿using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;

namespace TonSdk.Core.Crypto;
public static class Utils
{
    public static ushort Crc16(byte[] data)
    {
        const ushort POLY = 0x1021;
        ushort crc = 0;

        for (int i = 0; i < data.Length; i++)
        {
            crc ^= (ushort)(data[i] << 8);

            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x8000) == 0x8000)
                {
                    crc = (ushort)((crc << 1) ^ POLY);
                }
                else
                {
                    crc <<= 1;
                }
            }
        }

        return (ushort)(crc & 0xffff);
    }

    public static byte[] Crc16BytesBigEndian(byte[] data)
    {
        ushort crc = Crc16(data);
        byte[] bytes = new byte[2];

        bytes[0] = (byte)(crc >> 8);
        bytes[1] = (byte)crc;

        return bytes;
    }

    private static int BitArrayToInt(BitArray bitArray)
    {
        if (bitArray.Length > 32)
            throw new ArgumentException("Argument length shall be at most 32 bits.");

        int[] array = new int[1];
        bitArray.CopyTo(array, 0);
        return array[0];
    }

    public static string BytesToHex(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            sb.Append(bytes[i].ToString("X2"));
        }
        return sb.ToString();
    }

    public static byte[] HexToBytes(string hexString)
    {
        if (hexString.Length % 2 != 0)
        {
            throw new ArgumentException("Hex string must have an even number of characters.");
        }

        var byteArray = new byte[hexString.Length / 2];

        for (int i = 0; i < byteArray.Length; i++)
        {
            byteArray[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }

        return byteArray;
    }

    public static string[] GenerateWords()
    {
        byte[] entropy = GenerateRandomBytes(32);
        BitArray checkSumBits = DeriveChecksumBits(entropy);
        BitArray entropyBits = BytesToBits(entropy);
        BitArray fullBits = entropyBits.Concat(checkSumBits);

        List<BitArray> chunks = SplitBitArray(fullBits, 11);
        string[] words = chunks.Select(chunk => {
            int index = BitArrayToInt(chunk);
            return MnemonicWords.Bip0039En[index];
        }).ToArray();

        return words;
    }

    public static byte[] GenerateSeedBIP39(string[] mnemonic, string salt, int rounds, int keyLength)
    {
        var options = new Rfc2898DeriveBytes(Normalize(string.Join(" ", mnemonic)), Encoding.UTF8.GetBytes(salt), rounds, HashAlgorithmName.SHA512);
        return options.GetBytes(keyLength);
    }

    public static byte[] GenerateSeed(string[] mnemonic, string salt, int rounds, int keyLength)
    {
        byte[] entropy = MnemonicToEntropy(mnemonic, "");
        byte[] seed = Pbkdf2Sha512(entropy, salt, rounds);
        return seed.Take(keyLength).ToArray();
    }

    public static KeyPair GenerateKeyPair(byte[] seed)
    {
        Ed25519PrivateKeyParameters privateKey = new(seed, 0);
        Ed25519PublicKeyParameters publicKey = privateKey.GeneratePublicKey();

        byte[] privateKeyBytes = privateKey.GetEncoded();
        byte[] publicKeyBytes = publicKey.GetEncoded();

        KeyPair keyPair = new KeyPair(privateKeyBytes, publicKeyBytes);

        return keyPair;
    }

    public static string Normalize(string value)
    {
        return (value ?? "").Normalize(NormalizationForm.FormKD);
    }

    private static byte[] MnemonicToEntropy(string[] mnemonicArray, string password = "")
    {
        var mnemonicPhrase = string.Join(" ", mnemonicArray);

        return HmacSha512(mnemonicPhrase, password);
    }

    private static byte[] Pbkdf2Sha512(byte[] password, string salt, int iterations)
    {
        var generator = new Pkcs5S2ParametersGenerator(new Sha512Digest());
        generator.Init(password, Encoding.UTF8.GetBytes(salt), iterations);
        var key = (KeyParameter)generator.GenerateDerivedMacParameters(512);
        return key.GetKey();
    }

    private static byte[] HmacSha512(string key, string data)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using (HMACSHA512 hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashBytes = hmac.ComputeHash(dataBytes);
            return hashBytes;
        }
    }

    private static byte[] GenerateRandomBytes(int byteSize)
    {
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
        byte[] randomBytes = new byte[byteSize];
        randomNumberGenerator.GetBytes(randomBytes);
        return randomBytes;
    }

    private static BitArray DeriveChecksumBits(byte[] entropy)
    {
        int CS = (entropy.Length * 8) / 32;
        SHA256 sha256 = SHA256.Create();
        byte[] hashValue = sha256.ComputeHash(entropy);
        BitArray bits = new BitArray(hashValue);
        BitArray slicedBits = new BitArray(CS);
        for (int i = 0; i < CS; i++)
        {
            slicedBits[i] = bits[i];
        }
        return slicedBits;
    }

    private static BitArray BytesToBits(byte[] data)
    {
        BitArray bitArray = new(data);
        return bitArray;
    }

    private static BitArray Concat(this BitArray bits, BitArray newBits, bool inplace = false)
    {
        var _bits = inplace ? bits : (BitArray)bits.Clone();
        var offset = _bits.Length;
        _bits.Length += newBits.Length;
        return _bits.Write(newBits, offset);
    }

    private static BitArray Write(this BitArray bits, BitArray newBits, int offset, bool inplace = true)
    {
        var _bits = inplace ? bits : (BitArray)bits.Clone();
        var l = newBits.Length;
        var _newBits = newBits;
        _newBits.Length = bits.Length;
        _newBits.LeftShift(offset);
        _bits.Or(_newBits);
        _newBits.RightShift(offset);
        _newBits.Length = l;
        return _bits; // (на цикле не быстрее)
    }

    private static List<BitArray> SplitBitArray(BitArray bits, int chunkSize)
    {
        List<BitArray> chunks = new List<BitArray>();
        int index = 0;

        while (index < bits.Count)
        {
            int chunkCount = chunkSize;

            if (index + chunkSize > bits.Count) // adjust for the last chunk
            {
                chunkCount = bits.Count - index;
            }

            BitArray chunk = new BitArray(chunkCount);

            for (int i = 0; i < chunkCount; i++)
            {
                chunk[i] = bits[index + i];
            }

            chunks.Add(chunk);
            index += chunkSize;
        }

        return chunks;
    }
}
