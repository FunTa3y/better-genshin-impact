using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BetterGenshinImpact.ViewModel.Windows;

public abstract partial class FormViewModel<T> : ObservableObject
{
    [ObservableProperty] private ObservableCollection<T> _list;

    protected FormViewModel()
    {
        _list = new();
    }

    public void AddRange(List<T> itemList)
    {
        foreach (var item in itemList)
        {
            List.Add(item);
        }
    }

    /// <summary>
    /// Добавлено в шапку по умолчанию
    /// </summary>
    /// <param name="item"></param>
    [RelayCommand]
    public void OnAdd(T item)
    {
        List.Insert(0, item);
    }

    [RelayCommand]
    public void OnRemoveAt(int index)
    {
        List.RemoveAt(index);
    }

    [RelayCommand]
    public void OnEditAt(int index)
    {
        // Изменить строку данных
        // всплывающее окно редактирования
        // ...

        // Сохранить результаты
        // List[index] = ?;
    }

    [RelayCommand]
    public void OnSave()
    {
        // Сохраните весь измененный результат
    }
}