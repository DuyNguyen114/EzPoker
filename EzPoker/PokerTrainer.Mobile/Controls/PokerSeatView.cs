using Microsoft.Maui.Controls.Shapes;
using PokerTrainer.Mobile.ViewModels;

namespace PokerTrainer.Mobile.Controls;

public class PokerSeatView : ContentView
{
    private readonly PlayerSeatViewModel _seat;

    private readonly Border _container;
    private readonly Label _nameLabel;
    private readonly Label _statusLabel;

    public PokerSeatView(
        PlayerSeatViewModel seat)
    {
        _seat =
            seat ??
            throw new ArgumentNullException(
                nameof(seat));

        //WidthRequest = 112;
        //HeightRequest = 62;

        _nameLabel =
            new Label
            {
                FontSize = 12,
                FontAttributes =
                    FontAttributes.Bold,

                TextColor = Colors.White,

                LineBreakMode =
                    LineBreakMode.TailTruncation
            };

        _statusLabel =
            new Label
            {
                FontSize = 10,

                LineBreakMode =
                    LineBreakMode.TailTruncation
            };

        var contentLayout =
            new VerticalStackLayout
            {
                Spacing = 2,

                Children =
                {
                    _nameLabel,
                    _statusLabel
                }
            };

        _container =
            new Border
            {
                //WidthRequest = 112,
                //HeightRequest = 62,

                Padding =
                    new Thickness(9, 6),

                StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(12)
                    },

                Content =
                    contentLayout
            };

        Content =
            _container;

        Refresh();
    }

    public void Refresh()
    {
        _nameLabel.Text =
            $"{_seat.Position} • {_seat.Name}";

        //_statusLabel.Text = _seat.StatusText;

        _statusLabel.Text = $"{_seat.Stack:0.##} chip";

        IsVisible = true;
        Opacity = 1;

        if (_seat.HasFolded)
        {
            SetAppearance(
                backgroundColor: "#19201C",
                strokeColor: "#313B35",
                statusColor: "#E46B65",
                strokeThickness: 1);

            Opacity = 0.45;

            _statusLabel.Text += " · Fold";

            return;
        }

        if (_seat.IsAllIn)
        {
            SetAppearance(
                backgroundColor: "#3A2020",
                strokeColor: "#E46B65",
                statusColor: "#E46B65",
                strokeThickness: 3);

            _statusLabel.Text += " · All-in";

            return;
        }

        if (_seat.IsCurrentPlayer)
        {
            SetAppearance(
                backgroundColor: "#19201C",
                strokeColor: "#F0BD4F",
                statusColor: "#F0BD4F",
                strokeThickness: 4);

            return;
        }

        SetAppearance(
            backgroundColor: "#19201C",
            strokeColor: "#313B35",
            statusColor: "#A0AAA4",
            strokeThickness: 1);
    }

    private void SetAppearance(
        string backgroundColor,
        string strokeColor,
        string statusColor,
        double strokeThickness)
    {
        _container.Background =
            new SolidColorBrush(
                Color.FromArgb(
                    backgroundColor));

        _container.Stroke =
            new SolidColorBrush(
                Color.FromArgb(
                    strokeColor));

        _container.StrokeThickness =
            strokeThickness;

        _statusLabel.TextColor =
            Color.FromArgb(
                statusColor);
    }
}