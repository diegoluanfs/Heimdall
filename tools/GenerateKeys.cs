using System;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Uso: dotnet run <caminho-chave-privada> <caminho-chave-publica>");
            return;
        }

        string privateKeyPath = args[0];
        string publicKeyPath = args[1];

        Console.WriteLine("Gerando chaves RSA 2048-bit...");

        using (var rsa = RSA.Create(2048))
        {
            // Exportar chave privada em formato PKCS#8 PEM (padrao .NET)
            var privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
            File.WriteAllText(privateKeyPath, privateKeyPem);

            // Exportar chave publica em formato PKCS#8 PEM (padrao .NET)
            var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
            File.WriteAllText(publicKeyPath, publicKeyPem);
        }

        Console.WriteLine("Chaves geradas com sucesso!");
        Console.WriteLine($"Chave privada: {privateKeyPath}");
        Console.WriteLine($"Chave publica: {publicKeyPath}");
    }
}
