using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System;

partial class MichealMoves : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoManager videoManager;
    [SerializeField] private AudioController audioController;

    [Header("UIElement")]
    [SerializeField] private GameObject AnimationObj;
    [SerializeField] private GameObject MainAnimationObj;
    [SerializeField] private GameObject SmoothCriminalAnimationObj;
    [SerializeField] private GameObject MainSmoothCriminalAnimationObj;
    [SerializeField] private ColumnOverlays[] slotObjs = new ColumnOverlays[5];
    [SerializeField] private List<MovesPositions> beatitmovesPosition;
    [SerializeField] private Vector2[] smoothcriminalmovesPosition;
    [SerializeField] private GameObject[] rowObjsSmoothCriminal = new GameObject[5];

    [Header("Moves Animation Beat It")]
    [SerializeField] private List<Sprite> MichealLeftLegMove;     // 0
    [SerializeField] private List<Sprite> MichealRightLegMove;    // 1
    [SerializeField] private List<Sprite> MichealLeftArmMove;     // 2
    [SerializeField] private List<Sprite> MichealRightArmMove;    // 3
    [SerializeField] private List<Sprite> MichealLeftFingerMove;  // 4
    [SerializeField] private List<Sprite> MichealRightFingerMove; // 5

    [Header("Second Part Beat It animation")]
    [SerializeField] private List<Sprite> MichealSecondLeftLegMove;     // 0
    [SerializeField] private List<Sprite> MichealSecondRightLegMove;    // 1
    [SerializeField] private List<Sprite> MichealSecondLeftArmMove;     // 2
    [SerializeField] private List<Sprite> MichealSecondRightArmMove;    // 3
    [SerializeField] private List<Sprite> MichealSecondLeftFingerMove;  // 4
    [SerializeField] private List<Sprite> MichealSecondRightFingerMove; // 5

    [Header("Moves Animation Smooth Criminal")]
    [SerializeField] private List<Sprite> FirstSmoothCriminalMove; // 0
    [SerializeField] private List<Sprite> SecondSmoothCriminalMove; // 1
    [SerializeField] private List<Sprite> ThirdSmoothCriminalMove; // 2
    [SerializeField] private List<Sprite> FourthSmoothCriminalMove; // 3
    [SerializeField] private List<Sprite> FifthSmoothCriminalMove; // 4

    private RectTransform animationRect;
    private ImageAnimation animationObjAnim;
    private RectTransform smoothCriminalanimationRect;
    private ImageAnimation smoothCriminalAnimationObj;
    private bool sequenceRunning;

    private readonly List<GameObject> revealedSlots = new List<GameObject>();
    private int pendingSlotReveals = 0;

    private readonly List<GameObject> revealedRows = new List<GameObject>();
    private int pendingRowReveals = 0;
    private bool smoothCriminalSequenceRunning;

    internal bool allMovesDone = false;

    private void Awake()
    {
        if (AnimationObj != null)
        {
            animationRect = AnimationObj.GetComponent<RectTransform>();
            animationObjAnim = AnimationObj.GetComponent<ImageAnimation>();
        }
        if (SmoothCriminalAnimationObj != null)
        {
            smoothCriminalanimationRect = SmoothCriminalAnimationObj.GetComponent<RectTransform>();
            smoothCriminalAnimationObj = SmoothCriminalAnimationObj.GetComponent<ImageAnimation>();
        }
    }

    /// <summary>
    /// Entry point called from SlotView. "positions" uses the same "row_col" key format
    /// produced by SlotView.GetNewStickyWildPositions (row: 0=top..2=bottom, col: 0..4).
    /// For each position, in order: moves AnimationObj to that slot's assigned position,
    /// plays the matching Micheal move animation, waits for it to finish, then starts the
    /// slot's own ImageAnimation (its reveal animation, already set up on that slot object).
    /// </summary>
    internal void PlayRevealSequence(Dictionary<string, int> positions, Action onComplete)
    {
        if (positions == null || positions.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(RevealSequenceRoutine(positions, onComplete));
    }

    private IEnumerator RevealSequenceRoutine(Dictionary<string, int> positions, Action onComplete)
    {
        // If a previous reveal sequence is still running (e.g. player stopped the reels
        // very quickly again), let it finish first rather than fighting over AnimationObj.
        if (sequenceRunning)
        {
            yield return new WaitUntil(() => !sequenceRunning);
        }

        videoManager.PlayVideo(1); // Play Beat It video
        videoManager.isVideoPlaying = true;
        yield return new WaitUntil(() => !videoManager.isVideoPlaying);

        sequenceRunning = true;
        revealedSlots.Clear();
        pendingSlotReveals = 0;

        foreach (var kvp in positions)
        {
            string[] parts = kvp.Key.Split('_');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out int row) ||
                !int.TryParse(parts[1], out int col))
            {
                continue;
            }

            yield return PlayMoveForSlot(col, row);
        }

        // The last few slot reveals are fired off (not awaited) so the next Micheal move
        // can start while they play — wait for all of them to actually finish animating
        // before switching every revealed slot off at the same time.
        yield return new WaitUntil(() => pendingSlotReveals <= 0);

        foreach (var slotGO in revealedSlots)
        {
            if (slotGO != null) slotGO.SetActive(false);
        }
        revealedSlots.Clear();

        sequenceRunning = false;
        allMovesDone = true;
        onComplete?.Invoke();
    }

    private IEnumerator PlayMoveForSlot(int col, int row)
    {
        MovesPosition moveData = GetMovesPosition(col, row);

        if (moveData == null)
        {
            Debug.LogWarning($"[MichealMoves] No MovesPosition entry for col={col}, row={row} — revealing slot directly.");
            yield return StartCoroutine(StartSlotReveal(col, row));
            yield break;
        }

        List<Sprite> moveSprites = GetMoveSprites(moveData.animationType);

        if (AnimationObj == null || animationObjAnim == null || moveSprites == null || moveSprites.Count == 0)
        {
            Debug.LogWarning($"[MichealMoves] Missing AnimationObj/ImageAnimation/sprites for animationType={moveData.animationType} — revealing slot directly.");
            yield return StartCoroutine(StartSlotReveal(col, row));
            yield break;
        }

        // 1. Move the shared Micheal animation object to this slot's assigned position
        if (animationRect != null)
        {
            animationRect.anchoredPosition = moveData.position;
        }

        float animationSpeed = GetAnimationSpeed(moveData.animationType);

        // 2. Assign the correct move (leg/arm/finger) frames and play them once
        animationObjAnim.StopAnimation();
        animationObjAnim.textureArray = moveSprites;
        animationObjAnim.AnimationSpeed = animationSpeed;
        MainAnimationObj.SetActive(true);
        AnimationObj.SetActive(true);
        animationObjAnim.StartAnimation();

        yield return new WaitUntil(() => animationObjAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);
        
        // 3. Micheal's move finished — reveal the slot's own sticky wild animation
        StartCoroutine(StartSlotReveal(col, row));

        animationObjAnim.textureArray = GetSecondMoveSprites(moveData.animationType);
        animationObjAnim.AnimationSpeed = GetSecondAnimationSpeed(moveData.animationType);
        animationObjAnim.StartAnimation();

        yield return new WaitUntil(()=> animationObjAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);

        AnimationObj.SetActive(false);
        MainAnimationObj.SetActive(false);

    }

    /// <summary>
    /// Starts the slot object's own ImageAnimation. The texture array for that
    /// animation is already configured on each slot object in the inspector.
    /// Slots are left active/on-screen here — they all get switched off together
    /// once every slot in this reveal sequence has finished (see RevealSequenceRoutine).
    /// </summary>
    private IEnumerator StartSlotReveal(int col, int row)
    {
        //audioController.PlayDanceMoves();
        GameObject slotGO = GetSlotObject(col, row);
        if (slotGO == null) yield break;

        pendingSlotReveals++;
        revealedSlots.Add(slotGO);

        slotGO.SetActive(true);

        ImageAnimation slotAnim = slotGO.GetComponent<ImageAnimation>();
        if (slotAnim == null)
        {
            pendingSlotReveals--;
            yield break;
        }

        slotAnim.StartAnimation();
        yield return new WaitUntil(() => slotAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);

        pendingSlotReveals--;
    }

    /// <summary>
    /// Entry point for Smooth Criminal free spins. Unlike BeatIt, the Micheal move here
    /// depends on which spin number we're on (5 spins total) rather than on individual
    /// slot positions: spinNumber 0 plays FirstSmoothCriminalMove at
    /// smoothcriminalmovesPosition[0], spinNumber 1 plays SecondSmoothCriminalMove at
    /// smoothcriminalmovesPosition[1], and so on up to spinNumber 4.
    ///
    /// "newlyWildRows" keys are just the row index as a string (e.g. "2"), for whichever
    /// row(s) turned fully wild on this spin — no column is needed since the entire row
    /// is wild. After the move animation finishes, every one of those rows starts playing
    /// its own ImageAnimation (already configured on rowObjsSmoothCriminal in the
    /// inspector) in parallel — not one-at-a-time like BeatIt, since there's only a single
    /// shared move per spin here, not one per row. Once every row's animation has
    /// finished, all of them switch off at the same instant, revealing the sticky wilds
    /// together.
    /// </summary>
    internal void PlaySmoothCriminalRevealSequence(int spinNumber, Dictionary<string, int> newlyWildRows, Action onComplete)
    {
        if (newlyWildRows == null || newlyWildRows.Count == 0)
        {
            allMovesDone = true;
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(RevealSmoothCriminalSequenceRoutine(spinNumber, newlyWildRows, onComplete));
    }

    private IEnumerator RevealSmoothCriminalSequenceRoutine(int spinNumber, Dictionary<string, int> newlyWildRows, Action onComplete)
    {
        // If a previous Smooth Criminal reveal is still running, let it finish first
        // rather than fighting over the shared AnimationObj.
        if (smoothCriminalSequenceRunning)
        {
            yield return new WaitUntil(() => !smoothCriminalSequenceRunning);
        }

        // videoManager.PlayVideo(2); // Play Smooth Criminal video
        // videoManager.isVideoPlaying = true;
        // yield return new WaitUntil(() => !videoManager.isVideoPlaying);

        smoothCriminalSequenceRunning = true;
        revealedRows.Clear();
        pendingRowReveals = 0;

        // 1. Play the single Micheal move assigned to this spin number
        yield return PlaySmoothCriminalMove(spinNumber);

        // 2. All winning rows for this spin reveal in parallel
        foreach (var kvp in newlyWildRows)
        {
            if (!int.TryParse(kvp.Key, out int row)) continue;
            StartCoroutine(StartRowReveal(row));
        }

        // Wait for every row's reveal animation to actually finish before switching off
        yield return new WaitUntil(() => pendingRowReveals <= 0);

        // 3. Switch every revealed row off at the same time — this is the sticky wild reveal
        foreach (var rowGO in revealedRows)
        {
            if (rowGO != null) rowGO.SetActive(false);
        }
        revealedRows.Clear();

        smoothCriminalSequenceRunning = false;
        allMovesDone = true;
        onComplete?.Invoke();
    }

    private IEnumerator PlaySmoothCriminalMove(int spinNumber)
    {
        List<Sprite> moveSprites = GetSmoothCriminalMoveSprites(spinNumber);
        Vector2? position = GetSmoothCriminalPosition(spinNumber);
        float animationSpeed = GetSmoothCriminalAnimationSpeed(spinNumber);

        if (smoothCriminalAnimationObj == null || smoothCriminalAnimationObj == null || moveSprites == null || moveSprites.Count == 0 || position == null)
        {
            Debug.LogWarning($"[MichealMoves] Missing AnimationObj/ImageAnimation/sprites/position for Smooth Criminal spin {spinNumber} — skipping move animation.");
            yield break;
        }

        if (smoothCriminalanimationRect != null)
        {
            smoothCriminalanimationRect.anchoredPosition = position.Value;
        }

        smoothCriminalAnimationObj.StopAnimation();
        smoothCriminalAnimationObj.textureArray = moveSprites;
        smoothCriminalAnimationObj.AnimationSpeed = animationSpeed;
        MainSmoothCriminalAnimationObj.SetActive(true);
        SmoothCriminalAnimationObj.SetActive(true);
        smoothCriminalAnimationObj.StartAnimation();

        yield return new WaitUntil(() =>smoothCriminalAnimationObj.currentAnimationState == ImageAnimation.ImageState.FINISHED);

        SmoothCriminalAnimationObj.SetActive(false);
        MainSmoothCriminalAnimationObj.SetActive(false);
    }

    /// <summary>
    /// Starts a full row's own ImageAnimation (texture array already configured in the
    /// inspector on rowObjsSmoothCriminal). Rows are left active on-screen — they all get
    /// switched off together once every row in this reveal has finished (see
    /// RevealSmoothCriminalSequenceRoutine).
    /// </summary>
    private IEnumerator StartRowReveal(int row)
    {
        GameObject rowGO = GetSmoothCriminalRowObject(row);
        if (rowGO == null) yield break;

        pendingRowReveals++;
        revealedRows.Add(rowGO);

        rowGO.SetActive(true);

        ImageAnimation rowAnim = rowGO.GetComponent<ImageAnimation>();
        if (rowAnim == null)
        {
            pendingRowReveals--;
            yield break;
        }

        rowAnim.StartAnimation();
        yield return new WaitUntil(() => rowAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);

        pendingRowReveals--;
    }

    private GameObject GetSmoothCriminalRowObject(int row)
    {
        if (rowObjsSmoothCriminal == null || row < 0 || row >= rowObjsSmoothCriminal.Length) return null;
        return rowObjsSmoothCriminal[row];
    }

    private List<Sprite> GetSmoothCriminalMoveSprites(int spinNumber)
    {
        switch (spinNumber)
        {
            case 0: return FirstSmoothCriminalMove;
            case 1: return SecondSmoothCriminalMove;
            case 2: return ThirdSmoothCriminalMove;
            case 3: return FourthSmoothCriminalMove;
            case 4: return FifthSmoothCriminalMove;
            default: return null;
        }
    }

    private Vector2? GetSmoothCriminalPosition(int spinNumber)
    {
        if (smoothcriminalmovesPosition == null || spinNumber < 0 || spinNumber >= smoothcriminalmovesPosition.Length) return null;
        return smoothcriminalmovesPosition[spinNumber];
    }

    private GameObject GetSlotObject(int col, int row)
    {
        if (slotObjs == null || col < 0 || col >= slotObjs.Length) return null;

        var colOverlay = slotObjs[col];
        if (colOverlay?.rows == null || row < 0 || row >= colOverlay.rows.Length) return null;

        return colOverlay.rows[row];
    }

    private List<Sprite> GetMoveSprites(int animationType)
    {
        switch (animationType)
        {
            case 0: return MichealLeftLegMove;
            case 1: return MichealRightLegMove;
            case 2: return MichealLeftArmMove;
            case 3: return MichealRightArmMove;
            case 4: return MichealLeftFingerMove;
            case 5: return MichealRightFingerMove;
            default: return null;
        }
    }

    private List<Sprite> GetSecondMoveSprites(int animationType)
    {
        switch (animationType)
        {
            case 0: return MichealSecondLeftLegMove;
            case 1: return MichealSecondRightLegMove;
            case 2: return MichealSecondLeftArmMove;
            case 3: return MichealSecondRightArmMove;
            case 4: return MichealSecondLeftFingerMove;
            case 5: return MichealSecondRightFingerMove;
            default: return null;
        }
    }

    private float GetAnimationSpeed(int animationType)
    {
        switch (animationType)
        {
            case 0: return 36;
            case 1: return 36;
            case 2: return 19;
            case 3: return 19;
            case 4: return 12;
            case 5: return 12;
            default: return 45;
        }
    }

    private float GetSecondAnimationSpeed(int animationType)
    {
        switch (animationType)
        {
            case 0: return 15;
            case 1: return 15;
            case 2: return 10;
            case 3: return 10;
            case 4: return 10;
            case 5: return 10;
            default: return 4;
        }
    }

    private float GetSmoothCriminalAnimationSpeed(int spinNumber)
    {
        switch (spinNumber)
        {
            case 0: return 50;
            case 1: return 70;
            case 2: return 40;
            case 3: return 40;
            case 4: return 70;
            default: return 60;
        }
    }

    /// <summary>
    /// Looks up the MovesPosition for a given (col, row) slot.
    /// movesPosition is column-major, same shape as ColumnOverlays / slotObjs:
    /// movesPosition[col] holds one MovesPositions per column, and its .images list
    /// is indexed by row (0=top..2=bottom). Returns null if nothing is configured
    /// for that slot instead of throwing.
    /// </summary>
    private MovesPosition GetMovesPosition(int col, int row)
    {
        if (beatitmovesPosition == null || col < 0 || col >= beatitmovesPosition.Count) return null;

        var column = beatitmovesPosition[col];
        if (column?.images == null || row < 0 || row >= column.images.Count) return null;

        return column.images[row];
    }
}
[Serializable]
public class MovesPositions
{
    public List<MovesPosition> images = new List<MovesPosition>();
}

[Serializable]
public class MovesPosition
{
    public Vector2 position = new Vector2(0, 0);
    public int animationType = 0; // 0 = LeftLeg, 1 = RightLeg, 2 = LeftArm, 3 = RightArm, 4 = LeftFinger, 5 = RightFinger
}