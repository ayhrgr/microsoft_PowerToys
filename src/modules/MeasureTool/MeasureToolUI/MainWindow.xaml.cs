﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Graphics;
using WinUIEx;

namespace MeasureToolUI
{
    using static NativeMethods;

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        private const int WindowWidth = 216;
        private const int WindowHeight = 50;

        private PowerToys.MeasureToolCore.Core _coreLogic = new PowerToys.MeasureToolCore.Core();

        private AppWindow _appWindow;
        private PointInt32 _initialPosition;

        protected override void OnPositionChanged(PointInt32 position)
        {
            _appWindow.Move(_initialPosition);
            this.SetWindowSize(WindowWidth, WindowHeight);
        }

        public MainWindow()
        {
            InitializeComponent();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            var presenter = _appWindow.Presenter as OverlappedPresenter;
            presenter.IsAlwaysOnTop = true;
            this.SetIsAlwaysOnTop(true);
            this.SetIsShownInSwitchers(false);
            this.SetIsResizable(false);
            this.SetIsMinimizable(false);
            this.SetIsMaximizable(false);
            IsTitleBarVisible = false;

            DisplayArea displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            float dpiScale = _coreLogic.GetDPIScaleForWindow((int)hwnd);

            _initialPosition = new PointInt32(displayArea.WorkArea.X + (displayArea.WorkArea.Width / 2) - (int)(dpiScale * WindowWidth / 2), displayArea.WorkArea.Y + (int)(dpiScale * 12));

            _coreLogic.SetToolbarBoundingBox(
                _initialPosition.X,
                _initialPosition.Y,
                _initialPosition.X + (int)(dpiScale * WindowWidth),
                _initialPosition.Y + (int)(dpiScale * WindowHeight));
            OnPositionChanged(_initialPosition);
        }

        private void UpdateToolUsageCompletionEvent(object sender)
        {
            _coreLogic.SetToolCompletionEvent(new PowerToys.MeasureToolCore.ToolSessionCompleted(() =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ((ToggleButton)sender).IsChecked = false;
                });
            }));
        }

        private void UncheckOtherButtons(ToggleButton button)
        {
            var panel = button.Parent as Panel;
            foreach (var elem in panel.Children)
            {
                if (elem is ToggleButton otherButton)
                {
                    if (!button.Equals(otherButton))
                    {
                        otherButton.IsChecked = false;
                    }
                }
            }
        }

        private void HandleToolClick(object toolButton, Action startToolAction)
        {
            ToggleButton button = toolButton as ToggleButton;
            if (button == null)
            {
                return;
            }

            if (button.IsChecked.GetValueOrDefault())
            {
                UncheckOtherButtons(button);
                _coreLogic.ResetState();
                UpdateToolUsageCompletionEvent(toolButton);
                startToolAction();
            }
            else
            {
                _coreLogic.ResetState();
            }
        }

        private void BoundsTool_Click(object sender, RoutedEventArgs e)
        {
            HandleToolClick(sender, () => _coreLogic.StartBoundsTool());
        }

        private void MeasureTool_Click(object sender, RoutedEventArgs e)
        {
            HandleToolClick(sender, () => _coreLogic.StartMeasureTool(true, true));
        }

        private void HorizontalMeasureTool_Click(object sender, RoutedEventArgs e)
        {
            HandleToolClick(sender, () => _coreLogic.StartMeasureTool(true, false));
        }

        private void VerticalMeasureTool_Click(object sender, RoutedEventArgs e)
        {
            HandleToolClick(sender, () => _coreLogic.StartMeasureTool(false, true));
        }

        private void ClosePanelTool_Click(object sender, RoutedEventArgs e)
        {
            _coreLogic.ResetState();
            this.Close();
        }
    }
}
