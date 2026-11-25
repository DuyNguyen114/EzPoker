using PokerTrainer.Models;

namespace PokerTrainer.Mobile.Views;

public sealed class TableSetupPage : ContentPage
{
    private readonly Entry _name = new() { Text = "Bàn poker" };
    private readonly Entry _small = ChipEntry("10");
    private readonly Entry _big = ChipEntry("20");
    private readonly Picker _count = new() { Title = "Số người" };
    private readonly Picker _button = new() { Title = "Ghế giữ Button" };
    private readonly VerticalStackLayout _rows = new() { Spacing = 8 };
    private readonly Button _start = new() { Text = "BẮT ĐẦU HAND" };
    private readonly List<(Entry Name, Entry Stack)> _players = new();

    public TableSetupPage()
    {
        Title = "Thiết lập bàn";
        for (int i = 1; i <= 9; i++)
        {
            int seat = i;
            _players.Add((new Entry { Text = $"Người chơi {seat}" }, ChipEntry("1000")));
        }

        _count.ItemsSource = Enumerable.Range(2, 8).ToList();
        _count.SelectedIndexChanged += (_, _) => RenderRows();
        _start.Clicked += StartClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16, Spacing = 8,
                Children =
                {
                    new Label { Text = "Tên bàn" }, _name,
                    new Label { Text = "Số người chơi" }, _count,
                    new Label { Text = "Small Blind" }, _small,
                    new Label { Text = "Big Blind" }, _big,
                    new Label { Text = "Button đầu tiên" }, _button,
                    _rows, _start
                }
            }
        };
        _count.SelectedIndex = 1;
    }

    private static Entry ChipEntry(string text) => new() { Text = text, Keyboard = Keyboard.Numeric };

    private void RenderRows()
    {
        if (_count.SelectedItem is not int count)
            return;

        int oldIndex = _button.SelectedIndex;
        _button.ItemsSource = Enumerable.Range(1, count).Select(i => $"Ghế {i}").ToList();
        _button.SelectedIndex = Math.Clamp(oldIndex, 0, count - 1);
        foreach (var oldRow in _rows.Children.OfType<Grid>())
            oldRow.Children.Clear();
        _rows.Children.Clear();
        for (int i = 0; i < count; i++)
        {
            var player = _players[i];
            _rows.Children.Add(new Label { Text = $"Ghế {i + 1} — Tên / Stack" });
            var row = new Grid
            {
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = new GridLength(110) }
                }
            };
            Grid.SetColumn(player.Stack, 1);
            row.Children.Add(player.Name);
            row.Children.Add(player.Stack);
            _rows.Children.Add(row);
        }
    }

    private async void StartClicked(object? sender, EventArgs e)
    {
        if (_count.SelectedItem is not int count || _button.SelectedIndex < 0)
            return;

        _start.IsEnabled = false;
        try
        {
            if (!int.TryParse(_small.Text, out int small) || !int.TryParse(_big.Text, out int big)
                || small <= 0 || big < small)
                throw new ArgumentException("Nhập blind nguyên dương và Big Blind không nhỏ hơn Small Blind.");

            var table = new PokerTable(_name.Text ?? "");
            for (int i = 0; i < count; i++)
            {
                var input = _players[i];
                string name = input.Name.Text?.Trim() ?? "";
                if (name.Length == 0 || !int.TryParse(input.Stack.Text, out int stack) || stack <= 0)
                    throw new ArgumentException($"Ghế {i + 1}: cần tên và stack nguyên dương.");
                table.AddPlayer(new Player(name, i + 1, stack));
            }

            await Navigation.PushAsync(new PokerTablePage(table, _button.SelectedIndex + 1, small, big));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            await DisplayAlertAsync("Chưa thể bắt đầu", ex.Message, "OK");
        }
        finally
        {
            _start.IsEnabled = true;
        }
    }
}
