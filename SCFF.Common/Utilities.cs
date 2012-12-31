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

/// @file SCFF.Common/Utilities.cs
/// SCFF.Commonモジュール共通で利用する機能

namespace SCFF.Common {

using System;
using System.Text;

/// SCFF.Commonモジュール共通で利用する機能
public static class Utilities {

  /// Desktop(VirtualScreen)座標からScreen座標へ変換する
  private static void DesktopToScreen(int desktopX, int desktopY, out int screenX, out int screenY) {
    // desktopPointは仮想画面上の座標(左上が(0,0)であることが保障されている)
    // screenPointはプライマリモニタの左上の座標が(0,0)なので-になることもある
    screenX = desktopX + ExternalAPI.GetSystemMetrics(ExternalAPI.SM_XVIRTUALSCREEN);
    screenY = desktopY + ExternalAPI.GetSystemMetrics(ExternalAPI.SM_YVIRTUALSCREEN);
  }

  /// 特定のOSに依存せずにDesktopListViewWindowを取得する
  ///
  /// デスクトップのHWNDの階層関係は以下のとおり:
  /// - GetDesktopWindow()
  ///   - Progman (XP/Win7(No Aero)/Vista(No Aero))
  ///     - SHELLDLL_DefView (XP/Win7 No Aero/Vista No Aero?)
  ///       - Internet Exproler_Server (XP Active Desktop)
  ///       - SysListView32 (XP?/Win7 No Aero/Vista No Aero?)
  ///   - WorkerW[/WorkerW]* (Win7 Aero/Vista Aero?)
  ///     - SHELLDLL_DefView
  ///       - SysListView32
  ///   - EdgeUiInputWndClass (Win 8)
  ///
  /// パッと見る限り明らかに重いのはAero On時。EnumWindows必須。
  public static UIntPtr DesktopListViewWindow {
    get {
      UIntPtr progman = ExternalAPI.FindWindowEx(UIntPtr.Zero, UIntPtr.Zero, "Progman", null);
      if (progman != UIntPtr.Zero) {
        // XP/Win7(No Aero)/Vista(No Aero)
        UIntPtr shellDLLDefView = ExternalAPI.FindWindowEx(progman, UIntPtr.Zero, "SHELLDLL_DefView", null);
        if (shellDLLDefView != UIntPtr.Zero) {
          UIntPtr sysListView32 = ExternalAPI.FindWindowEx(shellDLLDefView, UIntPtr.Zero, "SysListView32", null);
          if (sysListView32 != UIntPtr.Zero) {
            // XP(No ActiveDesktop)/Win7(No Aero)/Vista(No Aero)
            return sysListView32;
          } 
          UIntPtr internetExprolerServer = ExternalAPI.FindWindowEx(shellDLLDefView, UIntPtr.Zero, "Internet Exproler_Server", null);
          if (internetExprolerServer != UIntPtr.Zero) {
            // XP(ActiveDesktop)
            return internetExprolerServer;
          }
        }
      }
      UIntPtr edgeUiInputWndClass = ExternalAPI.FindWindowEx(UIntPtr.Zero, UIntPtr.Zero, "EdgeUiInputWndClass", null);
      if (edgeUiInputWndClass != UIntPtr.Zero) {
        // Win8
        return edgeUiInputWndClass;
      }
      enumerateWindowResult = UIntPtr.Zero;
      ExternalAPI.EnumWindows(new ExternalAPI.WNDENUMProc(EnumerateWindow), IntPtr.Zero);
      if (enumerateWindowResult != UIntPtr.Zero) {
        // Win7(Aero)/Vista(Aero)
        UIntPtr sysListView32 = ExternalAPI.FindWindowEx(enumerateWindowResult, UIntPtr.Zero, "SysListView32", null);
        if (sysListView32 != UIntPtr.Zero) {
          return sysListView32;
        }
      }
      return ExternalAPI.GetDesktopWindow();
    }
  }

  //-------------------------------------------------------------------
  // private staticメソッド
  //-------------------------------------------------------------------

  /// EnumerateWindowの結果を格納するポインタ
  private static UIntPtr enumerateWindowResult = UIntPtr.Zero;
  /// FindWindowExに渡されるウィンドウ列挙関数
  private static bool EnumerateWindow(UIntPtr hWnd, IntPtr lParam) {
    StringBuilder className = new StringBuilder(256);
    ExternalAPI.GetClassName(hWnd, className, 256);
    // "WorkerW"以外はスキップ
    if (className.ToString() != "WorkerW") return true;

    // "WorkerW" > "SHELLDLL_DefView"になってなければスキップ
    UIntPtr shellDLLDefView = ExternalAPI.FindWindowEx(hWnd, UIntPtr.Zero, "SHELLDLL_DefView", null);
    if (shellDLLDefView == UIntPtr.Zero) return true;
      
    enumerateWindowResult = shellDLLDefView;
    return false;
  }
}
}
