using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace SuperScrollView
{
    public enum ItemCornerEnum
    {
        LeftBottom = 0,
        LeftTop,
        RightTop,
        RightBottom,
    }


    public enum ListItemArrangeType
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft,
    }

    public class ItemPool
    {
        GameObject mPrefabObj;
        string mPrefabName;
        int mInitCreateCount = 1;
        float mPadding = 0;
        float mStartPosOffset = 0;
        List<LoopListViewItem2> mTmpPooledItemList = new List<LoopListViewItem2>();
        List<LoopListViewItem2> mPooledItemList = new List<LoopListViewItem2>();
        static int mCurItemIdCount = 0;
        RectTransform mItemParent = null;

        public ItemPool()
        {

        }

        public void Init(GameObject prefabObj, float padding, float startPosOffset, int createCount, RectTransform parent)
        {
            this.mPrefabObj = prefabObj;
            this.mPrefabName = this.mPrefabObj.name;
            this.mInitCreateCount = createCount;
            this.mPadding = padding;
            this.mStartPosOffset = startPosOffset;
            this.mItemParent = parent;
            this.mPrefabObj.SetActive(false);
            for (int i = 0; i < this.mInitCreateCount; ++i)
            {
                LoopListViewItem2 tViewItem = this.CreateItem();
                this.RecycleItemReal(tViewItem);
            }
        }

        public LoopListViewItem2 GetItem()
        {
            mCurItemIdCount++;
            LoopListViewItem2 tItem = null;
            if (this.mTmpPooledItemList.Count > 0)
            {
                int count = this.mTmpPooledItemList.Count;
                tItem = this.mTmpPooledItemList[count - 1];
                this.mTmpPooledItemList.RemoveAt(count - 1);
                tItem.gameObject.SetActive(true);
            }
            else
            {
                int count = this.mPooledItemList.Count;
                if (count == 0)
                {
                    tItem = this.CreateItem();
                }
                else
                {
                    tItem = this.mPooledItemList[count - 1];
                    this.mPooledItemList.RemoveAt(count - 1);
                    tItem.gameObject.SetActive(true);
                }
            }
            tItem.Padding = this.mPadding;
            tItem.ItemId = mCurItemIdCount;
            return tItem;

        }

        public void DestroyAllItem()
        {
            this.ClearTmpRecycledItem();
            int count = this.mPooledItemList.Count;
            for (int i = 0; i < count; ++i)
            {
                Object.DestroyImmediate(this.mPooledItemList[i].gameObject);
            }
            this.mPooledItemList.Clear();
        }

        public LoopListViewItem2 CreateItem()
        {

            GameObject go = (GameObject)Object.Instantiate(this.mPrefabObj, Vector3.zero, Quaternion.identity);
            go.transform.SetParent(this.mItemParent, false);
            go.SetActive(true);
            RectTransform rf = go.GetComponent<RectTransform>();
            rf.localScale = Vector3.one;
            rf.localPosition = Vector3.zero;
            rf.localEulerAngles = Vector3.zero;
            LoopListViewItem2 tViewItem = go.GetComponent<LoopListViewItem2>();
            tViewItem.ItemPrefabName = this.mPrefabName;
            tViewItem.StartPosOffset = this.mStartPosOffset;
            return tViewItem;
        }

        void RecycleItemReal(LoopListViewItem2 item)
        {
            item.gameObject.SetActive(false);
            this.mPooledItemList.Add(item);
        }

        public void RecycleItem(LoopListViewItem2 item)
        {
            this.mTmpPooledItemList.Add(item);
        }

        public void ClearTmpRecycledItem()
        {
            int count = this.mTmpPooledItemList.Count;
            if (count == 0)
            {
                return;
            }
            for (int i = 0; i < count; ++i)
            {
                this.RecycleItemReal(this.mTmpPooledItemList[i]);
            }
            this.mTmpPooledItemList.Clear();
        }
    }

    [Serializable]
    public class ItemPrefabConfData
    {
        public GameObject mItemPrefab = null;
        public float mPadding = 0;
        public int mInitCreateCount = 0;
        public float mStartPosOffset = 0;
    }


    public class LoopListViewInitParam
    {
        // all the default values
        public float mDistanceForRecycle0 = 300; //mDistanceForRecycle0 should be larger than mDistanceForNew0
        public float mDistanceForNew0 = 200;
        public float mDistanceForRecycle1 = 300; //mDistanceForRecycle1 should be larger than mDistanceForNew1
        public float mDistanceForNew1 = 200;
        public float mSmoothDumpRate = 0.3f;
        public float mSnapFinishThreshold = 0.01f;
        public float mSnapVecThreshold = 145;
        public float mItemDefaultWithPaddingSize = 20;

        public static LoopListViewInitParam CopyDefaultInitParam()
        {
            return new LoopListViewInitParam();
        }
    }

    public enum SnapStatus
    {
        NoTargetSet = 0,
        TargetHasSet = 1,
        SnapMoving = 2,
        SnapMoveFinish = 3
    }



    public class LoopListView2 : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {

        class SnapData
        {
            public SnapStatus mSnapStatus = SnapStatus.NoTargetSet;
            public int mSnapTargetIndex = 0;
            public float mTargetSnapVal = 0;
            public float mCurSnapVal = 0;
            public bool mIsForceSnapTo = false;

            public void Clear()
            {
                this.mSnapStatus = SnapStatus.NoTargetSet;
                this.mIsForceSnapTo = false;
            }
        }

        Dictionary<string, ItemPool> mItemPoolDict = new Dictionary<string, ItemPool>();
        public List<ItemPool> mItemPoolList = new List<ItemPool>();

        public List<ItemPrefabConfData> mItemPrefabDataList = new List<ItemPrefabConfData>();

        [SerializeField]
        private ListItemArrangeType mArrangeType = ListItemArrangeType.TopToBottom;
        public ListItemArrangeType ArrangeType { get { return this.mArrangeType; } set { this.mArrangeType = value; } }

        List<LoopListViewItem2> mItemList = new List<LoopListViewItem2>();
        RectTransform mContainerTrans;
        ScrollRect mScrollRect = null;
        RectTransform mScrollRectTransform = null;
        RectTransform mViewPortRectTransform = null;
        float mItemDefaultWithPaddingSize = 20;
        int mItemTotalCount = 0;
        bool mIsVertList = false;
        Func<LoopListView2, int, LoopListViewItem2> mOnGetItemByIndex;
        Vector3[] mItemWorldCorners = new Vector3[4];
        Vector3[] mViewPortRectLocalCorners = new Vector3[4];
        int mCurReadyMinItemIndex = 0;
        int mCurReadyMaxItemIndex = 0;
        bool mNeedCheckNextMinItem = true;
        bool mNeedCheckNextMaxItem = true;
        ItemPosMgr mItemPosMgr = null;
        float mDistanceForRecycle0 = 300;
        float mDistanceForNew0 = 200;
        float mDistanceForRecycle1 = 300;
        float mDistanceForNew1 = 200;
        [SerializeField]
        bool mSupportScrollBar = true;
        bool mIsDraging = false;
        PointerEventData mPointerEventData = null;
        public Action mOnBeginDragAction = null;
        public Action mOnDragingAction = null;
        public Action mOnEndDragAction = null;
        int mLastItemIndex = 0;
        float mLastItemPadding = 0;
        float mSmoothDumpVel = 0;
        float mSmoothDumpRate = 0.3f;
        float mSnapFinishThreshold = 0.1f;
        float mSnapVecThreshold = 145;
        [SerializeField]
        bool mItemSnapEnable = false;


        Vector3 mLastFrameContainerPos = Vector3.zero;
        public Action<LoopListView2, LoopListViewItem2> mOnSnapItemFinished = null;
        public Action<LoopListView2, LoopListViewItem2> mOnSnapNearestChanged = null;
        int mCurSnapNearestItemIndex = -1;
        Vector2 mAdjustedVec;
        bool mNeedAdjustVec = false;
        int mLeftSnapUpdateExtraCount = 1;
        [SerializeField]
        Vector2 mViewPortSnapPivot = Vector2.zero;
        [SerializeField]
        Vector2 mItemSnapPivot = Vector2.zero;
        ClickEventListener mScrollBarClickEventListener = null;
        SnapData mCurSnapData = new SnapData();
        Vector3 mLastSnapCheckPos = Vector3.zero;
        bool mListViewInited = false;
        int mListUpdateCheckFrameCount = 0;
        public bool IsVertList { get { return this.mIsVertList; } }
        public int ItemTotalCount { get { return this.mItemTotalCount; } }

        public RectTransform ContainerTrans { get { return this.mContainerTrans; } }

        public ScrollRect ScrollRect { get { return this.mScrollRect; } }

        public bool IsDraging { get { return this.mIsDraging; } }

        public bool ItemSnapEnable { get { return this.mItemSnapEnable; } set { this.mItemSnapEnable = value; } }

        public bool SupportScrollBar { get { return this.mSupportScrollBar; } set { this.mSupportScrollBar = value; } }

        public ItemPrefabConfData GetItemPrefabConfData(string prefabName)
        {
            foreach (ItemPrefabConfData data in this.mItemPrefabDataList)
            {
                if (data.mItemPrefab == null)
                {
                    Debug.LogError("A item prefab is null ");
                    continue;
                }
                if (prefabName == data.mItemPrefab.name)
                {
                    return data;
                }

            }
            return null;
        }

        public void OnItemPrefabChanged(string prefabName)
        {
            ItemPrefabConfData data = this.GetItemPrefabConfData(prefabName);
            if (data == null)
            {
                return;
            }
            ItemPool pool = null;
            if (this.mItemPoolDict.TryGetValue(prefabName, out pool) == false)
            {
                return;
            }
            int firstItemIndex = -1;
            Vector3 pos = Vector3.zero;
            if (this.mItemList.Count > 0)
            {
                firstItemIndex = this.mItemList[0].ItemIndex;
                pos = this.mItemList[0].CachedRectTransform.localPosition;
            }
            this.RecycleAllItem();
            this.ClearAllTmpRecycledItem();
            pool.DestroyAllItem();
            pool.Init(data.mItemPrefab, data.mPadding, data.mStartPosOffset, data.mInitCreateCount, this.mContainerTrans);
            if (firstItemIndex >= 0)
            {
                this.RefreshAllShownItemWithFirstIndexAndPos(firstItemIndex, pos);
            }
        }

        /*
        InitListView method is to initiate the LoopListView2 component. There are 3 parameters:
        itemTotalCount: the total item count in the listview. If this parameter is set -1, then means there are infinite items, and scrollbar would not be supported, and the ItemIndex can be from –MaxInt to +MaxInt. If this parameter is set a value >=0 , then the ItemIndex can only be from 0 to itemTotalCount -1.
        onGetItemByIndex: when a item is getting in the scrollrect viewport, and this Action will be called with the item’ index as a parameter, to let you create the item and update its content.
        */
        public void InitListView(int itemTotalCount,
                                 Func<LoopListView2, int, LoopListViewItem2> onGetItemByIndex,
                                 LoopListViewInitParam initParam = null)
        {
            if (initParam != null)
            {
                this.mDistanceForRecycle0 = initParam.mDistanceForRecycle0;
                this.mDistanceForNew0 = initParam.mDistanceForNew0;
                this.mDistanceForRecycle1 = initParam.mDistanceForRecycle1;
                this.mDistanceForNew1 = initParam.mDistanceForNew1;
                this.mSmoothDumpRate = initParam.mSmoothDumpRate;
                this.mSnapFinishThreshold = initParam.mSnapFinishThreshold;
                this.mSnapVecThreshold = initParam.mSnapVecThreshold;
                this.mItemDefaultWithPaddingSize = initParam.mItemDefaultWithPaddingSize;
            }
            this.mScrollRect = this.gameObject.GetComponent<ScrollRect>();
            if (this.mScrollRect == null)
            {
                Debug.LogError("ListView Init Failed! ScrollRect component not found!");
                return;
            }
            if (this.mDistanceForRecycle0 <= this.mDistanceForNew0)
            {
                Debug.LogError("mDistanceForRecycle0 should be bigger than mDistanceForNew0");
            }
            if (this.mDistanceForRecycle1 <= this.mDistanceForNew1)
            {
                Debug.LogError("mDistanceForRecycle1 should be bigger than mDistanceForNew1");
            }
            this.mCurSnapData.Clear();
            this.mItemPosMgr = new ItemPosMgr(this.mItemDefaultWithPaddingSize);
            this.mScrollRectTransform = this.mScrollRect.GetComponent<RectTransform>();
            this.mContainerTrans = this.mScrollRect.content;
            this.mViewPortRectTransform = this.mScrollRect.viewport;
            if (this.mViewPortRectTransform == null)
            {
                this.mViewPortRectTransform = this.mScrollRectTransform;
            }
            if (this.mScrollRect.horizontalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport && this.mScrollRect.horizontalScrollbar != null)
            {
                Debug.LogError("ScrollRect.horizontalScrollbarVisibility cannot be set to AutoHideAndExpandViewport");
            }
            if (this.mScrollRect.verticalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport && this.mScrollRect.verticalScrollbar != null)
            {
                Debug.LogError("ScrollRect.verticalScrollbarVisibility cannot be set to AutoHideAndExpandViewport");
            }
            this.mIsVertList = (this.mArrangeType == ListItemArrangeType.TopToBottom || this.mArrangeType == ListItemArrangeType.BottomToTop);
            this.mScrollRect.horizontal = !this.mIsVertList;
            this.mScrollRect.vertical = this.mIsVertList;
            this.SetScrollbarListener();
            this.AdjustPivot(this.mViewPortRectTransform);
            this.AdjustAnchor(this.mContainerTrans);
            this.AdjustContainerPivot(this.mContainerTrans);
            this.InitItemPool();
            this.mOnGetItemByIndex = onGetItemByIndex;
            if (this.mListViewInited == true)
            {
                Debug.LogError("LoopListView2.InitListView method can be called only once.");
            }
            this.mListViewInited = true;
            this.ResetListView();
            this.SetListItemCount(itemTotalCount, true);
        }

        void SetScrollbarListener()
        {
            this.mScrollBarClickEventListener = null;
            Scrollbar curScrollBar = null;
            if (this.mIsVertList && this.mScrollRect.verticalScrollbar != null)
            {
                curScrollBar = this.mScrollRect.verticalScrollbar;

            }
            if (!this.mIsVertList && this.mScrollRect.horizontalScrollbar != null)
            {
                curScrollBar = this.mScrollRect.horizontalScrollbar;
            }
            if (curScrollBar == null)
            {
                return;
            }
            ClickEventListener listener = ClickEventListener.Get(curScrollBar.gameObject);
            this.mScrollBarClickEventListener = listener;
            listener.SetPointerUpHandler(this.OnPointerUpInScrollBar);
            listener.SetPointerDownHandler(this.OnPointerDownInScrollBar);
        }

        void OnPointerDownInScrollBar(GameObject obj)
        {
            this.mCurSnapData.Clear();
        }

        void OnPointerUpInScrollBar(GameObject obj)
        {
            this.ForceSnapUpdateCheck();
        }

        public void ResetListView()
        {
            this.mViewPortRectTransform.GetLocalCorners(this.mViewPortRectLocalCorners);
            this.mContainerTrans.localPosition = Vector3.zero;
            this.ForceSnapUpdateCheck();
        }


        /*
        This method may use to set the item total count of the scrollview at runtime. 
        If this parameter is set -1, then means there are infinite items,
        and scrollbar would not be supported, and the ItemIndex can be from –MaxInt to +MaxInt. 
        If this parameter is set a value >=0 , then the ItemIndex can only be from 0 to itemTotalCount -1.  
        If resetPos is set false, then the scrollrect’s content position will not changed after this method finished.
        */
        public void SetListItemCount(int itemCount, bool resetPos = true)
        {
            if (itemCount == this.mItemTotalCount)
            {
                return;
            }
            this.mCurSnapData.Clear();
            this.mItemTotalCount = itemCount;
            if (this.mItemTotalCount < 0)
            {
                this.mSupportScrollBar = false;
            }
            if (this.mSupportScrollBar)
            {
                this.mItemPosMgr.SetItemMaxCount(this.mItemTotalCount);
            }
            else
            {
                this.mItemPosMgr.SetItemMaxCount(0);
            }
            if (this.mItemTotalCount == 0)
            {
                this.mCurReadyMaxItemIndex = 0;
                this.mCurReadyMinItemIndex = 0;
                this.mNeedCheckNextMaxItem = false;
                this.mNeedCheckNextMinItem = false;
                this.RecycleAllItem();
                this.ClearAllTmpRecycledItem();
                this.UpdateContentSize();
                return;
            }
            this.mLeftSnapUpdateExtraCount = 1;
            this.mNeedCheckNextMaxItem = true;
            this.mNeedCheckNextMinItem = true;
            if (resetPos)
            {
                this.MovePanelToItemIndex(0, 0);
                return;
            }
            if (this.mItemList.Count == 0)
            {
                this.MovePanelToItemIndex(0, 0);
                return;
            }
            int maxItemIndex = this.mItemTotalCount - 1;
            int lastItemIndex = this.mItemList[this.mItemList.Count - 1].ItemIndex;
            if (lastItemIndex <= maxItemIndex)
            {
                this.UpdateContentSize();
                this.UpdateAllShownItemsPos();
                return;
            }
            this.MovePanelToItemIndex(maxItemIndex, 0);

        }

        //To get the visible item by itemIndex. If the item is not visible, then this method return null.
        public LoopListViewItem2 GetShownItemByItemIndex(int itemIndex)
        {
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return null;
            }
            if (itemIndex < this.mItemList[0].ItemIndex || itemIndex > this.mItemList[count - 1].ItemIndex)
            {
                return null;
            }
            int i = itemIndex - this.mItemList[0].ItemIndex;
            return this.mItemList[i];
        }

        public int ShownItemCount { get { return this.mItemList.Count; } }

        public float ViewPortSize
        {
            get
            {
                if (this.mIsVertList)
                {
                    return this.mViewPortRectTransform.rect.height;
                }
                else
                {
                    return this.mViewPortRectTransform.rect.width;
                }
            }
        }

        public float ViewPortWidth { get { return this.mViewPortRectTransform.rect.width; } }
        public float ViewPortHeight { get { return this.mViewPortRectTransform.rect.height; } }


        /*
         All visible items is stored in a List<LoopListViewItem2> , which is named mItemList;
         this method is to get the visible item by the index in visible items list. The parameter index is from 0 to mItemList.Count.
        */
        public LoopListViewItem2 GetShownItemByIndex(int index)
        {
            int count = this.mItemList.Count;
            if (index < 0 || index >= count)
            {
                return null;
            }
            return this.mItemList[index];
        }

        public LoopListViewItem2 GetShownItemByIndexWithoutCheck(int index)
        {
            return this.mItemList[index];
        }

        public int GetIndexInShownItemList(LoopListViewItem2 item)
        {
            if (item == null)
            {
                return -1;
            }
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return -1;
            }
            for (int i = 0; i < count; ++i)
            {
                if (this.mItemList[i] == item)
                {
                    return i;
                }
            }
            return -1;
        }


        public void DoActionForEachShownItem(Action<LoopListViewItem2, object> action, object param)
        {
            if (action == null)
            {
                return;
            }
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }
            for (int i = 0; i < count; ++i)
            {
                action(this.mItemList[i], param);
            }
        }


        public LoopListViewItem2 NewListViewItem(string itemPrefabName)
        {
            ItemPool pool = null;
            if (this.mItemPoolDict.TryGetValue(itemPrefabName, out pool) == false)
            {
                return null;
            }
            LoopListViewItem2 item = pool.GetItem();
            RectTransform rf = item.GetComponent<RectTransform>();
            rf.SetParent(this.mContainerTrans);
            rf.localScale = Vector3.one;
            rf.localPosition = Vector3.zero;
            rf.localEulerAngles = Vector3.zero;
            item.ParentListView = this;
            return item;
        }

        /*
        For a vertical scrollrect, when a visible item’s height changed at runtime, then this method should be called to let the LoopListView2 component reposition all visible items’ position.
        For a horizontal scrollrect, when a visible item’s width changed at runtime, then this method should be called to let the LoopListView2 component reposition all visible items’ position.
        */
        public void OnItemSizeChanged(int itemIndex)
        {
            LoopListViewItem2 item = this.GetShownItemByItemIndex(itemIndex);
            if (item == null)
            {
                return;
            }
            if (this.mSupportScrollBar)
            {
                if (this.mIsVertList)
                {
                    this.SetItemSize(itemIndex, item.CachedRectTransform.rect.height, item.Padding);
                }
                else
                {
                    this.SetItemSize(itemIndex, item.CachedRectTransform.rect.width, item.Padding);
                }
            }
            this.UpdateContentSize();
            this.UpdateAllShownItemsPos();
        }


        /*
        To update a item by itemIndex.if the itemIndex-th item is not visible, then this method will do nothing.
        Otherwise this method will first call onGetItemByIndex(itemIndex) to get a updated item and then reposition all visible items'position. 
        */
        public void RefreshItemByItemIndex(int itemIndex)
        {
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }
            if (itemIndex < this.mItemList[0].ItemIndex || itemIndex > this.mItemList[count - 1].ItemIndex)
            {
                return;
            }
            int firstItemIndex = this.mItemList[0].ItemIndex;
            int i = itemIndex - firstItemIndex;
            LoopListViewItem2 curItem = this.mItemList[i];
            Vector3 pos = curItem.CachedRectTransform.localPosition;
            this.RecycleItemTmp(curItem);
            LoopListViewItem2 newItem = this.GetNewItemByIndex(itemIndex);
            if (newItem == null)
            {
                this.RefreshAllShownItemWithFirstIndex(firstItemIndex);
                return;
            }
            this.mItemList[i] = newItem;
            if (this.mIsVertList)
            {
                pos.x = newItem.StartPosOffset;
            }
            else
            {
                pos.y = newItem.StartPosOffset;
            }
            newItem.CachedRectTransform.localPosition = pos;
            this.OnItemSizeChanged(itemIndex);
            this.ClearAllTmpRecycledItem();
        }

        //snap move will finish at once.
        public void FinishSnapImmediately()
        {
            this.UpdateSnapMove(true);
        }

        /*
        This method will move the scrollrect content’s position to ( the positon of itemIndex-th item + offset ),
        and in current version the itemIndex is from 0 to MaxInt, offset is from 0 to scrollrect viewport size. 
        */
        public void MovePanelToItemIndex(int itemIndex, float offset)
        {
            this.mScrollRect.StopMovement();
            this.mCurSnapData.Clear();
            if (itemIndex < 0 || this.mItemTotalCount == 0)
            {
                return;
            }
            if (this.mItemTotalCount > 0 && itemIndex >= this.mItemTotalCount)
            {
                itemIndex = this.mItemTotalCount - 1;
            }
            if (offset < 0)
            {
                offset = 0;
            }
            Vector3 pos = Vector3.zero;
            float viewPortSize = this.ViewPortSize;
            if (offset > viewPortSize)
            {
                offset = viewPortSize;
            }
            if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float containerPos = this.mContainerTrans.localPosition.y;
                if (containerPos < 0)
                {
                    containerPos = 0;
                }
                pos.y = -containerPos - offset;
            }
            else if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                float containerPos = this.mContainerTrans.localPosition.y;
                if (containerPos > 0)
                {
                    containerPos = 0;
                }
                pos.y = -containerPos + offset;
            }
            else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                float containerPos = this.mContainerTrans.localPosition.x;
                if (containerPos > 0)
                {
                    containerPos = 0;
                }
                pos.x = -containerPos + offset;
            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                float containerPos = this.mContainerTrans.localPosition.x;
                if (containerPos < 0)
                {
                    containerPos = 0;
                }
                pos.x = -containerPos - offset;
            }

            this.RecycleAllItem();
            LoopListViewItem2 newItem = this.GetNewItemByIndex(itemIndex);
            if (newItem == null)
            {
                this.ClearAllTmpRecycledItem();
                return;
            }
            if (this.mIsVertList)
            {
                pos.x = newItem.StartPosOffset;
            }
            else
            {
                pos.y = newItem.StartPosOffset;
            }
            newItem.CachedRectTransform.localPosition = pos;
            if (this.mSupportScrollBar)
            {
                if (this.mIsVertList)
                {
                    this.SetItemSize(itemIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                }
                else
                {
                    this.SetItemSize(itemIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                }
            }
            this.mItemList.Add(newItem);
            this.UpdateContentSize();
            this.UpdateListView(viewPortSize + 100, viewPortSize + 100, viewPortSize, viewPortSize);
            this.AdjustPanelPos();
            this.ClearAllTmpRecycledItem();
        }

        //update all visible items.
        public void RefreshAllShownItem()
        {
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }
            this.RefreshAllShownItemWithFirstIndex(this.mItemList[0].ItemIndex);
        }


        public void RefreshAllShownItemWithFirstIndex(int firstItemIndex)
        {
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }
            LoopListViewItem2 firstItem = this.mItemList[0];
            Vector3 pos = firstItem.CachedRectTransform.localPosition;
            this.RecycleAllItem();
            for (int i = 0; i < count; ++i)
            {
                int curIndex = firstItemIndex + i;
                LoopListViewItem2 newItem = this.GetNewItemByIndex(curIndex);
                if (newItem == null)
                {
                    break;
                }
                if (this.mIsVertList)
                {
                    pos.x = newItem.StartPosOffset;
                }
                else
                {
                    pos.y = newItem.StartPosOffset;
                }
                newItem.CachedRectTransform.localPosition = pos;
                if (this.mSupportScrollBar)
                {
                    if (this.mIsVertList)
                    {
                        this.SetItemSize(curIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                    }
                    else
                    {
                        this.SetItemSize(curIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                    }
                }

                this.mItemList.Add(newItem);
            }
            this.UpdateContentSize();
            this.UpdateAllShownItemsPos();
            this.ClearAllTmpRecycledItem();
        }


        public void RefreshAllShownItemWithFirstIndexAndPos(int firstItemIndex, Vector3 pos)
        {
            this.RecycleAllItem();
            LoopListViewItem2 newItem = this.GetNewItemByIndex(firstItemIndex);
            if (newItem == null)
            {
                return;
            }
            if (this.mIsVertList)
            {
                pos.x = newItem.StartPosOffset;
            }
            else
            {
                pos.y = newItem.StartPosOffset;
            }
            newItem.CachedRectTransform.localPosition = pos;
            if (this.mSupportScrollBar)
            {
                if (this.mIsVertList)
                {
                    this.SetItemSize(firstItemIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                }
                else
                {
                    this.SetItemSize(firstItemIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                }
            }
            this.mItemList.Add(newItem);
            this.UpdateContentSize();
            this.UpdateAllShownItemsPos();
            this.UpdateListView(this.mDistanceForRecycle0, this.mDistanceForRecycle1, this.mDistanceForNew0, this.mDistanceForNew1);
            this.ClearAllTmpRecycledItem();
        }


        void RecycleItemTmp(LoopListViewItem2 item)
        {
            if (item == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(item.ItemPrefabName))
            {
                return;
            }
            ItemPool pool = null;
            if (this.mItemPoolDict.TryGetValue(item.ItemPrefabName, out pool) == false)
            {
                return;
            }
            pool.RecycleItem(item);

        }


        void ClearAllTmpRecycledItem()
        {
            int count = this.mItemPoolList.Count;
            for (int i = 0; i < count; ++i)
            {
                this.mItemPoolList[i].ClearTmpRecycledItem();
            }
        }


        void RecycleAllItem()
        {
            foreach (LoopListViewItem2 item in this.mItemList)
            {
                this.RecycleItemTmp(item);
            }
            this.mItemList.Clear();
        }


        void AdjustContainerPivot(RectTransform rtf)
        {
            Vector2 pivot = rtf.pivot;
            if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                pivot.y = 0;
            }
            else if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                pivot.y = 1;
            }
            else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                pivot.x = 0;
            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                pivot.x = 1;
            }
            rtf.pivot = pivot;
        }


        void AdjustPivot(RectTransform rtf)
        {
            Vector2 pivot = rtf.pivot;

            if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                pivot.y = 0;
            }
            else if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                pivot.y = 1;
            }
            else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                pivot.x = 0;
            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                pivot.x = 1;
            }
            rtf.pivot = pivot;
        }

        void AdjustContainerAnchor(RectTransform rtf)
        {
            Vector2 anchorMin = rtf.anchorMin;
            Vector2 anchorMax = rtf.anchorMax;
            if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                anchorMin.y = 0;
                anchorMax.y = 0;
            }
            else if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                anchorMin.y = 1;
                anchorMax.y = 1;
            }
            else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                anchorMin.x = 0;
                anchorMax.x = 0;
            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                anchorMin.x = 1;
                anchorMax.x = 1;
            }
            rtf.anchorMin = anchorMin;
            rtf.anchorMax = anchorMax;
        }


        void AdjustAnchor(RectTransform rtf)
        {
            Vector2 anchorMin = rtf.anchorMin;
            Vector2 anchorMax = rtf.anchorMax;
            if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                anchorMin.y = 0;
                anchorMax.y = 0;
            }
            else if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                anchorMin.y = 1;
                anchorMax.y = 1;
            }
            else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                anchorMin.x = 0;
                anchorMax.x = 0;
            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                anchorMin.x = 1;
                anchorMax.x = 1;
            }
            rtf.anchorMin = anchorMin;
            rtf.anchorMax = anchorMax;
        }

        void InitItemPool()
        {
            foreach (ItemPrefabConfData data in this.mItemPrefabDataList)
            {
                if (data.mItemPrefab == null)
                {
                    Debug.LogError("A item prefab is null ");
                    continue;
                }
                string prefabName = data.mItemPrefab.name;
                if (this.mItemPoolDict.ContainsKey(prefabName))
                {
                    Debug.LogError("A item prefab with name " + prefabName + " has existed!");
                    continue;
                }
                RectTransform rtf = data.mItemPrefab.GetComponent<RectTransform>();
                if (rtf == null)
                {
                    Debug.LogError("RectTransform component is not found in the prefab " + prefabName);
                    continue;
                }
                this.AdjustAnchor(rtf);
                this.AdjustPivot(rtf);
                LoopListViewItem2 tItem = data.mItemPrefab.GetComponent<LoopListViewItem2>();
                if (tItem == null)
                {
                    data.mItemPrefab.AddComponent<LoopListViewItem2>();
                }
                ItemPool pool = new ItemPool();
                pool.Init(data.mItemPrefab, data.mPadding, data.mStartPosOffset, data.mInitCreateCount, this.mContainerTrans);
                this.mItemPoolDict.Add(prefabName, pool);
                this.mItemPoolList.Add(pool);
            }
        }



        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            this.mIsDraging = true;
            this.CacheDragPointerEventData(eventData);
            this.mCurSnapData.Clear();
            if (this.mOnBeginDragAction != null)
            {
                this.mOnBeginDragAction();
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            this.mIsDraging = false;
            this.mPointerEventData = null;
            if (this.mOnEndDragAction != null)
            {
                this.mOnEndDragAction();
            }
            this.ForceSnapUpdateCheck();
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            this.CacheDragPointerEventData(eventData);
            if (this.mOnDragingAction != null)
            {
                this.mOnDragingAction();
            }
        }

        void CacheDragPointerEventData(PointerEventData eventData)
        {
            if (this.mPointerEventData == null)
            {
                this.mPointerEventData = new PointerEventData(EventSystem.current);
            }
            this.mPointerEventData.button = eventData.button;
            this.mPointerEventData.position = eventData.position;
            this.mPointerEventData.pointerPressRaycast = eventData.pointerPressRaycast;
            this.mPointerEventData.pointerCurrentRaycast = eventData.pointerCurrentRaycast;
        }

        LoopListViewItem2 GetNewItemByIndex(int index)
        {
            if (this.mSupportScrollBar && index < 0)
            {
                return null;
            }
            if (this.mItemTotalCount > 0 && index >= this.mItemTotalCount)
            {
                return null;
            }
            LoopListViewItem2 newItem = this.mOnGetItemByIndex(this, index);
            if (newItem == null)
            {
                return null;
            }
            newItem.ItemIndex = index;
            newItem.ItemCreatedCheckFrameCount = this.mListUpdateCheckFrameCount;
            return newItem;
        }


        void SetItemSize(int itemIndex, float itemSize, float padding)
        {
            this.mItemPosMgr.SetItemSize(itemIndex, itemSize + padding);
            if (itemIndex >= this.mLastItemIndex)
            {
                this.mLastItemIndex = itemIndex;
                this.mLastItemPadding = padding;
            }
        }

        void GetPlusItemIndexAndPosAtGivenPos(float pos, ref int index, ref float itemPos)
        {
            this.mItemPosMgr.GetItemIndexAndPosAtGivenPos(pos, ref index, ref itemPos);
        }


        float GetItemPos(int itemIndex)
        {
            return this.mItemPosMgr.GetItemPos(itemIndex);
        }


        public Vector3 GetItemCornerPosInViewPort(LoopListViewItem2 item, ItemCornerEnum corner = ItemCornerEnum.LeftBottom)
        {
            item.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
            return this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[(int)corner]);
        }


        void AdjustPanelPos()
        {
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }
            this.UpdateAllShownItemsPos();
            float viewPortSize = this.ViewPortSize;
            float contentSize = this.GetContentPanelSize();
            if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                if (contentSize <= viewPortSize)
                {
                    Vector3 pos = this.mContainerTrans.localPosition;
                    pos.y = 0;
                    this.mContainerTrans.localPosition = pos;
                    this.mItemList[0].CachedRectTransform.localPosition = new Vector3(this.mItemList[0].StartPosOffset, 0, 0);
                    this.UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 topPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                if (topPos0.y < this.mViewPortRectLocalCorners[1].y)
                {
                    Vector3 pos = this.mContainerTrans.localPosition;
                    pos.y = 0;
                    this.mContainerTrans.localPosition = pos;
                    this.mItemList[0].CachedRectTransform.localPosition = new Vector3(this.mItemList[0].StartPosOffset, 0, 0);
                    this.UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem1 = this.mItemList[this.mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 downPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[0]);
                float d = downPos1.y - this.mViewPortRectLocalCorners[0].y;
                if (d > 0)
                {
                    Vector3 pos = this.mItemList[0].CachedRectTransform.localPosition;
                    pos.y = pos.y - d;
                    this.mItemList[0].CachedRectTransform.localPosition = pos;
                    this.UpdateAllShownItemsPos();
                    return;
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                if (contentSize <= viewPortSize)
                {
                    Vector3 pos = this.mContainerTrans.localPosition;
                    pos.y = 0;
                    this.mContainerTrans.localPosition = pos;
                    this.mItemList[0].CachedRectTransform.localPosition = new Vector3(this.mItemList[0].StartPosOffset, 0, 0);
                    this.UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 downPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[0]);
                if (downPos0.y > this.mViewPortRectLocalCorners[0].y)
                {
                    Vector3 pos = this.mContainerTrans.localPosition;
                    pos.y = 0;
                    this.mContainerTrans.localPosition = pos;
                    this.mItemList[0].CachedRectTransform.localPosition = new Vector3(this.mItemList[0].StartPosOffset, 0, 0);
                    this.UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem1 = this.mItemList[this.mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 topPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                float d = this.mViewPortRectLocalCorners[1].y - topPos1.y;
                if (d > 0)
                {
                    Vector3 pos = this.mItemList[0].CachedRectTransform.localPosition;
                    pos.y = pos.y + d;
                    this.mItemList[0].CachedRectTransform.localPosition = pos;
                    this.UpdateAllShownItemsPos();
                    return;
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                if (contentSize <= viewPortSize)
                {
                    Vector3 pos = this.mContainerTrans.localPosition;
                    pos.x = 0;
                    this.mContainerTrans.localPosition = pos;
                    this.mItemList[0].CachedRectTransform.localPosition = new Vector3(0, this.mItemList[0].StartPosOffset, 0);
                    this.UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 leftPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                if (leftPos0.x > this.mViewPortRectLocalCorners[1].x)
                {
                    Vector3 pos = this.mContainerTrans.localPosition;
                    pos.x = 0;
                    this.mContainerTrans.localPosition = pos;
                    this.mItemList[0].CachedRectTransform.localPosition = new Vector3(0, this.mItemList[0].StartPosOffset, 0);
                    this.UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem1 = this.mItemList[this.mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 rightPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[2]);
                float d = this.mViewPortRectLocalCorners[2].x - rightPos1.x;
                if (d > 0)
                {
                    Vector3 pos = this.mItemList[0].CachedRectTransform.localPosition;
                    pos.x = pos.x + d;
                    this.mItemList[0].CachedRectTransform.localPosition = pos;
                    this.UpdateAllShownItemsPos();
                    return;
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                if (contentSize <= viewPortSize)
                {
                    Vector3 pos = this.mContainerTrans.localPosition;
                    pos.x = 0;
                    this.mContainerTrans.localPosition = pos;
                    this.mItemList[0].CachedRectTransform.localPosition = new Vector3(0, this.mItemList[0].StartPosOffset, 0);
                    this.UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 rightPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[2]);
                if (rightPos0.x < this.mViewPortRectLocalCorners[2].x)
                {
                    Vector3 pos = this.mContainerTrans.localPosition;
                    pos.x = 0;
                    this.mContainerTrans.localPosition = pos;
                    this.mItemList[0].CachedRectTransform.localPosition = new Vector3(0, this.mItemList[0].StartPosOffset, 0);
                    this.UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem1 = this.mItemList[this.mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 leftPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                float d = leftPos1.x - this.mViewPortRectLocalCorners[1].x;
                if (d > 0)
                {
                    Vector3 pos = this.mItemList[0].CachedRectTransform.localPosition;
                    pos.x = pos.x - d;
                    this.mItemList[0].CachedRectTransform.localPosition = pos;
                    this.UpdateAllShownItemsPos();
                    return;
                }
            }



        }


        void Update()
        {
            if (this.mListViewInited == false)
            {
                return;
            }
            if (this.mNeedAdjustVec)
            {
                this.mNeedAdjustVec = false;
                if (this.mIsVertList)
                {
                    if (this.mScrollRect.velocity.y * this.mAdjustedVec.y > 0)
                    {
                        this.mScrollRect.velocity = this.mAdjustedVec;
                    }
                }
                else
                {
                    if (this.mScrollRect.velocity.x * this.mAdjustedVec.x > 0)
                    {
                        this.mScrollRect.velocity = this.mAdjustedVec;
                    }
                }

            }
            if (this.mSupportScrollBar)
            {
                this.mItemPosMgr.Update(false);
            }
            this.UpdateSnapMove();
            this.UpdateListView(this.mDistanceForRecycle0, this.mDistanceForRecycle1, this.mDistanceForNew0, this.mDistanceForNew1);
            this.ClearAllTmpRecycledItem();
            this.mLastFrameContainerPos = this.mContainerTrans.localPosition;
        }

        //update snap move. if immediate is set true, then the snap move will finish at once.
        void UpdateSnapMove(bool immediate = false)
        {
            if (this.mItemSnapEnable == false)
            {
                return;
            }
            if (this.mIsVertList)
            {
                this.UpdateSnapVertical(immediate);
            }
            else
            {
                this.UpdateSnapHorizontal(immediate);
            }
        }



        public void UpdateAllShownItemSnapData()
        {
            if (this.mItemSnapEnable == false)
            {
                return;
            }
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }
            LoopListViewItem2 tViewItem0 = this.mItemList[0];
            tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
            float start = 0;
            float end = 0;
            float itemSnapCenter = 0;
            float snapCenter = 0;
            if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                snapCenter = -(1 - this.mViewPortSnapPivot.y) * this.mViewPortRectTransform.rect.height;
                Vector3 topPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                start = topPos1.y;
                end = start - tViewItem0.ItemSizeWithPadding;
                itemSnapCenter = start - tViewItem0.ItemSize * (1 - this.mItemSnapPivot.y);
                for (int i = 0; i < count; ++i)
                {
                    this.mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
                    if ((i + 1) < count)
                    {
                        start = end;
                        end = end - this.mItemList[i + 1].ItemSizeWithPadding;
                        itemSnapCenter = start - this.mItemList[i + 1].ItemSize * (1 - this.mItemSnapPivot.y);
                    }
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                snapCenter = this.mViewPortSnapPivot.y * this.mViewPortRectTransform.rect.height;
                Vector3 bottomPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[0]);
                start = bottomPos1.y;
                end = start + tViewItem0.ItemSizeWithPadding;
                itemSnapCenter = start + tViewItem0.ItemSize * this.mItemSnapPivot.y;
                for (int i = 0; i < count; ++i)
                {
                    this.mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
                    if ((i + 1) < count)
                    {
                        start = end;
                        end = end + this.mItemList[i + 1].ItemSizeWithPadding;
                        itemSnapCenter = start + this.mItemList[i + 1].ItemSize * this.mItemSnapPivot.y;
                    }
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                snapCenter = -(1 - this.mViewPortSnapPivot.x) * this.mViewPortRectTransform.rect.width;
                Vector3 rightPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[2]);
                start = rightPos1.x;
                end = start - tViewItem0.ItemSizeWithPadding;
                itemSnapCenter = start - tViewItem0.ItemSize * (1 - this.mItemSnapPivot.x);
                for (int i = 0; i < count; ++i)
                {
                    this.mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
                    if ((i + 1) < count)
                    {
                        start = end;
                        end = end - this.mItemList[i + 1].ItemSizeWithPadding;
                        itemSnapCenter = start - this.mItemList[i + 1].ItemSize * (1 - this.mItemSnapPivot.x);
                    }
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                snapCenter = this.mViewPortSnapPivot.x * this.mViewPortRectTransform.rect.width;
                Vector3 leftPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                start = leftPos1.x;
                end = start + tViewItem0.ItemSizeWithPadding;
                itemSnapCenter = start + tViewItem0.ItemSize * this.mItemSnapPivot.x;
                for (int i = 0; i < count; ++i)
                {
                    this.mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
                    if ((i + 1) < count)
                    {
                        start = end;
                        end = end + this.mItemList[i + 1].ItemSizeWithPadding;
                        itemSnapCenter = start + this.mItemList[i + 1].ItemSize * this.mItemSnapPivot.x;
                    }
                }
            }
        }



        void UpdateSnapVertical(bool immediate = false)
        {
            if (this.mItemSnapEnable == false)
            {
                return;
            }
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }
            Vector3 pos = this.mContainerTrans.localPosition;
            bool needCheck = (pos.y != this.mLastSnapCheckPos.y);
            this.mLastSnapCheckPos = pos;
            if (!needCheck)
            {
                if (this.mLeftSnapUpdateExtraCount > 0)
                {
                    this.mLeftSnapUpdateExtraCount--;
                    needCheck = true;
                }
            }
            if (needCheck)
            {
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                int curIndex = -1;
                float start = 0;
                float end = 0;
                float itemSnapCenter = 0;
                float curMinDist = float.MaxValue;
                float curDist = 0;
                float curDistAbs = 0;
                float snapCenter = 0;
                if (this.mArrangeType == ListItemArrangeType.TopToBottom)
                {
                    snapCenter = -(1 - this.mViewPortSnapPivot.y) * this.mViewPortRectTransform.rect.height;
                    Vector3 topPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                    start = topPos1.y;
                    end = start - tViewItem0.ItemSizeWithPadding;
                    itemSnapCenter = start - tViewItem0.ItemSize * (1 - this.mItemSnapPivot.y);
                    for (int i = 0; i < count; ++i)
                    {
                        curDist = snapCenter - itemSnapCenter;
                        curDistAbs = Mathf.Abs(curDist);
                        if (curDistAbs < curMinDist)
                        {
                            curMinDist = curDistAbs;
                            curIndex = i;
                        }
                        else
                        {
                            break;
                        }

                        if ((i + 1) < count)
                        {
                            start = end;
                            end = end - this.mItemList[i + 1].ItemSizeWithPadding;
                            itemSnapCenter = start - this.mItemList[i + 1].ItemSize * (1 - this.mItemSnapPivot.y);
                        }
                    }
                }
                else if (this.mArrangeType == ListItemArrangeType.BottomToTop)
                {
                    snapCenter = this.mViewPortSnapPivot.y * this.mViewPortRectTransform.rect.height;
                    Vector3 bottomPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[0]);
                    start = bottomPos1.y;
                    end = start + tViewItem0.ItemSizeWithPadding;
                    itemSnapCenter = start + tViewItem0.ItemSize * this.mItemSnapPivot.y;
                    for (int i = 0; i < count; ++i)
                    {
                        curDist = snapCenter - itemSnapCenter;
                        curDistAbs = Mathf.Abs(curDist);
                        if (curDistAbs < curMinDist)
                        {
                            curMinDist = curDistAbs;
                            curIndex = i;
                        }
                        else
                        {
                            break;
                        }

                        if ((i + 1) < count)
                        {
                            start = end;
                            end = end + this.mItemList[i + 1].ItemSizeWithPadding;
                            itemSnapCenter = start + this.mItemList[i + 1].ItemSize * this.mItemSnapPivot.y;
                        }
                    }
                }

                if (curIndex >= 0)
                {
                    int oldNearestItemIndex = this.mCurSnapNearestItemIndex;
                    this.mCurSnapNearestItemIndex = this.mItemList[curIndex].ItemIndex;
                    if (this.mItemList[curIndex].ItemIndex != oldNearestItemIndex)
                    {
                        if (this.mOnSnapNearestChanged != null)
                        {
                            this.mOnSnapNearestChanged(this, this.mItemList[curIndex]);
                        }
                    }
                }
                else
                {
                    this.mCurSnapNearestItemIndex = -1;
                }
            }
            if (this.CanSnap() == false)
            {
                this.ClearSnapData();
                return;
            }
            float v = Mathf.Abs(this.mScrollRect.velocity.y);
            this.UpdateCurSnapData();
            if (this.mCurSnapData.mSnapStatus != SnapStatus.SnapMoving)
            {
                return;
            }
            if (v > 0)
            {
                this.mScrollRect.StopMovement();
            }
            float old = this.mCurSnapData.mCurSnapVal;
            this.mCurSnapData.mCurSnapVal = Mathf.SmoothDamp(this.mCurSnapData.mCurSnapVal, this.mCurSnapData.mTargetSnapVal, ref this.mSmoothDumpVel, this.mSmoothDumpRate);
            float dt = this.mCurSnapData.mCurSnapVal - old;

            if (immediate || Mathf.Abs(this.mCurSnapData.mTargetSnapVal - this.mCurSnapData.mCurSnapVal) < this.mSnapFinishThreshold)
            {
                pos.y = pos.y + this.mCurSnapData.mTargetSnapVal - old;
                this.mCurSnapData.mSnapStatus = SnapStatus.SnapMoveFinish;
                if (this.mOnSnapItemFinished != null)
                {
                    LoopListViewItem2 targetItem = this.GetShownItemByItemIndex(this.mCurSnapNearestItemIndex);
                    if (targetItem != null)
                    {
                        this.mOnSnapItemFinished(this, targetItem);
                    }
                }
            }
            else
            {
                pos.y = pos.y + dt;
            }

            if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float maxY = this.mViewPortRectLocalCorners[0].y + this.mContainerTrans.rect.height;
                pos.y = Mathf.Clamp(pos.y, 0, maxY);
                this.mContainerTrans.localPosition = pos;
            }
            else if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                float minY = this.mViewPortRectLocalCorners[1].y - this.mContainerTrans.rect.height;
                pos.y = Mathf.Clamp(pos.y, minY, 0);
                this.mContainerTrans.localPosition = pos;
            }

        }


        void UpdateCurSnapData()
        {
            int count = this.mItemList.Count;
            if (count == 0)
            {
                this.mCurSnapData.Clear();
                return;
            }

            if (this.mCurSnapData.mSnapStatus == SnapStatus.SnapMoveFinish)
            {
                if (this.mCurSnapData.mSnapTargetIndex == this.mCurSnapNearestItemIndex)
                {
                    return;
                }
                this.mCurSnapData.mSnapStatus = SnapStatus.NoTargetSet;
            }
            if (this.mCurSnapData.mSnapStatus == SnapStatus.SnapMoving)
            {
                if ((this.mCurSnapData.mSnapTargetIndex == this.mCurSnapNearestItemIndex) || this.mCurSnapData.mIsForceSnapTo)
                {
                    return;
                }
                this.mCurSnapData.mSnapStatus = SnapStatus.NoTargetSet;
            }
            if (this.mCurSnapData.mSnapStatus == SnapStatus.NoTargetSet)
            {
                LoopListViewItem2 nearestItem = this.GetShownItemByItemIndex(this.mCurSnapNearestItemIndex);
                if (nearestItem == null)
                {
                    return;
                }
                this.mCurSnapData.mSnapTargetIndex = this.mCurSnapNearestItemIndex;
                this.mCurSnapData.mSnapStatus = SnapStatus.TargetHasSet;
                this.mCurSnapData.mIsForceSnapTo = false;
            }
            if (this.mCurSnapData.mSnapStatus == SnapStatus.TargetHasSet)
            {
                LoopListViewItem2 targetItem = this.GetShownItemByItemIndex(this.mCurSnapData.mSnapTargetIndex);
                if (targetItem == null)
                {
                    this.mCurSnapData.Clear();
                    return;
                }
                this.UpdateAllShownItemSnapData();
                this.mCurSnapData.mTargetSnapVal = targetItem.DistanceWithViewPortSnapCenter;
                this.mCurSnapData.mCurSnapVal = 0;
                this.mCurSnapData.mSnapStatus = SnapStatus.SnapMoving;
            }

        }

        //Clear current snap target and then the LoopScrollView2 will auto snap to the CurSnapNearestItemIndex.
        public void ClearSnapData()
        {
            this.mCurSnapData.Clear();
        }

        public void SetSnapTargetItemIndex(int itemIndex)
        {
            this.mCurSnapData.mSnapTargetIndex = itemIndex;
            this.mCurSnapData.mSnapStatus = SnapStatus.TargetHasSet;
            this.mCurSnapData.mIsForceSnapTo = true;
        }

        //Get the nearest item index with the viewport snap point.
        public int CurSnapNearestItemIndex { get { return this.mCurSnapNearestItemIndex; } }

        public void ForceSnapUpdateCheck()
        {
            if (this.mLeftSnapUpdateExtraCount <= 0)
            {
                this.mLeftSnapUpdateExtraCount = 1;
            }
        }

        void UpdateSnapHorizontal(bool immediate = false)
        {
            if (this.mItemSnapEnable == false)
            {
                return;
            }
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }
            Vector3 pos = this.mContainerTrans.localPosition;
            bool needCheck = (pos.x != this.mLastSnapCheckPos.x);
            this.mLastSnapCheckPos = pos;
            if (!needCheck)
            {
                if (this.mLeftSnapUpdateExtraCount > 0)
                {
                    this.mLeftSnapUpdateExtraCount--;
                    needCheck = true;
                }
            }
            if (needCheck)
            {
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                int curIndex = -1;
                float start = 0;
                float end = 0;
                float itemSnapCenter = 0;
                float curMinDist = float.MaxValue;
                float curDist = 0;
                float curDistAbs = 0;
                float snapCenter = 0;
                if (this.mArrangeType == ListItemArrangeType.RightToLeft)
                {
                    snapCenter = -(1 - this.mViewPortSnapPivot.x) * this.mViewPortRectTransform.rect.width;
                    Vector3 rightPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[2]);
                    start = rightPos1.x;
                    end = start - tViewItem0.ItemSizeWithPadding;
                    itemSnapCenter = start - tViewItem0.ItemSize * (1 - this.mItemSnapPivot.x);
                    for (int i = 0; i < count; ++i)
                    {
                        curDist = snapCenter - itemSnapCenter;
                        curDistAbs = Mathf.Abs(curDist);
                        if (curDistAbs < curMinDist)
                        {
                            curMinDist = curDistAbs;
                            curIndex = i;
                        }
                        else
                        {
                            break;
                        }

                        if ((i + 1) < count)
                        {
                            start = end;
                            end = end - this.mItemList[i + 1].ItemSizeWithPadding;
                            itemSnapCenter = start - this.mItemList[i + 1].ItemSize * (1 - this.mItemSnapPivot.x);
                        }
                    }
                }
                else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
                {
                    snapCenter = this.mViewPortSnapPivot.x * this.mViewPortRectTransform.rect.width;
                    Vector3 leftPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                    start = leftPos1.x;
                    end = start + tViewItem0.ItemSizeWithPadding;
                    itemSnapCenter = start + tViewItem0.ItemSize * this.mItemSnapPivot.x;
                    for (int i = 0; i < count; ++i)
                    {
                        curDist = snapCenter - itemSnapCenter;
                        curDistAbs = Mathf.Abs(curDist);
                        if (curDistAbs < curMinDist)
                        {
                            curMinDist = curDistAbs;
                            curIndex = i;
                        }
                        else
                        {
                            break;
                        }

                        if ((i + 1) < count)
                        {
                            start = end;
                            end = end + this.mItemList[i + 1].ItemSizeWithPadding;
                            itemSnapCenter = start + this.mItemList[i + 1].ItemSize * this.mItemSnapPivot.x;
                        }
                    }
                }


                if (curIndex >= 0)
                {
                    int oldNearestItemIndex = this.mCurSnapNearestItemIndex;
                    this.mCurSnapNearestItemIndex = this.mItemList[curIndex].ItemIndex;
                    if (this.mItemList[curIndex].ItemIndex != oldNearestItemIndex)
                    {
                        if (this.mOnSnapNearestChanged != null)
                        {
                            this.mOnSnapNearestChanged(this, this.mItemList[curIndex]);
                        }
                    }
                }
                else
                {
                    this.mCurSnapNearestItemIndex = -1;
                }
            }
            if (this.CanSnap() == false)
            {
                this.ClearSnapData();
                return;
            }
            float v = Mathf.Abs(this.mScrollRect.velocity.x);
            this.UpdateCurSnapData();
            if (this.mCurSnapData.mSnapStatus != SnapStatus.SnapMoving)
            {
                return;
            }
            if (v > 0)
            {
                this.mScrollRect.StopMovement();
            }
            float old = this.mCurSnapData.mCurSnapVal;
            this.mCurSnapData.mCurSnapVal = Mathf.SmoothDamp(this.mCurSnapData.mCurSnapVal, this.mCurSnapData.mTargetSnapVal, ref this.mSmoothDumpVel, this.mSmoothDumpRate);
            float dt = this.mCurSnapData.mCurSnapVal - old;

            if (immediate || Mathf.Abs(this.mCurSnapData.mTargetSnapVal - this.mCurSnapData.mCurSnapVal) < this.mSnapFinishThreshold)
            {
                pos.x = pos.x + this.mCurSnapData.mTargetSnapVal - old;
                this.mCurSnapData.mSnapStatus = SnapStatus.SnapMoveFinish;
                if (this.mOnSnapItemFinished != null)
                {
                    LoopListViewItem2 targetItem = this.GetShownItemByItemIndex(this.mCurSnapNearestItemIndex);
                    if (targetItem != null)
                    {
                        this.mOnSnapItemFinished(this, targetItem);
                    }
                }
            }
            else
            {
                pos.x = pos.x + dt;
            }

            if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                float minX = this.mViewPortRectLocalCorners[2].x - this.mContainerTrans.rect.width;
                pos.x = Mathf.Clamp(pos.x, minX, 0);
                this.mContainerTrans.localPosition = pos;
            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                float maxX = this.mViewPortRectLocalCorners[1].x + this.mContainerTrans.rect.width;
                pos.x = Mathf.Clamp(pos.x, 0, maxX);
                this.mContainerTrans.localPosition = pos;
            }
        }

        bool CanSnap()
        {
            if (this.mIsDraging)
            {
                return false;
            }
            if (this.mScrollBarClickEventListener != null)
            {
                if (this.mScrollBarClickEventListener.IsPressd)
                {
                    return false;
                }
            }

            if (this.mIsVertList)
            {
                if (this.mContainerTrans.rect.height <= this.ViewPortHeight)
                {
                    return false;
                }
            }
            else
            {
                if (this.mContainerTrans.rect.width <= this.ViewPortWidth)
                {
                    return false;
                }
            }

            float v = 0;
            if (this.mIsVertList)
            {
                v = Mathf.Abs(this.mScrollRect.velocity.y);
            }
            else
            {
                v = Mathf.Abs(this.mScrollRect.velocity.x);
            }
            if (v > this.mSnapVecThreshold)
            {
                return false;
            }
            if (v < 2)
            {
                return true;
            }
            float diff = 3;
            Vector3 pos = this.mContainerTrans.localPosition;
            if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                float minX = this.mViewPortRectLocalCorners[2].x - this.mContainerTrans.rect.width;
                if (pos.x < (minX - diff) || pos.x > diff)
                {
                    return false;
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                float maxX = this.mViewPortRectLocalCorners[1].x + this.mContainerTrans.rect.width;
                if (pos.x > (maxX + diff) || pos.x < -diff)
                {
                    return false;
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float maxY = this.mViewPortRectLocalCorners[0].y + this.mContainerTrans.rect.height;
                if (pos.y > (maxY + diff) || pos.y < -diff)
                {
                    return false;
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                float minY = this.mViewPortRectLocalCorners[1].y - this.mContainerTrans.rect.height;
                if (pos.y < (minY - diff) || pos.y > diff)
                {
                    return false;
                }
            }
            return true;
        }



        public void UpdateListView(float distanceForRecycle0, float distanceForRecycle1, float distanceForNew0, float distanceForNew1)
        {
            this.mListUpdateCheckFrameCount++;
            if (this.mIsVertList)
            {
                bool needContinueCheck = true;
                int checkCount = 0;
                int maxCount = 9999;
                while (needContinueCheck)
                {
                    checkCount++;
                    if (checkCount >= maxCount)
                    {
                        Debug.LogError("UpdateListView Vertical while loop " + checkCount + " times! something is wrong!");
                        break;
                    }
                    needContinueCheck = this.UpdateForVertList(distanceForRecycle0, distanceForRecycle1, distanceForNew0, distanceForNew1);
                }
            }
            else
            {
                bool needContinueCheck = true;
                int checkCount = 0;
                int maxCount = 9999;
                while (needContinueCheck)
                {
                    checkCount++;
                    if (checkCount >= maxCount)
                    {
                        Debug.LogError("UpdateListView  Horizontal while loop " + checkCount + " times! something is wrong!");
                        break;
                    }
                    needContinueCheck = this.UpdateForHorizontalList(distanceForRecycle0, distanceForRecycle1, distanceForNew0, distanceForNew1);
                }
            }

        }



        bool UpdateForVertList(float distanceForRecycle0, float distanceForRecycle1, float distanceForNew0, float distanceForNew1)
        {
            if (this.mItemTotalCount == 0)
            {
                if (this.mItemList.Count > 0)
                {
                    this.RecycleAllItem();
                }
                return false;
            }
            if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                int itemListCount = this.mItemList.Count;
                if (itemListCount == 0)
                {
                    float curY = this.mContainerTrans.localPosition.y;
                    if (curY < 0)
                    {
                        curY = 0;
                    }
                    int index = 0;
                    float pos = -curY;
                    if (this.mSupportScrollBar)
                    {
                        this.GetPlusItemIndexAndPosAtGivenPos(curY, ref index, ref pos);
                        pos = -pos;
                    }
                    LoopListViewItem2 newItem = this.GetNewItemByIndex(index);
                    if (newItem == null)
                    {
                        return false;
                    }
                    if (this.mSupportScrollBar)
                    {
                        this.SetItemSize(index, newItem.CachedRectTransform.rect.height, newItem.Padding);
                    }
                    this.mItemList.Add(newItem);
                    newItem.CachedRectTransform.localPosition = new Vector3(newItem.StartPosOffset, pos, 0);
                    this.UpdateContentSize();
                    return true;
                }
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 topPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                Vector3 downPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[0]);

                if (!this.mIsDraging && tViewItem0.ItemCreatedCheckFrameCount != this.mListUpdateCheckFrameCount
                    && downPos0.y - this.mViewPortRectLocalCorners[1].y > distanceForRecycle0)
                {
                    this.mItemList.RemoveAt(0);
                    this.RecycleItemTmp(tViewItem0);
                    if (!this.mSupportScrollBar)
                    {
                        this.UpdateContentSize();
                        this.CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }

                LoopListViewItem2 tViewItem1 = this.mItemList[this.mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 topPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                Vector3 downPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[0]);
                if (!this.mIsDraging && tViewItem1.ItemCreatedCheckFrameCount != this.mListUpdateCheckFrameCount
                    && this.mViewPortRectLocalCorners[0].y - topPos1.y > distanceForRecycle1)
                {
                    this.mItemList.RemoveAt(this.mItemList.Count - 1);
                    this.RecycleItemTmp(tViewItem1);
                    if (!this.mSupportScrollBar)
                    {
                        this.UpdateContentSize();
                        this.CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }



                if (this.mViewPortRectLocalCorners[0].y - downPos1.y < distanceForNew1)
                {
                    if (tViewItem1.ItemIndex > this.mCurReadyMaxItemIndex)
                    {
                        this.mCurReadyMaxItemIndex = tViewItem1.ItemIndex;
                        this.mNeedCheckNextMaxItem = true;
                    }
                    int nIndex = tViewItem1.ItemIndex + 1;
                    if (nIndex <= this.mCurReadyMaxItemIndex || this.mNeedCheckNextMaxItem)
                    {
                        LoopListViewItem2 newItem = this.GetNewItemByIndex(nIndex);
                        if (newItem == null)
                        {
                            this.mCurReadyMaxItemIndex = tViewItem1.ItemIndex;
                            this.mNeedCheckNextMaxItem = false;
                            this.CheckIfNeedUpdataItemPos();
                        }
                        else
                        {
                            if (this.mSupportScrollBar)
                            {
                                this.SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            this.mItemList.Add(newItem);
                            float y = tViewItem1.CachedRectTransform.localPosition.y - tViewItem1.CachedRectTransform.rect.height - tViewItem1.Padding;
                            newItem.CachedRectTransform.localPosition = new Vector3(newItem.StartPosOffset, y, 0);
                            this.UpdateContentSize();
                            this.CheckIfNeedUpdataItemPos();

                            if (nIndex > this.mCurReadyMaxItemIndex)
                            {
                                this.mCurReadyMaxItemIndex = nIndex;
                            }
                            return true;
                        }

                    }

                }

                if (topPos0.y - this.mViewPortRectLocalCorners[1].y < distanceForNew0)
                {
                    if (tViewItem0.ItemIndex < this.mCurReadyMinItemIndex)
                    {
                        this.mCurReadyMinItemIndex = tViewItem0.ItemIndex;
                        this.mNeedCheckNextMinItem = true;
                    }
                    int nIndex = tViewItem0.ItemIndex - 1;
                    if (nIndex >= this.mCurReadyMinItemIndex || this.mNeedCheckNextMinItem)
                    {
                        LoopListViewItem2 newItem = this.GetNewItemByIndex(nIndex);
                        if (newItem == null)
                        {
                            this.mCurReadyMinItemIndex = tViewItem0.ItemIndex;
                            this.mNeedCheckNextMinItem = false;
                        }
                        else
                        {
                            if (this.mSupportScrollBar)
                            {
                                this.SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            this.mItemList.Insert(0, newItem);
                            float y = tViewItem0.CachedRectTransform.localPosition.y + newItem.CachedRectTransform.rect.height + newItem.Padding;
                            newItem.CachedRectTransform.localPosition = new Vector3(newItem.StartPosOffset, y, 0);
                            this.UpdateContentSize();
                            this.CheckIfNeedUpdataItemPos();
                            if (nIndex < this.mCurReadyMinItemIndex)
                            {
                                this.mCurReadyMinItemIndex = nIndex;
                            }
                            return true;
                        }

                    }

                }

            }
            else
            {

                if (this.mItemList.Count == 0)
                {
                    float curY = this.mContainerTrans.localPosition.y;
                    if (curY > 0)
                    {
                        curY = 0;
                    }
                    int index = 0;
                    float pos = -curY;
                    if (this.mSupportScrollBar)
                    {
                        this.GetPlusItemIndexAndPosAtGivenPos(-curY, ref index, ref pos);
                    }
                    LoopListViewItem2 newItem = this.GetNewItemByIndex(index);
                    if (newItem == null)
                    {
                        return false;
                    }
                    if (this.mSupportScrollBar)
                    {
                        this.SetItemSize(index, newItem.CachedRectTransform.rect.height, newItem.Padding);
                    }
                    this.mItemList.Add(newItem);
                    newItem.CachedRectTransform.localPosition = new Vector3(newItem.StartPosOffset, pos, 0);
                    this.UpdateContentSize();
                    return true;
                }
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 topPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                Vector3 downPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[0]);

                if (!this.mIsDraging && tViewItem0.ItemCreatedCheckFrameCount != this.mListUpdateCheckFrameCount
                    && this.mViewPortRectLocalCorners[0].y - topPos0.y > distanceForRecycle0)
                {
                    this.mItemList.RemoveAt(0);
                    this.RecycleItemTmp(tViewItem0);
                    if (!this.mSupportScrollBar)
                    {
                        this.UpdateContentSize();
                        this.CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }

                LoopListViewItem2 tViewItem1 = this.mItemList[this.mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 topPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                Vector3 downPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[0]);
                if (!this.mIsDraging && tViewItem1.ItemCreatedCheckFrameCount != this.mListUpdateCheckFrameCount
                    && downPos1.y - this.mViewPortRectLocalCorners[1].y > distanceForRecycle1)
                {
                    this.mItemList.RemoveAt(this.mItemList.Count - 1);
                    this.RecycleItemTmp(tViewItem1);
                    if (!this.mSupportScrollBar)
                    {
                        this.UpdateContentSize();
                        this.CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }

                if (topPos1.y - this.mViewPortRectLocalCorners[1].y < distanceForNew1)
                {
                    if (tViewItem1.ItemIndex > this.mCurReadyMaxItemIndex)
                    {
                        this.mCurReadyMaxItemIndex = tViewItem1.ItemIndex;
                        this.mNeedCheckNextMaxItem = true;
                    }
                    int nIndex = tViewItem1.ItemIndex + 1;
                    if (nIndex <= this.mCurReadyMaxItemIndex || this.mNeedCheckNextMaxItem)
                    {
                        LoopListViewItem2 newItem = this.GetNewItemByIndex(nIndex);
                        if (newItem == null)
                        {
                            this.mNeedCheckNextMaxItem = false;
                            this.CheckIfNeedUpdataItemPos();
                        }
                        else
                        {
                            if (this.mSupportScrollBar)
                            {
                                this.SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            this.mItemList.Add(newItem);
                            float y = tViewItem1.CachedRectTransform.localPosition.y + tViewItem1.CachedRectTransform.rect.height + tViewItem1.Padding;
                            newItem.CachedRectTransform.localPosition = new Vector3(newItem.StartPosOffset, y, 0);
                            this.UpdateContentSize();
                            this.CheckIfNeedUpdataItemPos();
                            if (nIndex > this.mCurReadyMaxItemIndex)
                            {
                                this.mCurReadyMaxItemIndex = nIndex;
                            }
                            return true;
                        }

                    }

                }


                if (this.mViewPortRectLocalCorners[0].y - downPos0.y < distanceForNew0)
                {
                    if (tViewItem0.ItemIndex < this.mCurReadyMinItemIndex)
                    {
                        this.mCurReadyMinItemIndex = tViewItem0.ItemIndex;
                        this.mNeedCheckNextMinItem = true;
                    }
                    int nIndex = tViewItem0.ItemIndex - 1;
                    if (nIndex >= this.mCurReadyMinItemIndex || this.mNeedCheckNextMinItem)
                    {
                        LoopListViewItem2 newItem = this.GetNewItemByIndex(nIndex);
                        if (newItem == null)
                        {
                            this.mNeedCheckNextMinItem = false;
                            return false;
                        }
                        else
                        {
                            if (this.mSupportScrollBar)
                            {
                                this.SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            this.mItemList.Insert(0, newItem);
                            float y = tViewItem0.CachedRectTransform.localPosition.y - newItem.CachedRectTransform.rect.height - newItem.Padding;
                            newItem.CachedRectTransform.localPosition = new Vector3(newItem.StartPosOffset, y, 0);
                            this.UpdateContentSize();
                            this.CheckIfNeedUpdataItemPos();
                            if (nIndex < this.mCurReadyMinItemIndex)
                            {
                                this.mCurReadyMinItemIndex = nIndex;
                            }
                            return true;
                        }

                    }
                }


            }

            return false;

        }





        bool UpdateForHorizontalList(float distanceForRecycle0, float distanceForRecycle1, float distanceForNew0, float distanceForNew1)
        {
            if (this.mItemTotalCount == 0)
            {
                if (this.mItemList.Count > 0)
                {
                    this.RecycleAllItem();
                }
                return false;
            }
            if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {

                if (this.mItemList.Count == 0)
                {
                    float curX = this.mContainerTrans.localPosition.x;
                    if (curX > 0)
                    {
                        curX = 0;
                    }
                    int index = 0;
                    float pos = -curX;
                    if (this.mSupportScrollBar)
                    {
                        this.GetPlusItemIndexAndPosAtGivenPos(-curX, ref index, ref pos);
                    }
                    LoopListViewItem2 newItem = this.GetNewItemByIndex(index);
                    if (newItem == null)
                    {
                        return false;
                    }
                    if (this.mSupportScrollBar)
                    {
                        this.SetItemSize(index, newItem.CachedRectTransform.rect.width, newItem.Padding);
                    }
                    this.mItemList.Add(newItem);
                    newItem.CachedRectTransform.localPosition = new Vector3(pos, newItem.StartPosOffset, 0);
                    this.UpdateContentSize();
                    return true;
                }
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 leftPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                Vector3 rightPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[2]);

                if (!this.mIsDraging && tViewItem0.ItemCreatedCheckFrameCount != this.mListUpdateCheckFrameCount
                    && this.mViewPortRectLocalCorners[1].x - rightPos0.x > distanceForRecycle0)
                {
                    this.mItemList.RemoveAt(0);
                    this.RecycleItemTmp(tViewItem0);
                    if (!this.mSupportScrollBar)
                    {
                        this.UpdateContentSize();
                        this.CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }

                LoopListViewItem2 tViewItem1 = this.mItemList[this.mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 leftPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                Vector3 rightPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[2]);
                if (!this.mIsDraging && tViewItem1.ItemCreatedCheckFrameCount != this.mListUpdateCheckFrameCount
                    && leftPos1.x - this.mViewPortRectLocalCorners[2].x > distanceForRecycle1)
                {
                    this.mItemList.RemoveAt(this.mItemList.Count - 1);
                    this.RecycleItemTmp(tViewItem1);
                    if (!this.mSupportScrollBar)
                    {
                        this.UpdateContentSize();
                        this.CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }



                if (rightPos1.x - this.mViewPortRectLocalCorners[2].x < distanceForNew1)
                {
                    if (tViewItem1.ItemIndex > this.mCurReadyMaxItemIndex)
                    {
                        this.mCurReadyMaxItemIndex = tViewItem1.ItemIndex;
                        this.mNeedCheckNextMaxItem = true;
                    }
                    int nIndex = tViewItem1.ItemIndex + 1;
                    if (nIndex <= this.mCurReadyMaxItemIndex || this.mNeedCheckNextMaxItem)
                    {
                        LoopListViewItem2 newItem = this.GetNewItemByIndex(nIndex);
                        if (newItem == null)
                        {
                            this.mCurReadyMaxItemIndex = tViewItem1.ItemIndex;
                            this.mNeedCheckNextMaxItem = false;
                            this.CheckIfNeedUpdataItemPos();
                        }
                        else
                        {
                            if (this.mSupportScrollBar)
                            {
                                this.SetItemSize(nIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                            }
                            this.mItemList.Add(newItem);
                            float x = tViewItem1.CachedRectTransform.localPosition.x + tViewItem1.CachedRectTransform.rect.width + tViewItem1.Padding;
                            newItem.CachedRectTransform.localPosition = new Vector3(x, newItem.StartPosOffset, 0);
                            this.UpdateContentSize();
                            this.CheckIfNeedUpdataItemPos();

                            if (nIndex > this.mCurReadyMaxItemIndex)
                            {
                                this.mCurReadyMaxItemIndex = nIndex;
                            }
                            return true;
                        }

                    }

                }

                if (this.mViewPortRectLocalCorners[1].x - leftPos0.x < distanceForNew0)
                {
                    if (tViewItem0.ItemIndex < this.mCurReadyMinItemIndex)
                    {
                        this.mCurReadyMinItemIndex = tViewItem0.ItemIndex;
                        this.mNeedCheckNextMinItem = true;
                    }
                    int nIndex = tViewItem0.ItemIndex - 1;
                    if (nIndex >= this.mCurReadyMinItemIndex || this.mNeedCheckNextMinItem)
                    {
                        LoopListViewItem2 newItem = this.GetNewItemByIndex(nIndex);
                        if (newItem == null)
                        {
                            this.mCurReadyMinItemIndex = tViewItem0.ItemIndex;
                            this.mNeedCheckNextMinItem = false;
                        }
                        else
                        {
                            if (this.mSupportScrollBar)
                            {
                                this.SetItemSize(nIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                            }
                            this.mItemList.Insert(0, newItem);
                            float x = tViewItem0.CachedRectTransform.localPosition.x - newItem.CachedRectTransform.rect.width - newItem.Padding;
                            newItem.CachedRectTransform.localPosition = new Vector3(x, newItem.StartPosOffset, 0);
                            this.UpdateContentSize();
                            this.CheckIfNeedUpdataItemPos();
                            if (nIndex < this.mCurReadyMinItemIndex)
                            {
                                this.mCurReadyMinItemIndex = nIndex;
                            }
                            return true;
                        }

                    }

                }

            }
            else
            {

                if (this.mItemList.Count == 0)
                {
                    float curX = this.mContainerTrans.localPosition.x;
                    if (curX < 0)
                    {
                        curX = 0;
                    }
                    int index = 0;
                    float pos = -curX;
                    if (this.mSupportScrollBar)
                    {
                        this.GetPlusItemIndexAndPosAtGivenPos(curX, ref index, ref pos);
                        pos = -pos;
                    }
                    LoopListViewItem2 newItem = this.GetNewItemByIndex(index);
                    if (newItem == null)
                    {
                        return false;
                    }
                    if (this.mSupportScrollBar)
                    {
                        this.SetItemSize(index, newItem.CachedRectTransform.rect.width, newItem.Padding);
                    }
                    this.mItemList.Add(newItem);
                    newItem.CachedRectTransform.localPosition = new Vector3(pos, newItem.StartPosOffset, 0);
                    this.UpdateContentSize();
                    return true;
                }
                LoopListViewItem2 tViewItem0 = this.mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 leftPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                Vector3 rightPos0 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[2]);

                if (!this.mIsDraging && tViewItem0.ItemCreatedCheckFrameCount != this.mListUpdateCheckFrameCount
                    && leftPos0.x - this.mViewPortRectLocalCorners[2].x > distanceForRecycle0)
                {
                    this.mItemList.RemoveAt(0);
                    this.RecycleItemTmp(tViewItem0);
                    if (!this.mSupportScrollBar)
                    {
                        this.UpdateContentSize();
                        this.CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }

                LoopListViewItem2 tViewItem1 = this.mItemList[this.mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(this.mItemWorldCorners);
                Vector3 leftPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[1]);
                Vector3 rightPos1 = this.mViewPortRectTransform.InverseTransformPoint(this.mItemWorldCorners[2]);
                if (!this.mIsDraging && tViewItem1.ItemCreatedCheckFrameCount != this.mListUpdateCheckFrameCount
                    && this.mViewPortRectLocalCorners[1].x - rightPos1.x > distanceForRecycle1)
                {
                    this.mItemList.RemoveAt(this.mItemList.Count - 1);
                    this.RecycleItemTmp(tViewItem1);
                    if (!this.mSupportScrollBar)
                    {
                        this.UpdateContentSize();
                        this.CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }



                if (this.mViewPortRectLocalCorners[1].x - leftPos1.x < distanceForNew1)
                {
                    if (tViewItem1.ItemIndex > this.mCurReadyMaxItemIndex)
                    {
                        this.mCurReadyMaxItemIndex = tViewItem1.ItemIndex;
                        this.mNeedCheckNextMaxItem = true;
                    }
                    int nIndex = tViewItem1.ItemIndex + 1;
                    if (nIndex <= this.mCurReadyMaxItemIndex || this.mNeedCheckNextMaxItem)
                    {
                        LoopListViewItem2 newItem = this.GetNewItemByIndex(nIndex);
                        if (newItem == null)
                        {
                            this.mCurReadyMaxItemIndex = tViewItem1.ItemIndex;
                            this.mNeedCheckNextMaxItem = false;
                            this.CheckIfNeedUpdataItemPos();
                        }
                        else
                        {
                            if (this.mSupportScrollBar)
                            {
                                this.SetItemSize(nIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                            }
                            this.mItemList.Add(newItem);
                            float x = tViewItem1.CachedRectTransform.localPosition.x - tViewItem1.CachedRectTransform.rect.width - tViewItem1.Padding;
                            newItem.CachedRectTransform.localPosition = new Vector3(x, newItem.StartPosOffset, 0);
                            this.UpdateContentSize();
                            this.CheckIfNeedUpdataItemPos();

                            if (nIndex > this.mCurReadyMaxItemIndex)
                            {
                                this.mCurReadyMaxItemIndex = nIndex;
                            }
                            return true;
                        }

                    }

                }

                if (rightPos0.x - this.mViewPortRectLocalCorners[2].x < distanceForNew0)
                {
                    if (tViewItem0.ItemIndex < this.mCurReadyMinItemIndex)
                    {
                        this.mCurReadyMinItemIndex = tViewItem0.ItemIndex;
                        this.mNeedCheckNextMinItem = true;
                    }
                    int nIndex = tViewItem0.ItemIndex - 1;
                    if (nIndex >= this.mCurReadyMinItemIndex || this.mNeedCheckNextMinItem)
                    {
                        LoopListViewItem2 newItem = this.GetNewItemByIndex(nIndex);
                        if (newItem == null)
                        {
                            this.mCurReadyMinItemIndex = tViewItem0.ItemIndex;
                            this.mNeedCheckNextMinItem = false;
                        }
                        else
                        {
                            if (this.mSupportScrollBar)
                            {
                                this.SetItemSize(nIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                            }
                            this.mItemList.Insert(0, newItem);
                            float x = tViewItem0.CachedRectTransform.localPosition.x + newItem.CachedRectTransform.rect.width + newItem.Padding;
                            newItem.CachedRectTransform.localPosition = new Vector3(x, newItem.StartPosOffset, 0);
                            this.UpdateContentSize();
                            this.CheckIfNeedUpdataItemPos();
                            if (nIndex < this.mCurReadyMinItemIndex)
                            {
                                this.mCurReadyMinItemIndex = nIndex;
                            }
                            return true;
                        }

                    }

                }

            }

            return false;

        }






        float GetContentPanelSize()
        {
            if (this.mSupportScrollBar)
            {
                float tTotalSize = this.mItemPosMgr.mTotalSize > 0 ? (this.mItemPosMgr.mTotalSize - this.mLastItemPadding) : 0;
                if (tTotalSize < 0)
                {
                    tTotalSize = 0;
                }
                return tTotalSize;
            }
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return 0;
            }
            if (count == 1)
            {
                return this.mItemList[0].ItemSize;
            }
            if (count == 2)
            {
                return this.mItemList[0].ItemSizeWithPadding + this.mItemList[1].ItemSize;
            }
            float s = 0;
            for (int i = 0; i < count - 1; ++i)
            {
                s += this.mItemList[i].ItemSizeWithPadding;
            }
            s += this.mItemList[count - 1].ItemSize;
            return s;
        }


        void CheckIfNeedUpdataItemPos()
        {
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }
            if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                LoopListViewItem2 firstItem = this.mItemList[0];
                LoopListViewItem2 lastItem = this.mItemList[this.mItemList.Count - 1];
                float viewMaxY = this.GetContentPanelSize();
                if (firstItem.TopY > 0 || (firstItem.ItemIndex == this.mCurReadyMinItemIndex && firstItem.TopY != 0))
                {
                    this.UpdateAllShownItemsPos();
                    return;
                }
                if ((-lastItem.BottomY) > viewMaxY || (lastItem.ItemIndex == this.mCurReadyMaxItemIndex && (-lastItem.BottomY) != viewMaxY))
                {
                    this.UpdateAllShownItemsPos();
                    return;
                }

            }
            else if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                LoopListViewItem2 firstItem = this.mItemList[0];
                LoopListViewItem2 lastItem = this.mItemList[this.mItemList.Count - 1];
                float viewMaxY = this.GetContentPanelSize();
                if (firstItem.BottomY < 0 || (firstItem.ItemIndex == this.mCurReadyMinItemIndex && firstItem.BottomY != 0))
                {
                    this.UpdateAllShownItemsPos();
                    return;
                }
                if (lastItem.TopY > viewMaxY || (lastItem.ItemIndex == this.mCurReadyMaxItemIndex && lastItem.TopY != viewMaxY))
                {
                    this.UpdateAllShownItemsPos();
                    return;
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                LoopListViewItem2 firstItem = this.mItemList[0];
                LoopListViewItem2 lastItem = this.mItemList[this.mItemList.Count - 1];
                float viewMaxX = this.GetContentPanelSize();
                if (firstItem.LeftX < 0 || (firstItem.ItemIndex == this.mCurReadyMinItemIndex && firstItem.LeftX != 0))
                {
                    this.UpdateAllShownItemsPos();
                    return;
                }
                if ((lastItem.RightX) > viewMaxX || (lastItem.ItemIndex == this.mCurReadyMaxItemIndex && lastItem.RightX != viewMaxX))
                {
                    this.UpdateAllShownItemsPos();
                    return;
                }

            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                LoopListViewItem2 firstItem = this.mItemList[0];
                LoopListViewItem2 lastItem = this.mItemList[this.mItemList.Count - 1];
                float viewMaxX = this.GetContentPanelSize();
                if (firstItem.RightX > 0 || (firstItem.ItemIndex == this.mCurReadyMinItemIndex && firstItem.RightX != 0))
                {
                    this.UpdateAllShownItemsPos();
                    return;
                }
                if ((-lastItem.LeftX) > viewMaxX || (lastItem.ItemIndex == this.mCurReadyMaxItemIndex && (-lastItem.LeftX) != viewMaxX))
                {
                    this.UpdateAllShownItemsPos();
                    return;
                }

            }

        }


        void UpdateAllShownItemsPos()
        {
            int count = this.mItemList.Count;
            if (count == 0)
            {
                return;
            }

            this.mAdjustedVec = (this.mContainerTrans.localPosition - this.mLastFrameContainerPos) / Time.deltaTime;

            if (this.mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float pos = 0;
                if (this.mSupportScrollBar)
                {
                    pos = -this.GetItemPos(this.mItemList[0].ItemIndex);
                }
                float pos1 = this.mItemList[0].CachedRectTransform.localPosition.y;
                float d = pos - pos1;
                float curY = pos;
                for (int i = 0; i < count; ++i)
                {
                    LoopListViewItem2 item = this.mItemList[i];
                    item.CachedRectTransform.localPosition = new Vector3(item.StartPosOffset, curY, 0);
                    curY = curY - item.CachedRectTransform.rect.height - item.Padding;
                }
                if (d != 0)
                {
                    Vector2 p = this.mContainerTrans.localPosition;
                    p.y = p.y - d;
                    this.mContainerTrans.localPosition = p;
                }

            }
            else if (this.mArrangeType == ListItemArrangeType.BottomToTop)
            {
                float pos = 0;
                if (this.mSupportScrollBar)
                {
                    pos = this.GetItemPos(this.mItemList[0].ItemIndex);
                }
                float pos1 = this.mItemList[0].CachedRectTransform.localPosition.y;
                float d = pos - pos1;
                float curY = pos;
                for (int i = 0; i < count; ++i)
                {
                    LoopListViewItem2 item = this.mItemList[i];
                    item.CachedRectTransform.localPosition = new Vector3(item.StartPosOffset, curY, 0);
                    curY = curY + item.CachedRectTransform.rect.height + item.Padding;
                }
                if (d != 0)
                {
                    Vector3 p = this.mContainerTrans.localPosition;
                    p.y = p.y - d;
                    this.mContainerTrans.localPosition = p;
                }
            }
            else if (this.mArrangeType == ListItemArrangeType.LeftToRight)
            {
                float pos = 0;
                if (this.mSupportScrollBar)
                {
                    pos = this.GetItemPos(this.mItemList[0].ItemIndex);
                }
                float pos1 = this.mItemList[0].CachedRectTransform.localPosition.x;
                float d = pos - pos1;
                float curX = pos;
                for (int i = 0; i < count; ++i)
                {
                    LoopListViewItem2 item = this.mItemList[i];
                    item.CachedRectTransform.localPosition = new Vector3(curX, item.StartPosOffset, 0);
                    curX = curX + item.CachedRectTransform.rect.width + item.Padding;
                }
                if (d != 0)
                {
                    Vector3 p = this.mContainerTrans.localPosition;
                    p.x = p.x - d;
                    this.mContainerTrans.localPosition = p;
                }

            }
            else if (this.mArrangeType == ListItemArrangeType.RightToLeft)
            {
                float pos = 0;
                if (this.mSupportScrollBar)
                {
                    pos = -this.GetItemPos(this.mItemList[0].ItemIndex);
                }
                float pos1 = this.mItemList[0].CachedRectTransform.localPosition.x;
                float d = pos - pos1;
                float curX = pos;
                for (int i = 0; i < count; ++i)
                {
                    LoopListViewItem2 item = this.mItemList[i];
                    item.CachedRectTransform.localPosition = new Vector3(curX, item.StartPosOffset, 0);
                    curX = curX - item.CachedRectTransform.rect.width - item.Padding;
                }
                if (d != 0)
                {
                    Vector3 p = this.mContainerTrans.localPosition;
                    p.x = p.x - d;
                    this.mContainerTrans.localPosition = p;
                }

            }
            if (this.mIsDraging)
            {
                this.mScrollRect.OnBeginDrag(this.mPointerEventData);
                this.mScrollRect.Rebuild(CanvasUpdate.PostLayout);
                this.mScrollRect.velocity = this.mAdjustedVec;
                this.mNeedAdjustVec = true;
            }
        }

        void UpdateContentSize()
        {
            float size = this.GetContentPanelSize();
            if (this.mIsVertList)
            {
                if (this.mContainerTrans.rect.height != size)
                {
                    this.mContainerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                }
            }
            else
            {
                if (this.mContainerTrans.rect.width != size)
                {
                    this.mContainerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                }
            }
        }
    }

}
