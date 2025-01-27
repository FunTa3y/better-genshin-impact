﻿using System;
using System.Threading;
using Vanara.PInvoke;

namespace BetterGenshinImpact.Core.Simulator;

/// <summary>
///     код виртуального ключа
///     https://learn.microsoft.com/zh-cn/windows/win32/inputdev/virtual-key-codes
///     User32.VK.VK_SPACE пробел на клавиатуре
/// </summary>
public class PostMessageSimulator
{
    public static readonly uint WM_LBUTTONDOWN = 0x201; //Нажмите левую кнопку мыши

    public static readonly uint WM_LBUTTONUP = 0x202; //Отпустите левую кнопку мыши

    public static readonly uint WM_RBUTTONDOWN = 0x204;
    public static readonly uint WM_RBUTTONUP = 0x205;

    private readonly IntPtr _hWnd;

    public PostMessageSimulator(IntPtr hWnd)
    {
        _hWnd = hWnd;
    }

    /// <summary>
    ///     Укажите местоположение и нажмите левую кнопку
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public PostMessageSimulator LeftButtonClick(int x, int y)
    {
        IntPtr p = (y << 16) | x;
        User32.PostMessage(_hWnd, WM_LBUTTONDOWN, IntPtr.Zero, p);
        Thread.Sleep(100);
        User32.PostMessage(_hWnd, WM_LBUTTONUP, IntPtr.Zero, p);
        return this;
    }

    /// <summary>
    ///     Укажите местоположение и нажмите левую кнопку
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public PostMessageSimulator LeftButtonClickBackground(int x, int y)
    {
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_ACTIVATE, 1, 0);
        var p = MakeLParam(x, y);
        User32.PostMessage(_hWnd, WM_LBUTTONDOWN, 1, p);
        Thread.Sleep(100);
        User32.PostMessage(_hWnd, WM_LBUTTONUP, 0, p);
        return this;
    }

    public static int MakeLParam(int x, int y) => (y << 16) | (x & 0xFFFF);

    public PostMessageSimulator LeftButtonClick()
    {
        IntPtr p = (16 << 16) | 16;
        User32.PostMessage(_hWnd, WM_LBUTTONDOWN, IntPtr.Zero, p);
        Thread.Sleep(100);
        User32.PostMessage(_hWnd, WM_LBUTTONUP, IntPtr.Zero, p);
        return this;
    }

    public PostMessageSimulator LeftButtonClickBackground()
    {
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_ACTIVATE, 1, 0);
        IntPtr p = (16 << 16) | 16;
        User32.PostMessage(_hWnd, WM_LBUTTONDOWN, IntPtr.Zero, p);
        Thread.Sleep(100);
        User32.PostMessage(_hWnd, WM_LBUTTONUP, IntPtr.Zero, p);
        return this;
    }

    /// <summary>
    ///     Положение по умолчанию, левая кнопка нажата
    /// </summary>
    public PostMessageSimulator LeftButtonDown()
    {
        User32.PostMessage(_hWnd, WM_LBUTTONDOWN, IntPtr.Zero);
        return this;
    }

    /// <summary>
    ///     Позиция по умолчанию. Отпуск левой кнопки.
    /// </summary>
    public PostMessageSimulator LeftButtonUp()
    {
        User32.PostMessage(_hWnd, WM_LBUTTONUP, IntPtr.Zero);
        return this;
    }

    /// <summary>
    ///     Позиция по умолчанию, щелкните правой кнопкой мыши
    /// </summary>
    public PostMessageSimulator RightButtonDown()
    {
        User32.PostMessage(_hWnd, WM_RBUTTONDOWN, IntPtr.Zero);
        return this;
    }

    /// <summary>
    ///     Щелкните правой кнопкой мыши и отпустите в положении по умолчанию.
    /// </summary>
    public PostMessageSimulator RightButtonUp()
    {
        User32.PostMessage(_hWnd, WM_RBUTTONUP, IntPtr.Zero);
        return this;
    }

    public PostMessageSimulator RightButtonClick()
    {
        IntPtr p = (16 << 16) | 16;
        User32.PostMessage(_hWnd, WM_RBUTTONDOWN, IntPtr.Zero, p);
        Thread.Sleep(100);
        User32.PostMessage(_hWnd, WM_RBUTTONUP, IntPtr.Zero, p);
        return this;
    }

    public PostMessageSimulator KeyPress(User32.VK vk)
    {
        //User32.PostMessage(_hWnd, User32.WindowMessage.WM_ACTIVATE, 1, 0);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYDOWN, (nint)vk, 0x1e0001);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_CHAR, (nint)vk, 0x1e0001);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYUP, (nint)vk, (nint)0xc01e0001);
        return this;
    }

    public PostMessageSimulator KeyPress(User32.VK vk, int ms)
    {
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYDOWN, (nint)vk, 0x1e0001);
        Thread.Sleep(ms);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_CHAR, (nint)vk, 0x1e0001);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYUP, (nint)vk, (nint)0xc01e0001);
        return this;
    }

    public PostMessageSimulator LongKeyPress(User32.VK vk)
    {
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYDOWN, (nint)vk, 0x1e0001);
        Thread.Sleep(1000);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_CHAR, (nint)vk, 0x1e0001);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYUP, (nint)vk, (nint)0xc01e0001);
        return this;
    }

    public PostMessageSimulator KeyDown(User32.VK vk)
    {
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYDOWN, (nint)vk, 0x1e0001);
        return this;
    }

    public PostMessageSimulator KeyUp(User32.VK vk)
    {
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYUP, (nint)vk, (nint)0xc01e0001);
        return this;
    }

    public PostMessageSimulator KeyPressBackground(User32.VK vk)
    {
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_ACTIVATE, 1, 0);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYDOWN, (nint)vk, 0x1e0001);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_CHAR, (nint)vk, 0x1e0001);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYUP, (nint)vk, (nint)0xc01e0001);
        return this;
    }

    public PostMessageSimulator KeyDownBackground(User32.VK vk)
    {
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_ACTIVATE, 1, 0);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYDOWN, (nint)vk, 0x1e0001);
        return this;
    }

    public PostMessageSimulator KeyUpBackground(User32.VK vk)
    {
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_ACTIVATE, 1, 0);
        User32.PostMessage(_hWnd, User32.WindowMessage.WM_KEYUP, (nint)vk, (nint)0xc01e0001);
        return this;
    }

    public PostMessageSimulator Sleep(int ms)
    {
        Thread.Sleep(ms);
        return this;
    }
}
