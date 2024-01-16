// See https://aka.ms/new-console-template for more information

using System.IO.Compression;
using System.Text;
using CryptSharp.Utility;
using Newtonsoft.Json.Linq;

Console.WriteLine("Hello, World!");
var filePath = @"C:\users\86198\Desktop\lex.wowsreplay";
if (!File.Exists(filePath))
{
    Console.WriteLine("no such file");
    return;
}
using var stream = File.Open(filePath, FileMode.Open);
using var reader = new BinaryReader(stream, Encoding.UTF8, false);
Console.WriteLine("opened file!");
var magic = reader.ReadInt32();
Console.WriteLine("magic is 0x{0:X}", magic);
var blockCount = reader.ReadInt32();
var blockSize = reader.ReadInt32();
var engineData = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(blockSize));
JObject engineDataJson = JObject.Parse(engineData);
JArray? vehicles = (JArray)engineDataJson["vehicles"]!;
// Console.WriteLine(vehicles);
Console.WriteLine($"block count = {blockCount}, block size = {blockSize}, engine data = {engineData}");
for (int i = 1; i < blockCount; i++)
{
    var dataLength = reader.ReadInt32();
    var dataContent = Encoding.UTF8.GetString(reader.ReadBytes(dataLength));
    Console.WriteLine($"block {i} data length {dataLength}\n{dataContent}");
}
var encryptedCompressedData = reader.ReadBytes(20000000); // 20MB
var compressedData = new byte[encryptedCompressedData.Length];
byte[] wowsPwd = {0x29, 0xB7, 0xC9, 0x09, 0x38, 0x3F, 0x84, 0x88, 0xFA, 0x98, 0xEC, 0x4E, 0x13, 0x19, 0x79, 0xFB};
var blowfish = BlowfishCipher.Create(wowsPwd);
for (int i = 0; i < compressedData.Length; i += 8)
{
    blowfish.Decipher(encryptedCompressedData, i, compressedData, i);
    if (i > 0)
    {
        for (int j = 0; j < 8; j++)
        {
            compressedData[i + j] ^= compressedData[i + j - 8];
        }
    }
}

using var outFile = File.Open("decrypted.hex", FileMode.OpenOrCreate);
outFile.Write(compressedData);

using var ms = new MemoryStream(compressedData);

byte[] decompressedData = new byte[20000000]; // 20MB
decompressedData[0] = (byte)ms.ReadByte();
decompressedData[1] = (byte)ms.ReadByte();
using var dfs = new DeflateStream(ms, CompressionMode.Decompress);
int decompressedLength = 2;

for (;; decompressedLength++)
{
    var j = dfs.ReadByte();
    if (j == -1)
    {
        break;
    }
    decompressedData[decompressedLength] = (byte)j;
}
