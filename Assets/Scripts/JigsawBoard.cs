using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class JigsawBoard : MonoBehaviour
{
    [SerializeField] private JigsawSlot[] slots;
    [SerializeField] private int width;
    [SerializeField] private int height;

    private readonly Dictionary<int, JigsawRuntimeData> _onBoardJigsawData = new();
    
    private void Awake()
    {
        Assert.IsTrue(width * height == slots.Length);
    }

    public void Init(int levelId)
    {
        foreach (var slot in slots)
        {
            slot.Init(levelId);
        }
    }

    public bool CanPut(JigsawRuntimeData jigsawData, JigsawSlot targetSlot)
    {
        if (!slots.Contains(targetSlot)) 
            return false;

        int index = System.Array.IndexOf(slots, targetSlot);

        // ❗ 已经被占
        if (_onBoardJigsawData.ContainsKey(index))
            return false;

        int x = index % width;
        int y = index / width;
        
        // ❗ 边界限制：Out 不能朝外
        if (y == 0 && jigsawData.UpEdgeType == JigsawEdgeType.Out)
            return false;

        if (y == height - 1 && jigsawData.DownEdgeType == JigsawEdgeType.Out)
            return false;

        if (x == 0 && jigsawData.LeftEdgeType == JigsawEdgeType.Out)
            return false;

        if (x == width - 1 && jigsawData.RightEdgeType == JigsawEdgeType.Out)
            return false;

        // 👉 上
        if (y > 0)
        {
            int upIndex = index - width;
            if (_onBoardJigsawData.TryGetValue(upIndex, out var upPiece))
            {
                if (!CanFit(jigsawData.UpEdgeType, upPiece.DownEdgeType))
                    return false;
            }
        }

        // 👉 下
        if (y < height - 1)
        {
            int downIndex = index + width;
            if (_onBoardJigsawData.TryGetValue(downIndex, out var downPiece))
            {
                if (!CanFit(jigsawData.DownEdgeType, downPiece.UpEdgeType))
                    return false;
            }
        }

        // 👉 左
        if (x > 0)
        {
            int leftIndex = index - 1;
            if (_onBoardJigsawData.TryGetValue(leftIndex, out var leftPiece))
            {
                if (!CanFit(jigsawData.LeftEdgeType, leftPiece.RightEdgeType))
                    return false;
            }
        }

        // 👉 右
        if (x < width - 1)
        {
            int rightIndex = index + 1;
            if (_onBoardJigsawData.TryGetValue(rightIndex, out var rightPiece))
            {
                if (!CanFit(jigsawData.RightEdgeType, rightPiece.LeftEdgeType))
                    return false;
            }
        }

        return true;
    }
    
    public bool IsFilled()
    {
        // 👉 1. 是否填满
        if (_onBoardJigsawData.Count != slots.Length)
            return false;

        for (int index = 0; index < slots.Length; index++)
        {
            var piece = _onBoardJigsawData[index];

            int x = index % width;
            int y = index / width;

            // 👉 上
            if (y > 0)
            {
                var up = _onBoardJigsawData[index - width];
                if (!Match(piece.UpEdgeType, up.DownEdgeType))
                    return false;
            }
            else
            {
                if (piece.UpEdgeType != JigsawEdgeType.Flat)
                    return false;
            }

            // 👉 下
            if (y < height - 1)
            {
                var down = _onBoardJigsawData[index + width];
                if (!Match(piece.DownEdgeType, down.UpEdgeType))
                    return false;
            }
            else
            {
                if (piece.DownEdgeType != JigsawEdgeType.Flat)
                    return false;
            }

            // 👉 左
            if (x > 0)
            {
                var left = _onBoardJigsawData[index - 1];
                if (!Match(piece.LeftEdgeType, left.RightEdgeType))
                    return false;
            }
            else
            {
                if (piece.LeftEdgeType != JigsawEdgeType.Flat)
                    return false;
            }

            // 👉 右
            if (x < width - 1)
            {
                var right = _onBoardJigsawData[index + 1];
                if (!Match(piece.RightEdgeType, right.LeftEdgeType))
                    return false;
            }
            else
            {
                if (piece.RightEdgeType != JigsawEdgeType.Flat)
                    return false;
            }
        }

        return true;
    }
    
    private bool CanFit(JigsawEdgeType a, JigsawEdgeType b)
    {
        if (a == JigsawEdgeType.In || b == JigsawEdgeType.In) return true;
        
        if (a == JigsawEdgeType.Flat && b == JigsawEdgeType.Flat)
            return true;

        return false;
    }
    
    private bool Match(JigsawEdgeType a, JigsawEdgeType b)
    {
        if (a == JigsawEdgeType.Flat && b == JigsawEdgeType.Flat)
            return true;

        if (a == JigsawEdgeType.In && b == JigsawEdgeType.Out)
            return true;

        if (a == JigsawEdgeType.Out && b == JigsawEdgeType.In)
            return true;

        return false;
    }

    public void Put(JigsawRuntimeData jigsawData, JigsawSlot targetSlot)
    {
        var index = System.Array.IndexOf(slots, targetSlot);
        _onBoardJigsawData[index] = jigsawData;

        if (IsFilled())
        {
            foreach (var slot in slots)
            {
                slot.Unlock();
            }
        }
    }

    public void ClearJigsaws()
    {
        foreach (var slot in slots)
        {
            slot.gameObject.SetActive(true);
            slot.ResetState();
        }
        _onBoardJigsawData.Clear();
    }
    
    public void ClearSlot(JigsawSlot sender)
    {
        var index = System.Array.IndexOf(slots, sender);
        _onBoardJigsawData.Remove(index);
        
        foreach (var slot in slots)
        {
            if (slot == sender)  continue;
            slot.Show();
        }
    }
}