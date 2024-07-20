﻿using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recorder;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wpf.Ui;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace BetterGenshinImpact.ViewModel.Pages;

public partial class KeyMouseRecordPageViewModel : ObservableObject, INavigationAware, IViewModel
{
    private readonly ILogger<KeyMouseRecordPageViewModel> _logger = App.GetLogger<KeyMouseRecordPageViewModel>();
    private readonly string scriptPath = Global.Absolute(@"User\KeyMouseScript");

    [ObservableProperty]
    private ObservableCollection<KeyMouseScriptItem> _scriptItems = [];

    [ObservableProperty]
    private bool _isRecording = false;

    private ISnackbarService _snackbarService;

    public KeyMouseRecordPageViewModel(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
    }

    private void InitScriptListViewData()
    {
        _scriptItems.Clear();
        var fileInfos = LoadScriptFiles(scriptPath);
        fileInfos = fileInfos.OrderByDescending(f => f.CreationTime).ToList();
        foreach (var f in fileInfos)
        {
            _scriptItems.Add(new KeyMouseScriptItem
            {
                Name = f.Name,
                CreateTime = f.CreationTime,
                CreateTimeStr = f.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
    }

    private List<FileInfo> LoadScriptFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var files = Directory.GetFiles(folder, "*.*",
            SearchOption.AllDirectories);

        return files.Select(file => new FileInfo(file)).ToList();
    }

    public void OnNavigatedTo()
    {
        InitScriptListViewData();
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    public void OnStartRecord()
    {
        if (!TaskContext.Instance().IsInitialized)
        {
            MessageBox.Show("Пожалуйста, сначала перейдите на стартовую страницу，Запустите программу создания снимков экрана, а затем используйте эту функцию");
            return;
        }
        if (!IsRecording)
        {
            IsRecording = true;
            GlobalKeyMouseRecord.Instance.StartRecord();
        }
    }

    [RelayCommand]
    public void OnStopRecord()
    {
        if (IsRecording)
        {
            IsRecording = false;
            var macro = GlobalKeyMouseRecord.Instance.StopRecord();
            // Genshin Copilot Macro
            File.WriteAllText(Path.Combine(scriptPath, $"BetterGI_GCM_{DateTime.Now:yyyyMMddHHmmssffff}.json"), macro);
            // обновитьListView
            InitScriptListViewData();
        }
    }

    [RelayCommand]
    public async Task OnStartPlay(string name)
    {
        _logger.LogInformation("Повтор начинается：{Name}", name);
        try
        {
            var s = await File.ReadAllTextAsync(Path.Combine(scriptPath, name));
            await KeyMouseMacroPlayer.PlayMacro(s);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Исключение произошло при воспроизведении сценария");
        }
        finally
        {
            _logger.LogInformation("Повтор заканчивается：{Name}", name);
        }
    }

    [RelayCommand]
    public void OnOpenScriptFolder()
    {
        Process.Start("explorer.exe", scriptPath);
    }

    [RelayCommand]
    public void OnDeleteScript(KeyMouseScriptItem item)
    {
        try
        {
            File.Delete(Path.Combine(scriptPath, item.Name));
            _snackbarService.Show(
                "успешно удалено",
                $"{item.Name} был удален",
                ControlAppearance.Success,
                null,
                TimeSpan.FromSeconds(2)
            );
        }
        catch (Exception e)
        {
            _snackbarService.Show(
                "не удалось удалить",
                $"{item.Name} не удалось удалить",
                ControlAppearance.Danger,
                null,
                TimeSpan.FromSeconds(3)
            );
        }
        finally
        {
            InitScriptListViewData();
        }
    }
}
