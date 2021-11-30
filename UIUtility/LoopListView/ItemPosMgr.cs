using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SuperScrollView
{

    public class ItemSizeGroup
    {

        public float[] mItemSizeArray = null;
        public float[] mItemStartPosArray = null;
        public int mItemCount = 0;
        int mDirtyBeginIndex = ItemPosMgr.mItemMaxCountPerGroup;
        public float mGroupSize = 0;
        public float mGroupStartPos = 0;
        public float mGroupEndPos = 0;
        public int mGroupIndex = 0;
        float mItemDefaultSize = 0;
        public ItemSizeGroup(int index,float itemDefaultSize)
        {
            this.mGroupIndex = index;
            this.mItemDefaultSize = itemDefaultSize;
            this.Init();
        }

        public void Init()
        {
            this.mItemSizeArray = new float[ItemPosMgr.mItemMaxCountPerGroup];
            if (this.mItemDefaultSize != 0)
            {
                for (int i = 0; i < this.mItemSizeArray.Length; ++i)
                    this.mItemSizeArray[i] = this.mItemDefaultSize;
            }
            this.mItemStartPosArray = new float[ItemPosMgr.mItemMaxCountPerGroup];
            this.mItemStartPosArray[0] = 0;
            this.mItemCount = ItemPosMgr.mItemMaxCountPerGroup;
            this.mGroupSize = this.mItemDefaultSize * this.mItemSizeArray.Length;
            this.mDirtyBeginIndex = this.mItemDefaultSize != 0 ? 0 : ItemPosMgr.mItemMaxCountPerGroup;
        }

        public float GetItemStartPos(int index)
        {
            return this.mGroupStartPos + this.mItemStartPosArray[index];
        }

        public bool IsDirty { get { return (this.mDirtyBeginIndex < this.mItemCount); } }

        public float SetItemSize(int index, float size)
        {
            float old = this.mItemSizeArray[index];
            if (old == size)
            {
                return 0;
            }
            this.mItemSizeArray[index] = size;
            if (index < this.mDirtyBeginIndex)
            {
                this.mDirtyBeginIndex = index;
            }
            float ds = size - old;
            this.mGroupSize = this.mGroupSize + ds;
            return ds;
        }

        public void SetItemCount(int count)
        {
            if (this.mItemCount == count)
            {
                return;
            }
            this.mItemCount = count;
            this.RecalcGroupSize();
        }

        public void RecalcGroupSize()
        {
            this.mGroupSize = 0;
            for (int i = 0; i < this.mItemCount; ++i)
            {
                this.mGroupSize += this.mItemSizeArray[i];
            }
        }

        public int GetItemIndexByPos(float pos)
        {
            if (this.mItemCount == 0)
            {
                return -1;
            }
            int low = 0;
            int high = this.mItemCount - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                float startPos = this.mItemStartPosArray[mid];
                float endPos = startPos + this.mItemSizeArray[mid];
                if (startPos <= pos && endPos >= pos)
                {
                    return mid;
                }
                else if (pos > endPos)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return -1;
        }

        public void UpdateAllItemStartPos()
        {
            if (this.mDirtyBeginIndex >= this.mItemCount)
                return;
            int startIndex = (this.mDirtyBeginIndex < 1) ? 1 : this.mDirtyBeginIndex;
            for (int i = startIndex; i < this.mItemCount; ++i)
                this.mItemStartPosArray[i] = this.mItemStartPosArray[i - 1] + this.mItemSizeArray[i - 1];
            this.mDirtyBeginIndex = this.mItemCount;
        }
    }

    public class ItemPosMgr
    {
        public const int mItemMaxCountPerGroup = 100;
        List<ItemSizeGroup> mItemSizeGroupList = new List<ItemSizeGroup>();
        int mDirtyBeginIndex = int.MaxValue;
        public float mTotalSize = 0;
        public float mItemDefaultSize = 20;

        public ItemPosMgr(float itemDefaultSize)
        {
            this.mItemDefaultSize = itemDefaultSize;
        }

        public void SetItemMaxCount(int maxCount)
        {
            this.mDirtyBeginIndex = 0;
            this.mTotalSize = 0;
            int st = maxCount % mItemMaxCountPerGroup;
            int lastGroupItemCount = st;
            int needMaxGroupCount = maxCount / mItemMaxCountPerGroup;
            if (st > 0)
            {
                needMaxGroupCount++;
            }
            else
            {
                lastGroupItemCount = mItemMaxCountPerGroup;
            }
            int count = this.mItemSizeGroupList.Count;
            if (count > needMaxGroupCount)
            {
                int d = count - needMaxGroupCount;
                this.mItemSizeGroupList.RemoveRange(needMaxGroupCount, d);
            }
            else if (count < needMaxGroupCount)
            {
                int d = needMaxGroupCount - count;
                for (int i = 0; i < d; ++i)
                {
                    ItemSizeGroup tGroup = new ItemSizeGroup(count + i, this.mItemDefaultSize);
                    this.mItemSizeGroupList.Add(tGroup);
                }
            }
            count = this.mItemSizeGroupList.Count;
            if (count == 0)
            {
                return;
            }
            for (int i = 0; i < count - 1; ++i)
            {
                this.mItemSizeGroupList[i].SetItemCount(mItemMaxCountPerGroup);
            }
            this.mItemSizeGroupList[count - 1].SetItemCount(lastGroupItemCount);
            for (int i = 0; i < count; ++i)
            {
                this.mTotalSize = this.mTotalSize + this.mItemSizeGroupList[i].mGroupSize;
            }

        }

        public void SetItemSize(int itemIndex, float size)
        {
            int groupIndex = itemIndex / mItemMaxCountPerGroup;
            int indexInGroup = itemIndex % mItemMaxCountPerGroup;
            ItemSizeGroup tGroup = this.mItemSizeGroupList[groupIndex];
            float changedSize = tGroup.SetItemSize(indexInGroup, size);
            if (changedSize != 0f)
            {
                if (groupIndex < this.mDirtyBeginIndex)
                {
                    this.mDirtyBeginIndex = groupIndex;
                }
            }
            this.mTotalSize += changedSize;
        }

        public float GetItemPos(int itemIndex)
        {
            this.Update(true);
            int groupIndex = itemIndex / mItemMaxCountPerGroup;
            int indexInGroup = itemIndex % mItemMaxCountPerGroup;
            return this.mItemSizeGroupList[groupIndex].GetItemStartPos(indexInGroup);
        }

        public void GetItemIndexAndPosAtGivenPos(float pos, ref int index, ref float itemPos)
        {
            this.Update(true);
            index = 0;
            itemPos = 0f;
            int count = this.mItemSizeGroupList.Count;
            if (count == 0)
            {
                return;
            }
            ItemSizeGroup hitGroup = null;

            int low = 0;
            int high = count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                ItemSizeGroup tGroup = this.mItemSizeGroupList[mid];
                if (tGroup.mGroupStartPos <= pos && tGroup.mGroupEndPos >= pos)
                {
                    hitGroup = tGroup;
                    break;
                }
                else if (pos > tGroup.mGroupEndPos)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            int hitIndex = -1;
            if (hitGroup != null)
            {
                hitIndex = hitGroup.GetItemIndexByPos(pos - hitGroup.mGroupStartPos);
            }
            else
            {
                return;
            }
            if (hitIndex < 0)
            {
                return;
            }
            index = hitIndex + hitGroup.mGroupIndex * mItemMaxCountPerGroup;
            itemPos = hitGroup.GetItemStartPos(hitIndex);
        }

        public void Update(bool updateAll)
        {
            int count = this.mItemSizeGroupList.Count;
            if (count == 0)
            {
                return;
            }
            if (this.mDirtyBeginIndex >= count)
            {
                return;
            }
            int loopCount = 0;
            for (int i = this.mDirtyBeginIndex; i < count; ++i)
            {
                loopCount++;
                ItemSizeGroup tGroup = this.mItemSizeGroupList[i];
                this.mDirtyBeginIndex++;
                tGroup.UpdateAllItemStartPos();
                if (i == 0)
                {
                    tGroup.mGroupStartPos = 0;
                    tGroup.mGroupEndPos = tGroup.mGroupSize;
                }
                else
                {
                    tGroup.mGroupStartPos = this.mItemSizeGroupList[i - 1].mGroupEndPos;
                    tGroup.mGroupEndPos = tGroup.mGroupStartPos + tGroup.mGroupSize;
                }
                if (!updateAll && loopCount > 1)
                {
                    return;
                }

            }
        }

    }
}