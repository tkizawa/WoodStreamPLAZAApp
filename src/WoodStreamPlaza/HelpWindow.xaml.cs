using System;
using System.Reflection;
using System.Windows;

namespace WoodStreamPlaza;

/// <summary>
/// 操作説明書ウィンドウの相互作用ロジック
/// </summary>
public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();

        // アセンブリから動的にバージョン番号を取得して表示
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            VersionTextBlock.Text = $"Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        else
        {
            VersionTextBlock.Text = "Version 1.0.0.0";
        }
    }

    /// <summary>
    /// 閉じるボタン押下時
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
