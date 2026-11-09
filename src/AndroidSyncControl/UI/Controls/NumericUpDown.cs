using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace AndroidSyncControl.UI.Controls
{
    /// <summary>
    /// A lightweight, fully theme-able integer up/down control (replacement for
    /// Xceed IntegerUpDown). Supports <see cref="Value"/> (two-way), <see cref="Minimum"/>,
    /// <see cref="Maximum"/> and <see cref="Increment"/>, plus mouse wheel and Up/Down keys.
    /// </summary>
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartUp, Type = typeof(RepeatButton))]
    [TemplatePart(Name = PartDown, Type = typeof(RepeatButton))]
    internal class NumericUpDown : Control
    {
        const string PartTextBox = "PART_TextBox";
        const string PartUp = "PART_Up";
        const string PartDown = "PART_Down";

        static NumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown),
                new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(int), typeof(NumericUpDown),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceValue));

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum), typeof(int), typeof(NumericUpDown),
            new FrameworkPropertyMetadata(int.MinValue, OnRangeChanged));

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum), typeof(int), typeof(NumericUpDown),
            new FrameworkPropertyMetadata(int.MaxValue, OnRangeChanged));

        public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
            nameof(Increment), typeof(int), typeof(NumericUpDown),
            new PropertyMetadata(1));

        public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public int Minimum { get => (int)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
        public int Maximum { get => (int)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
        public int Increment { get => (int)GetValue(IncrementProperty); set => SetValue(IncrementProperty, value); }

        TextBox _textBox;

        static object CoerceValue(DependencyObject d, object baseValue)
        {
            var c = (NumericUpDown)d;
            int v = (int)baseValue;
            if (v < c.Minimum) v = c.Minimum;
            if (v > c.Maximum) v = c.Maximum;
            return v;
        }

        static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => d.CoerceValue(ValueProperty);

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild(PartUp) is RepeatButton up)
                up.Click += (s, e) => Step(Increment);
            if (GetTemplateChild(PartDown) is RepeatButton down)
                down.Click += (s, e) => Step(-Increment);

            _textBox = GetTemplateChild(PartTextBox) as TextBox;
            if (_textBox != null)
            {
                _textBox.PreviewTextInput += TextBox_PreviewTextInput;
                _textBox.KeyDown += TextBox_KeyDown;
            }
        }

        void Step(int delta)
        {
            // Coercion (CoerceValue) clamps to [Minimum, Maximum]; the two-way binding
            // pushes the new value back to the bound source.
            SetCurrentValue(ValueProperty, Value + delta);
        }

        void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char ch in e.Text)
            {
                if (char.IsDigit(ch)) continue;
                if (ch == '-' && Minimum < 0) continue;
                e.Handled = true;
                return;
            }
        }

        void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _textBox?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                e.Handled = true;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (IsMouseOver || IsKeyboardFocusWithin)
            {
                Step(e.Delta > 0 ? Increment : -Increment);
                e.Handled = true;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Up) { Step(Increment); e.Handled = true; }
            else if (e.Key == Key.Down) { Step(-Increment); e.Handled = true; }
        }
    }
}
