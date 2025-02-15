﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STranslate.Helper;
using STranslate.Log;
using STranslate.Model;
using STranslate.ViewModels.Preference.History;
using STranslate.Views.Preference.History;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace STranslate.ViewModels.Preference
{
    public partial class HistoryViewModel : ObservableObject
    {
        public HistoryViewModel()
        {
            RefreshCommand.Execute(null);
        }

        [RelayCommand]
        private async Task RefreshAsync(ScrollViewer? scroll)
        {
            var historyModels = await SqlHelper.GetDataAsync();
            HistoryList = new BindingList<HistoryModel>(historyModels.Reverse().ToList());

            Count = HistoryList.Count;

            if (Count > 0)
            {
                SelectedIndex = 0;
                HistoryDetailContent = new HistoryContentPage(new HistoryContentViewModel(HistoryList.FirstOrDefault()));
            }
            else
            {
                HistoryDetailContent = null;
            }

            scroll?.ScrollToTop();

            ToastHelper.Show("刷新历史记录", WindowType.Preference);
        }

        [RelayCommand]
        private void Popup(Popup control) => control.IsOpen = true;

        /// <summary>
        /// 删除某条记录
        /// </summary>
        [RelayCommand]
        private async Task DeleteHistoryAsync()
        {
            if (HistoryList == null || HistoryList.Count < 1)
            {
                return;
            }

            var tmpIndex = SelectedIndex;
            var history = HistoryList.ElementAtOrDefault(SelectedIndex);
            if (history == null || !await SqlHelper.DeleteDataAsync(history))
            {
                LogService.Logger.Warn($"删除失败，{history}");

                ToastHelper.Show("删除失败", WindowType.Preference);
                return;
            }
            HistoryList.RemoveAt(SelectedIndex);
            SelectedIndex = tmpIndex < HistoryList.Count ? tmpIndex : tmpIndex - 1;
            Count--;

            UpdateHistoryDetailContent();

            ToastHelper.Show("删除成功", WindowType.Preference);
        }

        /// <summary>
        /// 删除所有记录
        /// </summary>
        [RelayCommand]
        private async Task DeleteAllHistoryAsync(Popup control)
        {
            if (await SqlHelper.DeleteAllDataAsync())
            {
                HistoryList?.Clear();
                Count = 0;
                SelectedIndex = -1;
                HistoryDetailContent = null;

                ToastHelper.Show("删除全部成功", WindowType.Preference);
            }

            control.IsOpen = false;
        }

        /// <summary>
        /// 更新历史详情UI
        /// </summary>
        private void UpdateHistoryDetailContent()
        {
            if (SelectedIndex != -1)
            {
                HistoryDetailContent = new HistoryContentPage(new HistoryContentViewModel(HistoryList![SelectedIndex]));
            }
            else
            {
                HistoryDetailContent = null;
            }
        }

        /// <summary>
        /// 导航页面
        /// </summary>
        /// <param name="model"></param>
        [RelayCommand]
        private void TogglePage(HistoryModel? model)
        {
            if (model == null)
                return;

            // 防止重入
            if (!_isSelectionChanging)
            {
                _isSelectionChanging = true;

                try
                {
                    var method = typeof(HistoryContentPage).GetMethod("UpdateVM");
                    method?.Invoke(HistoryDetailContent, new[] { new HistoryContentViewModel(model) });
                }
                catch (Exception ex)
                {
                    LogService.Logger.Error("历史记录导航出错", ex);
                }

                _isSelectionChanging = false;
            }
        }

        /// <summary>
        /// SelectedChanged 可能会先触发 "SelectionChanged" 事件
        /// 然后再触发 "LostFocus" 事件，导致 Command 被调用两次
        /// </summary>
        private bool _isSelectionChanging = false;


        [ObservableProperty]
        private List<string> eventList = ["清空全部"];

        [ObservableProperty]
        private int _selectedIndex;

        [ObservableProperty]
        private int _count = 0;

        [ObservableProperty]
        private UIElement? _historyDetailContent;

        [ObservableProperty]
        private BindingList<HistoryModel>? _historyList;
    }
}
