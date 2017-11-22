﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TinyFrameWork
{
    /// <summary>
    /// 游戏工具类
    /// Game Utility Tools
    /// </summary>
    public class GameUtility
    {
        /// <summary>
        /// Find Deep child with name
        /// </summary>
        public static Transform FindDeepChild(GameObject _target, string _childName)
        {
            Transform resultTrs = null;
            resultTrs = _target.transform.Find(_childName);
            if (resultTrs == null)
            {
                foreach (Transform trs in _target.transform)
                {
                    resultTrs = GameUtility.FindDeepChild(trs.gameObject, _childName);
                    if (resultTrs != null)
                        return resultTrs;
                }
            }
            return resultTrs;
        }

        /// <summary>
        /// Find component in Target Child
        /// </summary>
        public static T FindDeepChild<T>(GameObject _target, string _childName) where T : Component
        {
            Transform resultTrs = GameUtility.FindDeepChild(_target, _childName);
            if (resultTrs != null)
                return resultTrs.gameObject.GetComponent<T>();
            return (T)((object)null);
        }

        /// <summary>
        /// 添加子节点
        /// Add child to target
        /// </summary>
        public static void AddChildToTarget(Transform target, Transform child)
        {
            child.parent = target;
            child.localScale = Vector3.one;
            child.localPosition = Vector3.zero;
            child.localEulerAngles = Vector3.zero;

            ChangeChildLayer(child, target.gameObject.layer);
        }

        /// <summary>
        /// 修改子节点Layer  NGUITools.SetLayer();
        /// Change child layer
        /// </summary>
        public static void ChangeChildLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; ++i)
            {
                Transform child = t.GetChild(i);
                child.gameObject.layer = layer;
                ChangeChildLayer(child, layer);
            }
        }

        /// <summary>
        /// 返回最大或者最小Depth界面
        /// Get the max or min depth UIPanel
        /// </summary>
        public static GameObject GetPanelDepthMaxMin(GameObject target, bool maxDepth, bool includeInactive)
        {
            List<UIPanel> lsPanels = GetPanelSorted(target, includeInactive);
            if (lsPanels != null)
            {
                if (maxDepth)
                    return lsPanels[lsPanels.Count - 1].gameObject;
                else
                    return lsPanels[0].gameObject;
            }
            return null;
        }

        private class CompareSubPanels : IComparer<UIPanel>
        {
            public int Compare(UIPanel left, UIPanel right)
            {
                return left.depth - right.depth;
            }
        }

        private static List<UIPanel> GetPanelSorted(GameObject target, bool includeInactive = false)
        {
            UIPanel[] panels = target.transform.GetComponentsInChildren<UIPanel>(includeInactive);
            if (panels.Length > 0)
            {
                List<UIPanel> lsPanels = new List<UIPanel>(panels);
                lsPanels.Sort(new CompareSubPanels());
                return lsPanels;
            }
            return null;
        }

        /// <summary>
        /// Set the mini depth to target with given Sorted list
        /// </summary>
        public static void SetTargetMinPanelDepth(GameObject obj, int depth)
        {
            List<UIPanel> lsPanels = GameUtility.GetPanelSorted(obj, true);
            if (lsPanels != null)
            {
                int i = 0;
                while (i < lsPanels.Count)
                {
                    lsPanels[i].depth = depth + i;
                    i++;
                }
            }
        }

        /// <summary>
        /// 获得指定目标最大depth值
        /// Get the target Max depth
        /// </summary>
        public static int GetMaxTargetDepth(GameObject obj, bool includeInactive = false)
        {
            int minDepth = -1;
            List<UIPanel> lsPanels = GetPanelSorted(obj, includeInactive);
            if (lsPanels != null)
                return lsPanels[lsPanels.Count - 1].depth;
            return minDepth;
        }

        // <summary>
        /// 给目标添加Collider背景
        /// Add Collider Background for target
        /// </summary>
        public static GameObject AddColliderBgToTarget(GameObject target, string maskName, UIAtlas altas, bool isTransparent)
        {
            // 添加UIPaneldepth最小上面
            // 保证添加的Collider放置在屏幕中间
            Transform windowBg = GameUtility.FindDeepChild(target, "WindowBg");
            if (windowBg == null)
            {
                GameObject targetParent = GameUtility.GetPanelDepthMaxMin(target, false, true);
                if (targetParent == null)
                    targetParent = target;

                windowBg = (new GameObject("WindowBg")).transform;
                GameUtility.AddChildToTarget(targetParent.transform, windowBg);
            }

            Transform bg = GameUtility.FindDeepChild(target, "WindowColliderBg(Cool)");
            if (bg == null)
            {
                // add sprite or widget to ColliderBg
                UIWidget widget = null;
                if (!isTransparent)
                    widget = NGUITools.AddSprite(windowBg.gameObject, altas, maskName);
                else
                    widget = NGUITools.AddWidget<UIWidget>(windowBg.gameObject);

                widget.name = "WindowColliderBg(Cool)";
                bg = widget.transform;

                // fill the screen
                // You can use the new Anchor system
                UIStretch stretch = bg.gameObject.AddComponent<UIStretch>();
                stretch.style = UIStretch.Style.Both;
                // set relative size bigger
                stretch.relativeSize = new Vector2(1.5f, 1.5f);

                // set a lower depth
                widget.depth = -5;

                // set alpha
                widget.alpha = 0.6f;

                // add collider
                NGUITools.AddWidgetCollider(bg.gameObject);
            }
            return bg.gameObject;
        }
    }
}