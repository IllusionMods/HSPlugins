#if HONEYSELECT
using System;
using System.Collections.Generic;
using UILib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HSUS
{
    internal static class CharaMakerSearch
    {
        internal static InputField SpawnSearchBar(Transform parent, UnityAction<string> listener, float parentShift = -16f)
        {
            RectTransform rt = parent.FindChild("ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, parentShift);
            float newY = rt.offsetMax.y;
            rt = parent.FindChild("Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, parentShift);

            InputField searchBar = UIUtility.CreateInputField("Search Bar", parent);
            searchBar.GetComponent<Image>().sprite = HSUS._self._searchBarBackground;
            rt = searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            searchBar.placeholder.GetComponent<Text>().text = "Search...";
            searchBar.onValueChanged.AddListener(listener);
            foreach (Text t in searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;
            return searchBar;
        }
    }

    internal static class CharaMakerSort
    {
        internal static void SpawnSortButtons(Transform parent, UnityAction sortByNameListener, UnityAction sortByCreationDateListener, UnityAction resetListener)
        {
            RectTransform rt = parent.FindChild("ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, -20f);
            float newY = rt.offsetMax.y;
            rt = parent.FindChild("Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, -20f);

            RectTransform container = UIUtility.CreateNewUIObject("SortContainer", parent);
            container.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(4f, newY), new Vector2(-4f, newY + 20f));

            Text label = UIUtility.CreateText("Label", container, "Sort");
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.SetRect(Vector2.zero, new Vector2(0.25f, 1f));

            Button sortName = UIUtility.CreateButton("Name", container, "名前");
            sortName.transform.SetRect(new Vector2(0.25f, 0f), new Vector2(0.5f, 1f));
            sortName.GetComponentInChildren<Text>().rectTransform.SetRect();
            ((Image)sortName.targetGraphic).sprite = HSUS._self._buttonBackground;
            sortName.onClick.AddListener(sortByNameListener);

            Button sortCreationDate = UIUtility.CreateButton("CreationDate", container, "日付");
            sortCreationDate.transform.SetRect(new Vector2(0.5f, 0f), new Vector2(0.75f, 1f));
            sortCreationDate.GetComponentInChildren<Text>().rectTransform.SetRect();
            ((Image)sortCreationDate.targetGraphic).sprite = HSUS._self._buttonBackground;
            sortCreationDate.onClick.AddListener(sortByCreationDateListener);

            Button sortOriginal = UIUtility.CreateButton("Original", container, "リセット");
            sortOriginal.transform.SetRect(new Vector2(0.75f, 0f), Vector2.one);
            sortOriginal.GetComponentInChildren<Text>().rectTransform.SetRect();
            ((Image)sortOriginal.targetGraphic).sprite = HSUS._self._buttonBackground;
            sortOriginal.onClick.AddListener(resetListener);
        }

        internal static void GenericIntSort<T>(List<T> list, Func<T, int> getIntFunc, Func<T, GameObject> getGameObjectFunc, bool reverse = false)
        {
            list.Sort((x, y) => reverse ? getIntFunc(y).CompareTo(getIntFunc(x)) : getIntFunc(x).CompareTo(getIntFunc(y)));
            foreach (T elem in list)
                getGameObjectFunc(elem).transform.SetAsLastSibling();
        }

        internal static void GenericStringSort<T>(List<T> list, Func<T, string> getStringFunc, Func<T, GameObject> getGameObjectFunc, bool reverse = false)
        {
            list.Sort((x, y) => reverse ? string.Compare(getStringFunc(y), getStringFunc(x), StringComparison.CurrentCultureIgnoreCase) : string.Compare(getStringFunc(x), getStringFunc(y), StringComparison.CurrentCultureIgnoreCase));
            foreach (T elem in list)
                getGameObjectFunc(elem).transform.SetAsLastSibling();
        }

        internal static void GenericDateSort<T>(List<T> list, Func<T, DateTime> getDateFunc, Func<T, GameObject> getGameObjectFunc, bool reverse = false)
        {
            list.Sort((x, y) => reverse ? getDateFunc(y).CompareTo(getDateFunc(x)) : getDateFunc(x).CompareTo(getDateFunc(y)));
            foreach (T elem in list)
                getGameObjectFunc(elem).transform.SetAsLastSibling();
        }
    }

    internal static class CharaMakerCycleButtons
    {
        internal static void SpawnCycleButtons(Transform parent, UnityAction onUp, UnityAction onDown)
        {
            RectTransform scrollView = parent.Find("ScrollView").transform as RectTransform;
            scrollView.offsetMax -= new Vector2(20f, 0f);
            RectTransform scrollbar = parent.Find("Scrollbar") as RectTransform;
            scrollbar.anchoredPosition -= new Vector2(20f, 0f);
            RectTransform cycleButtons = UIUtility.CreateNewUIObject("CycleButtons", parent);
            cycleButtons.SetRect(Vector2.one, Vector2.one, new Vector2(scrollbar.offsetMax.x, scrollbar.offsetMin.y), new Vector2(scrollbar.offsetMax.x + 20f, scrollbar.offsetMax.y));

            Button upButton = UIUtility.CreateButton("UpButton", cycleButtons, "↑");
            upButton.transform.SetRect(new Vector2(0f, 0.5f), Vector2.one);
            upButton.onClick.AddListener(onUp);

            Button downButton = UIUtility.CreateButton("DownButton", cycleButtons, "↓");
            downButton.transform.SetRect(Vector2.zero, new Vector2(1f, 0.5f));
            downButton.onClick.AddListener(onDown);
        }
    }
}
#endif