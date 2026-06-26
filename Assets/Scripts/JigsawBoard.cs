using System.Linq;
using GameEvent;
using GameEvent.Args;
using UnityEngine;
using UnityEngine.Assertions;

public class JigsawBoard : MonoBehaviour
{
    [SerializeField] private JigsawSlot[] slots;
    [SerializeField] private int width;
    [SerializeField] private int height;

    private bool _isShowingSlots = true;
    
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
        
        var slotJigsawDataArray = slots.Select(slot => slot.JigsawData).ToArray();

        int index = System.Array.IndexOf(slots, targetSlot);

        // ❗ 已经被占
        if (slotJigsawDataArray[index].Source)
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
            if (slotJigsawDataArray[upIndex].Source)
            {
                if (!CanFit(jigsawData.UpEdgeType, slotJigsawDataArray[upIndex].DownEdgeType))
                    return false;
            }
        }

        // 👉 下
        if (y < height - 1)
        {
            int downIndex = index + width;
            if (slotJigsawDataArray[downIndex].Source)
            {
                if (!CanFit(jigsawData.DownEdgeType, slotJigsawDataArray[downIndex].UpEdgeType))
                    return false;
            }
        }

        // 👉 左
        if (x > 0)
        {
            int leftIndex = index - 1;
            if (slotJigsawDataArray[leftIndex].Source)
            {
                if (!CanFit(jigsawData.LeftEdgeType, slotJigsawDataArray[leftIndex].RightEdgeType))
                    return false;
            }
        }

        // 👉 右
        if (x < width - 1)
        {
            int rightIndex = index + 1;
            if (slotJigsawDataArray[rightIndex].Source)
            {
                if (!CanFit(jigsawData.RightEdgeType, slotJigsawDataArray[rightIndex].LeftEdgeType))
                    return false;
            }
        }

        return true;
    }
    
    public bool IsFilled()
    {
        // 👉 1. 是否填满
        var slotJigsawDataArray = slots.Select(slot => slot.JigsawData).ToArray();
        if (slotJigsawDataArray.Any(data => !data.Source))
            return false;

        for (int index = 0; index < slots.Length; index++)
        {
            var pieceData = slotJigsawDataArray[index];
            if (!pieceData.Source) return false;

            int x = index % width;
            int y = index / width;

            // 👉 上
            if (y > 0)
            {
                var up = slotJigsawDataArray[index - width];
                if (!Match(pieceData.UpEdgeType, up.DownEdgeType))
                    return false;
            }
            else
            {
                if (pieceData.UpEdgeType != JigsawEdgeType.Flat)
                    return false;
            }

            // 👉 下
            if (y < height - 1)
            {
                var down = slotJigsawDataArray[index + width];
                if (!Match(pieceData.DownEdgeType, down.UpEdgeType))
                    return false;
            }
            else
            {
                if (pieceData.DownEdgeType != JigsawEdgeType.Flat)
                    return false;
            }

            // 👉 左
            if (x > 0)
            {
                var left = slotJigsawDataArray[index - 1];
                if (!Match(pieceData.LeftEdgeType, left.RightEdgeType))
                    return false;
            }
            else
            {
                if (pieceData.LeftEdgeType != JigsawEdgeType.Flat)
                    return false;
            }

            // 👉 右
            if (x < width - 1)
            {
                var right = slotJigsawDataArray[index + 1];
                if (!Match(pieceData.RightEdgeType, right.LeftEdgeType))
                    return false;
            }
            else
            {
                if (pieceData.RightEdgeType != JigsawEdgeType.Flat)
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

    public void OnPutOnSlot()
    {
        var isFilled = IsFilled();
        if (isFilled)
        {
            foreach (var slot in slots)
            {
                slot.Unlock();
            }
            _isShowingSlots = false;
        }
        EventComponent.Instance.Fire(this, BoardStateChangedEventArgs.Create(isFilled, false));
    }

    public void ClearJigsaws()
    {
        foreach (var slot in slots)
        {
            slot.gameObject.SetActive(true);
            slot.ResetState();
        }
        EventComponent.Instance.Fire(this, BoardStateChangedEventArgs.Create(false, true));
    }
    
    public void ClearSlot(JigsawSlot sender)
    {
        foreach (var slot in slots)
        {
            if (slot == sender)  continue;
            slot.Show();
        }
    }

    public void ShowSlots()
    {
        if (_isShowingSlots) return;
        
        foreach (var slot in slots)
        {
            slot.Show();
        }
        
        _isShowingSlots = true;
    }
}