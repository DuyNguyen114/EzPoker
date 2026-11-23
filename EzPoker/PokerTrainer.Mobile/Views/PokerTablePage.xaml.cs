using Microsoft.Maui.Layouts;
using PokerTrainer.Mobile.Controls;
using PokerTrainer.Mobile.Services;
using PokerTrainer.Mobile.ViewModels;
using PokerTrainer.Models;
using PokerTrainer.Services;

namespace PokerTrainer.Mobile.Views;

public partial class PokerTablePage : ContentPage
{
    private const decimal BetStep = 20m;
    private const decimal MinimumRaiseTo = 80m;
    private const decimal MaximumRaiseTo = 740m;
    private readonly SeatLayoutService _seatLayoutService = new SeatLayoutService();
    private readonly List<PokerSeatView> _seatViews = new List<PokerSeatView>();
    private bool _hasCreatedSeats;
    private bool _hasStartedHand;
    private readonly PokerTable _table;
    private readonly HandLifecycleService _handLifecycleService;
    private readonly IPokerGameService _pokerGameService;
    private const int DemoButtonSeat = 1;
    public PokerTablePage()
    {
        InitializeComponent();

        IBettingService bettingService =
        new BettingService();

        ITurnOrderService turnOrderService =
            new TurnOrderService();

        _handLifecycleService =
            new HandLifecycleService(
                bettingService,
                turnOrderService);

        IPotService potService =
            new PotService();

        _pokerGameService =
            new PokerGameService(
                bettingService,
                turnOrderService,
                _handLifecycleService,
                potService);

        _table =
            CreateDemoTable();

        PokerTableLayout.Loaded +=
            PokerTableLayout_Loaded;
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

        RefreshActionButtons();
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
        await ExecuteActionAsync(
        () => _pokerGameService.Fold(
            _table));
    }

    private async void Call_Clicked(
        object sender,
        EventArgs e)
    {
        await ExecuteActionAsync(
        () =>
        {
            PokerHand hand =
                _table.CurrentHand
                ?? throw new InvalidOperationException(
                    "Không có hand đang diễn ra.");

            Player player =
                _table.CurrentPlayer
                ?? throw new InvalidOperationException(
                    "Không có người đang hành động.");

            decimal callAmount =
                hand.CurrentBet -
                player.StreetCommitted;

            if (callAmount <= 0)
            {
                _pokerGameService.Check(
                    _table);
            }
            else
            {
                _pokerGameService.Call(
                    _table);
            }
        });
    }

    private async void Raise_Clicked(
        object sender,
        EventArgs e)
    {
        decimal amount =
        GetBetAmount();

        await ExecuteActionAsync(
            () =>
            {
                PokerHand hand =
                    _table.CurrentHand
                    ?? throw new InvalidOperationException(
                        "Không có hand đang diễn ra.");

                if (hand.CurrentBet == 0)
                {
                    _pokerGameService.Bet(
                        _table,
                        amount);
                }
                else
                {
                    _pokerGameService.RaiseTo(
                        _table,
                        amount);
                }
            });
    }

    /*private void ShowDemoTable()
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
    }*/

    private static PokerTable CreateDemoTable()
    {
        var table =
            new PokerTable(
                "Dealer Table");

        table.AddPlayer(
            new Player("An", 1, 1000));

        table.AddPlayer(
            new Player("Bình", 2, 1000));

        table.AddPlayer(
            new Player("Cường", 3, 1000));

        table.AddPlayer(
            new Player("Dũng", 4, 1000));

        table.AddPlayer(
            new Player("Hùng", 5, 1000));

        table.AddPlayer(
            new Player("Lan", 6, 1000));

        table.AddPlayer(
            new Player("Minh", 7, 1000));

        table.AddPlayer(
            new Player("Nam", 8, 1000));

        table.AddPlayer(
            new Player("Vy", 9, 1000));

        return table;
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
        if (_hasStartedHand)
        {
            return;
        }

        _hasStartedHand = true;

        _handLifecycleService.StartNewHand(
            _table,
            buttonSeat: DemoButtonSeat,
            smallBlind: 10,
            bigBlind: 20);

        RefreshTable();
    }

    private static string[] GetPositionNames(
    int playerCount)
    {
        return playerCount switch
        {
            2 => new[]
            {
            "BTN/SB",
            "BB"
        },

            3 => new[]
            {
            "BTN",
            "SB",
            "BB"
        },

            4 => new[]
            {
            "BTN",
            "SB",
            "BB",
            "UTG"
        },

            5 => new[]
            {
            "BTN",
            "SB",
            "BB",
            "UTG",
            "CO"
        },

            6 => new[]
            {
            "BTN",
            "SB",
            "BB",
            "UTG",
            "HJ",
            "CO"
        },

            7 => new[]
            {
            "BTN",
            "SB",
            "BB",
            "UTG",
            "MP",
            "HJ",
            "CO"
        },

            8 => new[]
            {
            "BTN",
            "SB",
            "BB",
            "UTG",
            "UTG+1",
            "MP",
            "HJ",
            "CO"
        },

            9 => new[]
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
        },

            _ => throw new InvalidOperationException(
                "Chỉ hỗ trợ bàn từ 2 đến 9 người.")
        };
    }

    private Dictionary<int, string> BuildPositionBySeat()
    {
        int buttonSeat = _table.CurrentHand?.ButtonSeat
            ?? _table.HandHistory.LastOrDefault()?.ButtonSeat
            ?? _table.Players[0].SeatNumber;

        List<Player> orderedPlayers = _table.Players.OrderBy(x => x.SeatNumber).ToList();

        int buttonIndex = orderedPlayers.FindIndex(x => x.SeatNumber == buttonSeat);

        string[] positionNames = GetPositionNames(orderedPlayers.Count);

        var result = new Dictionary<int, string>();

        for(int offset = 0; offset < orderedPlayers.Count; offset++)
        {
            int playerIndex = (buttonIndex + offset) % orderedPlayers.Count;

            Player player = orderedPlayers[playerIndex];

            result[player.SeatNumber] = positionNames[offset];
        }

        return result;
    }

    private void RefreshTable()
    {
        Dictionary<int, string> positionBySeat = BuildPositionBySeat();

        var seats = new List<PlayerSeatViewModel>();

        foreach (Player player in _table.Players)
        {
            var seat = new PlayerSeatViewModel(player, positionBySeat[player.SeatNumber]);

            seat.IsCurrentPlayer = _table.CurrentPlayer?.Id == player.Id;

            seats.Add(seat);
        }

        _seatLayoutService.ArrangeSeats(seats);

        DisplaySeats(seats);

        PotLabel.Text = _table.CurrentPot.ToString("0");

        PokerHand? hand = _table.CurrentHand;

        if (hand is null)
        {
            StreetPotLabel.Text = "Hand đã kết thúc.";

            CallButton.IsEnabled = false;
            RaiseButton.IsEnabled = false;

            return;
        }

        decimal streetPot = hand.GetPotForStreet(hand.CurrentStreet);

        StreetPotLabel.Text =
        $"{hand.CurrentStreet}: " +
        $"{streetPot:0}";

        RefreshActionButtons();
    }
    private void RefreshActionButtons()
    {
        PokerHand? hand =
            _table.CurrentHand;

        Player? currentPlayer =
            _table.CurrentPlayer;

        if (hand is null ||
            currentPlayer is null)
        {
            CallButton.IsEnabled = false;
            RaiseButton.IsEnabled = false;
            return;
        }

        CallButton.IsEnabled = true;
        RaiseButton.IsEnabled = true;

        decimal callAmount =
            Math.Max(
                0,
                hand.CurrentBet -
                currentPlayer.StreetCommitted);

        CallButton.Text =
            callAmount == 0
                ? "CHECK"
                : $"CALL {callAmount:0}";

        decimal amount =
            GetBetAmount();

        RaiseButton.Text =
            hand.CurrentBet == 0
                ? $"BET {amount:0}"
                : $"RAISE {amount:0}";
    }

    private async Task ExecuteActionAsync(
    Action action)
    {
        try
        {
            action();

            RefreshTable();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Không thể thực hiện",
                ex.Message,
                "OK");
        }
    }

}