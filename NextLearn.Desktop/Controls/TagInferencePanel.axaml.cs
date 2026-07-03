using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace NextLearn.Desktop.Controls;

public partial class TagInferencePanel : UserControl
{
    public TagInferencePanel()
    {
        InitializeComponent();
    }

    public void FocusSearch()
    {
        DispatcherTimer.RunOnce(() => TagInferenceSearchBox?.Focus(), TimeSpan.FromMilliseconds(50));
    }

    public void ScrollBy(double dx, double dy)
    {
        var sv = this.FindControl<ScrollViewer>("TagInferenceScrollViewer");
        if (sv is not null)
        {
            sv.Offset = new Vector(sv.Offset.X + dx, sv.Offset.Y + dy);
        }
    }
}
