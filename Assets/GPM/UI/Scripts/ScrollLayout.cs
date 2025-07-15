namespace Gpm.Ui
{
    using System;

    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class ScrollLayout
    {
        [Serializable]
        public class LayoutValue
        {
            public enum ValueType
            {
                DEFAULT,
                RATE,
            }

            [HideInInspector]
            public ValueType valueType;
            public float value;
        }


        private InfiniteScroll scroll;

        private bool isVertical;

        public List<LayoutValue> values = new List<LayoutValue>();

        public void Initialize(InfiniteScroll scroll, bool isVertical)
        {
            this.scroll = scroll;
            this.isVertical = isVertical;
        }

        public void SetDefaults()
        {
            foreach (var layoutValue in values)
            {
                if (layoutValue.valueType == LayoutValue.ValueType.DEFAULT)
                {
                    layoutValue.valueType = LayoutValue.ValueType.RATE;
                    layoutValue.value = 1;
                }
            }
        }

        public int GetLineIndex(int index)
        {
            if (scroll.GetItemCount() == 0)
            {
                return 0;
            }

            if (index >= scroll.GetItemCount())
            {
                index = scroll.GetItemCount() - 1;
            }

            if (IsGrid() == true)
            {
                // Calculate grid line
                return index / values.Count;
            }
            return index;
        }

        public int GetLineCount(int index)
        {
            if (scroll.GetItemCount() == 0)
            {
                return 0;
            }

            if (index >= scroll.GetItemCount())
            {
                index = scroll.GetItemCount() - 1;
            }
            return GetLineIndex(index) + 1;
        }

        public int GetLineCount()
        {
            return GetLineCount(scroll.GetItemCount() - 1);
        }

        private float GetLineSizeFromIndex(int index)
        {
            if (scroll.GetItemCount() == 0)
            {
                return 0;
            }

            if (index >= scroll.GetItemCount())
            {
                index = scroll.GetItemCount() - 1;
            }

            if (IsGrid() == true)
            {
                return GetLineSize(GetLineIndex(index));
            }

            return scroll.GetItemSize(index);
        }


        public int GetLineFirstItemIndex(int lineIndex)
        {
            int lineCount = GetLineCount();

            if (lineIndex >= lineCount)
            {
                lineIndex = lineCount - 1;
            }

            int firstItemIndex = lineIndex;
            if (IsGrid() == true)
            {
                firstItemIndex = firstItemIndex * values.Count;
            }
            return firstItemIndex;
        }

        public float GetLineSize(int lineIndex)
        {
            int lineCount = GetLineCount();
            if (lineCount == 0)
            {
                return 0;
            }

            if (lineIndex >= lineCount)
            {
                lineIndex = lineCount - 1;
            }

            if (scroll.dynamicItemSize == true)
            {
                int itemCount = scroll.GetItemCount();
                if (itemCount > 0)
                {
                    if (IsGrid() == true)
                    {
                        int firstItemIndex = GetLineFirstItemIndex(lineIndex);
                        if (firstItemIndex < itemCount)
                        {
                            float lineSize = scroll.GetItemSize(firstItemIndex);

                            for (int gridIdx = 1; gridIdx < values.Count; gridIdx++)
                            {
                                int index = firstItemIndex + gridIdx;
                                if (index < itemCount)
                                {
                                    float gridSize = scroll.GetItemSize(index);
                                    if (lineSize < gridSize)
                                    {
                                        lineSize = gridSize;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }

                            return lineSize;
                        }
                    }
                }
            }

            return scroll.GetItemSize(lineIndex);
        }

        public void SetItemSizeAndPosition(RectTransform rectTransform, int dataIndex)
        {
            float passingItemSize = scroll.GetItemSizeSumToIndex(dataIndex);
            float size = scroll.GetItemSize(dataIndex);

            float firstItemPosition = scroll.GetFirstItemPosition();

            Vector2 currentSize = rectTransform.sizeDelta;

            if (isVertical == true)
            {
                if (IsGrid() == true)
                {
                    float inlineMaxSize = 0;
                    foreach (var layoutValue in values)
                    {
                        inlineMaxSize += layoutValue.value;
                    }

                    int inlineIndex = dataIndex % values.Count;

                    float inlinePos = 0;
                    float inlineSize = values[inlineIndex].value;

                    if (inlineMaxSize > 0 &&
                        inlineSize > 0)
                    {
                        
                        for (int index = 0; index < inlineIndex; index++)
                        {
                            inlinePos += values[index].value;
                        }
                        inlinePos = inlinePos / inlineMaxSize;
                        inlineSize = inlineSize / inlineMaxSize;

                        rectTransform.anchorMin = new Vector2(inlinePos, 1.0f);
                        rectTransform.anchorMax = new Vector2(inlinePos + inlineSize, 1.0f);
                    }
                    else
                    {
                        rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
                        rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
                    }
                    currentSize = rectTransform.sizeDelta;
                }
                else
                {
                    rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
                    rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
                }

                rectTransform.sizeDelta = new Vector2(currentSize.x, size);
                rectTransform.anchoredPosition = new Vector2(0, firstItemPosition - passingItemSize);

            }
            else
            {
                if (IsGrid() == true)
                {
                    float inlineMaxSize = 0;
                    foreach (var layoutValue in values)
                    {
                        inlineMaxSize += layoutValue.value;
                    }

                    int inlineIndex = dataIndex % values.Count;

                    float inlinePos = 0;
                    float inlineSize = values[inlineIndex].value;

                    if (inlineMaxSize > 0 &&
                        inlineSize > 0)
                    {
                        for (int index = 0; index < inlineIndex; index++)
                        {
                            inlinePos += values[index].value;
                        }
                        inlinePos = (inlineMaxSize - inlinePos) / inlineMaxSize;
                        inlineSize = inlineSize / inlineMaxSize;

                        rectTransform.anchorMin = new Vector2(0, inlinePos - inlineSize);
                        rectTransform.anchorMax = new Vector2(0, inlinePos);
                    }
                    else
                    {
                        rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                        rectTransform.anchorMax = new Vector2(0.0f, 0.0f);
                    }
                    currentSize = rectTransform.sizeDelta;
                }
                else
                {
                    rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                    rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
                }

                rectTransform.sizeDelta = new Vector2(size, currentSize.y);
                rectTransform.anchoredPosition = new Vector2(firstItemPosition + passingItemSize, 0);
            }
        }

        public bool IsGrid()
        {
            if (values.Count > 1)
            {
                return true;
            }

            return false;
        }
    }
}