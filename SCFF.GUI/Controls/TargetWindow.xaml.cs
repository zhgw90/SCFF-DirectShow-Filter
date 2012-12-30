﻿// Copyright 2012 Alalf <alalf.iQLc_at_gmail.com>
//
// This file is part of SCFF-DirectShow-Filter(SCFF DSF).
//
// SCFF DSF is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SCFF DSF is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SCFF DSF.  If not, see <http://www.gnu.org/licenses/>.

/// @file SCFF.GUI/Controls/TargetWindow.cs
/// ウィンドウ取り込み対象の設定用コントロール

namespace SCFF.GUI.Controls {

using SCFF.Common;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;

/// ウィンドウ取り込み対象の設定用コントロール
public partial class TargetWindow : UserControl, IProfileToControl {

  private IntPtr dummyPen = IntPtr.Zero;

  /// コンストラクタ
  public TargetWindow() {
    InitializeComponent();

    Debug.WriteLine("TargetWindow: Dummy Pen is created");
    this.dummyPen = ExternalAPI.CreatePen(ExternalAPI.PS_NULL, 1, 0x00000000);
    this.Dispatcher.ShutdownStarted += OnShutdownStarted;
  }

  private void OnShutdownStarted(object sender, EventArgs e) {
    if (this.dummyPen != IntPtr.Zero) {
      Debug.WriteLine("TargetWindow: Dummy Pen is deleted");
      ExternalAPI.DeleteObject(this.dummyPen);
      this.dummyPen = IntPtr.Zero;
    }
  }

  //===================================================================
  // IProfileToControlの実装
  //===================================================================

  /// @copydoc IProfileToControl.UpdateByProfile
  public void UpdateByProfile() {
    // *Changedイベントハンドラがないのでそのまま代入するだけ
    this.WindowCaption.Text = App.Profile.CurrentInputLayoutElement.WindowCaption;
  }

  /// @copydoc IProfileToControl.AttachChangedEventHandlers
  public void AttachChangedEventHandlers() {
    // nop
  }

  /// @copydoc IProfileToControl.DetachChangedEventHandlers
  public void DetachChangedEventHandlers() {
    // nop
  }

  //===================================================================
  // イベントハンドラ
  //===================================================================

  private bool dragHereMode = false;
  private UIntPtr currentTargetWindow = UIntPtr.Zero;
  private IntPtr currentTargetDC = IntPtr.Zero;
  private Brush originalBorderBrush;

  /// 現在のターゲットウィンドウをXORで塗りつぶす
  private void XorTargetRect() {
    ExternalAPI.RECT currentTargetRect;
    ExternalAPI.GetClientRect(this.currentTargetWindow, out currentTargetRect);
    
    var originalDrawMode = ExternalAPI.SetROP2(this.currentTargetDC, ExternalAPI.R2_XORPEN);
    var originalPen = ExternalAPI.SelectObject(this.currentTargetDC, this.dummyPen);

    ExternalAPI.Rectangle(this.currentTargetDC,
        currentTargetRect.Left, currentTargetRect.Top,
        currentTargetRect.Right, currentTargetRect.Bottom);

    ExternalAPI.SelectObject(this.currentTargetDC, originalPen);
    ExternalAPI.SetROP2(this.currentTargetDC, originalDrawMode);
  }

  /// 
  private void ClearTargetRect() {
    if (this.currentTargetWindow != UIntPtr.Zero) {
      // 取り込み対象範囲のXOR描画(もともとXORされていたので元に戻る)
      this.XorTargetRect();
    }
    // DCを破棄
    if (this.currentTargetDC != IntPtr.Zero) {
      ExternalAPI.ReleaseDC(this.currentTargetWindow, this.currentTargetDC);
      this.currentTargetDC = IntPtr.Zero;
    }
  }

  private void DragHere_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
    this.originalBorderBrush = this.DragHere.BorderBrush;
    this.DragHere.BorderBrush = Brushes.DarkOrange;

    this.dragHereMode = true;
    this.currentTargetWindow = UIntPtr.Zero;
    this.currentTargetDC = IntPtr.Zero;
  }

  private void DragHere_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
   if (!this.dragHereMode) {
      // nop
      return;
    }

    var screenPoint = this.DragHere.PointToScreen(e.GetPosition(this.DragHere));
    UIntPtr nextTargetWindow = ExternalAPI.WindowFromPoint((int)screenPoint.X, (int)screenPoint.Y);
    if (nextTargetWindow == UIntPtr.Zero) {
      // nop
      return;
    }

    if (this.currentTargetWindow == UIntPtr.Zero) {
      // 初回実行: DragHereボタンになっているはずなので無視
      this.currentTargetWindow = nextTargetWindow;
    }

    if (nextTargetWindow == this.currentTargetWindow) {
      // 対象が変わってないのでnop
      return;
    }

    // ウィンドウが異なる場合、枠線を描画していたウィンドウをアップデートして枠線を消しておく
    this.ClearTargetRect();

    // 現在処理中のウィンドウを更新
    this.currentTargetWindow = nextTargetWindow;
    this.currentTargetDC = ExternalAPI.GetDC(this.currentTargetWindow);

    // 取り込み対象範囲の描画
    this.XorTargetRect();
  }

  private void UpdateProfile(WindowTypes nextWindowType, UIntPtr nextTargetWindow) {
    // Window
    switch (nextWindowType) {
      case WindowTypes.Normal: {
        App.Profile.CurrentOutputLayoutElement.SetWindow(nextTargetWindow);
        break;
      }
      case WindowTypes.DesktopListView: {
        App.Profile.CurrentOutputLayoutElement.SetWindowToDesktopListView();
        break;
      }
      case WindowTypes.Desktop: {
        App.Profile.CurrentOutputLayoutElement.SetWindowToDesktop();
        break;
      }
      default: {
        Debug.Fail("UpdateProfile: Invalid WindowType");
        return;
      }
    }
    App.Profile.CurrentOutputLayoutElement.Fit = true;

    // Clipping*WithoutFitの補正
    // とりあえず0,0を原点にもってくる(ウィンドウが変われば左上座標に意味はなくなるから)
    App.Profile.CurrentOutputLayoutElement.ClippingXWithoutFit = 0;
    App.Profile.CurrentOutputLayoutElement.ClippingYWithoutFit = 0;
    // Width/HeightはFitした時の値をとりあえず上限とする
    App.Profile.CurrentOutputLayoutElement.ClippingWidthWithoutFit = Math.Min(
      App.Profile.CurrentInputLayoutElement.ClippingWidthWithoutFit,
      App.Profile.CurrentInputLayoutElement.WindowWidth);
    App.Profile.CurrentOutputLayoutElement.ClippingHeightWithoutFit = Math.Min(
      App.Profile.CurrentInputLayoutElement.ClippingHeightWithoutFit,
      App.Profile.CurrentInputLayoutElement.WindowHeight);

    // コマンドをMainWindowに送信して関連するコントロールを更新
    Commands.ChangeTargetWindowCommand.Execute(null, null);
  }

  private void DragHere_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
    this.DragHere.BorderBrush = this.originalBorderBrush;

    // 後処理
    this.dragHereMode = false;
    this.ClearTargetRect();

    // マウスカーソルからウィンドウを取得
    var screenPoint = this.DragHere.PointToScreen(e.GetPosition(this.DragHere));
    UIntPtr nextTargetWindow = ExternalAPI.WindowFromPoint((int)screenPoint.X, (int)screenPoint.Y);

    // Profileを更新
    this.UpdateProfile(WindowTypes.Normal, nextTargetWindow);
  }

  private void Desktop_Click(object sender, System.Windows.RoutedEventArgs e) {
    // Profileを更新
    this.UpdateProfile(WindowTypes.Desktop, UIntPtr.Zero);
  }

  private void ListView_Click(object sender, System.Windows.RoutedEventArgs e) {
    // Profileを更新
    this.UpdateProfile(WindowTypes.DesktopListView, UIntPtr.Zero);
  }
}
}