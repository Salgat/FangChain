﻿using FangChain.CLI;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.IO;


var services = new ServiceCollection();
services.AddTransient<IKeyCreation, KeyCreation>();
services.AddTransient<IBlockchainCreation, BlockchainCreation>();
var serviceProvider = services.BuildServiceProvider();

var argument = new Argument<string>("command", "Command name.");
var credentialsOption = new Option<string?>("--credentials-path", "The path of the credentials to create or read from. Defaults to current working directory.");
var blockchainPathOption = new Option<string?>("--blockchain-path", "The path of the blockchain to create or read from. Defaults to the current working directory.");
var command = new RootCommand
{
    argument, 
    credentialsOption,
    blockchainPathOption
};
command.Description = "FangChain CLI";

command.SetHandler(async (string argument, string? credentialsPath, string? blockchainPath, CancellationToken cancellationToken) => 
{
    try
    {
        argument = argument.ToLowerInvariant();
        var defaultDirectory = Directory.GetCurrentDirectory();
        if (argument == "create-keys")
        {
            Console.WriteLine($"Creating public and private keys.");
            var keyCreation = serviceProvider.GetRequiredService<IKeyCreation>();
            var keys = keyCreation.CreatePublicAndPrivateKeys();

            var destination = credentialsPath ?? Path.Join(defaultDirectory, $"keys-{DateTimeOffset.UtcNow:yyyyMMddTHHmmss}.json");
            await keyCreation.StoreKeysAsync(destination, keys, cancellationToken);
            Console.WriteLine($"Public and private keys stored in file path '{destination}'.");
        }
        else if (argument == "create-blockchain")
        {
            Console.WriteLine("Creating blockchain.");
            if (credentialsPath is null)
            {
                throw new ArgumentException($"The '{credentialsOption.Name}' option must be populated with the base58 public key.");
            }

            var blockchainCreation = serviceProvider.GetRequiredService<IBlockchainCreation>();
            var blockchainDirectory = new DirectoryInfo(blockchainPath ?? defaultDirectory);
            await blockchainCreation.CreateBlockChainAsync(blockchainDirectory, credentialsPath);
            Console.WriteLine($"Blockchain created at location '{Path.GetFullPath(blockchainDirectory.FullName)}'.");
        }
    } 
    catch (Exception e)
    {
        Console.WriteLine($"Error Occurred: {e.Message}");
    }
}, argument, credentialsOption, blockchainPathOption);

return await command.InvokeAsync(args);