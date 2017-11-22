﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace TinyFrameWork
{
    /// <summary>
    /// UI Main Manager "the Master" most important Class
    ///         Control all the "Big Parent" window:UIRank,UIShop,UIGame,UIMainMenu and so on.
    ///         UIRankManager: control the rank window logic (UIRankDetail sub window)
    ///         May be UIShopManager:control the UIShopDetailWindow or UIShopSubTwoWindow
    /// 
    /// 枢纽中心，控制整个大界面的显示逻辑UIRank，UIMainMenu等
    ///         UIRank排行榜界面可能也会有自己的Manager用来管理Rank系统中自己的子界面，这些子界面不交给"老大"UICenterMasterManager管理
    ///         分而治之，不是中央集权
    /// </summary>
    public class UICenterMasterManager : UIManagerBase
    {
        // save the UIRoot
        public Transform UIRoot;
        // NormalWindow node
        [System.NonSerialized]
        public Transform UINormalWindowRoot;
        // PopUpWindow node
        [System.NonSerialized]
        public Transform UIPopUpWindowRoot;
        // FixedWindow node
        [System.NonSerialized]
        public Transform UIFixedWidowRoot;

        // Each Type window start Depth
        private const int fixedWindowDepth = 100;
        private const int popUpWindowDepth = 150;
        private const int normalWindowDepth = 2;

        // Atlas reference
        // Mask Atlas for sprite mask(Common Collider window background)
        public UIAtlas maskAtlas;

        private static UICenterMasterManager instance;
        public static UICenterMasterManager Instance
        {
            get { return instance; }
            private set { }
        }

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            InitWindowManager();
            Debuger.Log("## UICenterMasterManager is call awake.");
        }

        public override void ShowWindow(WindowID id, ShowWindowData showData = null)
        {
            UIWindowBase baseWindow = ReadyToShowBaseWindow(id, showData);
            if (baseWindow != null)
            {
                RealShowWindow(baseWindow, id, showData);
            }
        }

        protected override UIWindowBase ReadyToShowBaseWindow(WindowID id, ShowWindowData showData = null)
        {
            // Check the window control state
            if (!this.IsWindowInControl(id))
            {
                Debuger.Log("## UIManager has no control power of " + id.ToString());
                return null;
            }

            // If the window in shown list just return
            if (dicShownWindows.ContainsKey((int)id))
                return null;

            UIWindowBase baseWindow = GetGameWindow(id);

            // If window not in scene start Instantiate new window to scene
            bool newAdded = false;
            if (!baseWindow)
            {
                newAdded = true;
                if (UIResourceDefine.windowPrefabPath.ContainsKey((int)id))
                {
                    string prefabPath = UIResourceDefine.UIPrefabPath + UIResourceDefine.windowPrefabPath[(int)id];
                    GameObject prefab = Resources.Load<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        GameObject uiObject = (GameObject)GameObject.Instantiate(prefab);
                        NGUITools.SetActive(uiObject, true);
                        // NOTE: You can add component to the window in the inspector
                        // Or just AddComponent<UIxxxxWindow>() to the target
                        baseWindow = uiObject.GetComponent<UIWindowBase>();
                        if (baseWindow.ID != id)
                        {
                            Debuger.LogError(string.Format("<color=cyan>[BaseWindowId :{0} != shownWindowId :{1}]</color>", baseWindow.ID, id));
                            return null;
                        }
                        // Get the window target root parent
                        Transform targetRoot = GetTargetRoot(baseWindow.windowData.windowType);
                        GameUtility.AddChildToTarget(targetRoot, baseWindow.gameObject.transform);
                        dicAllWindows[(int)id] = baseWindow;
                    }
                }
            }

            if (baseWindow == null)
                Debuger.LogError("[window instance is null.]" + id.ToString());

            // Call reset window when first load new window
            // Or get forceResetWindow param
            if (newAdded || (showData != null && showData.forceResetWindow))
                baseWindow.ResetWindow();

            if (showData == null || (showData != null && showData.executeNavLogic))
            {
                // refresh the navigation data
                ExecuteNavigationLogic(baseWindow, showData);
            }

            // Adjust the window depth
            AdjustBaseWindowDepth(baseWindow);

            // Add common background collider to window
            AddColliderBgForWindow(baseWindow);
            return baseWindow;
        }

        public override void HideWindow(WindowID id, Action onComplete = null)
        {
            CheckDirectlyHide(id, onComplete);
        }

        public override void InitWindowManager()
        {
            base.InitWindowManager();
            InitWindowControl();
            isNeedWaitHideOver = true;

            DontDestroyOnLoad(UIRoot);

            if (UIFixedWidowRoot == null)
            {
                UIFixedWidowRoot = new GameObject("UIFixedWidowRoot").transform;
                GameUtility.AddChildToTarget(UIRoot, UIFixedWidowRoot);
                GameUtility.ChangeChildLayer(UIFixedWidowRoot, UIRoot.gameObject.layer);
            }
            if (UIPopUpWindowRoot == null)
            {
                UIPopUpWindowRoot = new GameObject("UIPopUpWindowRoot").transform;
                GameUtility.AddChildToTarget(UIRoot, UIPopUpWindowRoot);
                GameUtility.ChangeChildLayer(UIPopUpWindowRoot, UIRoot.gameObject.layer);
            }
            if (UINormalWindowRoot == null)
            {
                UINormalWindowRoot = new GameObject("UINormalWindowRoot").transform;
                GameUtility.AddChildToTarget(UIRoot, UINormalWindowRoot);
                GameUtility.ChangeChildLayer(UINormalWindowRoot, UIRoot.gameObject.layer);
            }
        }

        protected override void InitWindowControl()
        {
            managedWindowIds.Clear();
            AddWindowInControl(WindowID.WindowID_Level);
            AddWindowInControl(WindowID.WindowID_Rank);
            AddWindowInControl(WindowID.WindowID_MainMenu);
            AddWindowInControl(WindowID.WindowID_TopBar);
            AddWindowInControl(WindowID.WindowID_MessageBox);
            AddWindowInControl(WindowID.WindowID_LevelDetail);
            AddWindowInControl(WindowID.WindowID_Matching);
            AddWindowInControl(WindowID.WindowID_MatchResult);
            AddWindowInControl(WindowID.WindowID_Skill);
            AddWindowInControl(WindowID.WindowID_Shop);
        }

        public override void ClearAllWindow()
        {
            base.ClearAllWindow();
        }

        /// <summary>
        /// Return logic 
        /// When return back navigation check current window's Return Logic
        /// If true just execute the return logic
        /// If false immediately enter the RealReturnWindow() logic
        /// </summary>
        public override bool PopNavigationWindow()
        {
            if(curNavigationWindow != null)
            {
                bool needReturn = curNavigationWindow.ExecuteReturnLogic();
                if(needReturn)
                    return false;
            }
            return RealPopNavigationWindow();
        }

        public void CloseWindow ( WindowID wndId )
        {
            if (!IsWindowInControl(wndId))
            {
                Debuger.LogError("## Current UI Manager has no control power of " + wndId.ToString());
                return;
            }

            if (!dicShownWindows.ContainsKey((int) wndId))
                return;

            UIWindowBase window = dicShownWindows[(int) wndId];
            if (this.backSequence.Count > 0)
            {
                NavigationData seqData = this.backSequence.Peek();
                if (seqData != null && seqData.hideTargetWindow == window)
                {
                    PopNavigationWindow();
                    Debuger.Log("<color=magenta>## close window use PopNavigationWindow() ##</color>");
                    return;
                }
            }
            HideWindow(wndId);
            Debuger.Log("<color=magenta>## close window without PopNavigationWindow() ##</color>");
        }

        /// <summary>
        /// Calculate right depth with windowType
        /// </summary>
        /// <param name="baseWindow"></param>
        private void AdjustBaseWindowDepth(UIWindowBase baseWindow)
        {
            UIWindowType windowType = baseWindow.windowData.windowType;
            int needDepth = 1;
            if (windowType == UIWindowType.Normal)
            {
                needDepth = Mathf.Clamp(GameUtility.GetMaxTargetDepth(UINormalWindowRoot.gameObject, false) + 1, normalWindowDepth, int.MaxValue);
                Debuger.Log(string.Format("<color=cyan>[UIWindowType.Normal] maxDepth is {0} , {1}.</color>", needDepth.ToString(), baseWindow.ID.ToString()));
            }
            else if (windowType == UIWindowType.PopUp)
            {
                needDepth = Mathf.Clamp(GameUtility.GetMaxTargetDepth(UIPopUpWindowRoot.gameObject) + 1, popUpWindowDepth, int.MaxValue);
                Debuger.Log(string.Format("<color=cyan>[UIWindowType.PopUp] maxDepth is {0} , {1}.</color>", needDepth.ToString(), baseWindow.ID.ToString()));
            }
            else if (windowType == UIWindowType.Fixed)
            {
                needDepth = Mathf.Clamp(GameUtility.GetMaxTargetDepth(UIFixedWidowRoot.gameObject) + 1, fixedWindowDepth, int.MaxValue);
                Debuger.Log(string.Format("<color=cyan>[UIWindowType.Fixed] maxDepth is {0} , {1}.</color>", needDepth.ToString(), baseWindow.ID.ToString()));
            }
            if (baseWindow.MinDepth != needDepth)
                GameUtility.SetTargetMinPanelDepth(baseWindow.gameObject, needDepth);

            // send window added message to game client
            if (baseWindow.windowData.windowType == UIWindowType.PopUp)
            {
                // trigger the window PopRoot added window event
                EventDispatcher.GetInstance().UIFrameWorkEventManager.TriggerEvent(EventId.PopRootWindowAdded);
            }
            baseWindow.MinDepth = needDepth;
        }

        /// <summary>
        /// Add Collider and BgTexture for target window
        /// </summary>
        private void AddColliderBgForWindow(UIWindowBase baseWindow)
        {
            UIWindowColliderMode colliderMode = baseWindow.windowData.colliderMode;
            if (colliderMode == UIWindowColliderMode.None)
                return;
            GameObject bgObj = null;
            if (colliderMode == UIWindowColliderMode.Normal)
                bgObj = GameUtility.AddColliderBgToTarget(baseWindow.gameObject, "Mask02", maskAtlas, true);
            if (colliderMode == UIWindowColliderMode.WithBg)
                bgObj = GameUtility.AddColliderBgToTarget(baseWindow.gameObject, "Mask02", maskAtlas, false);
            baseWindow.OnAddColliderBg(bgObj);
        }

        private void ExecuteNavigationLogic(UIWindowBase baseWindow, ShowWindowData showData)
        {
            WindowCoreData windowData = baseWindow.windowData;
            if (baseWindow.RefreshBackSeqData)
            {
                this.RefreshBackSequenceData(baseWindow, showData);
            }
            else if (windowData.showMode == UIWindowShowMode.HideOtherWindow)
            {
                HideAllShownWindow();
            }

            // If target window is mark as force clear all the navigation sequence data
            // Show data need force clear the back seq data
            if (baseWindow.windowData.forceClearNavigation || (showData != null && showData.forceClearBackSeqData))
            {
                Debuger.Log("<color=cyan>## [Enter the start window, reset the backSequenceData for the navigation system.]##</color>");
                ClearBackSequence();
            }
            else
            {
                if ((showData != null && showData.checkNavigation))
                    CheckBackSequenceData(baseWindow);
            }
        }

        private void RefreshBackSequenceData(UIWindowBase targetWindow, ShowWindowData showData)
        {
            WindowCoreData coreData = targetWindow.windowData;
            bool dealBackSequence = true;
            if (dicShownWindows.Count > 0 && dealBackSequence)
            {
                List<WindowID> removedKey = null;
                List<UIWindowBase> sortedHiddenWindows = new List<UIWindowBase>();

                NavigationData backData = new NavigationData();
                foreach (KeyValuePair<int, UIWindowBase> window in dicShownWindows)
                {
                    if (coreData.showMode != UIWindowShowMode.DoNothing)
                    {
                        if (window.Value.windowData.windowType == UIWindowType.Fixed)
                            continue;
                        if (removedKey == null)
                            removedKey = new List<WindowID>();
                        removedKey.Add((WindowID)window.Key);
                        window.Value.HideWindowDirectly();
                    }

                    if (window.Value.windowData.windowType != UIWindowType.Fixed)
                        sortedHiddenWindows.Add(window.Value);
                }

                if (removedKey != null)
                {
                    for (int i = 0; i < removedKey.Count; i++)
                        dicShownWindows.Remove((int)removedKey[i]);
                }

                // Push new navigation data
                if (coreData.navigationMode == UIWindowNavigationMode.NormalNavigation &&
                    (showData == null || (!showData.ignoreAddNavData)))
                {
                    // Add to return show target list
                    sortedHiddenWindows.Sort(this.compareWindowFun);
                    List<WindowID> navHiddenWindows = new List<WindowID>();
                    for (int i = 0; i < sortedHiddenWindows.Count; i++)
                    {
                        WindowID pushWindowId = sortedHiddenWindows[i].ID;
                        navHiddenWindows.Add(pushWindowId);
                    }
                    backData.hideTargetWindow = targetWindow;
                    backData.backShowTargets = navHiddenWindows;
                    backSequence.Push(backData);
                    Debuger.Log("<color=cyan>### !!!Push new Navigation data!!! ###</color>");
                }
            }
        }

        // 如果当前存在BackSequence数据
        // 1.栈顶界面不是当前要显示的界面需要清空BackSequence(导航被重置)
        // 2.栈顶界面是当前显示界面,如果类型为(NeedBack)则需要显示所有backShowTargets界面

        // 栈顶不是即将显示界面(导航序列被打断)
        // 如果当前导航队列顶部元素和当前显示的界面一致，表示和当前的导航数衔接上，后续导航直接使用导航数据
        // 不一致则表示，导航已经失效，下次点击返回按钮，我们直接根据window的preWindowId确定跳转到哪一个界面

        // 如果测试：进入到demo的 关卡详情，点击失败按钮，然后你可以选择从游戏中跳转到哪一个界面，查看导航输出信息
        // 可以知道是否破坏了导航数据

        // if the navigation stack top window not equals to current show window just clear the navigation stack
        // check whether the navigation is broken

        // Example:(we from mainmenu to uilevelwindow to uileveldetailwindow)
        // UILevelDetailWindow <- UILevelWindow <- UIMainMenu   (current navigation stack top element is UILevelDetailWindow)

        // click the GotoGame in UILevelDetailWindow to enter the real Game

        // 1. Exit game we want to enter UILevelDetailWindow(OK, the same as navigation stack top UILevelDetailWindow) so we not break the navigation
        // when we enter the UILevelDetailWindow our system will follow the navigation system

        // 2. Exit game we want to enter UISkillWindow(OK, not the same as navigation stack top UILevelDetailWindow)so we break the navigation
        // reset the navigation data 
        // when we click return Button in the UISkillWindow we will find UISkillWindow's preWindowId to navigation because our navigation data is empty
        // we should use preWindowId for navigating to next window

        // HOW to Test
        // when you in the MatchResultWindow , you need click the lose button choose to different window and check the ConsoleLog find something useful
        private void CheckBackSequenceData(UIWindowBase baseWindow)
        {
            if (baseWindow.RefreshBackSeqData)
            {
                if (backSequence.Count > 0)
                {
                    NavigationData backData = backSequence.Peek();
                    if (backData.hideTargetWindow != null)
                    {
                        if (backData.hideTargetWindow.ID != baseWindow.ID)
                        {
                            Debuger.Log("<color=cyan>## UICenterMasterManager : clear sequence data ##</color>");
                            Debuger.Log("## UICenterMasterManager : Hide target window and show window id is " + backData.hideTargetWindow.ID + " != " + baseWindow.ID);
                            ClearBackSequence();
                        }
                    }
                    else
                        Debuger.LogError("Back data hide target window is null!");
                }
            }
        }

        private Transform GetTargetRoot(UIWindowType type)
        {
            if (type == UIWindowType.Fixed)
                return UIFixedWidowRoot;
            if (type == UIWindowType.Normal)
                return UINormalWindowRoot;
            if (type == UIWindowType.PopUp)
                return UIPopUpWindowRoot;
            return UIRoot;
        }

        /// <summary>
        /// MessageBox
        /// </summary>
        /// 
        public void ShowMessageBox(string msg)
        {
            UIWindowBase msgWindow = ReadyToShowBaseWindow(WindowID.WindowID_MessageBox);
            if (msgWindow != null)
            {
                ((UIMessageBox)msgWindow).SetMsg(msg);
                ((UIMessageBox)msgWindow).ResetWindow();
                RealShowWindow(msgWindow, WindowID.WindowID_MessageBox);
            }
        }

        public void ShowMessageBox(string msg, string centerStr, UIEventListener.VoidDelegate callBack)
        {
            UIWindowBase msgWindow = ReadyToShowBaseWindow(WindowID.WindowID_MessageBox);
            if (msgWindow != null)
            {
                UIMessageBox messageBoxWindow = ((UIMessageBox)msgWindow);
                ((UIMessageBox)msgWindow).ResetWindow();
                messageBoxWindow.SetMsg(msg);
                messageBoxWindow.SetCenterBtnCallBack(centerStr, callBack);
                RealShowWindow(msgWindow, WindowID.WindowID_MessageBox);
            }
        }

        public void ShowMessageBox(string msg, string leftStr, UIEventListener.VoidDelegate leftCallBack, string rightStr, UIEventListener.VoidDelegate rightCallBack)
        {
            UIWindowBase msgWindow = ReadyToShowBaseWindow(WindowID.WindowID_MessageBox);
            if (msgWindow != null)
            {
                UIMessageBox messageBoxWindow = ((UIMessageBox)msgWindow);
                ((UIMessageBox)msgWindow).ResetWindow();
                messageBoxWindow.SetMsg(msg);
                messageBoxWindow.SetRightBtnCallBack(rightStr, rightCallBack);
                messageBoxWindow.SetLeftBtnCallBack(leftStr, leftCallBack);
                RealShowWindow(msgWindow, WindowID.WindowID_MessageBox);
            }
        }

        public void CloseMessageBox(Action onClosed = null)
        {
            HideWindow(WindowID.WindowID_MessageBox);
        }

        // 
        // Depth Helper Functions
        // 

        // Push target GameObject to top depth
        // Case: when you open multi PopWindow
        // You want one of these PopWindow stay at the Top 
        // You can register the EventSystemDefine.EventUIFrameWorkPopRootWindowAdded 
        // Call this method to push window to top
        public static void AdjustTargetWindowDepthToTop(UIWindowBase targetWindow)
        {
            if (targetWindow == null)
                return;

            Transform windowRoot = UICenterMasterManager.Instance.GetTargetRoot(targetWindow.windowData.windowType);
            int needDepth = Mathf.Clamp(GameUtility.GetMaxTargetDepth(windowRoot.gameObject, true) + 1, popUpWindowDepth, int.MaxValue);
            GameUtility.SetTargetMinPanelDepth(targetWindow.gameObject, needDepth);
            targetWindow.MinDepth = needDepth;
        }
    }
}