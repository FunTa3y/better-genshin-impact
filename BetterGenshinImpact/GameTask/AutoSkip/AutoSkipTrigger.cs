﻿using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoSkip.Assets;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Service;
using BetterGenshinImpact.View.Drawable;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Sdcb.PaddleOCR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Vanara.PInvoke;
using static System.Net.Mime.MediaTypeNames;

namespace BetterGenshinImpact.GameTask.AutoSkip;

/// <summary>
/// 自动剧情有选项点击
/// </summary>
public class AutoSkipTrigger : ITaskTrigger
{
    private readonly ILogger<AutoSkipTrigger> _logger = App.GetLogger<AutoSkipTrigger>();

    public string Name => "自动剧情";
    public bool IsEnabled { get; set; }
    public int Priority => 20;
    public bool IsExclusive => false;

    private readonly AutoSkipAssets _autoSkipAssets;

    private readonly AutoSkipConfig _config;

    /// <summary>
    /// 不自动点击的选项，优先级低于橙色文字点击
    /// </summary>
    private List<string> _defaultPauseList = new();

    /// <summary>
    /// 不自动点击的选项
    /// </summary>
    private List<string> _pauseList = new();

    /// <summary>
    /// 优先自动点击的选项
    /// </summary>
    private List<string> _selectList = new();


    public AutoSkipTrigger()
    {
        _autoSkipAssets = new AutoSkipAssets();
        _config = TaskContext.Instance().Config.AutoSkipConfig;
    }

    public void Init()
    {
        IsEnabled = TaskContext.Instance().Config.AutoSkipConfig.Enabled;

        try
        {
            var defaultPauseListJson = Global.ReadAllTextIfExist(@"User\AutoSkip\default_pause_options.json");
            if (!string.IsNullOrEmpty(defaultPauseListJson))
            {
                _defaultPauseList = JsonSerializer.Deserialize<List<string>>(defaultPauseListJson, ConfigService.JsonOptions) ?? new List<string>();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "读取自动剧情默认暂停点击关键词列表失败");
            MessageBox.Show("读取自动剧情默认暂停点击关键词列表失败，请确认修改后的自动剧情默认暂停点击关键词内容格式是否正确！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        try
        {
            var pauseListJson = Global.ReadAllTextIfExist(@"User\AutoSkip\pause_options.json");
            if (!string.IsNullOrEmpty(pauseListJson))
            {
                _pauseList = JsonSerializer.Deserialize<List<string>>(pauseListJson, ConfigService.JsonOptions) ?? new List<string>();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "读取自动剧情暂停点击关键词列表失败");
            MessageBox.Show("读取自动剧情暂停点击关键词列表失败，请确认修改后的自动剧情暂停点击关键词内容格式是否正确！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        try
        {
            var selectListJson = Global.ReadAllTextIfExist(@"User\AutoSkip\select_options.json");
            if (!string.IsNullOrEmpty(selectListJson))
            {
                _selectList = JsonSerializer.Deserialize<List<string>>(selectListJson, ConfigService.JsonOptions) ?? new List<string>();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "读取自动剧情优先点击选项列表失败");
            MessageBox.Show("读取自动剧情优先点击选项列表失败，请确认修改后的自动剧情优先点击选项内容格式是否正确！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 上一次播放中的帧
    /// </summary>
    private DateTime _prevPlayingTime = DateTime.MinValue;

    private DateTime _prevExecute = DateTime.MinValue;

    private DateTime _prevGetDailyRewardsTime = DateTime.MinValue;

    private DateTime _prevClickTime = DateTime.MinValue;

    public void OnCapture(CaptureContent content)
    {
        if ((DateTime.Now - _prevExecute).TotalMilliseconds <= 200)
        {
            return;
        }

        _prevExecute = DateTime.Now;


        var assetScale = TaskContext.Instance().SystemInfo.AssetScale;

        GetDailyRewardsEsc(_config, content);

        // 找左上角剧情自动的按钮
        using var foundRectArea = content.CaptureRectArea.Find(_autoSkipAssets.StopAutoButtonRo);

        var isPlaying = !foundRectArea.IsEmpty(); // 播放中

        // 播放中图标消失3s内OCR判断文字
        if (!isPlaying && (DateTime.Now - _prevPlayingTime).TotalSeconds <= 3)
        {
            // 找播放中的文字
            content.CaptureRectArea.Find(_autoSkipAssets.PlayingTextRo, _ => { isPlaying = true; });
            if (!isPlaying)
            {
                var textRa = content.CaptureRectArea.Crop(_autoSkipAssets.PlayingTextRo.RegionOfInterest);
                // 过滤出白色
                var hsvFilterMat = OpenCvCommonHelper.InRangeHsv(textRa.SrcMat, new Scalar(0, 0, 170), new Scalar(255, 80, 245));
                var result = OcrFactory.Paddle.Ocr(hsvFilterMat);
                if (result.Contains("播") || result.Contains("番") || result.Contains("放") || result.Contains("中") || result.Contains("潘") || result.Contains("故"))
                {
                    VisionContext.Instance().DrawContent.PutRect("PlayingText", textRa.ConvertRelativePositionToCaptureArea().ToRectDrawable());
                    isPlaying = true;
                }
            }


            if (!isPlaying)
            {
                // 关闭弹出页
                content.CaptureRectArea.Find(_autoSkipAssets.PageCloseRo, pageCloseRoRa =>
                {
                    pageCloseRoRa.ClickCenter();

                    AutoSkipLog("关闭弹出页");
                    isPlaying = true;
                    pageCloseRoRa.Dispose();
                });
            }
        }
        else
        {
            VisionContext.Instance().DrawContent.RemoveRect("PlayingText");
        }

        if (isPlaying)
        {
            _prevPlayingTime = DateTime.Now;
            if (TaskContext.Instance().Config.AutoSkipConfig.QuicklySkipConversationsEnabled)
            {
                Simulation.SendInputEx.Keyboard.KeyPress(User32.VK.VK_SPACE);
            }

            ChatOptionChoose(content);

            // // 领取每日委托奖励
            // if (config.AutoGetDailyRewardsEnabled)
            // {
            //     var dailyRewardIconRa = content.CaptureRectArea.Find(_autoSkipAssets.DailyRewardIconRo);
            //     if (!dailyRewardIconRa.IsEmpty())
            //     {
            //         var text = GetOrangeOptionText(content.CaptureRectArea.SrcMat, dailyRewardIconRa, (int)(config.ChatOptionTextWidth * assetScale));
            //
            //         if (text.Contains("每日委托"))
            //         {
            //             if (Math.Abs(content.FrameIndex - _prevOtherClickFrameIndex) >= 8)
            //             {
            //                 _logger.LogInformation("自动选择：{Text}", text);
            //             }
            //
            //             dailyRewardIconRa.ClickCenter();
            //             dailyRewardIconRa.Dispose();
            //             _prevGetDailyRewards = DateTime.Now; // 记录领取时间
            //             return;
            //         }
            //
            //         _prevOtherClickFrameIndex = content.FrameIndex;
            //         dailyRewardIconRa.Dispose();
            //     }
            // }
            //
            // // 领取探索派遣奖励
            // if (config.AutoReExploreEnabled)
            // {
            //     var exploreIconRa = content.CaptureRectArea.Find(_autoSkipAssets.ExploreIconRo);
            //     if (!exploreIconRa.IsEmpty())
            //     {
            //         var text = GetOrangeOptionText(content.CaptureRectArea.SrcMat, exploreIconRa, (int)(config.ExpeditionOptionTextWidth * assetScale));
            //         if (text.Contains("探索派遣"))
            //         {
            //             if (Math.Abs(content.FrameIndex - _prevOtherClickFrameIndex) >= 8)
            //             {
            //                 _logger.LogInformation("自动选择：{Text}", text);
            //             }
            //
            //             exploreIconRa.ClickCenter();
            //
            //             // 等待探索派遣界面打开
            //             Thread.Sleep(800);
            //             new OneKeyExpeditionTask().Run(_autoSkipAssets);
            //             exploreIconRa.Dispose();
            //             return;
            //         }
            //
            //         _prevOtherClickFrameIndex = content.FrameIndex;
            //         exploreIconRa.Dispose();
            //         return;
            //     }
            // }
            //
            // // 找右下的对话选项按钮
            // content.CaptureRectArea.Find(_autoSkipAssets.OptionIconRo, (optionButtonRectArea) =>
            // {
            //     TaskControl.Sleep(config.AfterChooseOptionSleepDelay);
            //     optionButtonRectArea.ClickCenter();
            //
            //     if (Math.Abs(content.FrameIndex - _prevClickFrameIndex) >= 8)
            //     {
            //         _logger.LogInformation("自动剧情：{Text}", "点击选项");
            //     }
            //
            //     _prevClickFrameIndex = content.FrameIndex;
            //     optionButtonRectArea.Dispose();
            // });
        }
        else
        {
            // 黑屏剧情要点击鼠标（多次） 几乎全黑的时候不用点击
            using var grayMat = new Mat(content.CaptureRectArea.SrcGreyMat, new Rect(0, content.CaptureRectArea.SrcGreyMat.Height / 3, content.CaptureRectArea.SrcGreyMat.Width, content.CaptureRectArea.SrcGreyMat.Height / 3));
            var blackCount = OpenCvCommonHelper.CountGrayMatColor(grayMat, 0);
            var rate = blackCount * 1d / (grayMat.Width * grayMat.Height);
            if (rate is >= 0.5 and < 0.999)
            {
                Simulation.SendInputEx.Mouse.LeftButtonClick();
                if ((DateTime.Now - _prevClickTime).TotalMilliseconds > 1000)
                {
                    _logger.LogInformation("自动剧情：{Text} 比例 {Rate}", "点击黑屏", rate.ToString("F"));
                }

                _prevClickTime = DateTime.Now;
            }

            // TODO 自动交付材料
        }
    }

    /// <summary>
    /// 获取橙色选项的文字
    /// </summary>
    /// <param name="captureMat"></param>
    /// <param name="foundIconRectArea"></param>
    /// <param name="chatOptionTextWidth"></param>
    /// <returns></returns>
    private string GetOrangeOptionText(Mat captureMat, RectArea foundIconRectArea, int chatOptionTextWidth)
    {
        var textRect = new Rect(foundIconRectArea.X + foundIconRectArea.Width, foundIconRectArea.Y, chatOptionTextWidth, foundIconRectArea.Height);
        using var mat = new Mat(captureMat, textRect);
        // 只提取橙色
        using var bMat = OpenCvCommonHelper.Threshold(mat, new Scalar(247, 198, 50), new Scalar(255, 204, 54));
        // Cv2.ImWrite("log/每日委托.png", bMat);
        var whiteCount = OpenCvCommonHelper.CountGrayMatColor(bMat, 255);
        var rate = whiteCount * 1.0 / (bMat.Width * bMat.Height);
        if (rate < 0.06)
        {
            Debug.WriteLine($"识别到橙色文字区域占比:{rate}");
            return string.Empty;
        }

        var text = OcrFactory.Paddle.Ocr(bMat);
        return text;
    }


    private bool IsOrangeOption(Mat textMat)
    {
        // 只提取橙色
        Cv2.ImWrite($"log/text{DateTime.Now:yyyyMMddHHmmssffff}.png", textMat);
        using var bMat = OpenCvCommonHelper.Threshold(textMat, new Scalar(247, 198, 50), new Scalar(255, 204, 54));
        var whiteCount = OpenCvCommonHelper.CountGrayMatColor(bMat, 255);
        var rate = whiteCount * 1.0 / (bMat.Width * bMat.Height);
        Debug.WriteLine($"识别到橙色文字区域占比:{rate}");
        if (rate > 0.06)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 领取每日委托奖励 后 10s 寻找原石是否出现，出现则按下esc
    /// </summary>
    private void GetDailyRewardsEsc(AutoSkipConfig config, CaptureContent content)
    {
        if (!config.AutoGetDailyRewardsEnabled)
        {
            return;
        }

        if ((DateTime.Now - _prevGetDailyRewardsTime).TotalSeconds > 10)
        {
            return;
        }

        content.CaptureRectArea.Find(_autoSkipAssets.PrimogemRo, primogemRa =>
        {
            Thread.Sleep(100);
            Simulation.SendInputEx.Keyboard.KeyPress(User32.VK.VK_ESCAPE);
            _prevGetDailyRewardsTime = DateTime.MinValue;
            primogemRa.Dispose();
        });
    }

    /// <summary>
    /// 新的对话选项选择
    /// </summary>
    private void ChatOptionChoose(CaptureContent content)
    {
        var captureArea = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        var assetScale = TaskContext.Instance().SystemInfo.AssetScale;

        // 感叹号识别 遇到直接点击
        var exclamationIconRa = content.CaptureRectArea.Find(_autoSkipAssets.ExclamationIconRo);
        if (!exclamationIconRa.IsEmpty())
        {
            exclamationIconRa.ClickCenter();
            AutoSkipLog("点击感叹号选项");
            exclamationIconRa.Dispose();
            return;
        }

        // 气泡识别
        var chatOptionResultList = MatchTemplateHelper.MatchOnePicForOnePic(content.CaptureRectArea.SrcGreyMat[_autoSkipAssets.OptionRoi], _autoSkipAssets.OptionIconRo.TemplateImageGreyMat);
        if (chatOptionResultList.Count > 0)
        {
            // 第一个元素就是最下面的
            chatOptionResultList = chatOptionResultList.OrderByDescending(x => x.Y).ToList();

            // 通过最下面的气泡框来文字识别
            var lowest = chatOptionResultList[0];
            var ocrRect = new Rect(_autoSkipAssets.OptionRoi.X + (int)(lowest.X + lowest.Width + 8 * assetScale), 0,
                (int)(535 * assetScale), (int)(lowest.Y + lowest.Height + 30 * assetScale));
            using var ocrMat = new Mat(content.CaptureRectArea.SrcGreyMat, ocrRect);
            // Cv2.ImWrite("log/ocrMat.png", ocrMat);
            var ocrRes = OcrFactory.Paddle.OcrResult(ocrMat);

            if (ocrRes.Regions.Length > 0)
            {
                var clickOffset = new ClickOffset(captureArea.X + ocrRect.X, captureArea.Y + ocrRect.Y, assetScale);

                // 用户自定义关键词 匹配
                foreach (var item in ocrRes.Regions)
                {
                    // 选择关键词
                    if (_selectList.Any(s => item.Text.Contains(s)))
                    {
                        ClickOcrRegion(clickOffset, item);
                        return;
                    }

                    // 不选择关键词
                    if (_pauseList.Any(s => item.Text.Contains(s)))
                    {
                        return;
                    }
                }

                // 橙色选项
                foreach (var item in ocrRes.Regions)
                {
                    var textRect = item.Rect.BoundingRect();
                    var textMat = new Mat(content.CaptureRectArea.SrcMat, new Rect(ocrRect.X + textRect.X, ocrRect.Y + textRect.Y, textRect.Width, textRect.Height));
                    if (IsOrangeOption(textMat))
                    {
                        if (item.Text.Contains("每日委托"))
                        {
                            ClickOcrRegion(clickOffset, item);
                            _prevGetDailyRewardsTime = DateTime.Now; // 记录领取时间
                        }
                        else if (item.Text.Contains("探索派遣"))
                        {
                            Thread.Sleep(800); // 等待探索派遣界面打开
                            new OneKeyExpeditionTask().Run(_autoSkipAssets);
                        }
                        else
                        {
                            ClickOcrRegion(clickOffset, item);
                        }

                        return;
                    }
                }

                // 默认不选择关键词
                foreach (var item in ocrRes.Regions)
                {
                    // 不选择关键词
                    if (_defaultPauseList.Any(s => item.Text.Contains(s)))
                    {
                        return;
                    }
                }

                // 最后，选择默认选项
                var clickRegion = ocrRes.Regions[^1];
                if (_config.ClickFirstOptionEnabled)
                {
                    clickRegion = ocrRes.Regions[0];
                }

                ClickOcrRegion(clickOffset, clickRegion);
                AutoSkipLog(clickRegion.Text);
            }
            else
            {
                var clickRect = lowest;
                if (_config.ClickFirstOptionEnabled)
                {
                    clickRect = chatOptionResultList[^1];
                }

                // 没OCR到文字，直接选择气泡选项
                var clickOffset = new ClickOffset(captureArea.X + _autoSkipAssets.OptionRoi.X, captureArea.Y + _autoSkipAssets.OptionRoi.Y, assetScale);
                clickOffset.ClickWithoutScale(clickRect.X + clickRect.Width / 2, clickRect.Y + clickRect.Height / 2);
                var msg = _config.ClickFirstOptionEnabled ? "上" : "下";
                AutoSkipLog($"点击最{msg}选项");
            }
        }
    }

    private void ClickOcrRegion(ClickOffset clickOffset, PaddleOcrResultRegion clickRegion)
    {
        clickOffset.ClickWithoutScale(clickRegion.Rect.Center.X, clickRegion.Rect.Center.Y);
        AutoSkipLog(clickRegion.Text);
    }

    private void AutoSkipLog(string text)
    {
        if ((DateTime.Now - _prevClickTime).TotalMilliseconds > 1000)
        {
            _logger.LogInformation("自动剧情：{Text}", text);
        }

        _prevClickTime = DateTime.Now;
    }
}