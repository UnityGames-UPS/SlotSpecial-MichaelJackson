using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SlotView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Symbol Sprites - Assign by Name")]
    [Tooltip("Symbol sprites assigned by name. Array order: Wild, Glove, Hat, Sunglasses, Shoes, Ace, King, Queen, Jack, Ten, Nine, Jackpot, Bonus, MoonwalkWild, StackedWild")]
    [SerializeField] private Sprite spriteWild;              // ID: 0
    [SerializeField] private Sprite spriteGlove;             // ID: 1
    [SerializeField] private Sprite spriteHat;               // ID: 2
    [SerializeField] private Sprite spriteSunglasses;        // ID: 3
    [SerializeField] private Sprite spriteShoes;             // ID: 4
    [SerializeField] private Sprite spriteAce;               // ID: 5
    [SerializeField] private Sprite spriteKing;              // ID: 6
    [SerializeField] private Sprite spriteQueen;             // ID: 7
    [SerializeField] private Sprite spriteJack;              // ID: 8
    [SerializeField] private Sprite spriteTen;               // ID: 9
    [SerializeField] private Sprite spriteNine;              // ID: 10
    [SerializeField] private Sprite spriteJackpot;           // ID: 11
    [SerializeField] private Sprite spriteBonus;             // ID: 12
    [SerializeField] private Sprite spriteMoonwalkWild;      // ID: 13
    [SerializeField] private Sprite spriteMoonwalkWildBonus; // ID: 1013
    [SerializeField] private Sprite spriteMoonwalkWildJackpot; // ID: 2013
    [SerializeField] private Sprite spriteStackedWild;       // ID: 14
    [SerializeField] private Sprite spriteStackedWildBonus;  // ID: 1014
    [SerializeField] private Sprite spriteStackedWildJackpot; // ID: 2014

    private Dictionary<int, Sprite> symbolSpritesMap;

    [Header("Win Animation Sprite Arrays - One per Symbol ID")]
    [Tooltip("Animation sprite arrays for each symbol. Array index = symbol ID (0-14)")]

    [SerializeField] private List<Sprite> animSpritesWild;           // ID: 0
    [SerializeField] private List<Sprite> animSpritesJackpot;        // ID: 11
    [SerializeField] private List<Sprite> animSpritesBonus;          // ID: 12
    [SerializeField] private List<Sprite> animSpritesMoonwalkWild;   // ID: 13
    [SerializeField] private List<Sprite> animSpritesMoonwalkWildBonus; // ID: 1013
    [SerializeField] private List<Sprite> animSpritesMoonwalkWildJackpot; // ID: 2013
    [SerializeField] private List<Sprite> animSpritesStackedWild;    // ID: 14
    [SerializeField] private List<Sprite> animSpritesStackedWildBonus; // ID: 1014
    [SerializeField] private List<Sprite> animSpritesStackedWildJackpot; // ID: 2014

    // Internal map of animation sprite lists
    private Dictionary<int, List<Sprite>> animationSpriteMap;

    [Header("Reel Containers")]
    [SerializeField] private Transform[] reelTransforms;

    [Header("Reel Images - 16 images per reel")]
    [SerializeField] private List<ReelImages> reelImagesList;

    [Header("Spin Settings")]
    [SerializeField] private float symbolHeight = 100f;
    [SerializeField] private float spinSpeed = 0.05f;
    [SerializeField] private float reelStartStagger = 0.08f;
    [SerializeField] private float reelStopStagger = 0.12f;

    [Header("Animation Settings - Casino Style")]
    [SerializeField] private float anticipationUpDistance = 30f;
    [SerializeField] private float anticipationUpDuration = 0.15f;
    [SerializeField] private float dropDownDistance = 15f;
    [SerializeField] private float dropDownDuration = 0.12f;
    [SerializeField] private float settleBounceDuration = 0.18f;

    [Header("Win Animation Settings")]
    [SerializeField] private float winPopDuration = 0.4f;
    [SerializeField] private int winPopRepeat = 3;


    [Header("Stop Animation Settings")]
    [SerializeField] private float stopOvershootDistance = 50f;
    [SerializeField] private float stopOvershootDuration = 0.15f;
    [SerializeField] private float stopBounceBackDistance = 15f;
    [SerializeField] private float stopBounceBackDuration = 0.25f;
    [SerializeField] private float stopSettleDuration = 0.35f;

    [Header("Quick Spin Settings")]
    [SerializeField] private float quickStopStagger = 0.06f;
    [SerializeField] private float quickStopOvershoot = 20f;
    [SerializeField] private float quickStopDuration = 0.2f;
    [SerializeField] private int minSpinCyclesBeforeStop = 3;

    [Header("Scatter Anticipation Settings")]
    [SerializeField] private int scatterSymbolId = 12;
    [SerializeField] private float anticipationExtraSpins = 3f;
    [SerializeField] private float anticipationSpeedMultiplier = 1.5f;

    [Header("Win Animation Settings")]
    [SerializeField] private float winAnimationDuration = 3.0f; // Total duration each win symbol animation plays
    [SerializeField] private float winSymbolLoopDuration = 1.5f;
    [SerializeField] private int winSymbolLoopCount = 3;
    [Tooltip("Delay between enabling winBox overlay and starting the ImageAnimation - for sync timing")]
    [SerializeField] private float winLineBoxToAnimationDelay = 0.05f;

    [Header("Win Box Overlays — Col 0..4  (each has 4 rows: 0=top .. 3=bottom)")]
    [SerializeField] private ColumnOverlays[] winBoxColumns = new ColumnOverlays[5];

    [Header("Win Animation Objects — Col 0..4  (each has 3 rows, contains ImageAnimation component)")]
    [Tooltip("GameObject references for win animations. Each should have an ImageAnimation component attached.")]
    [SerializeField] private ColumnOverlays[] winAnimationColumns = new ColumnOverlays[5];

    [Header("Sticky Wild Overlays — Col 0..4  (each has 3 rows)")]
    [SerializeField] private ColumnOverlays[] stickyWildColumns = new ColumnOverlays[5];



    private float middlePosition = 0f;
    private float cycleDistance;


    private List<Tween> spinTweens = new List<Tween>();
    private List<Tween> winTweens = new List<Tween>();
    private List<int> reelCycleCount = new List<int>();
    private Coroutine winAnimationCoroutine;


    private List<List<int>> currentDisplayMatrix;

    private bool isSpinning;
    private bool scatterAnticipationActive = false;

    // Stores the accumulated sticky wild positions keyed as "row_col" -> symbolId
    private Dictionary<string, int> currentStickyWilds;

    // How many seconds before the last reel stops to reveal newly-added sticky wilds
    [Header("BeatIt Sticky Wild Settings")]
    [SerializeField] private float newStickyWildPreRevealTime = 1.2f;

    #region Initialization

    private void Start()
    {
        BuildSymbolSpriteArray();
        InitializeReels();
        DisableAllOverlays();
    }

    private void DisableAllOverlays()
    {
        DisableColumns(winBoxColumns);
        DisableColumns(winAnimationColumns);
        DisableColumns(stickyWildColumns);

    }

    private static void DisableColumns(ColumnOverlays[] cols)
    {
        if (cols == null) return;
        foreach (var col in cols)
            if (col?.rows != null)
                foreach (var go in col.rows)
                    if (go) go.SetActive(false);
    }

    private static GameObject WinBox(ColumnOverlays[] cols, int col, int row)
        => (col >= 0 && col < cols?.Length && cols[col]?.rows != null && row >= 0 && row < cols[col].rows.Length)
            ? cols[col].rows[row] : null;

    private void BuildSymbolSpriteArray()
    {
        symbolSpritesMap = new Dictionary<int, Sprite>();
        
        symbolSpritesMap[0] = spriteWild;
        symbolSpritesMap[1] = spriteGlove;
        symbolSpritesMap[2] = spriteHat;
        symbolSpritesMap[3] = spriteSunglasses;
        symbolSpritesMap[4] = spriteShoes;
        symbolSpritesMap[5] = spriteAce;
        symbolSpritesMap[6] = spriteKing;
        symbolSpritesMap[7] = spriteQueen;
        symbolSpritesMap[8] = spriteJack;
        symbolSpritesMap[9] = spriteTen;
        symbolSpritesMap[10] = spriteNine;
        symbolSpritesMap[11] = spriteJackpot;
        symbolSpritesMap[12] = spriteBonus;
        
        // MoonwalkWild Variants
        symbolSpritesMap[13] = spriteMoonwalkWild;
        symbolSpritesMap[1013] = spriteMoonwalkWildBonus;
        symbolSpritesMap[2013] = spriteMoonwalkWildJackpot;
        
        // StackedWild Variants
        symbolSpritesMap[14] = spriteStackedWild;
        symbolSpritesMap[1014] = spriteStackedWildBonus;
        symbolSpritesMap[2014] = spriteStackedWildJackpot;

        // Build the animation sprite map
        animationSpriteMap = new Dictionary<int, List<Sprite>>();
        
        animationSpriteMap[0] = animSpritesWild;
        animationSpriteMap[11] = animSpritesJackpot;
        animationSpriteMap[12] = animSpritesBonus;
        
        // MoonwalkWild Variants
        animationSpriteMap[13] = animSpritesMoonwalkWild;
        animationSpriteMap[1013] = animSpritesMoonwalkWildBonus;
        animationSpriteMap[2013] = animSpritesMoonwalkWildJackpot;
        
        // StackedWild Variants
        animationSpriteMap[14] = animSpritesStackedWild;
        animationSpriteMap[1014] = animSpritesStackedWildBonus;
        animationSpriteMap[2014] = animSpritesStackedWildJackpot;
    }

    private void InitializeReels()
    {
        cycleDistance = symbolHeight;

        middlePosition = 0f;
        if (reelTransforms != null && reelTransforms.Length > 0 && reelTransforms[0] != null)
        {
            middlePosition = reelTransforms[0].localPosition.y;
        }

        currentDisplayMatrix = new List<List<int>>();
        for (int col = 0; col < 5; col++)
        {
            currentDisplayMatrix.Add(new List<int> { 0, 0, 0 });
            reelCycleCount.Add(0);
        }
    }

    internal void SetInitialMatrix(List<List<int>> matrix)
    {
        if (matrix == null || matrix.Count != 5) return;

        for (int col = 0; col < 5; col++)
        {
            if (matrix[col].Count != 3) return;
        }

        currentDisplayMatrix = matrix;

        for (int col = 0; col < 5; col++)
        {
            SetReelSymbols(col, matrix[col], true);
        }
    }

    #endregion

    #region Symbol Display

    private void SetReelSymbols(int columnIndex, List<int> visibleSymbolIds, bool isInitial = false)
    {
        if (columnIndex >= reelImagesList.Count)
        {
            Debug.LogError($"SetReelSymbols: Invalid column index {columnIndex}, max is {reelImagesList.Count - 1}");
            return;
        }

        if (visibleSymbolIds == null || visibleSymbolIds.Count != 3)
        {
            Debug.LogError($"SetReelSymbols: Invalid visibleSymbolIds count {visibleSymbolIds?.Count}, expected 3");
            return;
        }

        var reel = reelImagesList[columnIndex];

        if (reel.images == null || reel.images.Count != 18)
        {
            Debug.LogError($"SetReelSymbols: Reel {columnIndex} has invalid image count {reel.images?.Count}, expected 18");
            return;
        }

        for (int row = 0; row < 3; row++)
        {
            int imageIndex = 8 + row;
            int symbolId = visibleSymbolIds[row];
            reel.images[imageIndex].sprite = GetSymbolSprite(symbolId);
        }

        for (int i = 0; i < 8; i++)
        {
            reel.images[i].sprite = GetSymbolSprite(Random.Range(0, 15));
        }

        for (int i = 11; i < 18; i++)
        {
            reel.images[i].sprite = GetSymbolSprite(Random.Range(0, 15));
        }

        if (isInitial && reelTransforms[columnIndex] != null)
        {
            reelTransforms[columnIndex].localPosition = new Vector3(
                reelTransforms[columnIndex].localPosition.x,
                middlePosition,
                0
            );
        }
    }

    private Sprite GetSymbolSprite(int symbolId)
    {
        if (symbolSpritesMap.TryGetValue(symbolId, out Sprite sprite))
        {
            if (sprite == null)
            {
                Debug.LogError($"[SlotView] Symbol sprite for ID {symbolId} is null!");
                return symbolSpritesMap[0];
            }
            return sprite;
        }

        Debug.LogWarning($"[SlotView] Invalid symbolId {symbolId}, using default sprite 0.");
        return symbolSpritesMap[0];
    }

    private bool IsBonusSymbol(int symId)
    {
        int bonusId = gameManager?.gameConfig != null ? gameManager.gameConfig.bonusSymbolId : 12;
        return symId == bonusId || symId == 1013 || symId == 1014;
    }

    private bool IsWildSymbol(int symId)
    {
        int wildId = gameManager?.gameConfig != null ? gameManager.gameConfig.wildSymbolId : 0;
        return symId == wildId || symId == 13 || symId == 14 || symId == 1013 || symId == 1014 || symId == 2013 || symId == 2014;
    }

    private bool IsSpecialSymbol(int symId)
    {
        int jackpotId = gameManager?.gameConfig != null ? gameManager.gameConfig.jackpotSymbolId : 11;
        return IsBonusSymbol(symId) || IsWildSymbol(symId) || symId == jackpotId;
    }

    #endregion

    #region Spin Animation

    internal void StartSpin()
    {
        if (isSpinning) return;

        isSpinning = true;
        scatterAnticipationActive = false;
        KillAllTweens();

        DisableAllOverlays();

        // Re-enable OLD sticky wilds immediately so they remain visible while reels spin
        if (currentStickyWilds != null && currentStickyWilds.Count > 0)
        {
            ApplyStickyWilds(currentStickyWilds);
        }

        for (int i = 0; i < reelCycleCount.Count; i++)
        {
            reelCycleCount[i] = 0;
        }

        for (int col = 0; col < 5; col++)
        {
            StartReelCycle(col);
        }
    }

    private void StartReelCycle(int columnIndex)
    {
        if (columnIndex >= reelTransforms.Length) return;
        if (!isSpinning) return;

        Transform slotTransform = reelTransforms[columnIndex];

        slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);

        float currentSpeed = spinSpeed;
        if (scatterAnticipationActive && columnIndex == 4)
        {
            currentSpeed = spinSpeed / anticipationSpeedMultiplier;
        }

        Sequence cycleSequence = DOTween.Sequence();

        cycleSequence.Append(
            slotTransform.DOLocalMoveY(middlePosition - cycleDistance, currentSpeed)
                .SetEase(Ease.Linear)
        );

        cycleSequence.OnComplete(() => {
            if (isSpinning)
            {
                CycleReelSymbols(columnIndex);

                slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);

                if (columnIndex < reelCycleCount.Count)
                {
                    reelCycleCount[columnIndex]++;
                }

                StartReelCycle(columnIndex);
            }
        });

        cycleSequence.Play();

        if (spinTweens.Count <= columnIndex)
            spinTweens.Add(cycleSequence);
        else
            spinTweens[columnIndex] = cycleSequence;
    }

    private void CycleReelSymbols(int columnIndex)
    {
        var reel = reelImagesList[columnIndex];
        if (reel.images == null || reel.images.Count != 18) return;

        Sprite bottomSprite = reel.images[17].sprite;

        for (int i = 17; i > 0; i--)
        {
            reel.images[i].sprite = reel.images[i - 1].sprite;
        }

        reel.images[0].sprite = GetSymbolSprite(Random.Range(0, 15));
    }

    #endregion

    #region Stop Spin

    internal void StopSpin(List<List<int>> resultMatrix, System.Action onComplete)
    {
        if (!isSpinning)
        {
            currentDisplayMatrix = resultMatrix;
            for (int col = 0; col < 5; col++)
            {
                SetReelSymbols(col, resultMatrix[col], false);
            }
            
       
            
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(StopSpinSequence(resultMatrix, onComplete, false));
    }

    private IEnumerator StopSpinSequence(List<List<int>> resultMatrix, System.Action onComplete, bool isQuickStop)
    {
        currentDisplayMatrix = resultMatrix;

        var currentResult = gameManager.lastResult;

        // ---- Wait until minimum spin cycles are complete ----
        while (true)
        {
            bool allReelsReady = true;
            for (int col = 0; col < 5; col++)
            {
                if (reelCycleCount[col] < minSpinCyclesBeforeStop)
                {
                    allReelsReady = false;
                    break;
                }
            }
            if (allReelsReady) break;
            yield return null;
        }

        float stagger = isQuickStop ? quickStopStagger : reelStopStagger;

        // longestStopTime = time from the moment StopSingleReel coroutines start until
        // the last reel finishes its settle animation.
        float longestStopTime;
        if (isQuickStop)
            longestStopTime = (4 * stagger) + quickStopDuration;
        else
            longestStopTime = (4 * stagger) + stopOvershootDuration + stopBounceBackDuration;

        // ---- BeatIt sticky wild pre-reveal ----
        // For beatIt free games: newly added sticky wilds appear ~1.2s before the last
        // reel stops so the player sees them "snap in" before the reels settle.
        bool isBeatIt = currentResult?.freeGameData != null &&
                        currentResult.freeGameData.gameType == "beatIt";

        if (isBeatIt && !isQuickStop)
        {
            var newPositions = currentResult.freeGameData.stickyWildPositions;
            if (newPositions != null && newPositions.Count > 0)
            {
                // Build the set of newly-added positions (not present in the previous spin)
                var newlyAdded = GetNewStickyWildPositions(newPositions);

                if (newlyAdded != null && newlyAdded.Count > 0)
                {
                    // Delay = total stop time minus pre-reveal window, clamped to >= 0
                    float preRevealDelay = Mathf.Max(0f, longestStopTime - newStickyWildPreRevealTime);
                    StartCoroutine(RevealNewStickyWildsAfterDelay(preRevealDelay, newlyAdded));
                }
            }
        }

        // ---- SmoothCriminal wild pre-reveal ----
        // SmoothCriminal has no server-side stickyWildPositions. Instead, the server
        // fills entire reel columns with wild (ID 0) each spin. We detect those
        // all-wild columns from the result matrix and flash the stickyWild overlay
        // ~1.2s early so the player "sees" the wild land before the reel settles.
        // The overlay is cleared once the reel stops — the actual reel sprite takes over.
        bool isSmoothCriminal = currentResult?.freeGameData != null &&
                                currentResult.freeGameData.gameType == "smoothCriminal";

        if (isSmoothCriminal && !isQuickStop)
        {
            var wildCols = GetSmoothCriminalWildPositions(resultMatrix);
            if (wildCols != null && wildCols.Count > 0)
            {
                float preRevealDelay = Mathf.Max(0f, longestStopTime - newStickyWildPreRevealTime);
                StartCoroutine(RevealNewStickyWildsAfterDelay(preRevealDelay, wildCols));
            }
        }

        // ---- Start stopping each reel ----
        for (int col = 0; col < 5; col++)
        {
            float delay = col * stagger;
            StartCoroutine(StopSingleReel(col, resultMatrix[col], delay, isQuickStop));
        }

        yield return new WaitForSeconds(longestStopTime);

        // ---- Reels have landed — hide all sticky wild overlays ----
        // beatIt: re-applies them below with the authoritative set.
        // smoothCriminal: leaves them off — actual reel sprites are now visible.
        DisableColumns(stickyWildColumns);

        isSpinning = false;
        scatterAnticipationActive = false;

        // ---- beatIt: persist and re-apply the full updated sticky wild set ----
        if (isBeatIt && currentResult?.freeGameData?.stickyWildPositions != null)
        {
            UpdateStickyWildsFromResult(currentResult.freeGameData.stickyWildPositions);
            ApplyStickyWilds(currentStickyWilds);
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// Returns the subset of incoming sticky wild positions that are NEW
    /// (i.e., not already present in currentStickyWilds).
    /// Server positions are [row, col] pairs.
    /// </summary>
    private Dictionary<string, int> GetNewStickyWildPositions(List<List<int>> incomingPositions)
    {
        var result = new Dictionary<string, int>();
        if (incomingPositions == null) return result;

        foreach (var pos in incomingPositions)
        {
            if (pos == null || pos.Count < 2) continue;
            int row = pos[0];
            int col = pos[1];
            string key = $"{row}_{col}";

            // Only include if NOT already in the current (old) sticky wilds
            if (currentStickyWilds == null || !currentStickyWilds.ContainsKey(key))
            {
                result[key] = gameManager?.gameConfig?.wildSymbolId ?? 0;
            }
        }
        return result;
    }

    /// <summary>
    /// For smoothCriminal free spins, scans the result matrix for columns where
    /// ALL 3 rows are the wild symbol and returns their positions for overlay display.
    /// Matrix format: [col][row], 5 cols x 3 rows.
    /// </summary>
    private Dictionary<string, int> GetSmoothCriminalWildPositions(List<List<int>> matrix)
    {
        var result = new Dictionary<string, int>();
        if (matrix == null) return result;

        int wildId = gameManager?.gameConfig?.wildSymbolId ?? 0;

        for (int col = 0; col < matrix.Count; col++)
        {
            var column = matrix[col];
            if (column == null || column.Count < 3) continue;

            // Only apply overlay when the entire column is wild
            bool allWild = true;
            for (int row = 0; row < column.Count; row++)
            {
                if (column[row] != wildId) { allWild = false; break; }
            }

            if (allWild)
            {
                for (int row = 0; row < column.Count; row++)
                {
                    result[$"{row}_{col}"] = wildId;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Waits for a delay then reveals newly-added sticky wild overlays
    /// while the reels are still spinning (pre-stop reveal).
    /// </summary>
    private IEnumerator RevealNewStickyWildsAfterDelay(float delay, Dictionary<string, int> newPositions)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // Only reveal if we're still in the stopping phase (guard against quick-stop race)
        ApplyStickyWilds(newPositions);
    }

    /// <summary>
    /// Builds/updates currentStickyWilds from the authoritative server position list.
    /// Server sends positions as [[row, col], [row, col], ...].
    /// </summary>
    private void UpdateStickyWildsFromResult(List<List<int>> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            currentStickyWilds = null;
            return;
        }

        currentStickyWilds = new Dictionary<string, int>();
        int wildId = gameManager?.gameConfig?.wildSymbolId ?? 0;

        foreach (var pos in positions)
        {
            if (pos == null || pos.Count < 2) continue;
            int row = pos[0];
            int col = pos[1];
            currentStickyWilds[$"{row}_{col}"] = wildId;
        }
    }

    private IEnumerator StopSingleReel(int columnIndex, List<int> targetSymbols, float delay, bool isQuickStop)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        if (columnIndex < spinTweens.Count && spinTweens[columnIndex] != null)
        {
            spinTweens[columnIndex].Kill();
        }

        Transform slotTransform = reelTransforms[columnIndex];

        SetReelSymbols(columnIndex, targetSymbols, false);

        float currentY = slotTransform.localPosition.y;
        float targetY = middlePosition;
        float offset = (currentY - targetY) % cycleDistance;
        if (offset < 0) offset += cycleDistance;

        slotTransform.localPosition = new Vector3(
            slotTransform.localPosition.x,
            targetY + offset,
            0
        );

        AudioManager.Instance?.PlayReelStop();

        if (currentDisplayMatrix != null && columnIndex < currentDisplayMatrix.Count)
        {
            bool hasBonus = false;
            bool hasWild = false;
            foreach (int sym in currentDisplayMatrix[columnIndex])
            {
                if (IsBonusSymbol(sym)) hasBonus = true;
                if (IsWildSymbol(sym)) hasWild = true;
            }
            if (hasBonus) AudioManager.Instance?.PlayBonusHit();
            else if (hasWild) AudioManager.Instance?.PlayWildHit();
        }

        if (isQuickStop)
        {
            Sequence quickStopSequence = DOTween.Sequence();

            quickStopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - quickStopOvershoot, quickStopDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );

            quickStopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition, quickStopDuration * 0.7f)
                    .SetEase(Ease.OutQuad)
            );

            quickStopSequence.OnComplete(() => PlayStopAnimationsForColumn(columnIndex));

            spinTweens[columnIndex] = quickStopSequence;
        }
        else
        {
            Sequence stopSequence = DOTween.Sequence();

            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - stopOvershootDistance, stopOvershootDuration)
                    .SetEase(Ease.OutQuad)
            );

            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition, stopBounceBackDuration)
                    .SetEase(Ease.OutQuad)
            );

            stopSequence.OnComplete(() => PlayStopAnimationsForColumn(columnIndex));

            spinTweens[columnIndex] = stopSequence;
        }
    }

    #endregion

    #region Quick Spin

    internal void QuickStop(List<List<int>> resultMatrix)
    {
        if (!isSpinning)
        {
            currentDisplayMatrix = resultMatrix;
            for (int col = 0; col < 5; col++)
            {
                if (col < reelTransforms.Length)
                {
                    SetReelSymbols(col, resultMatrix[col], false);
                    reelTransforms[col].localPosition = new Vector3(
                        reelTransforms[col].localPosition.x,
                        middlePosition,
                        0
                    );
                    PlayStopAnimationsForColumn(col);
                }
            }
            
            
            return;
        }

        StartCoroutine(StopSpinSequence(resultMatrix, null, true));
    }

    #endregion




    #region Stop Symbol Animations

    private void PlayStopAnimationsForColumn(int col)
    {
        if (currentDisplayMatrix == null || col >= currentDisplayMatrix.Count) return;

        for (int row = 0; row < currentDisplayMatrix[col].Count; row++)
        {
            int symId = currentDisplayMatrix[col][row];
            if (IsSpecialSymbol(symId))
            {
                AnimateSymbolSingleLoop(col, row, 1);
            }
        }
    }

    private void AnimateSymbolSingleLoop(int column, int row, int loopCount = 1)
    {
        if (!TryGetAnimationComponents(column, row, out Image symbolImage, out GameObject animGO, out ImageAnimation imageAnim, out List<Sprite> animSprites))
            return;

        BuildSymbolAnimationSequence(symbolImage, animGO, imageAnim, animSprites, loopCount, 0f);
    }

    private bool TryGetAnimationComponents(int column, int row, out Image symbolImage, out GameObject animGO, out ImageAnimation imageAnim, out List<Sprite> animSprites)
    {
        symbolImage = null;
        animGO = null;
        imageAnim = null;
        animSprites = null;

        if (column >= reelImagesList.Count) return false;
        var reel = reelImagesList[column];
        if (reel.images == null || reel.images.Count < 10) return false;

        int imageIndex = 8 + row;
        if (imageIndex >= reel.images.Count) return false;

        symbolImage = reel.images[imageIndex];
        if (symbolImage == null) return false;

        animGO = WinBox(winAnimationColumns, column, row);
        if (animGO == null) return false;

        imageAnim = animGO.GetComponent<ImageAnimation>();
        if (imageAnim == null) return false;

        int symbolId = currentDisplayMatrix[column][row];
        if (!animationSpriteMap.TryGetValue(symbolId, out animSprites) || animSprites == null || animSprites.Count == 0)
            return false;

        return true;
    }

    private void BuildSymbolAnimationSequence(Image symbolImage, GameObject animGO, ImageAnimation imageAnim, List<Sprite> animSprites, int loopCount, float preDelay)
    {
        imageAnim.textureArray = animSprites;
        imageAnim.useDynamicFramerate = true;
        imageAnim.dynamicLoopDuration = winSymbolLoopDuration;

        Color originalColor = new Color(symbolImage.color.r, symbolImage.color.g, symbolImage.color.b, 1f);

        Sequence seq = DOTween.Sequence();

        seq.AppendCallback(() => {
            animGO.SetActive(true);
            Image animRenderer = imageAnim.rendererDelegate;
            if (animRenderer != null)
            {
                animRenderer.DOKill();
                Color c = animRenderer.color;
                animRenderer.color = new Color(c.r, c.g, c.b, 0f);
                animRenderer.DOFade(1f, 0.2f);
            }
            symbolImage.DOKill();
            symbolImage.DOFade(0f, 0.2f);
        });

        if (preDelay > 0)
        {
            seq.AppendInterval(preDelay);
            seq.AppendCallback(() => imageAnim.StartAnimation());
        }
        else
        {
            seq.AppendCallback(() => imageAnim.StartAnimation());
        }

        seq.AppendInterval(winSymbolLoopDuration * loopCount);

        seq.AppendCallback(() => {
            Image animRenderer = imageAnim != null ? imageAnim.rendererDelegate : null;
            if (animRenderer != null)
            {
                animRenderer.DOKill();
                animRenderer.DOFade(0f, 0.2f).OnComplete(() => {
                    if (imageAnim != null) imageAnim.StopAnimation();
                    if (animGO != null) animGO.SetActive(false);
                });
            }
            else
            {
                if (imageAnim != null) imageAnim.StopAnimation();
                if (animGO != null) animGO.SetActive(false);
            }

            if (symbolImage != null)
            {
                symbolImage.DOKill();
                symbolImage.DOFade(originalColor.a, 0.2f);
            }
        });

        winTweens.Add(seq);
    }

    #endregion

    #region Win Line Animation

    internal void ShowWinLineAnimation(List<WinLine> winLines, System.Action onComplete)
    {

        if (winLines == null || winLines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        for (int i = 0; i < winLines.Count; i++)
        {
            var line = winLines[i];
    
        }

        KillWinTweens();
        winAnimationCoroutine = StartCoroutine(PlayWinLinesSequentially(winLines, onComplete));
    }


    private IEnumerator PlayWinLinesSequentially(List<WinLine> winLines, System.Action onComplete)
    {
        int loopCount = 1; // Play only once per user request
        float lineDuration = winSymbolLoopDuration * loopCount;

        List<int> prevPositions = null;

        Debug.Log($"[PlayWinLinesSequentially] Starting win animation for {winLines.Count} lines");

        foreach (var winLine in winLines)
        {
            if (winLine.positions == null || winLine.positions.Count == 0) continue;

           
            if (prevPositions != null)
            {
                KillWinTweens(false);
                foreach (int flatIdx in prevPositions)
                {
                    int r = flatIdx / 5;
                    int c = flatIdx % 5;
                    DisableWinBox(c, r);
                    ResetSymbolScale(c, r);
                }
            }

            AudioManager.Instance?.PlayWinLine();

            foreach (int flatIndex in winLine.positions)
            {
                int row = flatIndex / 5;
                int col = flatIndex % 5;


                if (col < 0 || col >= 5 || row < 0 || row >= 4)
                {
                    Debug.LogWarning($"[PlayWinLinesSequentially] Invalid position! col: {col}, row: {row}");
                    continue;
                }

                EnableWinBox(col, row);

                AnimateWinSymbol(col, row);
            }

            prevPositions = new List<int>(winLine.positions);

            yield return new WaitForSeconds(lineDuration);
        }

        AudioManager.Instance?.StopWinLine();
        KillWinTweens(false);

        onComplete?.Invoke();
    }
    private void EnableWinBox(int col, int row)
    {
        var go = WinBox(winBoxColumns, col, row);
        if (go != null)
        {
            go.SetActive(true);
        }
 
    }

    private void DisableWinBox(int col, int row)
    {
        var go = WinBox(winBoxColumns, col, row);
        if (go != null) go.SetActive(false);
    }

    private void ResetSymbolScale(int col, int row)
    {
        if (col >= reelImagesList.Count) return;
        var reel = reelImagesList[col];
        if (reel.images == null) return;
        int imageIndex = 8 + row;
        if (imageIndex >= reel.images.Count) return;
        if (reel.images[imageIndex] != null)
        {
            reel.images[imageIndex].DOKill();
            reel.images[imageIndex].transform.localScale = Vector3.one;
            // Restore alpha to full opacity
            Color c = reel.images[imageIndex].color;
            reel.images[imageIndex].color = new Color(c.r, c.g, c.b, 1f);
        }

        // Also ensure the corresponding animation object is disabled
        var animGO = WinBox(winAnimationColumns, col, row);
        if (animGO != null)
        {
            ImageAnimation imageAnim = animGO.GetComponent<ImageAnimation>();
            if (imageAnim != null)
            {
                if (imageAnim.rendererDelegate != null) imageAnim.rendererDelegate.DOKill();
                imageAnim.StopAnimation();
            }
            animGO.SetActive(false);
        }
    }


    private void AnimateWinSymbol(int column, int row)
    {
        if (!TryGetAnimationComponents(column, row, out Image symbolImage, out GameObject animGO, out ImageAnimation imageAnim, out List<Sprite> animSprites))
            return;

        int symbolId = currentDisplayMatrix[column][row];
        if (!IsSpecialSymbol(symbolId))
        {
            symbolImage.DOKill();
            symbolImage.transform.localScale = Vector3.one;
            return;
        }

        BuildSymbolAnimationSequence(symbolImage, animGO, imageAnim, animSprites, 1, winLineBoxToAnimationDelay);
    }

    private void KillWinTweens(bool stopCoroutine = true)
    {
        foreach (var tween in winTweens)
        {
            tween?.Kill();
        }
        winTweens.Clear();

        if (stopCoroutine && winAnimationCoroutine != null)
        {
            StopCoroutine(winAnimationCoroutine);
            winAnimationCoroutine = null;
        }
        AudioManager.Instance?.StopWinLine();

        // Stop all win animations and disable animation GameObjects
        if (winAnimationColumns != null)
        {
            foreach (var col in winAnimationColumns)
            {
                if (col?.rows != null)
                {
                    foreach (var animGO in col.rows)
                    {
                        if (animGO != null && animGO.activeSelf)
                        {
                            ImageAnimation imageAnim = animGO.GetComponent<ImageAnimation>();
                            if (imageAnim != null)
                            {
                                if (imageAnim.rendererDelegate != null) imageAnim.rendererDelegate.DOKill();
                                imageAnim.StopAnimation();
                            }
                            animGO.SetActive(false);
                        }
                    }
                }
            }
        }

        DisableColumns(winBoxColumns);

        // Restore all symbol image alphas to full opacity
        foreach (var reel in reelImagesList)
        {
            if (reel.images != null)
            {
                foreach (var image in reel.images)
                {
                    if (image != null)
                    {
                        image.DOKill();
                        image.transform.localScale = Vector3.one;
                        Color c = image.color;
                        image.color = new Color(c.r, c.g, c.b, 1f);
                    }
                }
            }
        }
    }

    #endregion

  
    internal List<List<int>> GetCurrentDisplayMatrix()
    {
        return currentDisplayMatrix;
    }

    internal bool IsSpinning()
    {
        return isSpinning;
    }

    private void KillAllTweens()
    {
        foreach (var tween in spinTweens)
        {
            tween?.Kill();
        }
        spinTweens.Clear();

        KillWinTweens();
    }

    private void ApplyStickyWilds(Dictionary<string, int> stickyWilds)
    {
        if (stickyWilds == null) return;

        foreach (var kvp in stickyWilds)
        {
            string[] parts = kvp.Key.Split('_');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int row) &&
                int.TryParse(parts[1], out int col))
            {
                var go = WinBox(stickyWildColumns, col, row);
                if (go)
                {
                    go.SetActive(true);

                    // Set the overlay image to the correct wild multiplier sprite
                    Image img = go.GetComponent<Image>();
                    int wildId = gameManager?.gameConfig != null ? gameManager.gameConfig.wildSymbolId : 0;
                    if (img != null && symbolSpritesMap.TryGetValue(wildId, out Sprite wildSprite))
                    {
                        img.sprite = wildSprite;
                    }
                }
            }
        }
    }

    internal void ClearStickyWilds()
    {
        currentStickyWilds = null;
        DisableColumns(stickyWildColumns);
    }



    #region Cleanup

    private void OnDestroy()
    {
        KillAllTweens();
    }

    #endregion
}

[System.Serializable]
public class ReelImages
{
    public List<Image> images = new List<Image>(18);
}


[System.Serializable]
public class ColumnOverlays
{
    [Tooltip("Row 0 = top, Row 1, Row 2 = bottom")]
    public GameObject[] rows = new GameObject[3];
}
