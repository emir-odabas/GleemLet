using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GleemLet.Models;
using GleemLet.Services;

namespace GleemLet.ViewModels;

public partial class StudyViewModel : BaseViewModel
{
    private readonly DataService _ds = DataService.Instance;

    // ── Session State ──────────────────────────────────────
    [ObservableProperty] private string    _studyTitle   = "";
    [ObservableProperty] private string    _counterText  = "";
    [ObservableProperty] private string    _timerText    = "";
    [ObservableProperty] private double    _progressPct;
    [ObservableProperty] private bool      _isFinished;

    // ── Current Card ──────────────────────────────────────
    [ObservableProperty] private string    _wordText     = "";
    [ObservableProperty] private string    _translationText = "";
    [ObservableProperty] private string    _exampleText  = "";
    [ObservableProperty] private bool      _isFlipped;
    [ObservableProperty] private bool      _showActions;

    // ── Results ───────────────────────────────────────────
    [ObservableProperty] private int    _correctCount;
    [ObservableProperty] private int    _wrongCount;
    [ObservableProperty] private double _accuracy;
    [ObservableProperty] private int    _durationSeconds;
    [ObservableProperty] private List<string> _newBadges = [];

    // ── Internal ──────────────────────────────────────────
    private string            _setId        = "";
    private StudyMode         _mode         = StudyMode.Flashcard;
    private List<Flashcard>   _queue        = [];
    private int               _index;
    private bool              _answered;
    private DateTime          _sessionStart;
    private System.Windows.Threading.DispatcherTimer? _timer;
    private int               _timedSeconds;

    // Dışarıya bildirim — MainWindow hâlâ UI'ı çiziyor
    public event Action?         SessionEnded;
    public event Action<string>? NavigateBack;

    public StudyViewModel()
    {
        Title = "Study";
    }

    // ── Start ─────────────────────────────────────────────
    public void StartSession(string setId, StudyMode mode)
    {
        var set = _ds.Data.Sets.FirstOrDefault(s => s.Id == setId);
        if (set == null || set.Words.Count == 0) return;

        _setId        = setId;
        _mode         = mode;
        _queue        = [.. set.Words];

        if (_ds.Data.Profile.ShuffleDefault || mode == StudyMode.Timed)
            _queue = [.. _queue.OrderBy(_ => Random.Shared.Next())];

        _index        = 0;
        CorrectCount  = 0;
        WrongCount    = 0;
        _answered     = false;
        _sessionStart = DateTime.Now;
        IsFinished    = false;

        StudyTitle = mode.ToLabel();

        if (mode == StudyMode.Timed)
        {
            _timedSeconds = 0;
            TimerText     = "0:00";
            _timer        = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) =>
            {
                _timedSeconds++;
                TimerText = $"{_timedSeconds / 60}:{_timedSeconds % 60:D2}";
            };
            _timer.Start();
        }
        else
        {
            TimerText = "";
            _timer?.Stop();
        }

        LoadCurrentCard();
    }

    // ── Card Loading ──────────────────────────────────────
    private void LoadCurrentCard()
    {
        _answered = false;

        if (_index >= _queue.Count)
        {
            FinishSession();
            return;
        }

        var w = _queue[_index];
        ProgressPct  = _queue.Count > 0 ? (double)_index / _queue.Count : 0;
        CounterText  = $"{_index + 1}/{_queue.Count}  ·  ✓{CorrectCount} ✗{WrongCount}";
        WordText     = w.En ?? "";
        TranslationText = w.Tr ?? "";
        ExampleText  = w.Example ?? "";
        IsFlipped    = false;
        ShowActions  = false;
    }

    // ── Flashcard Actions ─────────────────────────────────
    [RelayCommand]
    public void FlipCard()
    {
        if (IsFlipped) return;
        IsFlipped   = true;
        ShowActions = true;
    }

    [RelayCommand]
    public void MarkKnown()
    {
        if (_index >= _queue.Count) return;
        var w = _queue[_index];
        w.Learned = true;
        w.CorrectCount++;
        CorrectCount++;
        _ds.Save();
        _index++;
        LoadCurrentCard();
    }

    [RelayCommand]
    public void MarkUnknown()
    {
        if (_index >= _queue.Count) return;
        _queue[_index].WrongCount++;
        WrongCount++;
        _index++;
        LoadCurrentCard();
    }

    [RelayCommand]
    public void NextCard()
    {
        _index++;
        LoadCurrentCard();
    }

    [RelayCommand]
    public void PrevCard()
    {
        if (_index > 0) { _index--; LoadCurrentCard(); }
    }

    [RelayCommand]
    public void ShuffleQueue()
    {
        _queue  = [.. _queue.OrderBy(_ => Random.Shared.Next())];
        _index  = 0;
        LoadCurrentCard();
    }

    // ── End ───────────────────────────────────────────────
    [RelayCommand]
    public void EndSession()
    {
        _timer?.Stop();
        NavigateBack?.Invoke(_setId);
    }

    private void FinishSession()
    {
        _timer?.Stop();
        IsFinished = true;

        int total   = CorrectCount + WrongCount;
        Accuracy    = total > 0 ? (double)CorrectCount / total * 100 : 0;
        DurationSeconds = (int)(DateTime.Now - _sessionStart).TotalSeconds;

        var session = new StudySession
        {
            SetId           = _setId,
            SetName         = _ds.Data.Sets.FirstOrDefault(s => s.Id == _setId)?.Name ?? "",
            Mode            = _mode.ToSerializedString(),
            Correct         = CorrectCount,
            Wrong           = WrongCount,
            Total           = total,
            DurationSeconds = DurationSeconds
        };

        NewBadges = _ds.RecordSession(session);
        SessionEnded?.Invoke();
    }

    [RelayCommand]
    public void Retry() => StartSession(_setId, _mode);

    public Flashcard? CurrentCard => _index < _queue.Count ? _queue[_index] : null;
    public StudyMode  CurrentMode => _mode;
    public string     CurrentSetId => _setId;
}
