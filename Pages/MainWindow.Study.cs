using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GleemLet.Models;
using GleemLet.Services;
using MaterialDesignThemes.Wpf;
using SoundSvc = GleemLet.Services.SoundService;

namespace GleemLet;

public partial class MainWindow
{
    // ═══════════════════════════════════════
    //  STUDY MODES
    // ═══════════════════════════════════════
    private void StartStudy(StudyMode mode)
    {
        var set = _ds.Data.Sets.FirstOrDefault(s => s.Id == _currentSetId);
        if (set == null || set.Words.Count == 0) { ShowMsg(L.AddWordsFirst); return; }
        if (set.Words.Count < 2 && mode == StudyMode.Learn) { ShowMsg(L.NeedTwoWords); return; }

        if (mode == StudyMode.Learn)
        {
            int totalOthers = _ds.Data.Sets.SelectMany(s => s.Words).Count(w => w.Id != set.Words.FirstOrDefault()?.Id);
            if (totalOthers < 3 && set.Words.Count < 4) { ShowMsg(L.NeedFourWords); return; }
        }

        _studyMode    = mode;
        _studyQueue   = [.. set.Words];
        if (_ds.Data.Profile.ShuffleDefault || mode == StudyMode.Timed)
            _studyQueue = [.. _studyQueue.OrderBy(_ => Random.Shared.Next())];
        _studyIndex   = 0;
        _studyCorrect = 0;
        _studyWrong   = 0;
        _answered     = false;
        _sessionStart = DateTime.Now;

        _currentPage = "study";
        HideAllPages();
        foreach (var nb in new[] { NavHome, NavSets, NavStats, NavBadges, NavProfile })
            nb.IsChecked = false;

        PageStudy.Visibility = Visibility.Visible;
        StudyTitle.Text = mode.ToLabel();

        if (mode == StudyMode.Timed)
        {
            _timedSeconds  = 0;
            TimerText.Text = "0:00";
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                _timedSeconds++;
                TimerText.Text = $"{_timedSeconds / 60}:{_timedSeconds % 60:D2}";
            };
            _timer.Start();
        }
        else { TimerText.Text = ""; _timer?.Stop(); }

        RenderStudyQuestion();
    }

    private void RenderStudyQuestion()
    {
        StudyContent.Children.Clear();
        _answered = false;
        if (_studyIndex >= _studyQueue.Count) { ShowResults(); return; }

        var w   = _studyQueue[_studyIndex];
        double pct = _studyQueue.Count > 0 ? (double)_studyIndex / _studyQueue.Count : 0;
        StudyCounter.Text     = $"{_studyIndex + 1}/{_studyQueue.Count}";
        StudyCorrectText.Text = _studyCorrect.ToString();
        StudyWrongText.Text   = _studyWrong.ToString();
        Dispatcher.InvokeAsync(() => ProgressFill.Width = ProgressBorder.ActualWidth * pct, DispatcherPriority.Loaded);

        switch (_studyMode)
        {
            case StudyMode.Flashcard:
            case StudyMode.Timed:   RenderFlashcard(w); break;
            case StudyMode.Learn:   RenderLearn(w);     break;
            case StudyMode.Test:    RenderTestQ(w);      break;
        }
    }

    // ── FLASHCARD ──
    private void RenderFlashcard(Flashcard w)
    {
        _fcFlipped = false;
        var container = new Grid();
        container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var cardBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 58)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16),
            Cursor = Cursors.Hand, Margin = new Thickness(80, 40, 80, 20), Padding = new Thickness(40),
        };

        var inner     = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var labelText = MakeText(L.ClickToFlip, 10, "#4A4A60");
        labelText.HorizontalAlignment = HorizontalAlignment.Center;
        labelText.Margin = new Thickness(0, 0, 0, 24);

        var wordText = new TextBlock
        {
            Text = w.En, FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 36, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 240)),
            HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center
        };
        var trText = new TextBlock
        {
            Text = w.Tr, FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 20,
            Foreground = new SolidColorBrush(Color.FromRgb(79, 172, 130)),
            HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 16, 0, 0)
        };
        var exText = new TextBlock
        {
            Text = w.Example, FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 13, FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush(Color.FromRgb(74, 74, 96)),
            HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 12, 0, 0)
        };

        inner.Children.Add(labelText); inner.Children.Add(wordText);
        inner.Children.Add(trText);    inner.Children.Add(exText);
        cardBorder.Child = inner;

        var actionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16), Visibility = Visibility.Collapsed
        };
        var knowBtn  = new Button { Content = L.GotIt, Padding = new Thickness(24, 10, 24, 10), Margin = new Thickness(0, 0, 12, 0) };
        knowBtn.Style = (Style)FindResource("PrimaryButton");
        var stillBtn = new Button { Content = L.StillLearning, Padding = new Thickness(16, 10, 16, 10) };
        stillBtn.Style = (Style)FindResource("GhostButton");
        actionPanel.Children.Add(stillBtn); actionPanel.Children.Add(knowBtn);

        var navPanel   = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 32) };
        var prevBtn    = new Button { Content = L.Prev,    Style = (Style)FindResource("GhostButton"),   Padding = new Thickness(20, 10, 20, 10), Margin = new Thickness(0, 0, 10, 0) };
        var nextBtn    = new Button { Content = L.Next,    Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(20, 10, 20, 10) };
        var shuffleBtn = new Button { Content = L.Shuffle, Style = (Style)FindResource("GhostButton"),   Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(10, 0, 0, 0) };
        navPanel.Children.Add(prevBtn); navPanel.Children.Add(nextBtn); navPanel.Children.Add(shuffleBtn);

        Grid.SetRow(cardBorder, 0);
        var bottomPanel = new StackPanel();
        bottomPanel.Children.Add(actionPanel); bottomPanel.Children.Add(navPanel);
        Grid.SetRow(bottomPanel, 1);
        container.Children.Add(cardBorder); container.Children.Add(bottomPanel);
        StudyContent.Children.Add(container);

        void Flip()
        {
            if (_fcFlipped) return;
            _fcFlipped = true;
            trText.Visibility   = Visibility.Visible;
            exText.Visibility   = !string.IsNullOrEmpty(w.Example) ? Visibility.Visible : Visibility.Collapsed;
            labelText.Text      = L.Meaning;
            wordText.Foreground = new SolidColorBrush(Color.FromRgb(79, 172, 130));
            actionPanel.Visibility = Visibility.Visible;
            cardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 130));
        }

        cardBorder.MouseLeftButtonDown += (s, e) => Flip();
        cardBorder.KeyDown += (s, e) => { if (e.Key == Key.Space) Flip(); };
        knowBtn.Click    += (s, e) => { w.Learned = true; w.CorrectCount++; _studyCorrect++; _ds.Save(); _studyIndex++; RenderStudyQuestion(); };
        stillBtn.Click   += (s, e) => { w.WrongCount++; _studyWrong++;                                   _studyIndex++; RenderStudyQuestion(); };
        nextBtn.Click    += (s, e) => { _studyIndex++; RenderStudyQuestion(); };
        prevBtn.Click    += (s, e) => { if (_studyIndex > 0) { _studyIndex--; RenderStudyQuestion(); } };
        shuffleBtn.Click += (s, e) => { _studyQueue = [.. _studyQueue.OrderBy(_ => Random.Shared.Next())]; _studyIndex = 0; RenderStudyQuestion(); };
    }

    // ── LEARN (MULTIPLE CHOICE) ──
    private void RenderLearn(Flashcard correct)
    {
        var panel = new StackPanel { Margin = new Thickness(60, 32, 60, 32) };
        panel.Children.Add(MakeText(L.WhatMeaning, 12, "#8888A0"));

        var qCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)), BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 58)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(28, 24, 28, 24), Margin = new Thickness(0, 10, 0, 24)
        };
        var qInner = new StackPanel();
        qInner.Children.Add(MakeText(correct.En, 30, "#E8E8F0", isBold: true));
        if (!string.IsNullOrEmpty(correct.Example))
            qInner.Children.Add(MakeText(correct.Example, 12, "#4A4A60", margin: new Thickness(0, 8, 0, 0)));
        qCard.Child = qInner;
        panel.Children.Add(qCard);

        var allWords = _ds.Data.Sets.SelectMany(s => s.Words).Where(w => w.Id != correct.Id).ToList();
        var wrong3   = allWords.OrderBy(_ => Random.Shared.Next()).Take(3).ToList();
        if (wrong3.Count < 3)
        {
            var extraFromSet = _studyQueue.Where(w => w.Id != correct.Id).Take(3 - wrong3.Count).ToList();
            wrong3.AddRange(extraFromSet.Where(e => wrong3.All(x => x.Id != e.Id)));
        }

        var options      = wrong3.Append(correct).OrderBy(_ => Random.Shared.Next()).ToList();
        var resultBorder = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 14, 0, 0), Visibility = Visibility.Collapsed };
        var resultText   = MakeText("", 13, "#E8E8F0");
        resultBorder.Child = resultText;
        var nextBtn = new Button
        {
            Content = L.Next, Style = (Style)FindResource("PrimaryButton"),
            Padding = new Thickness(24, 10, 24, 10), HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0), Visibility = Visibility.Collapsed
        };

        var optGrid = new UniformGrid { Columns = 2 };
        foreach (var opt in options)
        {
            var btn = new Button
            {
                Content = opt.Tr, Style = (Style)FindResource("GhostButton"),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 8, 8),
                FontSize = 13, Tag = opt.Id
            };
            var capturedOpt = opt;
            btn.Click += (s, e) =>
            {
                if (_answered) return;
                _answered = true;
                foreach (Button b in optGrid.Children) b.IsEnabled = false;
                if (capturedOpt.Id == correct.Id)
                {
                    if (_ds.Data.Profile.SoundEnabled) SoundSvc.PlayCorrect();
                    AnimationHelper.PulseGreen(btn);
                    btn.Background  = new SolidColorBrush(Color.FromRgb(26, 46, 36));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                    btn.Foreground  = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                    correct.Learned = true; correct.CorrectCount++; _studyCorrect++;
                    resultBorder.Background   = new SolidColorBrush(Color.FromRgb(26, 46, 36));
                    resultBorder.BorderBrush  = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                    resultBorder.BorderThickness = new Thickness(1);
                    resultText.Text       = L.Correct;
                    resultText.Foreground = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                }
                else
                {
                    if (_ds.Data.Profile.SoundEnabled) SoundSvc.PlayWrong();
                    AnimationHelper.Shake(btn);
                    AnimationHelper.PulseRed(btn);
                    btn.Background  = new SolidColorBrush(Color.FromRgb(46, 26, 30));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                    btn.Foreground  = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                    _studyWrong++;
                    foreach (Button b in optGrid.Children)
                    {
                        if ((string?)b.Tag == correct.Id)
                        {
                            b.Background  = new SolidColorBrush(Color.FromRgb(26, 46, 36));
                            b.BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                            b.Foreground  = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                        }
                    }
                    resultBorder.Background   = new SolidColorBrush(Color.FromRgb(46, 26, 30));
                    resultBorder.BorderBrush  = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                    resultBorder.BorderThickness = new Thickness(1);
                    resultText.Text       = L.Incorrect(correct.Tr);
                    resultText.Foreground = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                }
                _ds.Save();
                resultBorder.Visibility = Visibility.Visible;
                nextBtn.Visibility      = Visibility.Visible;
            };
            optGrid.Children.Add(btn);
        }
        panel.Children.Add(optGrid);
        panel.Children.Add(resultBorder);
        nextBtn.Click += (s, e) => { _studyIndex++; RenderStudyQuestion(); };
        panel.Children.Add(nextBtn);
        var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        StudyContent.Children.Add(sv);
    }

    // ── TEST (TYPE ANSWER) ──
    private void RenderTestQ(Flashcard w)
    {
        var panel = new StackPanel { Margin = new Thickness(80, 40, 80, 32) };
        panel.Children.Add(MakeText(L.TypeMeaning, 12, "#8888A0"));

        var qCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)), BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 58)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(28, 24, 28, 24), Margin = new Thickness(0, 10, 0, 24)
        };
        var qInner = new StackPanel();
        qInner.Children.Add(MakeText(w.En, 30, "#E8E8F0", isBold: true));
        if (!string.IsNullOrEmpty(w.Example))
            qInner.Children.Add(MakeText(w.Example, 12, "#4A4A60", margin: new Thickness(0, 8, 0, 0)));
        qCard.Child = qInner;
        panel.Children.Add(qCard);

        var input        = new TextBox { Style = (Style)Application.Current.Resources["DarkTextBox"], FontSize = 15, Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 12) };
        var feedback     = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 12), Visibility = Visibility.Collapsed };
        var feedbackText = MakeText("", 13, "#E8E8F0");
        feedback.Child   = feedbackText;
        var checkBtn = new Button { Content = L.Check, Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(24, 10, 24, 10), HorizontalAlignment = HorizontalAlignment.Right };
        var nextBtn  = new Button { Content = L.Next,  Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(24, 10, 24, 10), HorizontalAlignment = HorizontalAlignment.Right, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 10, 0, 0) };

        panel.Children.Add(input); panel.Children.Add(feedback);

        var addSynonymBtn = new Button
        {
            Content    = L.Lang == AppLanguage.Turkish ? "Bu eş anlamlı mı? Ekle ➕" : "Is this a synonym? Add ➕",
            Visibility = Visibility.Collapsed, Margin = new Thickness(0, -6, 0, 12),
            Style      = (Style)FindResource("GhostButton")
        };
        panel.Children.Add(addSynonymBtn);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        btnPanel.Children.Add(checkBtn); btnPanel.Children.Add(nextBtn);
        panel.Children.Add(btnPanel);

        string Normalize(string s) => s
            .Replace("ş", "s").Replace("ç", "c").Replace("ğ", "g")
            .Replace("ı", "i").Replace("ö", "o").Replace("ü", "u");

        void Check()
        {
            if (_answered) return;
            var answer = input.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(answer)) return;
            _answered = true;
            string normAnswer = Normalize(answer);
            var corrects = w.Tr.Split([',', '/', ';'], StringSplitOptions.TrimEntries).Select(x => x.ToLower()).ToList();
            bool ok = corrects.Any(c => {
                string nc = Normalize(c);
                return nc == normAnswer || normAnswer.Contains(nc) || nc.Contains(normAnswer) || GetSimilarity(nc, normAnswer) >= 0.60;
            });
            input.IsEnabled = false;
            if (ok)
            {
                if (_ds.Data.Profile.SoundEnabled) SoundSvc.PlayCorrect();
                input.BorderBrush        = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                feedback.Background      = new SolidColorBrush(Color.FromRgb(26, 46, 36));
                feedback.BorderBrush     = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                feedback.BorderThickness = new Thickness(1);
                feedbackText.Text        = L.Correct;
                feedbackText.Foreground  = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                w.Learned = true; w.CorrectCount++; _studyCorrect++;
            }
            else
            {
                if (_ds.Data.Profile.SoundEnabled) SoundSvc.PlayWrong();
                input.BorderBrush        = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                feedback.Background      = new SolidColorBrush(Color.FromRgb(46, 26, 30));
                feedback.BorderBrush     = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                feedback.BorderThickness = new Thickness(1);
                feedbackText.Text        = L.Lang == AppLanguage.Turkish ? $"✗  Doğrusu: {corrects[0]}" : $"✗  Correct answer: {corrects[0]}";
                feedbackText.Foreground  = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                _studyWrong++;
                addSynonymBtn.Visibility = Visibility.Visible;
                addSynonymBtn.IsEnabled  = true;
                addSynonymBtn.Content    = L.Lang == AppLanguage.Turkish ? "Bu eş anlamlı mı? Ekle ➕" : "Is this a synonym? Add ➕";
                var wordToUpdate = w;
                var userAnswer   = answer;
                addSynonymBtn.Click -= AddSynonym_Click;
                void AddSynonym_Click(object sender, RoutedEventArgs args)
                {
                    var existing = wordToUpdate.Tr.Split([',', '/', ';']).Select(x => x.Trim().ToLower());
                    if (!existing.Contains(userAnswer)) { wordToUpdate.Tr += $", {userAnswer}"; _ds.Save(); }
                    addSynonymBtn.Content   = L.Lang == AppLanguage.Turkish ? "✓ Eş anlamlı eklendi!" : "✓ Synonym added!";
                    addSynonymBtn.IsEnabled = false;
                }
                addSynonymBtn.Click += AddSynonym_Click;
            }
            _ds.Save();
            feedback.Visibility = Visibility.Visible;
            checkBtn.Visibility = Visibility.Collapsed;
            nextBtn.Visibility  = Visibility.Visible;
        }

        checkBtn.Click += (s, e) => Check();
        input.KeyDown  += (s, e) => { if (e.Key == Key.Enter) Check(); };
        nextBtn.Click  += (s, e) => { _studyIndex++; RenderStudyQuestion(); };
        var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        StudyContent.Children.Add(sv);
        Dispatcher.InvokeAsync(() => input.Focus(), DispatcherPriority.Loaded);
    }

    private static double GetSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        int maxLen = Math.Max(a.Length, b.Length);
        if (maxLen == 0) return 1.0;
        return 1.0 - (double)LevenshteinDistance(a, b) / maxLen;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                dp[i, j] = a[i-1] == b[j-1] ? dp[i-1, j-1] : 1 + Math.Min(dp[i-1, j-1], Math.Min(dp[i-1, j], dp[i, j-1]));
        return dp[a.Length, b.Length];
    }

    // ── RESULTS ──
    private void ShowResults()
    {
        _timer?.Stop();
        int total   = _studyCorrect + _studyWrong;
        double pct  = total > 0 ? (double)_studyCorrect / total * 100 : 0;
        var session = new StudySession
        {
            SetId   = _currentSetId,
            SetName = _ds.Data.Sets.FirstOrDefault(s => s.Id == _currentSetId)?.Name ?? "",
            Mode    = _studyMode.ToSerializedString(),
            Correct = _studyCorrect, Wrong = _studyWrong,
            Total   = total, DurationSeconds = (int)(DateTime.Now - _sessionStart).TotalSeconds
        };
        var newBadges = _ds.RecordSession(session);

        StudyContent.Children.Clear();
        var sv    = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(40) };

        var resultIcon = pct == 100 ? PackIconKind.Trophy : pct >= 80 ? PackIconKind.PartyPopper : pct >= 60 ? PackIconKind.ThumbUp : PackIconKind.ArmFlex;
        panel.Children.Add(new PackIcon { Kind = resultIcon, Width = 64, Height = 64, Foreground = new SolidColorBrush(pct >= 60 ? Color.FromRgb(79, 172, 130) : Color.FromRgb(224, 108, 117)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 16) });
        panel.Children.Add(MakeText(pct == 100 ? L.Perfect : pct >= 80 ? L.GreatJob : pct >= 60 ? L.GoodEffort : L.KeepGoing, 28, "#E8E8F0", isBold: true, margin: new Thickness(0, 0, 0, 6)));
        panel.Children.Add(MakeText(L.Score(pct), 18, "#4FAC82", isBold: true, margin: new Thickness(0, 0, 0, 24)));

        var breakdown = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 24) };
        breakdown.Children.Add(MakeStatBox(_studyCorrect.ToString(), L.CorrectLabel, "#4FAC82"));
        breakdown.Children.Add(MakeStatBox(_studyWrong.ToString(), L.WrongLabel, "#E06C75"));
        breakdown.Children.Add(MakeStatBox(total.ToString(), L.TotalLabel, "#61AFEF"));
        breakdown.Children.Add(MakeStatBox(session.DurationSeconds < 60 ? $"{session.DurationSeconds}s" : $"{session.DurationSeconds / 60}m{session.DurationSeconds % 60}s", L.TimeLabel, "#E5C07B"));
        panel.Children.Add(breakdown);

        if (newBadges.Count > 0)
        {
            var badgeBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(26, 46, 36)), BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 130)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 0, 20), MaxWidth = 400 };
            var badgeInner  = new StackPanel();
            badgeInner.Children.Add(MakeText(L.NewBadges, 13, "#4FAC82", isBold: true, margin: new Thickness(0, 0, 0, 6)));
            foreach (var b in newBadges)
                badgeInner.Children.Add(MakeText(b, 13, "#E8E8F0", margin: new Thickness(0, 2, 0, 0)));
            badgeBorder.Child = badgeInner;
            panel.Children.Add(badgeBorder);
        }

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        var backBtn  = new Button { Content = L.BackToSet, Style = (Style)FindResource("GhostButton"),   Padding = new Thickness(20, 10, 20, 10), Margin = new Thickness(0, 0, 10, 0) };
        var retryBtn = new Button { Content = L.TryAgain,  Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(20, 10, 20, 10) };
        backBtn.Click  += (s, e) => ShowDetail(_currentSetId);
        retryBtn.Click += (s, e) => StartStudy(_studyMode);
        btnPanel.Children.Add(backBtn); btnPanel.Children.Add(retryBtn);
        panel.Children.Add(btnPanel);
        panel.Children.Add(MakeText("created by gleemron · emirodabas.dev", 10, "#2E2E3A", margin: new Thickness(0, 32, 0, 0)));

        sv.Content = panel;
        StudyContent.Children.Add(sv);
        ProgressFill.Width = ProgressBorder.ActualWidth;
        UpdateSidebar();
    }

    // ═══════════════════════════════════════
    //  STUDY EVENT HANDLERS
    // ═══════════════════════════════════════
    private void EndStudy_Click(object s, RoutedEventArgs e)
    {
        _timer?.Stop();
        if (!string.IsNullOrEmpty(_currentSetId)) ShowDetail(_currentSetId);
        else ShowHome();
    }

    private void QuickStudy_Click(object s, RoutedEventArgs e)
    {
        var set = _ds.Data.Sets.OrderByDescending(x => x.LastStudied).FirstOrDefault();
        if (set == null) { ShowMsg(L.CreateSetFirst); return; }
        _currentSetId = set.Id;
        NavSets.IsChecked = true;
        foreach (var nb in new[] { NavHome, NavBadges, NavStats, NavProfile }) nb.IsChecked = false;
        StartStudy(StudyMode.Flashcard);
    }
}
