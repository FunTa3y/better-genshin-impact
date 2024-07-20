﻿using Wpf.Ui.Controls;

namespace BetterGenshinImpact.Helpers;

public class WpfUiMessageBoxHelper
{
    public static void Show(string title, string content)
    {
        var uiMessageBox = new MessageBox
        {
            Title = title,
            Content = content,
            CloseButtonText = "Конечно",
        };

        uiMessageBox.ShowDialogAsync();
    }

    public static void Show(string content)
    {
        Show("намекать", content);
    }
}
