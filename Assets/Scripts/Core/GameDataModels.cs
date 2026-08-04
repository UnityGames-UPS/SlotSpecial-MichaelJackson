using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

#region Server Communication Models


[Serializable]
public class InitData
{
    public string id = "initData";
    public ServerGameData gameData;
    public ServerFeatures features;
    public ServerPlayer player;
    public ServerUIData uiData;
}

[Serializable]
public class ServerGameData
{
    public List<List<int>> lines;
    public List<double> bets;
}

[Serializable]
public class ServerFeatures
{
    public BonusBetFeature bonusBet;
    public FeatureDescription stackedWild;
    public FeatureDescription moonwalkWild;
    public WheelBonusConfig wheelBonus;
    public FeatureDescription beatItFreeGames;
    public FeatureDescription smoothCriminalFreeGames;
    public PayRulesData payRules;
}

[Serializable]
public class WheelBonusConfig
{
    public List<WheelSegment> wheelSegments;
    public List<int> multiplierWheelSegments;
}

[Serializable]
public class WheelSegment
{
    public string type;         // "credits", "freeGames", "multiplierWheel"
    public double value;        // Credit value or initial credit for multiplier wheel
    public int? count;          // Free games count
    public string feature;      // "beatIt" or "smoothCriminal"
}

[Serializable]
public class BonusBetFeature
{
    public BonusBetDescription description;
}

[Serializable]
public class BonusBetDescription
{
    public bool enabled;
    public int multiplier;
    public string description;
}

[Serializable]
public class FeatureDescription
{
    public string description;
}

[Serializable]
public class PayRulesData
{
    public string linePayDirection;
    public bool highestWinPerLineOnly;
    public bool scatterPaysAdditional;
    public string linePayMultipliedBy;
    public string scatterPayMultipliedBy;
    public List<string> wildSubstitutesExcept;
    public List<int> bonusAppearsOnReels;
}

[Serializable]
public class ServerUIData
{
    public PaylineData paylines;
}

[Serializable]
public class PaylineData
{
    public List<ServerSymbolInfo> symbols;
}

[Serializable]
public class ServerSymbolInfo
{
    public int id;
    public string name;
    public string displayName;
    public string description;
    public List<double> multiplier;
    public List<double> scatterMultiplier;
    public List<ServerSymbolVariant> variants;
}

[Serializable]
public class ServerSymbolVariant
{
    public string type;
    public bool isBonusSymbol;
    public bool isJackpotSymbol;
}

[Serializable]
public class ServerPlayer
{
    public double balance;
}

// ============================================================================
// Server Spin Response — matches result JSON
// ============================================================================

[Serializable]
public class ServerSpinResponse
{
    public string id = "ResultData";
    public bool success;
    public List<List<string>> matrix;  // 3 rows x 5 cols at top level
    public Dictionary<string, string> cellMetadata;
    public ServerSpinPayload payload;
    public ServerSpinFeatures features;
    public ServerPlayerBalance player;
}

[Serializable]
public class ServerPlayerBalance
{
    public double? balance;
}

[Serializable]
public class ServerSpinPayload
{
    public double currentWinning;
    public List<ServerLineWin> lineWins;
    public double jackpotScatterWin;
    public int jackpotScatterCount;
    public double bonusScatterWin;
}

[Serializable]
public class ServerLineWin
{
    public int lineIndex;
    public List<int> positions;  // Flat list of column indices [0,1,2,3]
    public double win;
}

[Serializable]
public class ServerSpinFeatures
{
    public ServerFreeGameResult freeGame;
    public ServerWheelBonusResult wheelBonus;
    public ServerMoonwalkWildsResult moonwalkWilds;
    public ServerStackedWildsResult stackedWilds;
}

[Serializable]
public class ServerFreeGameResult
{
    public bool isFreeGame;
    public int freeGameCount;
    public bool freeGameAdded;
    public string gameType;  // "beatIt" or "smoothCriminal" or null
    public List<List<int>> stickyWildPositions;
    public int currentGameIndex;
    public double totalRoundWin;
}

[Serializable]
public class ServerWheelBonusResult
{
    public bool triggered;
    public WheelBonusResultDetail result;
    public int? multiplierResult;
    public double creditAward;
}

[Serializable]
public class WheelBonusResultDetail
{
    public string type;
    public double value;
    public int? count;
    public string feature;
}

[Serializable]
public class ServerMoonwalkWildsResult
{
    public List<List<int>> positions;  // [[col,row], [col,row], ...]
    public bool hasMoonwalkWild;
}

[Serializable]
public class ServerStackedWildsResult
{
    public List<int> reels;  // Which reels are fully stacked wild
    public bool hasStackedWild;
}

// ============================================================================
// Client-Side Spin Request
// ============================================================================

[Serializable]
public class SpinRequest
{
    public string type = "SPIN";
    public SpinPayload payload;
}

[Serializable]
public class SpinPayload
{
    public int betIndex;
    public int spins;
}

[Serializable]
public class BetHistoryRequest
{
    public string type = "BET_HISTORY";
    public string userId;
    public BetHistoryPayload payload;
}

[Serializable]
public class BetHistoryPayload
{
    public int page = 1;
    public int limit = 10;
}

[Serializable]
public class BetHistoryResponse
{
    public string id = "BetHistory";
    public bool success;
    public BetHistoryData payload;
}

[Serializable]
public class BetHistoryData
{
    public List<BetHistoryItem> betHistory;
    public PaginationInfo pagination;
}

[Serializable]
public class BetHistoryItem
{
    public string betSlipNumber;
    public string gameMode;
    public double startingBalance;
    public double bet;
    public double winLoss;
    public double balance;
    public string date;
}

[Serializable]
public class PaginationInfo
{
    public int page;
    public int limit;
    public int total;
    public double totalBetAmount;
    public double totalWinLoss;
    public int totalPages;
}

#endregion

#region Game Configuration (Client Side Converted)

[Serializable]
public class GameConfig
{
    public int reelCount = 5;
    public int rowCount = 3;
    public int symbolCount = 15;
    public int paylineCount = 25;
    public List<List<int>> paylines;
    public List<double> availableBets;
    public List<SymbolInfo> symbols;

    // Symbol ID configuration (Updated to match new server IDs)
    public int wildSymbolId = 0;
    public int jackpotSymbolId = 11;
    public int bonusSymbolId = 12;
    public int moonwalkWildSymbolId = 13;
    public int stackedWildSymbolId = 14;

    // Pay rules
    public string linePayDirection = "leftToRight";
    public bool highestWinPerLineOnly = true;

    // Bonus bet (for later)
    public bool bonusBetEnabled;
    public int bonusBetMultiplier;

    // Wheel bonus
    public WheelBonusConfig wheelBonus;
}

[Serializable]
public class SymbolInfo
{
    public int id;
    public string name;
    public string displayName;
    public List<double> multipliers;
    public List<double> scatterMultipliers;
    public bool isWild;
    public bool isMoonwalkWild;
    public bool isStackedWild;
    public bool isBonus;
    public bool isJackpot;
}

#endregion

#region Player & Game State (Client Side)

[Serializable]
public class PlayerData
{
    public double balance;
    public int currentBetIndex;
}

[Serializable]
public class BalanceSyncPayload
{
    public double balance;
}

[Serializable]
public class SpinResult
{
    public List<List<int>> resultMatrix;  // Client uses int matrix [col][row], 5 cols x 3 rows
    public double winAmount;
    public List<WinLine> winLines;
    public PlayerData playerData;

    // Feature results
    public FreeGameData freeGameData;
    public bool wheelBonusTriggered;
    public ServerWheelBonusResult wheelBonusResult;
    public List<List<int>> moonwalkWildPositions;  // [[col,row], ...]
    public bool hasMoonwalkWild;
    public List<int> stackedWildReels;
    public bool hasStackedWild;

    // Jackpot/Bonus scatter info
    public double jackpotScatterWin;
    public int jackpotScatterCount;
    public double bonusScatterWin;

    // Free game state
    public int serverFreeGameCount;
    public int serverCurrentGameIndex;
    public bool isRoundOver;
}

[Serializable]
public class WinLine
{
    public int lineId;
    public int symbolId;
    public List<int> positions;  // Flat list: [col*rowCount+row, ...]
    public double winAmount;
}

[Serializable]
public class FreeGameData
{
    public bool isFreeGame;
    public int freeGameCount;
    public bool freeGameAdded;
    public string gameType;  // "beatIt" or "smoothCriminal"
    public List<List<int>> stickyWildPositions;
    public int currentGameIndex;
    public double totalRoundWin;
}

#endregion

#region Platform Communication

[Serializable]
public class AuthData
{
    public string token;
    public string socketURL;
    public string nameSpace;
}

#endregion

#region Enums

public enum GameState
{
    Initializing,
    Idle,
    Spinning,
    Stopping,
    ShowingWin,
    FreeSpinMode
}

#endregion

#region Helper Classes for Conversion

/// <summary>
/// Converts server data to client GameConfig
/// </summary>
public static class InitDataConverter
{
    internal static GameConfig ConvertToGameConfig(InitData serverData)
    {
        var config = new GameConfig
        {
            reelCount = 5,
            rowCount = 3,
            symbolCount = serverData.uiData?.paylines?.symbols?.Count ?? 14,
            paylineCount = serverData.gameData.lines?.Count ?? 25,
            paylines = serverData.gameData.lines,
            availableBets = serverData.gameData.bets,
            symbols = new List<SymbolInfo>()
        };

        if (serverData.uiData?.paylines?.symbols != null)
        {
            foreach (var serverSymbol in serverData.uiData.paylines.symbols)
            {
                var symbolInfo = new SymbolInfo
                {
                    id = serverSymbol.id,
                    name = serverSymbol.name,
                    displayName = serverSymbol.displayName ?? serverSymbol.name,
                    multipliers = serverSymbol.multiplier ?? new List<double>(),
                    scatterMultipliers = serverSymbol.scatterMultiplier ?? new List<double>(),
                    isWild = serverSymbol.name == "Wild",
                    isMoonwalkWild = serverSymbol.name == "MoonwalkWild",
                    isStackedWild = serverSymbol.name == "StackedWild",
                    isBonus = serverSymbol.name == "Bonus",
                    isJackpot = serverSymbol.name == "Jackpot"
                };

                config.symbols.Add(symbolInfo);

                if (symbolInfo.isWild) config.wildSymbolId = symbolInfo.id;
                if (symbolInfo.isMoonwalkWild) config.moonwalkWildSymbolId = symbolInfo.id;
                if (symbolInfo.isStackedWild) config.stackedWildSymbolId = symbolInfo.id;
                if (symbolInfo.isBonus) config.bonusSymbolId = symbolInfo.id;
                if (symbolInfo.isJackpot) config.jackpotSymbolId = symbolInfo.id;
            }
        }

        // Pay rules
        if (serverData.features?.payRules != null)
        {
            config.linePayDirection = serverData.features.payRules.linePayDirection ?? "leftToRight";
            config.highestWinPerLineOnly = serverData.features.payRules.highestWinPerLineOnly;
        }

        // Bonus bet
        if (serverData.features?.bonusBet?.description != null)
        {
            config.bonusBetEnabled = serverData.features.bonusBet.description.enabled;
            config.bonusBetMultiplier = serverData.features.bonusBet.description.multiplier;
        }

        // Wheel bonus
        if (serverData.features?.wheelBonus != null)
        {
            config.wheelBonus = serverData.features.wheelBonus;
        }

        return config;
    }

    internal static PlayerData ConvertToPlayerData(ServerPlayer serverPlayer, int defaultBetIndex = 0)
    {
        return new PlayerData
        {
            balance = serverPlayer.balance,
            currentBetIndex = defaultBetIndex
        };
    }

    /// <summary>
    /// Converts server spin response to client SpinResult.
    /// Server sends matrix as 3 rows x 5 cols (string[][]).
    /// Client needs 5 cols x 3 rows (int[][]).
    /// </summary>
    internal static SpinResult ConvertServerResponseToSpinResult(ServerSpinResponse serverResponse, double currentBalance, double betAmount, GameConfig gameConfig)
    {
        double newBalance = serverResponse.player?.balance ?? currentBalance;

        var result = new SpinResult
        {
            resultMatrix = ConvertReelsToMatrix(serverResponse.matrix, serverResponse.cellMetadata, gameConfig),
            winAmount = serverResponse.payload.currentWinning,
            winLines = ConvertLineWins(serverResponse.payload.lineWins, gameConfig),

            playerData = new PlayerData
            {
                balance = newBalance,
                currentBetIndex = 0
            },

            // Jackpot/Bonus scatter
            jackpotScatterWin = serverResponse.payload.jackpotScatterWin,
            jackpotScatterCount = serverResponse.payload.jackpotScatterCount,
            bonusScatterWin = serverResponse.payload.bonusScatterWin,

            // Free game data
            freeGameData = serverResponse.features?.freeGame != null
                ? new FreeGameData
                {
                    isFreeGame = serverResponse.features.freeGame.isFreeGame,
                    freeGameCount = serverResponse.features.freeGame.freeGameCount,
                    freeGameAdded = serverResponse.features.freeGame.freeGameAdded,
                    gameType = serverResponse.features.freeGame.gameType,
                    stickyWildPositions = serverResponse.features.freeGame.stickyWildPositions,
                    currentGameIndex = serverResponse.features.freeGame.currentGameIndex,
                    totalRoundWin = serverResponse.features.freeGame.totalRoundWin
                }
                : null,

            // Wheel bonus
            wheelBonusTriggered = serverResponse.features?.wheelBonus?.triggered ?? false,
            wheelBonusResult = serverResponse.features?.wheelBonus,

            // Moonwalk wilds
            moonwalkWildPositions = serverResponse.features?.moonwalkWilds?.positions,
            hasMoonwalkWild = serverResponse.features?.moonwalkWilds?.hasMoonwalkWild ?? false,

            // Stacked wilds
            stackedWildReels = serverResponse.features?.stackedWilds?.reels,
            hasStackedWild = serverResponse.features?.stackedWilds?.hasStackedWild ?? false,

            // Free game state
            serverFreeGameCount = serverResponse.features?.freeGame?.freeGameCount ?? 0,
            serverCurrentGameIndex = serverResponse.features?.freeGame?.currentGameIndex ?? 0,
            isRoundOver = false
        };

        return result;
    }

    /// <summary>
    /// Server sends matrix as 3 rows x 5 cols (string[][]).
    /// Client needs 5 cols x 3 rows (int[][]).
    /// USES cellMetadata for variant ID mapping.
    /// </summary>
    private static List<List<int>> ConvertReelsToMatrix(List<List<string>> serverMatrix, Dictionary<string, string> cellMetadata, GameConfig gameConfig)
    {
        if (serverMatrix == null || serverMatrix.Count != 3)
        {
            UnityEngine.Debug.LogError($"Invalid server matrix: expected 3 rows, got {serverMatrix?.Count}");
            return GenerateDefaultMatrix();
        }

        var matrix = new List<List<int>>();

        for (int col = 0; col < 5; col++)
        {
            var column = new List<int>();

            for (int row = 0; row < 3; row++)
            {
                if (col >= serverMatrix[row].Count)
                {
                    UnityEngine.Debug.LogError($"Invalid server data at row {row}, col {col}");
                    column.Add(0);
                    continue;
                }

                string symbolStr = serverMatrix[row][col];
                string metadataKey = $"{row}:{col}";

                if (cellMetadata != null && cellMetadata.TryGetValue(metadataKey, out string variantType))
                {
                    symbolStr = variantType;
                }

                if (!int.TryParse(symbolStr, out int symbolId))
                {
                    // Handle variants by string name mapping to base IDs or internal variant IDs
                    switch (symbolStr)
                    {
                        case "moonwild": symbolId = 13; break;
                        case "moonwildbonus": symbolId = 1013; break;
                        case "moonwildjackpot": symbolId = 2013; break;
                        case "stwild": symbolId = 14; break;
                        case "stwildbonus": symbolId = 1014; break;
                        case "stwildjackpot": symbolId = 2014; break;
                        default:
                            UnityEngine.Debug.LogError($"Failed to parse symbol: {symbolStr}");
                            symbolId = 0;
                            break;
                    }
                }

                column.Add(symbolId);
            }

            matrix.Add(column);
        }

        return matrix;
    }

    private static List<List<int>> GenerateDefaultMatrix()
    {
        var matrix = new List<List<int>>();
        for (int col = 0; col < 5; col++)
        {
            var column = new List<int>();
            for (int row = 0; row < 3; row++)
            {
                column.Add(0);
            }
            matrix.Add(column);
        }
        return matrix;
    }

    /// <summary>
    /// Converts server lineWins to client winLines.
    /// Server sends lineIndex + positions (flat column indices).
    /// Uses payline definition to resolve row positions.
    /// Encodes as flat index = col * rowCount + row (rowCount = 3).
    /// </summary>
    private static List<WinLine> ConvertLineWins(List<ServerLineWin> serverLineWins, GameConfig gameConfig)
    {
        var winLines = new List<WinLine>();
        if (serverLineWins == null) return winLines;

        foreach (var serverLine in serverLineWins)
        {
            var flatPositions = new List<int>();

            if (serverLine.positions != null && serverLine.positions.Count > 0 &&
                gameConfig?.paylines != null &&
                serverLine.lineIndex >= 0 &&
                serverLine.lineIndex < gameConfig.paylines.Count)
            {
                var payline = gameConfig.paylines[serverLine.lineIndex];

                // positions is a list of column indices that matched
                foreach (int col in serverLine.positions)
                {
                    if (col >= 0 && col < payline.Count)
                    {
                        int row = payline[col];
                        int flatIndex = row * 5 + col;
                        flatPositions.Add(flatIndex);
                    }
                }
            }

            winLines.Add(new WinLine
            {
                lineId = serverLine.lineIndex,
                symbolId = -1,  // Server doesn't send symbolId in lineWins
                positions = flatPositions,
                winAmount = serverLine.win   // Individual line win not sent, total is in currentWinning
            });
        }

        return winLines;
    }
}

#endregion