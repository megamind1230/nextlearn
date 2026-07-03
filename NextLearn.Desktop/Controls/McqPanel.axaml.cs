using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using NativeWebView.Core;
using NextLearn.Desktop.Services;
using NextLearn.Desktop.ViewModels;
using NextLearn.Desktop.Views;

#pragma warning disable CA1031

namespace NextLearn.Desktop.Controls;

public partial class McqPanel : UserControl
{
    public McqPanel()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs args)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            if (vm.McqPanelViewModel != null)
            {
                vm.McqPanelViewModel.Quiz.PropertyChanged += OnQuizPropertyChanged;
            }

            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.McqPanelViewModel))
                {
                    if (vm.McqPanelViewModel?.Quiz != null)
                    {
                        vm.McqPanelViewModel.Quiz.PropertyChanged += OnQuizPropertyChanged;
                    }
                }
            };
        }
    }

    private void OnQuizPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(McqQuizViewModel.CurrentQuestionHtml)
            or nameof(McqQuizViewModel.ReviewHtml))
        {
            Dispatcher.UIThread.Post(() => LoadQuizHtml());
        }
    }

    private void LoadQuizHtml()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var html = vm.McqPanelViewModel.Quiz.CurrentQuestionHtml
                   ?? vm.McqPanelViewModel.Quiz.ReviewHtml;

        if (McqQuizWebView == null || string.IsNullOrEmpty(html))
        {
            return;
        }

        try
        {
            html = WebViewBridge.EnrichHtml(html);
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
            McqQuizWebView.Source = new Uri($"data:text/html;base64,{base64}");
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to load quiz HTML");
        }
    }

    private void OnMcqQuizWebViewNavigationStarted(object? sender, NativeWebViewNavigationStartedEventArgs e)
    {
        var uri = e.Uri;
        if (uri == null)
        {
            return;
        }

        if (uri.Scheme == "http" && uri.Host == "mcq-answer.local")
        {
            e.Cancel = true;
            var path = uri.AbsolutePath.TrimStart('/').ToUpperInvariant();
            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    switch (path)
                    {
                        case "NEXT":
                            vm.McqPanelViewModel.Quiz.HandleNext();
                            break;
                        case "PREV":
                            vm.McqPanelViewModel.Quiz.HandlePrev();
                            break;
                        case "QUIT":
                            vm.McqPanelViewModel.QuitQuizCommand.Execute(null);
                            break;
                        case "F1":
                            MainWindow.OpenInBrowser("https://github.com/megamind1230/nextlearn/blob/master/README.org");
                            break;
                        default:
                            vm.McqPanelViewModel.Quiz.HandleAnswer(path);
                            break;
                    }
                }
            });
            return;
        }

        if (uri.Scheme is "data" or "about")
        {
            return;
        }

        e.Cancel = true;
    }

    public void FocusSearch()
    {
        DispatcherTimer.RunOnce(() => McqTabPanelSearchBox?.Focus(), TimeSpan.FromMilliseconds(50));
    }

    public void ScrollBy(double dx, double dy)
    {
        var sv = this.FindControl<ScrollViewer>("McqTabPanelScrollViewer");
        if (sv is not null)
        {
            sv.Offset = new Vector(sv.Offset.X + dx, sv.Offset.Y + dy);
        }
    }
}
