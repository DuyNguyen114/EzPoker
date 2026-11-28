using Microsoft.Maui.Layouts;
using PokerTrainer.Enum;
using PokerTrainer.Mobile.Controls;
using PokerTrainer.Mobile.Services;
using PokerTrainer.Mobile.ViewModels;
using PokerTrainer.Models;
using PokerTrainer.Services;

namespace PokerTrainer.Mobile.Views;

public partial class PokerTablePage : ContentPage
{
    private decimal BetStep => _table.CurrentHand?.BigBlind ?? 1m;
    private decimal MinimumRaiseTo => _table.CurrentHand?.MinimumRaiseTo ?? 0m;
    private readonly SeatLayoutService _seatLayoutService = new SeatLayoutService();
    private readonly List<PokerSeatView> _seatViews = new List<PokerSeatView>();
    private bool _hasCreatedSeats;
    private bool _hasStartedHand;
    private readonly PokerTable _table;
    private readonly IHandLifecycleService _handLifecycleService;
    private readonly IPokerGameService _pokerGameService;
    private readonly ITurnOrderService _turnOrderService;
    private readonly PotService _potService = new();
    private readonly List<(int Number, List<(Guid Id, CheckBox Box)> Choices)> _potChoices = new();
    private readonly IBettingUndo _bettingUndo = new BettingUndo();
    private decimal _chipsToAdd;
    private readonly HandHistoryStoreService _handHistoryStore = new();
    private bool _isStartingNewHand;
    public PokerTablePage(PokerTable table, int buttonSeat, decimal smallBlind, decimal bigBlind)
    {
        InitializeComponent();

        PokerTableLayout.SizeChanged += (_, _) => PositionSeatViews();

        var betting = new BettingService();
        _turnOrderService = new TurnOrderService();
        _handLifecycleService = new HandLifecycleService(betting, _turnOrderService);
        _pokerGameService = new PokerGameService(betting, _turnOrderService, _handLifecycleService, _potService);
        _table = table;

        BetAmountEntry.TextChanged += (_, _) => RefreshBetDraft();

        _handLifecycleService.StartNewHand(table, buttonSeat, smallBlind, bigBlind);
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
        if (_table.CurrentPlayer is not { } player)
            return;
        decimal max = player.StreetCommitted + player.Stack;
        if (max <= MinimumRaiseTo)
            return;
        amount = Math.Clamp(amount, MinimumRaiseTo, max - 1m);

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
        }, true);
    }

    private async void Raise_Clicked(object? sender, EventArgs e)
    {
        var hand = _table.CurrentHand;
        var player = _table.CurrentPlayer;

        if (hand is null || player is null ||
            !TryReadChipDraft(out decimal amount) ||
            amount < GetMinimumChipsToAdd(hand, player) ||
            amount >= player.Stack)
        {
            RefreshBetDraft();
            return;
        }

        await ExecuteActionAsync(() =>
        {
            if (hand.CurrentBet == 0)
            {
                _pokerGameService.Bet(_table, amount);
            }
            else
            {
                decimal raiseTo = player.StreetCommitted + amount;
                _pokerGameService.RaiseTo(_table, raiseTo);
            }
        }, undoable: true);
    }

    private async void AllIn_Clicked(
    object sender,
    EventArgs e)
    {
        await ExecuteActionAsync(
            () => _pokerGameService.AllIn(
                _table), true);
    }

    private async void NextStreet_Clicked(
    object sender,
    EventArgs e)
    {
        await ExecuteActionAsync(
            () => _pokerGameService
                .MoveToNextStreet(
                    _table));
    }

    private void RefreshStreetDisplay(
    PokerStreet currentStreet)
    {
        Color inactiveBackground =
            Color.FromArgb("#222B25");

        Color inactiveText =
            Color.FromArgb("#A0AAA4");

        Color activeBackground =
            Color.FromArgb("#2FA96D");

        PreFlopBorder.BackgroundColor =
            inactiveBackground;

        FlopBorder.BackgroundColor =
            inactiveBackground;

        TurnBorder.BackgroundColor =
            inactiveBackground;

        RiverBorder.BackgroundColor =
            inactiveBackground;

        PreFlopLabel.TextColor =
            inactiveText;

        FlopLabel.TextColor =
            inactiveText;

        TurnLabel.TextColor =
            inactiveText;

        RiverLabel.TextColor =
            inactiveText;

        switch (currentStreet)
        {
            case PokerStreet.PreFlop:
                PreFlopBorder.BackgroundColor =
                    activeBackground;

                PreFlopLabel.TextColor =
                    Colors.White;

                break;

            case PokerStreet.Flop:
                FlopBorder.BackgroundColor =
                    activeBackground;

                FlopLabel.TextColor =
                    Colors.White;

                break;

            case PokerStreet.Turn:
                TurnBorder.BackgroundColor =
                    activeBackground;

                TurnLabel.TextColor =
                    Colors.White;

                break;

            case PokerStreet.River:
                RiverBorder.BackgroundColor =
                    activeBackground;

                RiverLabel.TextColor =
                    Colors.White;

                break;
        }
    }


    /*private void DisplaySeats(
    IReadOnlyList<PlayerSeatViewModel> seats)
    {
        *//*double seatWidth = seats.Count == 9 ? 88 : 112;
        double seatHeight = seats.Count == 9 ? 50 : 62;*//*

        double preferredWidth = seats.Count switch
        {
            <= 4 => 112,
            <= 6 => 100,
            _    => 88
        };

        double tableWidth = PokerTableLayout.Width;
        double seatWidth = preferredWidth;

        if(tableWidth > 0 && seats.Count >= 7)
        {
            const double gap = 8;
            //vi tri 3 ghe tren cung
            seatWidth = Math.Min(preferredWidth, (tableWidth - 2 * gap) / 3);
        }

        double seatHeight = seats.Count switch
        {
            <= 4 => 62,
            <= 6 => 56,
            _ => 50
        };

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
                    IsVisible = true,
                    WidthRequest = seatWidth,
                    HeightRequest = seatHeight
                };

            AbsoluteLayout.SetLayoutBounds(
                seatView,
                new Rect(
                    seat.X,
                    seat.Y,
                    seatWidth,
                    seatHeight));

            AbsoluteLayout.SetLayoutFlags(
                seatView,
                AbsoluteLayoutFlags.PositionProportional);

            PokerTableLayout.Children.Add(
                seatView);

            _seatViews.Add(
                seatView);
        }

        *//*System.Diagnostics.Debug.WriteLine(
            $"Số ghế đã hiển thị: {_seatViews.Count}");

        System.Diagnostics.Debug.WriteLine(
            $"Số control trong bàn: {PokerTableLayout.Children.Count}");*//*
    }*/

    private void DisplaySeats(IReadOnlyList<PlayerSeatViewModel> seats)
    {
        // Chỉ xóa các thẻ ghế; giữ nguyên mặt bàn và pot.
        foreach (var oldView in _seatViews)
            PokerTableLayout.Children.Remove(oldView);

        _seatViews.Clear();

        foreach (var seat in seats)
        {
            var view = new PokerSeatView(seat) { ZIndex = 10 };

            _seatViews.Add(view);
            PokerTableLayout.Children.Add(view);
        }

        PositionSeatViews();
    }

    private void PositionSeatViews()
    {
        double width = PokerTableLayout.Width;
        double height = PokerTableLayout.Height;
        int count = _seatViews.Count;

        if (width <= 0 || height <= 0 || count == 0)
            return;

        double seatWidth = count <= 4 ? 112 : count <= 6 ? 100 : 88;
        double seatHeight = count <= 4 ? 62 : count <= 6 ? 56 : 50;

        if (count >= 7)
            seatWidth = Math.Min(seatWidth, width / 4);

        for (int i = 0; i < count; i++)
        {
            var view = _seatViews[i];
            var center = _seatLayoutService.GetSeatCenter(
                i, count, width, height, seatWidth, seatHeight);

            view.WidthRequest = seatWidth;
            view.HeightRequest = seatHeight;

            AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.None);
            AbsoluteLayout.SetLayoutBounds(view, new Rect(
                center.X - seatWidth / 2,
                center.Y - seatHeight / 2,
                seatWidth,
                seatHeight));
        }
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

        List<Player> orderedPlayers = _table.Players
            .Where(p => p.Stack + p.TotalCommitted > 0)
            .OrderBy(x => x.SeatNumber).ToList();
        if (orderedPlayers.Count < 2 || orderedPlayers.All(p => p.SeatNumber != buttonSeat))
            orderedPlayers = _table.Players.OrderBy(p => p.SeatNumber).ToList();

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
        var playersInSeatOrder = _table.Players
        .OrderBy(p => p.SeatNumber)
        .ToList();

        UndoButton.IsEnabled = _bettingUndo.CanUndo(_table);

        Dictionary<int, string> positionBySeat = BuildPositionBySeat();

        var seats = new List<PlayerSeatViewModel>();

        foreach (Player player in _table.Players.OrderBy(p => p.SeatNumber))
        {
            var seat = new PlayerSeatViewModel(player,
                positionBySeat.GetValueOrDefault(player.SeatNumber, "Hết chip"));

            seat.IsCurrentPlayer = _table.CurrentPlayer?.Id == player.Id;

            seats.Add(seat);
        }

        //_seatLayoutService.ArrangeSeats(seats);

        DisplaySeats(seats);

        PotLabel.Text = _table.CurrentPot.ToString("0");

        PokerHand? hand = _table.CurrentHand;

        RefreshBoard(hand);

        BlindLabel.Text = hand is null ? _table.Name
            : $"{_table.Name} • {hand.SmallBlind:0.##} / {hand.BigBlind:0.##}";
        RefreshEndOfHandControls();

        RefreshBoard(hand);

        if (hand is null)
        {
            StreetPotLabel.Text = "Hand đã kết thúc.";

            NextStreetButton.IsVisible = false;
            RefreshActionButtons();

            return;
        }


        RefreshStreetDisplay(
            hand.CurrentStreet);

        bool bettingRoundComplete =
            _turnOrderService
                .IsBettingRoundComplete(
                    _table);

        bool canMoveToNextStreet =
            bettingRoundComplete &&
            hand.CurrentStreet !=
                PokerStreet.Showdown;

        NextStreetButton.IsVisible =
            canMoveToNextStreet;

        NextStreetButton.IsEnabled =
            canMoveToNextStreet;

        NextStreetButton.Text =
            hand.CurrentStreet switch
            {
                PokerStreet.PreFlop =>
                    "CHUYỂN SANG FLOP",

                PokerStreet.Flop =>
                    "CHUYỂN SANG TURN",

                PokerStreet.Turn =>
                    "CHUYỂN SANG RIVER",

                PokerStreet.River =>
                    "CHUYỂN SANG SHOWDOWN",

                _ =>
                    "VÒNG CƯỢC ĐÃ KẾT THÚC"
            };

        RefreshActionButtons();

        decimal streetPot = hand.GetPotForStreet(hand.CurrentStreet);

        StreetPotLabel.Text =
        $"{hand.CurrentStreet}: " +
        $"{streetPot:0}";

        int? handNumber = _table.CurrentHand?.HandNumber ?? _table.HandHistory.LastOrDefault()?.HandNumber;

        HandTitleLabel.Text = handNumber is int number ? $"Hand #{number}" : "Chưa có hand";

        RefreshActionButtons();
    }
    private void RefreshActionButtons()
    {
        var hand = _table.CurrentHand;
        var player = _table.CurrentPlayer;
        bool canAct = hand is not null && player is not null;

        FoldButton.IsEnabled = canAct;
        CallButton.IsEnabled = canAct;
        AllInButton.IsEnabled = canAct;

        // Nơi duy nhất cập nhật BetDraftPanel và RaiseButton.
        RefreshBetDraft();

        if (hand is null || player is null)
        {
            CallButton.Text = "CHECK / CALL";
            AllInButton.Text = "ALL-IN";
            return;
        }

        decimal callAmount = Math.Min(
            player.Stack,
            Math.Max(0m, hand.CurrentBet - player.StreetCommitted));

        CallButton.Text = callAmount == 0m
            ? "CHECK"
            : $"CALL {callAmount:N0}";

        AllInButton.Text = $"ALL-IN\n{player.Stack:N0}";
    }

    private async Task ExecuteActionAsync(
    Action action, bool undoable = false)
    {
        try
        {
            int previousHandCount =
            _table.HandHistory.Count;

            //action();
            if (undoable)
            {
                _bettingUndo.Execute(_table, action);
            }
            else
            {
                action();
                _bettingUndo.Clear();
            }

            BetAmountEntry.Text = string.Empty;

            RefreshTable();

            if (_table.HandHistory.Count >
            previousHandCount)
            {
                PokerHand completedHand =
                    _table.HandHistory.Last();

                await DisplayAlertAsync(
                    "Hand kết thúc",
                    FormatResult(completedHand),
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Không thể thực hiện",
                ex.Message,
                "OK");
        }
    }

    private static string FormatResult(PokerHand hand) =>
        string.Join("\n", hand.Payments
            .GroupBy(p => (p.PlayerId, p.PlayerName, Refund: p.PotNumber is null))
            .Select(g => $"{g.Key.PlayerName}: {(g.Key.Refund ? "hoàn" : "thắng")} {g.Sum(p => p.Amount):0.##} chip"));

    private void RefreshEndOfHandControls()
    {
        var hand = _table.CurrentHand;
        var lastHand = _table.HandHistory.LastOrDefault();
        ShowdownPanel.IsVisible = hand?.CurrentStreet == PokerStreet.Showdown;
        _potChoices.Clear();
        PotChoicesLayout.Children.Clear();
        AwardPotButton.IsEnabled = false;
        if (ShowdownPanel.IsVisible)
        {
            var breakdown = _potService.Preview(_table);
            foreach (var refund in breakdown.Refunds)
                PotChoicesLayout.Children.Add(new Label
                {
                    Text = $"Hoàn {refund.Amount:0.##} chip cho {refund.PlayerName}",
                    TextColor = Colors.White
                });

            foreach (var pot in breakdown.Pots)
            {
                string title = pot.Number == 1 ? "Main pot" : $"Side pot {pot.Number - 1}";
                PotChoicesLayout.Children.Add(new Label
                {
                    Text = $"{title}: {pot.Amount:0.##} chip",
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold
                });
                var choices = new List<(Guid Id, CheckBox Box)>();
                _potChoices.Add((pot.Number, choices));
                foreach (var player in _table.Players.Where(p => pot.EligibleIds.Contains(p.Id)))
                {
                    var box = new CheckBox
                    {
                        IsChecked = pot.EligibleIds.Count == 1,
                        IsEnabled = pot.EligibleIds.Count > 1
                    };
                    string name = $"{player.Name} — Ghế {player.SeatNumber}";
                    SemanticProperties.SetDescription(box, name);
                    box.CheckedChanged += (_, _) => RefreshAwardButton();
                    choices.Add((player.Id, box));
                    PotChoicesLayout.Children.Add(new HorizontalStackLayout
                    {
                        Children = { box, new Label { Text = name, TextColor = Colors.White,
                            VerticalOptions = LayoutOptions.Center } }
                    });
                }
            }
            RefreshAwardButton();
        }

        bool finished = hand is null && lastHand is not null;
        bool canRestart = _table.Players.Count(p => p.Stack > 0) >= 2;
        HandResultLabel.IsVisible = finished;
        HandResultLabel.Text = finished
            ? FormatResult(lastHand!) + (canRestart ? "" : "\nCần ít nhất hai người còn chip.")
            : "";
        NewHandButton.IsVisible = finished;
        NewHandButton.IsEnabled = finished && canRestart;
    }

    private void RefreshAwardButton() =>
        AwardPotButton.IsEnabled = ShowdownPanel.IsVisible && ShowdownPanel.IsEnabled &&
            _potChoices.All(group => group.Choices.Any(choice => choice.Box.IsChecked));

    private async void AwardPot_Clicked(object? sender, EventArgs e)
    {
        if (!AwardPotButton.IsEnabled || _table.CurrentHand is not { } hand)
            return;

        var winners = _potChoices.ToDictionary(group => group.Number,
            group => group.Choices.Where(choice => choice.Box.IsChecked).Select(choice => choice.Id).ToArray());
        ShowdownPanel.IsEnabled = false;
        try
        {
            if (await DisplayAlertAsync("Xác nhận", "Trao pot theo các lựa chọn đã đánh dấu?", "Trao pot", "Hủy")
                && _table.CurrentHand?.Id == hand.Id)
                await ExecuteActionAsync(() => _potService.SettleShowdown(_table, winners));
        }
        finally
        {
            ShowdownPanel.IsEnabled = true;
            RefreshAwardButton();
        }
    }

    private async void NewHand_Clicked(object? sender, EventArgs e)
    {
        if (_isStartingNewHand || _table.CurrentHand is not null)
            return;

        PokerHand? lastHand = _table.HandHistory.LastOrDefault();

        if(lastHand is null)
        {
            return;
        }

        var playablePlayers = _table.Players.Where(p => p.Stack > 0).OrderBy(p => p.SeatNumber).ToList();

        if (playablePlayers.Count < 2)
        {
            return;
        }

        // Tìm ghế kế tiếp theo số ghế; nếu tới cuối bàn thì vòng về đầu.
        /*int nextButtonSeat = playablePlayers
            .FirstOrDefault(p => p.SeatNumber > lastHand.ButtonSeat)
            ?.SeatNumber
            ?? playablePlayers[0].SeatNumber;*/
        int nextButtonSeat = NextButtonSeatSelector.Select(
        _table, lastHand.ButtonSeat);

        _isStartingNewHand = true;

        try
        {
            try
            {
                await _handHistoryStore.SaveAsync(lastHand.CreateHistoryRecord());
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException)
            {
                await DisplayAlertAsync("Chưa lưu được hand", ex.Message, "OK");
                return;
            }

            await ExecuteActionAsync(() =>
                _handLifecycleService.StartNewHand(
                    _table,
                    buttonSeat: nextButtonSeat,
                    smallBlind: lastHand.SmallBlind,
                    bigBlind: lastHand.BigBlind));
        }
        finally
        {
            _isStartingNewHand = false;
        }
    }

    private void Undo_Clicked(object? sender, EventArgs e)
    {
        if (_bettingUndo.TryUndo(_table)) RefreshTable();
    }

    private decimal GetMinimumChipsToAdd(PokerHand hand, Player player)
    {
        return hand.CurrentBet == 0 ? hand.BigBlind : Math.Max(0m, hand.MinimumRaiseTo - player.StreetCommitted);
    }

    private void ChipDenomination_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button button ||
            !int.TryParse(button.CommandParameter?.ToString(), out int chip) ||
            chip <= 0 ||
            _table.CurrentPlayer is null)
        {
            return;
        }

        decimal current = 0m;

        if (!string.IsNullOrWhiteSpace(BetAmountEntry.Text) &&
            !TryReadChipDraft(out current))
        {
            // Đang nhập tay chưa hợp lệ: không âm thầm ghi đè.
            return;
        }

        if (current <= decimal.MaxValue - chip)
            BetAmountEntry.Text = (current + chip).ToString("0");
    }

    private bool TryReadChipDraft(out decimal amount)
    {
        return decimal.TryParse(BetAmountEntry.Text, out amount)
            && amount > 0
            && amount == decimal.Truncate(amount);
    }

    private void MinimumChips_Clicked(object? sender, EventArgs e)
    {
        if (_table.CurrentHand is { } hand &&
            _table.CurrentPlayer is { } player)
        {
            BetAmountEntry.Text = GetMinimumChipsToAdd(hand, player).ToString("0");
        }
    }

    private void ClearChipDraft_Clicked(object? sender, EventArgs e)
    {
        BetAmountEntry.Text = string.Empty;
    }

    private void RefreshBetDraft()
    {
        var hand = _table.CurrentHand;
        var player = _table.CurrentPlayer;

        bool canAct = hand is not null && player is not null;
        BetDraftPanel.IsEnabled = canAct;
        RaiseButton.IsEnabled = false;
        BetDraftLabel.Text = string.Empty;

        if (hand is null || player is null)
            return;

        bool isBet = hand.CurrentBet == 0;
        RaiseButton.Text = isBet ? "BET" : "RAISE";

        decimal minimum = GetMinimumChipsToAdd(hand, player);

        if (!TryReadChipDraft(out decimal amount))
        {
            BetDraftLabel.Text = string.IsNullOrWhiteSpace(BetAmountEntry.Text)
                ? $"Bỏ thêm tối thiểu: {minimum:N0}"
                : "Nhập số chip nguyên dương.";
            return;
        }

        if (amount > player.Stack)
        {
            BetDraftLabel.Text = $"Vượt stack: chỉ còn {player.Stack:N0} chip.";
            return;
        }

        if (amount == player.Stack)
        {
            BetDraftLabel.Text = "Bằng toàn bộ stack — dùng nút ALL-IN.";
            return;
        }

        if (amount < minimum)
        {
            BetDraftLabel.Text = $"Bỏ thêm {amount:N0} — tối thiểu {minimum:N0}.";
            return;
        }

        decimal target = player.StreetCommitted + amount;
        BetDraftLabel.Text = isBet
            ? $"Bỏ thêm {amount:N0} chip"
            : $"Bỏ thêm {amount:N0} · Raise lên {target:N0}";

        RaiseButton.Text = isBet ? $"BET {amount:N0}" : $"RAISE {target:N0}";
        RaiseButton.IsEnabled = true;
    }

    private void RefreshBoard(PokerHand? hand)
    {
        EditBoardButton.IsVisible = hand?.CurrentStreet is
            PokerStreet.Flop or PokerStreet.Turn or
            PokerStreet.River or PokerStreet.Showdown;

        if (hand is null || hand.CurrentStreet == PokerStreet.PreFlop)
        {
            BoardLabel.Text = string.Empty;
            BoardLabel.IsVisible = false;
            return;
        }

        BoardLabel.IsVisible = true;

        var slots = new List<string>();

        foreach (var (street, count) in new[]
        {
        (PokerStreet.Flop, 3),
        (PokerStreet.Turn, 1),
        (PokerStreet.River, 1)
    })
        {
            if (street > hand.CurrentStreet)
                break;

            var cards = hand.GetRevealedCards(street);

            if (cards.Count == 0)
                slots.AddRange(Enumerable.Repeat("?", count));
            else
                slots.AddRange(cards.Select(FormatCard));
        }

        BoardLabel.Text = string.Join("    ", slots);
    }

    private static string FormatCard(PokerCard card)
    {
        string rank = card.Rank switch
        {
            11 => "J",
            12 => "Q",
            13 => "K",
            14 => "A",
            _ => card.Rank.ToString()
        };

        string suit = card.Suit switch
        {
            CardSuit.Clubs => "♣",
            CardSuit.Diamonds => "♦",
            CardSuit.Hearts => "♥",
            CardSuit.Spades => "♠",
            _ => "?"
        };

        return rank + suit;
    }

    private async void EditBoard_Clicked(object sender, EventArgs e)
    {
        var hand = _table.CurrentHand;

        string[] options = hand?.CurrentStreet switch
        {
            PokerStreet.Flop => ["Flop"],
            PokerStreet.Turn => ["Flop", "Turn"],
            PokerStreet.River or PokerStreet.Showdown => ["Flop", "Turn", "River"],
            _ => []
        };

        if(hand is null || options.Length == 0)
        {
            return;
        }

        string? choice = options.Length == 1 ? options[0] : await DisplayActionSheetAsync("Nhập bài vòng nào?", "Hủy", null, options);

        PokerStreet? street = choice switch
        {
            "Flop" => PokerStreet.Flop,
            "Turn" => PokerStreet.Turn,
            "River" => PokerStreet.River,
            _=> null
        };
        if (street is not PokerStreet selectedStreet)
            return;

        int expectedCount = selectedStreet == PokerStreet.Flop ? 3 : 1;
        string example = expectedCount == 3 ? "Kh 8s 3d" : "4c";

        string? input = await DisplayPromptAsync(
            $"Nhập {choice}",
            $"Nhập {expectedCount} lá, cách nhau bằng dấu cách. Ví dụ: {example}");

        if (input is null || !ReferenceEquals(hand, _table.CurrentHand))
            return;

        string[] tokens = input.Split(
            ' ', StringSplitOptions.RemoveEmptyEntries);

        var cards = new PokerCard[expectedCount];

        if (tokens.Length != expectedCount)
        {
            await DisplayAlertAsync(
                "Sai số lá", $"{choice} cần đúng {expectedCount} lá.", "OK");
            return;
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            if (!TryParseCard(tokens[i], out cards[i]))
            {
                await DisplayAlertAsync(
                    "Lá bài không hợp lệ", $"Không đọc được: {tokens[i]}", "OK");
                return;
            }
        }

        try
        {
            hand.SetBoardCards(selectedStreet, cards);
        }
        catch (Exception ex) when (
            ex is ArgumentException or InvalidOperationException)
        {
            await DisplayAlertAsync("Không thể lưu bài", ex.Message, "OK");
            return;
        }

        RefreshTable();
    }

    private static bool TryParseCard(string token, out PokerCard card)
    {
        card = default;
        token = token.ToUpperInvariant();

        if (token.Length != 2)
            return false;

        int rankIndex = "23456789TJQKA".IndexOf(token[0]);

        CardSuit? suit = token[1] switch
        {
            'C' => CardSuit.Clubs,
            'D' => CardSuit.Diamonds,
            'H' => CardSuit.Hearts,
            'S' => CardSuit.Spades,
            _ => null
        };

        if (rankIndex < 0 || suit is not CardSuit validSuit)
            return false;

        card = new PokerCard(rankIndex + 2, validSuit);
        return true;
    }

    private async void EditShowdownCards_Clicked(object? sender, EventArgs e)
    {
        var hand = _table.CurrentHand;

        if (hand is null || hand.CurrentStreet != PokerStreet.Showdown)
            return;

        var playersByOption = _table.Players
            .Where(p => !p.HasFolded && p.SeatNumber != hand.HeroSeatNumber)
            .OrderBy(p => p.SeatNumber)
            .ToDictionary(p => $"Ghế {p.SeatNumber} · {p.Name}");

        if (playersByOption.Count == 0)
        {
            await DisplayAlertAsync(
                "Showdown", "Không có ghế đối thủ để nhập bài.", "OK");
            return;
        }

        string? choice = await DisplayActionSheetAsync(
            "Ai đã lật bài?",
            "Hủy",
            null,
            playersByOption.Keys.ToArray());

        if (choice is null ||
            !playersByOption.TryGetValue(choice, out var player))
            return;

        string? input = await DisplayPromptAsync(
            choice,
            "Nhập đúng hai lá đã lật, cách nhau bằng dấu cách. Ví dụ: As Kd.",
            accept: "Ghi bài",
            cancel: "Hủy");

        // Không ghi vào hand cũ nếu trạng thái đã đổi khi hộp thoại đang mở.
        if (input is null ||
            !ReferenceEquals(hand, _table.CurrentHand) ||
            hand.CurrentStreet != PokerStreet.Showdown)
            return;

        string[] tokens = input.Split(
            ' ', StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length != 2 ||
            !TryParseCard(tokens[0], out var first) ||
            !TryParseCard(tokens[1], out var second))
        {
            await DisplayAlertAsync(
                "Bài không hợp lệ", "Hãy nhập đúng hai lá, ví dụ: As Kd.", "OK");
            return;
        }

        try
        {
            hand.SetShowDownCards(player.SeatNumber, first, second);
        }
        catch (Exception ex) when (
            ex is ArgumentException or InvalidOperationException)
        {
            await DisplayAlertAsync("Không thể ghi bài", ex.Message, "OK");
            return;
        }

        await DisplayAlertAsync("Đã ghi bài", choice, "OK");
    }
}
