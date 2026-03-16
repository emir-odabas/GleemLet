using System.Windows.Controls;
using System.Windows;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;


namespace GleemLet.Views;

public class StatBox : Control
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(StatBox));
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(PackIconKind), typeof(StatBox));
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(StatBox));
    public static readonly DependencyProperty MainColorProperty = DependencyProperty.Register("MainColor", typeof(Brush), typeof(StatBox));

    public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public PackIconKind Icon { get => (PackIconKind)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public Brush MainColor { get => (Brush)GetValue(MainColorProperty); set => SetValue(MainColorProperty, value); }

    static StatBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StatBox), new FrameworkPropertyMetadata(typeof(StatBox)));
    }
}
