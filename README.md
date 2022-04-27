# FangChain


## Summary

FangChain is a blockchain implementation supporting account balances, non-fungible tokens (NFTs), and decentralized hosting. Instead of utilizing proof-of-work or proof-of-stake (or similar schemes), it utilizes authorities established by the blockchain through voting. Transactions (which cause change against the blockchain, such as transferring coins, generating and sending NFTs, etc) utilize voting by the signatures attached to the transaction, where different types of transactions require different signers. The main utilization for this blockchain is where an established authority is present and needed, such as a blockchain managed by a company or consortium.


## Installation and Running

For hosting,
1. Install the [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
2. In the FangChain.CLI folder, run the following console command to generate the founding user's public and private keys,
    * **dotnet run create-keys --credentials-path "my/desired/credentials/path/my-keys.json"**
3. In the FangChain.CLI folder, run the following console command to generate and initialize a new blockchain
    * **dotnet run create-blockchain --credentials-path "my/desired/credentials/path/my-keys.json" --blockchain-path "my/desired/blockchain/path"**
4. In the FangChain.Server folder, run the following console command to startup the blockchain host server,
    * **dotnet run host --credentials-path "my/desired/credentials/path/my-keys.json" --blockchain-path "my/desired/blockchain/path"**
5. To test that the server is running, do an HTTP REST request to get the blockchain's current state, which should have one block with two transactions.
    * **GET http://localhost:5293/blockchain/blocks?fromIndex=0&toIndex=50**

For running tests,
* Using Visual Studio
    1. Install the free [Visual Studio Community Edition](https://visualstudio.microsoft.com/vs/) (or your own preferred IDE)
    2. Run Visual Studio and open **FangChain.sln**
    3. Select **Test->Run All Tests**
    4. If you cannot see the results in the Test Explorer, select **Test->Test Explorer**
* Using command line
    1. Install the [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
    2. In the main repository folder, run the following console command to run all tests,
        * **dotnet test**


## Functionality Details

* It is important to understand that the "centralized authority" is not through traditional means. There's no central server to authenticate through and no username/password, rather your cryptographic key to sign transactions is the only authentication used, and the blockchain itself designates what your authenticated keys are authorized to do. This means that if someone tries to commit a transaction without sufficient authorized signers, the transaction will be rejected and any blockchain containing that transaction would be considered invalid (such as generating new coins on the blockchain, which would typically require the highest designated users to sign the transaction in majority).
* The keys used to create and initialize the blockchain are automatically elevated to *"SuperAdministrator"* designation, with an alias *"creator"*. This is done automatically by initializing the blockchain with a *DesignateUserTransaction* and a *SetAliasTransaction*.
* Certain transactions, such as generating tokens, adding to a user's coin balance, setting a user's designation, etc require a majority of *SuperAdministrators* to sign the transaction for it to be valid, otherwise it will be rejected when proposed. This allows *SuperAdministrators* to operate a blockchain through a quorum, including promoting other users to the *SuperAdministrator* designation to involve them in decisions.
* Anyone can generate keys on their own to participate in blockchain, however their initial designation will be *Anonymous*. This only allows them to receive and send coins and NFTs. Additionally, the user's public key (in base58 form) is their identity/address, similar to Bitcoin. Signing transactions utilizes their private key. Sending coins and NFTs for example only requires the sender's signature on the transaction.
* The current implementation stores the blockchain as a series of json files, where each json file is one block in the blockchain.
* The current implementation supports blockchain compaction, which allows for aggregating a sequence of blocks into a single block that summarizes all changes. This is meant to reduce space and processing utilization, with both the compacted and non-compacted version being simultaneously valid, and only one version needs to be maintained by a host to be able to validate future transactions/blocks. It is important to still provide the non-compacted version of the blockchain, as it contains contextual information about transactions, such as which transactions were lumped together.
* Lumped transactions allow for multiple transactions to occur atomically. This means that whoever signs the lumped transaction will know that if the lumped transaction is committed to the blockchain, all transactions in the lumped transaction will be committed (no partial commits allowed). This allows for things such as transfering coins in exchange for an NFT. The signatures on the lumped transaction are automatically applied to every transaction in the lumped transaction.
* For examples of interacting with the host server to modify the blockchain, see the integration tests present under the **FangChain.Test** project in the **BlockchainTests.cs** file.
* Since no proof-of-work or proof-of-stake is used, transactions are extremely efficient to add to the blockchain. It simply has to check that the valid number of signatures are attached to a transaction, and when ready generate a new block.


## Use Cases

Since a centralized authority is used, this blockchain is intended for companies and consortiums, and any organization in general, that needs to retain control over the blockchain. This differs from a traditional database in that not only is the entire blockchain distributed, but a quorum of users acting on their own is required for all changes to the blockchain.

One use case could be for logistics, where a consortium of transportation companies are designated as *SuperAdministrators* to manage the blockchain. When a company recieves a package, they generate an NFT tied to that package, and upon handing that physical package off to another logistics company (such as the local post office or to a different country), they create a transaction to exchange the NFT for coins, with signatures from both companies acknowledging this exchange has occurred. This both establishes an immutable record of chain of handling, but also allows for secure exchange of delivered items for compensation.

Another use case could be for a government managing a currency, where different mints around the country could participate as *SuperAdministrators*. This way a single compromised key at one location wouldn't allow for printing infinite money, and the other mints could sign a transaction to remove that compromised key's *SuperAdministrator* designation and elevate a new key to *SuperAdministrator* to replace it.

Companies such as TicketMaster or digital asset retailers (think video games, DLC, etc) could utilize this blockchain, where the NFT could represent a redeemable product. The blockchain rules could be modified to prevent transfer of NFTs by normal users if an NFT needs to be permanently associated with that user, with only *SuperAdministrators* able to modify/generate the NFTs. They could also be redeemable, such as purchasing an NFT for a 30 day subscription card, sending it to a friend, and then they send the NFT to a company's blockchain address in exchange for adding the time to their account. The main advantage here is that these companies are typically hesitant to use blockchains due to their immutability and lack of control. If TicketMaster wants to correct fraud, they will need some sort of centralized authority to achieve this, which is either not possible on traditional blockchains or involves a very convoluted process that defeats the purpose of that blockchain.
