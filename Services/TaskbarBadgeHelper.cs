using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;
using MediaPen = System.Windows.Media.Pen;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaPoint = System.Windows.Point;
using MediaFontFamily = System.Windows.Media.FontFamily;
using WpfFlowDirection = System.Windows.FlowDirection;

namespace WoodStreamPlaza.Services;

/// <summary>
/// タスクバーアイコン用未読バッジ画像の動的生成ヘルパー
/// </summary>
public static class TaskbarBadgeHelper
{
    /// <summary>
    /// 未読件数を表示するタスクバーオーバーレイ画像（16x16）を生成します。
    /// 未読件数が 0 の場合は null を返してバッジを非表示にします。
    /// </summary>
    /// <param name="unreadCount">未読件数</param>
    /// <returns>タスクバーオーバーレイ用 ImageSource</returns>
    public static ImageSource? CreateBadge(int unreadCount)
    {
        if (unreadCount <= 0) return null;

        const int size = 16;
        const double dpi = 96.0;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // 赤丸の背景（白枠線付き）
            var bgBrush = new SolidColorBrush(MediaColor.FromRgb(0xEF, 0x44, 0x44)); // #EF4444 (鮮やかなRed)
            var borderPen = new MediaPen(MediaBrushes.White, 1.2);

            double radius = (size / 2.0) - 0.6;
            var center = new MediaPoint(size / 2.0, size / 2.0);
            dc.DrawEllipse(bgBrush, borderPen, center, radius, radius);

            // 数字テキスト（1〜9は通常フォント、2桁以上は縮小表示、99超は99+）
            string text = unreadCount > 99 ? "99+" : (unreadCount > 9 ? $"{unreadCount}" : unreadCount.ToString());
            double fontSize = text.Length > 2 ? 6.5 : (text.Length == 2 ? 7.5 : 9.0);

            var typeface = new Typeface(
                new MediaFontFamily("Segoe UI, Meiryo, sans-serif"),
                FontStyles.Normal,
                FontWeights.Bold,
                FontStretches.Normal);

            var formattedText = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                WpfFlowDirection.LeftToRight,
                typeface,
                fontSize,
                MediaBrushes.White,
                VisualTreeHelper.GetDpi(visual).PixelsPerDip);

            double textX = (size - formattedText.Width) / 2.0;
            double textY = (size - formattedText.Height) / 2.0;
            dc.DrawText(formattedText, new MediaPoint(textX, textY));
        }

        var rtb = new RenderTargetBitmap(size, size, dpi, dpi, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze(); // スレッド間の安全な共有と描画パフォーマンスの最適化
        return rtb;
    }
}
