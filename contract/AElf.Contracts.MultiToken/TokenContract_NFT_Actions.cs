using System.Linq;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    private Empty CreateNFTCollection(CreateInput input)
    {
        return CreateToken(input, SymbolType.NftCollection);
    }

    private Empty CreateNFTInfo(CreateInput input)
    {
        var nftCollectionInfo = AssertNftCollectionExist(input.Symbol);
        input.IssueChainId = input.IssueChainId == 0 ? nftCollectionInfo.IssueChainId : input.IssueChainId;
        Assert(
            input.IssueChainId == nftCollectionInfo.IssueChainId,
            "NFT issue ChainId must be collection's issue chainId");
        if (nftCollectionInfo.ExternalInfo != null && nftCollectionInfo.ExternalInfo.Value.TryGetValue(
                TokenContractConstants.NftCreateChainIdExternalInfoKey,
                out var nftCreateChainId) && long.TryParse(nftCreateChainId, out var nftCreateChainIdLong))
        {
            Assert(nftCreateChainIdLong == Context.ChainId,
                "NFT create ChainId must be collection's NFT create chainId");
        }
        else
        {
            Assert(State.SideChainCreator.Value == null,
                "Failed to create token if side chain creator already set.");
        }
        
        var owner = nftCollectionInfo.Owner ?? nftCollectionInfo.Issuer;
        Assert(Context.Sender == owner && owner == input.Owner, "NFT owner must be collection's owner");
        if (nftCollectionInfo.Symbol == TokenContractConstants.SeedCollectionSymbol)
        {
            Assert(input.Decimals == 0 && input.TotalSupply == 1, "SEED must be unique.");
            Assert(input.ExternalInfo.Value.TryGetValue(TokenContractConstants.SeedOwnedSymbolExternalInfoKey,
                    out var ownedSymbol), "OwnedSymbol does not exist.");
            Assert(input.ExternalInfo.Value.TryGetValue(TokenContractConstants.SeedExpireTimeExternalInfoKey,
                       out var expirationTime)
                   && long.TryParse(expirationTime, out var expirationTimeLong) &&
                   Context.CurrentBlockTime.Seconds <= expirationTimeLong, "Invalid ownedSymbol.");
            var ownedSymbolType = GetSymbolType(ownedSymbol);
            Assert(ownedSymbolType != SymbolType.Nft, "Invalid OwnedSymbol.");
            CheckSymbolLength(ownedSymbol, ownedSymbolType);
            CheckTokenAndCollectionExists(ownedSymbol);
            CheckSymbolSeed(ownedSymbol);
            State.SymbolSeedMap[ownedSymbol] = input.Symbol;
        }

        return CreateToken(input, SymbolType.Nft);
    }

    private void CheckSymbolSeed(string ownedSymbol)
    {
        var oldSymbolSeed = State.SymbolSeedMap[ownedSymbol];

        Assert(oldSymbolSeed == null || !State.TokenInfos[oldSymbolSeed].ExternalInfo.Value
                   .TryGetValue(TokenContractConstants.SeedExpireTimeExternalInfoKey,
                       out var oldSymbolSeedExpireTime) ||
               !long.TryParse(oldSymbolSeedExpireTime, out var symbolSeedExpireTime)
               || Context.CurrentBlockTime.Seconds > symbolSeedExpireTime,
            "OwnedSymbol has been created");
    }


    private void DoTransferFrom(Address from, Address to, Address spender, string symbol, long amount, string memo)
    {
        AssertValidInputAddress(from);
        AssertValidInputAddress(to);
        
        // First check allowance.
        var allowance = GetAllowance(from, spender, symbol, amount, out var allowanceSymbol);
        if (allowance < amount)
        {
            if (IsInWhiteList(new IsInWhiteListInput { Symbol = symbol, Address = spender }).Value)
            {
                DoTransfer(from, to, symbol, amount, memo);
                DealWithExternalInfoDuringTransfer(new TransferFromInput()
                    { From = from, To = to, Symbol = symbol, Amount = amount, Memo = memo });
                return;
            }

            Assert(false,
                $"[TransferFrom]Insufficient allowance. Token: {symbol}; {allowance}/{amount}.\n" +
                $"From:{from}\tSpender:{spender}\tTo:{to}");
        }

        DoTransfer(from, to, symbol, amount, memo);
        DealWithExternalInfoDuringTransfer(new TransferFromInput()
            { From = from, To = to, Symbol = symbol, Amount = amount, Memo = memo });
        State.Allowances[from][spender][allowanceSymbol] = allowance.Sub(amount);
    }

    private long GetAllowance(Address from, Address spender, string sourceSymbol, long amount,
        out string allowanceSymbol)
    {
        allowanceSymbol = sourceSymbol;
        var allowance = State.Allowances[from][spender][sourceSymbol];
        if (allowance >= amount) return allowance;
        var tokenType = GetSymbolType(sourceSymbol);
        if (tokenType == SymbolType.Token)
        {
            allowance = GetGlobalAllowance(from, spender, out allowanceSymbol);
        }
        else
        {
            allowance = GetNftGlobalAllowance(from, spender, sourceSymbol, out allowanceSymbol);
            if (allowance >= amount) return allowance;
            allowance = GetGlobalAllowance(from, spender, out allowanceSymbol);
        }

        return allowance;
    }
    

    private long GetGlobalAllowance(Address from, Address spender, out string allowanceSymbol)
    {
        allowanceSymbol = GetGlobalAllowanceSymbol();
        return State.Allowances[from][spender][allowanceSymbol];
    }

    private long GetNftGlobalAllowance(Address from, Address spender, string sourceSymbol,
        out string allowanceSymbol)
    {
        allowanceSymbol = GetNftGlobalAllowanceSymbol(sourceSymbol);
        return State.Allowances[from][spender][allowanceSymbol];
    }

    private string GetNftGlobalAllowanceSymbol(string sourceSymbol)
    {
        // "AAA-*"
        return $"{sourceSymbol.Split(TokenContractConstants.NFTSymbolSeparator)[0]}-{TokenContractConstants.GlobalAllowanceIdentifier}";
    }

    private string GetGlobalAllowanceSymbol()
    {
        // "*"
        return TokenContractConstants.GlobalAllowanceIdentifier.ToString();
    }


    private string GetNftCollectionSymbol(string inputSymbol)
    {
        var symbol = inputSymbol;
        var words = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        const int tokenSymbolLength = 1;
        if (words.Length == tokenSymbolLength) return null;
        Assert(words.Length == 2 && words[1].All(IsValidItemIdChar), "Invalid NFT Symbol Input");
        return symbol == $"{words[0]}-0" ? null : $"{words[0]}-0";
    }

    private TokenInfo AssertNftCollectionExist(string symbol)
    {
        var collectionSymbol = GetNftCollectionSymbol(symbol);
        if (collectionSymbol == null) return null;
        var collectionInfo = State.TokenInfos[collectionSymbol];
        Assert(collectionInfo != null, "NFT collection not exist");
        return collectionInfo;
    }
}