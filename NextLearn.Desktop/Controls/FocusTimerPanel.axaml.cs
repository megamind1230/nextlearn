using Avalonia.Controls;
using Avalonia.Input;
using NextLearn.Desktop.ViewModels;

namespace NextLearn.Desktop.Controls;

public partial class FocusTimerPanel : UserControl
{
    public FocusTimerPanel()
    {
        InitializeComponent();
    }

    private void OnTodoTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainWindowViewModel vm)
        {
            vm.FocusTimerViewModel.AddTodoCommand.Execute(null);
        }
    }
}
