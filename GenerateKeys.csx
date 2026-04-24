using System;
using System.Security.Cryptography;

var rsa = RSA.Create(2048);

var privateKey = rsa.ExportRSAPrivateKeyPem();
var publicKey = rsa.ExportRSAPublicKeyPem();

Console.WriteLine("=== CHAVE PRIVADA ===");
Console.WriteLine(privateKey);
Console.WriteLine();
Console.WriteLine("=== CHAVE PÚBLICA ===");
Console.WriteLine(publicKey);
