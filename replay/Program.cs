﻿// See https://aka.ms/new-console-template for more information

using System.IO.Compression;
using System.Text;
using CryptSharp.Utility;

Console.WriteLine("Hello, World!");
var filePath = @"C:\users\86198\Desktop\lex.wowsreplay";
var startTime = DateTime.Now;
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
/*
JObject engineDataJson = JObject.Parse(engineData);
JArray? vehicles = (JArray)engineDataJson["vehicles"]!;
*/
// Console.WriteLine(vehicles);
Console.WriteLine($"block count = {blockCount}, block size = {blockSize}");
for (int i = 1; i < blockCount; i++)
{
    var dataLength = reader.ReadInt32();
    var dataContent = Encoding.UTF8.GetString(reader.ReadBytes(dataLength));
    Console.WriteLine($"block {i} data length {dataLength}\n{dataContent}");
}
var encryptedCompressedData = reader.ReadBytes(20000000); // 20MB
var compressedData = new byte[encryptedCompressedData.Length];
for (int i = 0; i < 8; i++)
{
    compressedData[i] = encryptedCompressedData[i];
}
byte[] wowsPwd = {0x29, 0xB7, 0xC9, 0x09, 0x38, 0x3F, 0x84, 0x88, 0xFA, 0x98, 0xEC, 0x4E, 0x13, 0x19, 0x79, 0xFB};
var blowfish = BlowfishCipher.Create(wowsPwd);
for (int i = 8; i < compressedData.Length; i += 8)
{
    blowfish.Decipher(encryptedCompressedData, i, compressedData, i);
    if (i > 8)
    {
        for (int j = 0; j < 8; j++)
        {
            compressedData[i + j] ^= compressedData[i + j - 8];
        }
    }
}

using var outFile = File.Open("decrypted.hex", FileMode.OpenOrCreate);
outFile.Write(compressedData);

using var ms = new MemoryStream(compressedData, 10, compressedData.Length - 10);

byte[] decompressedData = new byte[20000000]; // 20MB
//decompressedData[0] = (byte)ms.ReadByte();
//decompressedData[1] = (byte)ms.ReadByte();
using var dfs = new DeflateStream(ms, CompressionMode.Decompress);
using var packetsBinReader = new BinaryReader(dfs);
//int decompressedLength = 0;

/*for (;; decompressedLength++)
{
    var j = dfs.ReadByte();
    if (j == -1)
    {
        break;
    }
    decompressedData[decompressedLength] = (byte)j;
}*/
/*using var outDecompressedFile = File.Open("decompressed data.hex", FileMode.OpenOrCreate);
dfs.CopyTo(outDecompressedFile, 20000000);*/
//outDecompressedFile.Write(decompressedData, 0, decompressedLength);
bool notEnd = true;
int packetCount = 0;
while (notEnd)
{
    try
    {
        var packetSize = packetsBinReader.ReadInt32();
        var packetType = packetsBinReader.ReadInt32();
        var packetTime = packetsBinReader.ReadSingle();
        var packetData = packetsBinReader.ReadBytes(packetSize);
        packetCount++;
        //Console.WriteLine($"[{packetTime}] packet with size {packetSize} of type {packetType}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"end of stream");
        notEnd = false;
    }
}
Console.WriteLine($"{packetCount} packets detected");
var endTime = DateTime.Now;
Console.WriteLine($"use time {endTime - startTime}");
Console.ReadKey();