using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SuperScrollView
{

    public class LoopListViewItem2 : MonoBehaviour
    {
        int mItemIndex = -1;
        int mItemId = -1;
        LoopListView2 mParentListView = null;
        bool mIsInitHandlerCalled = false;
        string mItemPrefabName;
        RectTransform mCachedRectTransform;
        float mPadding;
        float mDistanceWithViewPortSnapCenter = 0;
        int mItemCreatedCheckFrameCount = 0;
        float mStartPosOffset = 0;

        object mUserObjectData = null;
        int mUserIntData1 = 0;
        int mUserIntData2 = 0;
        string mUserStringData1 = null;
        string mUserStringData2 = null;

        public object UserObjectData { get { return this.mUserObjectData; } set { this.mUserObjectData = value; } }
        public int UserIntData1 { get { return this.mUserIntData1; } set { this.mUserIntData1 = value; } }
        public int UserIntData2 { get { return this.mUserIntData2; } set { this.mUserIntData2 = value; } }
        public string UserStringData1 { get { return this.mUserStringData1; } set { this.mUserStringData1 = value; } }
        public string UserStringData2 { get { return this.mUserStringData2; } set { this.mUserStringData2 = value; } }

        public float DistanceWithViewPortSnapCenter { get { return this.mDistanceWithViewPortSnapCenter; } set { this.mDistanceWithViewPortSnapCenter = value; } }

        public float StartPosOffset { get { return this.mStartPosOffset; } set { this.mStartPosOffset = value; } }

        public int ItemCreatedCheckFrameCount { get { return this.mItemCreatedCheckFrameCount; } set { this.mItemCreatedCheckFrameCount = value; } }

        public float Padding { get { return this.mPadding; } set { this.mPadding = value; } }

        public RectTransform CachedRectTransform
        {
            get
            {
                if (this.mCachedRectTransform == null)
                {
                    this.mCachedRectTransform = this.gameObject.GetComponent<RectTransform>();
                }
                return this.mCachedRectTransform;
            }
        }

        public string ItemPrefabName { get { return this.mItemPrefabName; } set { this.mItemPrefabName = value; } }

        public int ItemIndex { get { return this.mItemIndex; } set { this.mItemIndex = value; } }
        public int ItemId { get { return this.mItemId; } set { this.mItemId = value; } }


        public bool IsInitHandlerCalled { get { return this.mIsInitHandlerCalled; } set { this.mIsInitHandlerCalled = value; } }

        public LoopListView2 ParentListView { get { return this.mParentListView; } set { this.mParentListView = value; } }

        public float TopY
        {
            get
            {
                ListItemArrangeType arrageType = this.ParentListView.ArrangeType;
                if (arrageType == ListItemArrangeType.TopToBottom)
                {
                    return this.CachedRectTransform.localPosition.y;
                }
                else if (arrageType == ListItemArrangeType.BottomToTop)
                {
                    return this.CachedRectTransform.localPosition.y + this.CachedRectTransform.rect.height;
                }
                return 0;
            }
        }

        public float BottomY
        {
            get
            {
                ListItemArrangeType arrageType = this.ParentListView.ArrangeType;
                if (arrageType == ListItemArrangeType.TopToBottom)
                {
                    return this.CachedRectTransform.localPosition.y - this.CachedRectTransform.rect.height;
                }
                else if (arrageType == ListItemArrangeType.BottomToTop)
                {
                    return this.CachedRectTransform.localPosition.y;
                }
                return 0;
            }
        }


        public float LeftX
        {
            get
            {
                ListItemArrangeType arrageType = this.ParentListView.ArrangeType;
                if (arrageType == ListItemArrangeType.LeftToRight)
                {
                    return this.CachedRectTransform.localPosition.x;
                }
                else if (arrageType == ListItemArrangeType.RightToLeft)
                {
                    return this.CachedRectTransform.localPosition.x - this.CachedRectTransform.rect.width;
                }
                return 0;
            }
        }

        public float RightX
        {
            get
            {
                ListItemArrangeType arrageType = this.ParentListView.ArrangeType;
                if (arrageType == ListItemArrangeType.LeftToRight)
                {
                    return this.CachedRectTransform.localPosition.x + this.CachedRectTransform.rect.width;
                }
                else if (arrageType == ListItemArrangeType.RightToLeft)
                {
                    return this.CachedRectTransform.localPosition.x;
                }
                return 0;
            }
        }

        public float ItemSize
        {
            get
            {
                if (this.ParentListView.IsVertList)
                {
                    return this.CachedRectTransform.rect.height;
                }
                else
                {
                    return this.CachedRectTransform.rect.width;
                }
            }
        }

        public float ItemSizeWithPadding { get { return this.ItemSize + this.mPadding; } }

    }
}