using Microsoft.Maui.Layouts;
using PokerTrainer.Mobile.Controls;
using PokerTrainer.Mobile.Services;
using PokerTrainer.Mobile.ViewModels;
using PokerTrainer.Models;

namespace PokerTrainer.Mobile.Views;

public partial class PokerTablePage : ContentPage
{
    private const decimal BetStep = 20m;
    private const decimal MinimumRaiseTo = 80m;
    private const decimal MaximumRaiseTo = 740m;
    private readonly SeatLayoutService _seatLayoutService = new SeatLayoutService();
    private readonly List<PokerSeatView> _seatViews = new List<PokerSeatView>();
    private bool _hasCreatedSeats;

    public PokerTablePage()
    {
        InitializeComponent();

        PokerTableLayout.Loaded += PokerTableLayout_Loaded;
    }

    private decimal GetBetAmount()
    {
        bool isValid =
            decimal.TryParse(
                BetAmountEntry.Text,
                out decimal amount);

        if (!isValid)
        {
            return MinimumRaiseTo;
        }

        return amount;
    }

    private void SetBetAmount(decimal amount)
    {
        amount =
            Math.Clamp(
                amount,
                MinimumRaiseTo,
                MaximumRaiseTo);

        BetAmountEntry.Text =
            amount.ToString("0");

        RaiseButton.Text =
            $"RAISE {amount:0}";
    }

    private void DecreaseBet_Clicked(
        object sender,
        EventArgs e)
    {
        decimal amount =
            GetBetAmount() - BetStep;

        SetBetAmount(amount);
    }

    private void IncreaseBet_Clicked(
        object sender,
        EventArgs e)
    {
        decimal amount =
            GetBetAmount() + BetStep;

        SetBetAmount(amount);
    }

    private async void Fold_Clicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlertAsync(
            "Hành động",
            "Nam fold",
            "OK");
    }

    private async void Call_Clicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlertAsync(
            "Hành động",
            "Nam call 40 chip",
            "OK");
    }

    private async void Raise_Clicked(
        object sender,
        EventArgs e)
    {
        decimal amount =
            GetBetAmount();

        await DisplayAlertAsync(
            "Hành động",
            $"Nam raise lên {amount:0} chip",
            "OK");
    }

    private void ShowDemoTable()
    {
        var players =
            new List<Player>
            {
            new("An", 1, 1000),
            new("Bình", 2, 1000),
            new("Cường", 3, 1000),
            new("Dũng", 4, 1000),
            new("Hùng", 5, 1000),
            new("Lan", 6, 1000),
            new("Minh", 7, 1000),
            new("Nam", 8, 1000),
            new("Vy", 9, 1000)
            };

        string[] positions =
        {
        "BTN",
        "SB",
        "BB",
        "UTG",
        "UTG+1",
        "MP",
        "LJ",
        "HJ",
        "CO"
    };

        var seats =
            new List<PlayerSeatViewModel>();

        for (int index = 0;
             index < players.Count;
             index++)
        {
            var seat =
                new PlayerSeatViewModel(
                    players[index],
                    positions[index]);

            // Tạm thời đánh dấu Nam là người đang hành động
            seat.IsCurrentPlayer =
                players[index].Name == "Nam";

            seats.Add(seat);
        }

        _seatLayoutService.ArrangeSeats(seats);

        DisplaySeats(seats);
    }

    private void DisplaySeats(
    IReadOnlyList<PlayerSeatViewModel> seats)
    {
        /*
         * Chỉ xóa các ghế cũ.
         * Không xóa Border mặt bàn đang có trong XAML.
         */
        foreach (PokerSeatView oldSeatView
                 in _seatViews)
        {
            PokerTableLayout.Children.Remove(
                oldSeatView);
        }

        _seatViews.Clear();

        foreach (PlayerSeatViewModel seat
                 in seats)
        {
            var seatView =
                new PokerSeatView(seat)
                {
                    ZIndex = 10,
                    IsVisible = true
                };

            AbsoluteLayout.SetLayoutBounds(
                seatView,
                new Rect(
                    seat.X,
                    seat.Y,
                    112,
                    62));

            AbsoluteLayout.SetLayoutFlags(
                seatView,
                AbsoluteLayoutFlags.PositionProportional);

            PokerTableLayout.Children.Add(
                seatView);

            _seatViews.Add(
                seatView);
        }

        System.Diagnostics.Debug.WriteLine(
            $"Số ghế đã hiển thị: {_seatViews.Count}");

        System.Diagnostics.Debug.WriteLine(
            $"Số control trong bàn: {PokerTableLayout.Children.Count}");
    }

    private void PokerTableLayout_Loaded(
    object? sender,
    EventArgs e)
    {
        if (_hasCreatedSeats)
        {
            return;
        }

        _hasCreatedSeats = true;

        System.Diagnostics.Debug.WriteLine(
            $"Kích thước bàn: " +
            $"{PokerTableLayout.Width} x " +
            $"{PokerTableLayout.Height}");

        ShowDemoTable();
    }
}