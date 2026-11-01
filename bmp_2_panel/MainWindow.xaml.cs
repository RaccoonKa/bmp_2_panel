using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace bmp_2_panel;

public partial class MainWindow
{
    private string _currentMode = "";

    private List<TutorialStep> _currentTutorial = new List<TutorialStep>();
    private int _tutorialStepIndex;

    private readonly Dictionary<string, bool> _capStates = new Dictionary<string, bool>();
    private readonly BitmapImage _imgCapClose = new(new Uri("/bmp_2_panel;component/assets/buttons/cap_close.png", UriKind.Relative));
    private readonly BitmapImage _imgCapClose1B = new(new Uri("/bmp_2_panel;component/assets/buttons/cap_close_1b.png", UriKind.Relative));
    private readonly BitmapImage _imgCapClose2B = new(new Uri("/bmp_2_panel;component/assets/buttons/cap_close_2b.png", UriKind.Relative));
    private readonly BitmapImage _imgCapOpen = new(new Uri("/bmp_2_panel;component/assets/buttons/cap_open.png", UriKind.Relative));
    private readonly BitmapImage _imgCapPressed = new(new Uri("/bmp_2_panel;component/assets/buttons/cap_pressed.png", UriKind.Relative));

    private readonly BitmapImage _imgGreenOff = new(new Uri("/bmp_2_panel;component/assets/lights/green.png", UriKind.Relative));
    private readonly BitmapImage _imgGreenBright = new(new Uri("/bmp_2_panel;component/assets/lights/green_bright.png", UriKind.Relative));
    private readonly BitmapImage _imgRedOff = new(new Uri("/bmp_2_panel;component/assets/lights/red.png", UriKind.Relative));
    private readonly BitmapImage _imgRedBright = new(new Uri("/bmp_2_panel;component/assets/lights/red_bright.png", UriKind.Relative));

    private FrameworkElement? _activeKnob;
    private Point _lastKnobMousePos;
    private double _accumulatedRotation = 0; // Переменная для отслеживания степени "откручивания"

    private class TutorialStep
    {
        public string TargetControlName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public MainWindow()
    {
        InitializeComponent();
        AttachDynamicHandlers();

        Loaded += (s, e) => ResetPanelState();

        EventManager.RegisterClassHandler(typeof(UIElement), PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(GlobalClickFilter));
    }

    private void GlobalClickFilter(object sender, MouseButtonEventArgs e)
    {
        if (_currentMode == "Free" || _currentMode == "") return;

        if (e.OriginalSource is DependencyObject el)
        {
            var parentBtn = FindParent<Button>(el);
            if (parentBtn != null && (parentBtn.Name == "BtnAbout" || parentBtn.Name == "BtnExit" || parentBtn.Name == "BtnModeSelect" || parentBtn.Content?.ToString() == "СМЕНИТЬ РЕЖИМ" || parentBtn.Content?.ToString() == "ЗАКРЫТЬ КРЫШКУ"))
            {
                return;
            }

            var parentBorder = FindParent<Border>(el);
            if (parentBorder != null && (parentBorder.Name == "MainMenuOverlay" || parentBorder.Name == "TopHud" || parentBorder.Name == "FuseInfoOverlay"))
            {
                return;
            }

            if (_tutorialStepIndex >= _currentTutorial.Count)
            {
                e.Handled = true;
                return;
            }

            string targetName = _currentTutorial[_tutorialStepIndex].TargetControlName;

            if (targetName == "Intro")
            {
                AdvanceTutorial("Intro");
                e.Handled = true;
                return;
            }

            if (IsTargetOrChild(el, targetName))
            {
                return;
            }
        }

        e.Handled = true;
    }

    private bool IsTargetOrChild(DependencyObject child, string targetName)
    {
        DependencyObject? current = child;
        while (current != null)
        {
            if (current is FrameworkElement fe)
            {
                if (fe.Name == targetName || fe.Tag?.ToString() == targetName) return true;
                if (fe.Name != null && "Cover" + fe.Name == targetName) return true;
                if (fe.Tag != null && "Cover" + fe.Tag?.ToString() == targetName) return true;
            }

            DependencyObject parent = VisualTreeHelper.GetParent(current);
            if (parent == null && current is FrameworkElement fwElement)
            {
                parent = fwElement.Parent;
            }
            current = parent;
        }
        return false;
    }

    private T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }

    private void AttachDynamicHandlers()
    {
        foreach (UIElement child in MainCanvas.Children)
        {
            if (child is Image img && img.Name != null && img.Name.StartsWith("Lamp"))
            {
                img.Cursor = Cursors.Hand;
                img.MouseLeftButtonDown += Lamp_MouseLeftButtonDown;
            }
        }

        if (FindName("SensorHourMeter") is Image hourMeter)
        {
            hourMeter.Cursor = Cursors.Hand;
            hourMeter.Tag = "HourMeter";
            hourMeter.MouseLeftButtonDown += Sensor_MouseDown;
        }
    }

    private async void Lamp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Image lamp) return;

        ShowControlInfo(lamp, lamp.Name);

        bool isGreenLamp = true;
        if (lamp.Source != null && lamp.Source.ToString().Contains("red", StringComparison.OrdinalIgnoreCase))
        {
            isGreenLamp = false;
        }

        UpdateLampVisual(lamp, true, isGreenLamp);

        try
        {
            await Task.Delay(2000);
        }
        catch { }

        bool shouldBeOn = false;
        string toggleName = lamp.Name.Replace("LampTop", "Toggle");
        if (FindName(toggleName) is ToggleButton toggle)
        {
            shouldBeOn = toggle.IsChecked == true;
        }

        UpdateLampVisual(lamp, shouldBeOn, isGreenLamp);
    }

    private void BtnModeSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string modeTag) return;

        _currentMode = modeTag;
        _tutorialStepIndex = 0;
        _currentTutorial.Clear();
        TargetHighlighter.Visibility = Visibility.Collapsed;

        ResetPanelState();

        switch (modeTag)
        {
            case "Free":
                TxtCurrentMode.Text = "СВОБОДНОЕ ОЗНАКОМЛЕНИЕ";
                TacticalInfoCard.Visibility = Visibility.Collapsed;
                break;
            case "Normal":
                TxtCurrentMode.Text = "ШТАТНЫЙ ПУСК ДВИГАТЕЛЯ";
                LoadNormalStartTutorial();
                ShowCurrentTutorialStep();
                break;
            case "Pump":
                TxtCurrentMode.Text = "ПУСК ДВИГАТЕЛЯ С ПОДКАЧКОЙ";
                LoadPumpStartTutorial();
                ShowCurrentTutorialStep();
                break;
            case "Winter":
                TxtCurrentMode.Text = "ЗИМНИЙ ПУСК С ПОДОГРЕВОМ";
                LoadWinterStartTutorial();
                ShowCurrentTutorialStep();
                break;
        }

        MainMenuOverlay.Visibility = Visibility.Collapsed;
        TopHud.Visibility = Visibility.Visible;
    }

    private void ResetPanelState()
    {
        ToggleButton[] toggles = { ToggleL1, ToggleL2, ToggleL3, ToggleL4, ToggleL5, ToggleL6, ToggleL7, ToggleL8,
                                   ToggleR1, ToggleR2, ToggleR3, ToggleR4, ToggleR5, ToggleR6, ToggleR7, ToggleR8 };
        foreach (var t in toggles)
        {
            t.IsChecked = false;
            string lampName = t.Name.Replace("Toggle", "LampTop");
            if (FindName(lampName) is Image lamp)
            {
                UpdateLampVisual(lamp, false, true);
            }
        }

        TogglePanelLight.IsChecked = true;
        NightDimOverlay.BeginAnimation(UIElement.OpacityProperty, null);
        NightDimOverlay.Opacity = 0;
        FuseInfoOverlay.Visibility = Visibility.Collapsed;

        if (FindName("SensorHourMeterBright") is Image hm) { hm.BeginAnimation(UIElement.OpacityProperty, null); hm.Opacity = 0; }
        if (FindName("SensorAmmeterBright") is Image am) { am.BeginAnimation(UIElement.OpacityProperty, null); am.Opacity = 0; }
        if (FindName("SensorTurnoverBright") is Image to) { to.BeginAnimation(UIElement.OpacityProperty, null); to.Opacity = 0; }
        if (FindName("SensorSpeedometerBright") is Image sm) { sm.BeginAnimation(UIElement.OpacityProperty, null); sm.Opacity = 0; }
        if (FindName("SensorTemperatureBright") is Image tm) { tm.BeginAnimation(UIElement.OpacityProperty, null); tm.Opacity = 0; }
        if (FindName("SensorOilBright") is Image om) { om.BeginAnimation(UIElement.OpacityProperty, null); om.Opacity = 0; }
        if (FindName("SensorLubricantBright") is Image lm) { lm.BeginAnimation(UIElement.OpacityProperty, null); lm.Opacity = 0; }

        _capStates.Clear();
        string[] coverNames = { "CoverBtn1B", "CoverBtn2B", "CoverBtnL1", "CoverBtnL2", "CoverBtnL3", "CoverBtnR3" };
        foreach (var c in coverNames)
        {
            UpdateCapVisual(c, GetCloseImage(c));
        }

        if (NeedleTemperatureLeft.RenderTransform is RotateTransform r1) { r1.BeginAnimation(RotateTransform.AngleProperty, null); r1.Angle = 140; }
        if (NeedleTemperatureRight.RenderTransform is RotateTransform r2) { r2.BeginAnimation(RotateTransform.AngleProperty, null); r2.Angle = -140; }
        if (NeedleAmmeter.RenderTransform is RotateTransform r3) { r3.BeginAnimation(RotateTransform.AngleProperty, null); r3.Angle = -30; }
        if (NeedleTurnover.RenderTransform is RotateTransform r4) { r4.BeginAnimation(RotateTransform.AngleProperty, null); r4.Angle = -165; }
        if (NeedleSpeedometer.RenderTransform is RotateTransform r5) { r5.BeginAnimation(RotateTransform.AngleProperty, null); r5.Angle = -100; }
        if (NeedleOil.RenderTransform is RotateTransform r6) { r6.BeginAnimation(RotateTransform.AngleProperty, null); r6.Angle = -85; }
        if (NeedleLubricant.RenderTransform is RotateTransform r7) { r7.BeginAnimation(RotateTransform.AngleProperty, null); r7.Angle = -85; }
    }

    private void LoadNormalStartTutorial()
    {
        _currentTutorial = new List<TutorialStep>
        {
            new TutorialStep { TargetControlName = "Intro", Title = "ШАГ 1: ВВОДНАЯ ПОДГОТОВКА", Subtitle = "Внешние системы машины", Description = "Убедитесь, что рычаг КП в нейтрали, открыт топливный кран и выжата педаль главного фрикциона.\n\n(Кликните в любом месте экрана для продолжения)" },
            new TutorialStep { TargetControlName = "ToggleR8", Title = "ШАГ 2: ВЫКЛЮЧАТЕЛЬ БАТАРЕЙ", Subtitle = "Включение бортовой сети", Description = "Включите аккумуляторные батареи. Амперметр должен показать наличие напряжения." },
            new TutorialStep { TargetControlName = "CoverBtnL3", Title = "ШАГ 3: ПРОКАЧКА СИСТЕМЫ", Subtitle = "Маслозакачивающий насос", Description = "Откройте крышку и нажмите кнопку МЗН для прокачки системы смазки. Держите до создания давления 2-2.5 кгс/см²." },
            new TutorialStep { TargetControlName = "BtnCapR2", Title = "ШАГ 4: ОСНОВНОЙ ПУСК", Subtitle = "Пуск воздухом", Description = "Нажмите кнопку пуска воздухом. Это основной способ пуска БМП-2, сберегающий батареи." },
            new TutorialStep { TargetControlName = "BtnCapR1", Title = "ШАГ 5: КОМБИНИРОВАННЫЙ ПУСК", Subtitle = "Стартер", Description = "Дополнительно нажмите стартер для уверенного запуска. Двигатель выйдет на обороты холостого хода." }
        };
    }

    private void LoadPumpStartTutorial()
    {
        _currentTutorial = new List<TutorialStep>
        {
            new TutorialStep { TargetControlName = "Intro", Title = "ШАГ 1: ВВОДНАЯ ПОДГОТОВКА", Subtitle = "Внешние системы машины", Description = "Убедитесь, что рычаг КП в нейтрали, открыт топливный кран и выжата педаль главного фрикциона.\n\n(Кликните в любом месте экрана для продолжения)" },
            new TutorialStep { TargetControlName = "ToggleR8", Title = "ШАГ 2: ВЫКЛЮЧАТЕЛЬ БАТАРЕЙ", Subtitle = "Включение бортовой сети", Description = "Переведите тумблер вверх для подключения батарей." },
            new TutorialStep { TargetControlName = "ToggleL4", Title = "ШАГ 3: ПОДКАЧКА ТОПЛИВА", Subtitle = "Включение БЦН", Description = "Включите насос БЦН на 10-15 сек для удаления воздушных пробок в топливной системе." },
            new TutorialStep { TargetControlName = "CoverBtnL3", Title = "ШАГ 4: МАСЛОЗАКАЧИВАЮЩИЙ НАСОС", Subtitle = "Прокачка маслом", Description = "Создайте начальное давление масла в системе дизеля перед пуском." },
            new TutorialStep { TargetControlName = "BtnCapR1", Title = "ШАГ 5: ДУБЛИРУЮЩИЙ ПУСК", Subtitle = "Пуск стартером", Description = "Запустите двигатель электростартером С5-2С." },
            new TutorialStep { TargetControlName = "ToggleL4", Title = "ШАГ 6: ОТКЛЮЧЕНИЕ БЦН", Subtitle = "Переход в штатный режим", Description = "После устойчивой работы двигателя выключите топливный насос БЦН." }
        };
    }

    private void LoadWinterStartTutorial()
    {
        _currentTutorial = new List<TutorialStep>
        {
            new TutorialStep { TargetControlName = "Intro", Title = "ШАГ 1: ВВОДНАЯ ПОДГОТОВКА", Subtitle = "Внешние системы машины", Description = "Убедитесь, что рычаг КП в нейтрали, открыт топливный кран и выжата педаль главного фрикциона.\n\n(Кликните в любом месте экрана для продолжения)" },
            new TutorialStep { TargetControlName = "ToggleR8", Title = "ШАГ 2: БОРТОВАЯ СЕТЬ", Subtitle = "Включение батарей", Description = "Включите питание перед запуском подогревателя." },
            new TutorialStep { TargetControlName = "ToggleR5", Title = "ШАГ 3: СВЕЧА ПОДОГРЕВАТЕЛЯ", Subtitle = "Накал свечи", Description = "Подайте питание на свечу для воспламенения топлива в котле." },
            new TutorialStep { TargetControlName = "ToggleR6", Title = "ШАГ 4: ОБОГРЕВ ДВИГАТЕЛЯ", Subtitle = "Запуск котла", Description = "Включите насосный узел котла-подогревателя. Ожидайте разогрева ОЖ до +30°C." },
            new TutorialStep { TargetControlName = "ToggleR5", Title = "ШАГ 5: ОТКЛЮЧЕНИЕ СВЕЧИ", Subtitle = "Прекращение накала", Description = "После воспламенения топлива выключите свечу." },
            new TutorialStep { TargetControlName = "ToggleR6", Title = "ШАГ 6: ОТКЛЮЧЕНИЕ ОБОГРЕВА", Subtitle = "Двигатель прогрет", Description = "Выключите подогреватель после достижения рабочей температуры." },
            new TutorialStep { TargetControlName = "CoverBtnL3", Title = "ШАГ 7: ПРОКАЧКА МАСЛОМ", Subtitle = "Подготовка к пуску", Description = "Прокачайте систему смазки перед запуском." },
            new TutorialStep { TargetControlName = "BtnCapR2", Title = "ШАГ 8: ПУСК ВОЗДУХОМ", Subtitle = "Пневмопуск", Description = "Нажмите пуск воздухом (первая фаза комбинированного пуска)." },
            new TutorialStep { TargetControlName = "BtnCapR1", Title = "ШАГ 9: КОМБИНИРОВАННЫЙ ПУСК", Subtitle = "Стартер", Description = "Нажмите кнопку стартера в дополнение к воздуху. После выхода на 600-800 об/мин двигатель запущен." }
        };
    }

    private void ShowCurrentTutorialStep()
    {
        if (_tutorialStepIndex >= _currentTutorial.Count)
        {
            TacticalInfoCard.SetInfo("СЦЕНАРИЙ ЗАВЕРШЕН", "Двигатель успешно запущен", "Выполнены все действия согласно инструкции по эксплуатации БМП-2.");
            TacticalInfoCard.Visibility = Visibility.Visible;
            Canvas.SetLeft(TacticalInfoCard, 1920 / 2.0 - 220);
            Canvas.SetTop(TacticalInfoCard, 100);
            TargetHighlighter.Visibility = Visibility.Collapsed;
            
            StartEngineAnimation();
            return;
        }

        var step = _currentTutorial[_tutorialStepIndex];
        TacticalInfoCard.SetInfo(step.Title, step.Subtitle, step.Description);

        if (step.TargetControlName == "Intro")
        {
            TargetHighlighter.Visibility = Visibility.Collapsed;
            Canvas.SetLeft(TacticalInfoCard, 1920 / 2.0 - 220);
            Canvas.SetTop(TacticalInfoCard, 200);
        }
        else
        {
            FrameworkElement? targetEl = FindName(step.TargetControlName) as FrameworkElement 
                                         ?? FindName("Cover" + step.TargetControlName.Replace("Cover", "")) as FrameworkElement;

            if (targetEl != null)
            {
                PositionInfoCardNear(targetEl);
                PositionHighlighter(targetEl);
            }
        }

        TacticalInfoCard.Visibility = Visibility.Visible;
    }

    private void StartEngineAnimation()
    {
        AnimateNeedleRunning(NeedleTurnover, -60, -25, 1.5); 
        AnimateNeedleRunning(NeedleOil, -35, 10, 1.0);       
        AnimateNeedleRunning(NeedleLubricant, -40, 5, 1.0);  
        AnimateNeedleRunning(NeedleAmmeter, -20, 15, 0.8);   
        AnimateNeedleRunning(NeedleTemperatureLeft, 70, 45, 0.5); 
        AnimateNeedleRunning(NeedleTemperatureRight, -70, -45, 0.5); 
        AnimateNeedleRunning(NeedleSpeedometer, -100, -45, 0.3); 
    }

    private void AnimateNeedleRunning(Image needle, double fromAngle, double toAngle, double jitterAmp)
    {
        if (needle.RenderTransform is not RotateTransform rotate) return;

        DoubleAnimation startupAnim = new DoubleAnimation
        {
            From = fromAngle,
            To = toAngle,
            Duration = TimeSpan.FromMilliseconds(2000),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        startupAnim.Completed += (s, e) =>
        {
            if (_currentMode != "" && _currentMode != "Free")
            {
                DoubleAnimation jitter = new DoubleAnimation
                {
                    From = toAngle - jitterAmp,
                    To = toAngle + jitterAmp,
                    Duration = TimeSpan.FromMilliseconds(70),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                rotate.BeginAnimation(RotateTransform.AngleProperty, jitter);
            }
        };

        rotate.BeginAnimation(RotateTransform.AngleProperty, startupAnim);
    }

    private void PositionHighlighter(FrameworkElement target)
    {
        Point pos = target.TranslatePoint(new Point(0, 0), MainCanvas);
        
        double width = target.ActualWidth > 0 ? target.ActualWidth : (double.IsNaN(target.Width) ? 60 : target.Width);
        double height = target.ActualHeight > 0 ? target.ActualHeight : (double.IsNaN(target.Height) ? 60 : target.Height);

        TargetHighlighter.Width = width + 12;
        TargetHighlighter.Height = height + 12;
        Canvas.SetLeft(TargetHighlighter, pos.X - 6);
        Canvas.SetTop(TargetHighlighter, pos.Y - 6);
        
        TargetHighlighter.Visibility = Visibility.Visible;
    }

    private void AdvanceTutorial(string actionedControlName)
    {
        if (_currentMode == "Free" || _tutorialStepIndex >= _currentTutorial.Count) return;

        if (_currentTutorial[_tutorialStepIndex].TargetControlName == actionedControlName ||
            ("Cover" + actionedControlName) == _currentTutorial[_tutorialStepIndex].TargetControlName ||
            actionedControlName.Replace("Cover", "") == _currentTutorial[_tutorialStepIndex].TargetControlName)
        {
            _tutorialStepIndex++;
            ShowCurrentTutorialStep();
        }
    }

    private void BtnShowMenu_Click(object sender, RoutedEventArgs e)
    {
        _currentMode = "";
        TargetHighlighter.Visibility = Visibility.Collapsed;
        MainMenuOverlay.Visibility = Visibility.Visible;
        TopHud.Visibility = Visibility.Collapsed;
        TacticalInfoCard.Visibility = Visibility.Collapsed;
    }

    private static DropShadowEffect CreateLampGlow(Color color)
    {
        return new DropShadowEffect
        {
            Color = color,
            BlurRadius = 35,
            ShadowDepth = 0,
            Opacity = 1.0
        };
    }

    private void UpdateLampVisual(Image lamp, bool isLit, bool isGreen)
    {
        if (isGreen)
        {
            lamp.Source = isLit ? _imgGreenBright : _imgGreenOff;
            lamp.Effect = isLit ? CreateLampGlow(Color.FromRgb(40, 255, 80)) : null;
        }
        else
        {
            lamp.Source = isLit ? _imgRedBright : _imgRedOff;
            lamp.Effect = isLit ? CreateLampGlow(Color.FromRgb(255, 45, 25)) : null;
        }

        Panel.SetZIndex(lamp, isLit ? 50 : 0);
    }

    private void ShowControlInfo(FrameworkElement? targetElement, string tagOrName)
    {
        if (_currentMode != "Free") return;

        var (title, subtitle, desc) = GetControlInfoText(tagOrName);
        TacticalInfoCard.SetInfo(title, subtitle, desc);

        if (targetElement != null)
        {
            PositionInfoCardNear(targetElement);
        }

        TacticalInfoCard.Visibility = Visibility.Visible;
    }

    private void PositionInfoCardNear(FrameworkElement target)
    {
        Point pos = target.TranslatePoint(new Point(0, 0), MainCanvas);

        double targetWidth = target.ActualWidth > 0 ? target.ActualWidth : (double.IsNaN(target.Width) ? 60 : target.Width);
        double targetHeight = target.ActualHeight > 0 ? target.ActualHeight : (double.IsNaN(target.Height) ? 60 : target.Height);

        const double cardWidth = 440;
        const double cardHeight = 220;
        const double spacing = 20;

        double cardX;
        if ((pos.X + targetWidth + spacing + cardWidth) <= 1900)
        {
            cardX = pos.X + targetWidth + spacing;
        }
        else if ((pos.X - spacing - cardWidth) >= 20)
        {
            cardX = pos.X - spacing - cardWidth;
        }
        else
        {
            double spaceRight = 1920 - (pos.X + targetWidth);
            double spaceLeft = pos.X;
            cardX = spaceRight >= spaceLeft ? (pos.X + targetWidth + spacing) : (pos.X - cardWidth - spacing);
        }

        cardX = Math.Clamp(cardX, 20, 1920 - cardWidth - 20);

        double cardY = pos.Y + (targetHeight / 2.0) - (cardHeight / 2.0);
        cardY = Math.Clamp(cardY, 30, 1080 - cardHeight - 30);

        Canvas.SetLeft(TacticalInfoCard, cardX);
        Canvas.SetTop(TacticalInfoCard, cardY);
    }

    private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle) return;

        if (_currentMode == "Free")
        {
            ShowControlInfo(toggle, toggle.Name);
        }
        else
        {
            AdvanceTutorial(toggle.Name);
        }

        if (toggle.Name == "TogglePanelLight")
        {
            bool isNight = toggle.IsChecked == false;

            var dimAnim = new DoubleAnimation
            {
                To = isNight ? 0.82 : 0.0,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            NightDimOverlay.BeginAnimation(UIElement.OpacityProperty, dimAnim);

            var dialAnim = new DoubleAnimation
            {
                To = isNight ? 1.0 : 0.0,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            if (FindName("SensorHourMeterBright") is Image hm) hm.BeginAnimation(UIElement.OpacityProperty, dialAnim);
            if (FindName("SensorAmmeterBright") is Image am) am.BeginAnimation(UIElement.OpacityProperty, dialAnim);
            if (FindName("SensorTurnoverBright") is Image to) to.BeginAnimation(UIElement.OpacityProperty, dialAnim);
            if (FindName("SensorSpeedometerBright") is Image sm) sm.BeginAnimation(UIElement.OpacityProperty, dialAnim);
            if (FindName("SensorTemperatureBright") is Image tm) tm.BeginAnimation(UIElement.OpacityProperty, dialAnim);
            if (FindName("SensorOilBright") is Image om) om.BeginAnimation(UIElement.OpacityProperty, dialAnim);
            if (FindName("SensorLubricantBright") is Image lm) lm.BeginAnimation(UIElement.OpacityProperty, dialAnim);
            return;
        }

        string lampName = toggle.Name.Replace("Toggle", "LampTop");
        if (FindName(lampName) is Image lamp)
        {
            UpdateLampVisual(lamp, toggle.IsChecked == true, isGreen: true);
        }
    }

    private void PushButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        if (_currentMode == "Free")
        {
            ShowControlInfo(btn, btn.Name);
        }
        else
        {
            AdvanceTutorial(btn.Name);
        }
    }

    private BitmapImage GetCloseImage(string btnName)
    {
        return btnName switch
        {
            "CoverBtn1B" => _imgCapClose1B,
            "CoverBtn2B" => _imgCapClose2B,
            _ => _imgCapClose
        };
    }

    private void CapArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not string btnName) return;

        bool isOpen = _capStates.GetValueOrDefault(btnName, false);
        isOpen = !isOpen;
        _capStates[btnName] = isOpen;

        UpdateCapVisual(btnName, isOpen ? _imgCapOpen : GetCloseImage(btnName));
    }

    private void ButtonArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not string btnName) return;

        bool isOpen = _capStates.GetValueOrDefault(btnName, false);
        if (!isOpen)
        {
            _capStates[btnName] = true;
            UpdateCapVisual(btnName, _imgCapOpen);
            return; 
        }

        if (_currentMode == "Free")
        {
            ShowControlInfo(el, btnName);
        }
        else
        {
            AdvanceTutorial(btnName);
        }

        el.CaptureMouse();
        UpdateCapVisual(btnName, _imgCapPressed);
    }

    private void ButtonArea_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not string btnName) return;

        if (el.IsMouseCaptured)
        {
            el.ReleaseMouseCapture();
        }

        bool isOpen = _capStates.GetValueOrDefault(btnName, false);
        if (isOpen)
        {
            UpdateCapVisual(btnName, _imgCapOpen);
        }
    }

    private void ButtonArea_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not string btnName) return;

        if (el.IsMouseCaptured)
        {
            el.ReleaseMouseCapture();
        }

        bool isOpen = _capStates.GetValueOrDefault(btnName, false);
        if (isOpen)
        {
            UpdateCapVisual(btnName, _imgCapOpen);
        }
    }

    private void UpdateCapVisual(string btnName, BitmapImage image)
    {
        if (FindName("Img" + btnName) is Image img1)
        {
            img1.Source = image;
        }
        else if (FindName("Img_" + btnName) is Image img2)
        {
            img2.Source = image;
        }
    }

    private void Sensor_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not string tag) return;

        if (_currentMode == "Free")
        {
            ShowControlInfo(el, tag);
            var rand = new Random();
            AnimateNeedlesRecursively(el, rand);
        }
    }

    private void AnimateNeedlesRecursively(DependencyObject parent, Random rand)
    {
        if (parent is not Panel panel) return;

        foreach (UIElement child in panel.Children)
        {
            if (child is Image { Name: not null } img && img.Name.StartsWith("Needle") && img.RenderTransform is RotateTransform rotate)
            {
                double baseAngle = img.Name switch
                {
                    "NeedleTemperatureLeft" => 70,
                    "NeedleTemperatureRight" => -70,
                    "NeedleAmmeter" => -20,
                    "NeedleTurnover" => -60,
                    "NeedleSpeedometer" => -100,
                    "NeedleOil" => -35,
                    "NeedleLubricant" => -40,
                    _ => 0
                };

                double targetAngle = img.Name switch
                {
                    "NeedleTemperatureLeft" => rand.Next(80, 125),
                    "NeedleTemperatureRight" => rand.Next(-125, -80),
                    "NeedleAmmeter" => rand.Next(10, 30),
                    "NeedleTurnover" => rand.Next(-20, 20),
                    "NeedleSpeedometer" => rand.Next(-45, 90),
                    "NeedleOil" => rand.Next(0, 25),
                    "NeedleLubricant" => rand.Next(0, 25),
                    _ => rand.Next(-60, 60)
                };

                var anim = new DoubleAnimation
                {
                    From = baseAngle,
                    To = targetAngle,
                    Duration = TimeSpan.FromMilliseconds(1000),
                    AutoReverse = true,
                    EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 5, EasingMode = EasingMode.EaseOut }
                };
                rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
            }
            else
            {
                AnimateNeedlesRecursively(child, rand);
            }
        }
    }

    private static (string Title, string Subtitle, string Description) GetControlInfoText(string id)
    {
        return id switch
        {
            "Ammeter" => ("АМПЕРМЕТР В-1", "Контроль тока", "Показывает силу зарядного (в плюсовой зоне) или разрядного (в минусовой зоне) тока аккумуляторных батарей. При работающем двигателе стрелка должна быть в плюсовой зоне."),
            "HourMeter" => ("СЧЕТЧИК МОТОЧАСОВ", "Учёт времени", "Регистрирует суммарное время работы силовой установки (дизеля УТД-20С1) в часах."),
            "Turnover" => ("ТАХОМЕТР ТЭ-48", "Обороты двигателя", "Указывает частоту вращения коленчатого вала в сотнях об/мин. Максимальные рабочие обороты двигателя БМП-2 — 2600 об/мин."),
            "Speedometer" => ("СПИДОМЕТР", "Скорость и пробег", "Отображает текущую скорость движения машины (км/ч) и пройденный километраж. Максимальная скорость по шоссе — 65 км/ч."),
            "Temperature" => ("ТЕРМОМЕТР ТУЭ-48", "Контроль температуры", "Двойной указатель: отображает температуру охлаждающей жидкости (слева) и масла двигателя (справа)."),
            "Oil" => ("МАСЛО ДВ.", "Давление масла двигателя", "Показывает давление в системе смазки дизеля. Нормальное эксплуатационное значение — 4-5 кгс/см²."),
            "Lubricant" => ("СМАЗКА КПП", "Давление смазки", "Отображает теку давление масла в системе смазки силовой передачи (коробки передач)."),

            "ToggleL1" => ("ВЫХОД ЗА ГАБАРИТ", "Сигнализация", "Включение системы звуковой и световой сигнализации при выходе вооружения за безопасные габариты машины."),
            "ToggleL2" => ("ТДА", "Термодымовая аппаратура", "Включение насоса ТДА многократного действия для впрыска топлива в выпускной коллектор и постановки плотной дымовой завесы."),
            "ToggleL3" => ("ГПК", "Гирополукомпас", "Включение питания электрического гирополукомпаса (ГПК-59) для ориентирования на местности и выдерживания курса."),
            "ToggleL4" => ("БЦН", "Топливный насос", "Включение топливного центробежного насоса для подкачки дизельного топлива перед пуском двигателя."),
            "ToggleL5" => ("ОХЛАЖД. ДВИГАТ.", "Система охлаждения", "Управление режимами работы вентилятора системы жидкостного высокотемпературного охлаждения двигателя."),
            "ToggleL6" => ("ОТКАЧКА ВОДЫ НОС", "Водооткачивающий насос", "Включение центробежного носового водооткачивающего насоса (подача от 100 л/мин) для удаления воды при движении на плаву."),
            "ToggleL7" => ("ОТКАЧКА ВОДЫ КОРМА", "Водооткачивающий насос", "Включение кормового центробежного насоса для откачки воды из десантного и силового отделений машины."),
            "ToggleL8" => ("ПАЗ", "Противоатомная защита", "Включение системы защиты экипажа от оружия массового поражения (ОМП). Герметизирует корпус и включает ФВУ."),

            "ToggleR1" => ("ФАРА ОНВ", "Наружное освещение", "Включение фары (ФГ-125) инфракрасного/белого света для работы прибора ночного видения ТВНЕ-1ПА механика-водителя."),
            "ToggleR2" => ("ФАРА ТФ", "Танковая фара", "Включение основной фары со светомаскировочным устройством (ФГ-127) для освещения пути при ночных маршах."),
            "ToggleR3" => ("НАГНЕТАТЕЛЬ", "Система ФВУ", "Включение нагнетателя-сепаратора (ВНСЦ-200) для очистки воздуха от пыли, радиоактивных веществ и создания избыточного давления."),
            "ToggleR4" => ("ГАБАРИТ К.Д.С.", "Дорожная сигнализация", "Включение передних и кормовых габаритных фонарей, а также системы коробки дорожной сигнализации (КДС1-2С)."),
            "ToggleR5" => ("СВЕЧА ПОДОГРЕВАТЕЛЯ", "Система пуска", "Включение запальной свечи форсуночного подогревателя для воспламенения топлива при разогреве двигателя зимой."),
            "ToggleR6" => ("ОБОГРЕВ ДВИГАТ.", "Подогреватель", "Включение насосного узла котла-подогревателя для принудительной циркуляции разогретой охлаждающей жидкости и масла."),
            "ToggleR7" => ("ПЛАВ", "Водоотражательный щиток", "Управление пневмоцилиндрами для подъема переднего водоотражательного щитка перед преодолением водных преград."),
            "ToggleR8" => ("ВЫКЛ. БАТАР.", "Выключатель батарей", "Подключение двух свинцовых стартерных аккумуляторных батарей (6СТЭН-140М) к бортовой электросети машины."),

            "CoverBtnL1" => ("КЛАПАН ПАЗ", "Герметизация корпуса", "Ручное или аварийное управление клапанами системы противоатомной защиты (ПАЗ) для герметизации обитаемых отделений."),
            "CoverBtnL2" => ("РИОВ (ПРХР)", "Химическая разведка", "Включение прибора радиационной и химической разведки (ПРХР). Выдает команды на автоматическое срабатывание системы ПАЗ."),
            "CoverBtnL3" => ("НАСОС (МЗН)", "Маслозакачивающий насос", "Включение электромотора маслозакачивающего насоса (МЗН) для создания начального давления масла перед пуском дизеля."),

            "CoverBtn1B" => ("ППО - БАЛЛОН 1", "Пожарное оборудование", "Ручной пуск первого баллона автоматической системы ППО с огнегасящим составом (Хладон 114В2) при пожаре."),
            "CoverBtn2B" => ("ППО - БАЛЛОН 2", "Пожарное оборудование", "Ручной пуск второго (резервного) баллона автоматической системы противопожарного оборудования."),

            "BtnRed" => ("ВЫКЛ. ОХЛ. ДВИГАТ.", "Аварийное отключение", "Принудительное отключение вентилятора системы охлаждения (закрытие жалюзи) перед входом машины в воду."),
            "BtnCapR1" => ("СТАРТЕР", "Пуск двигателя", "Включение электростартера (С5-2С мощностью 11 кВт) для прокрутки коленчатого вала при основном пуске двигателя."),
            "BtnCapR2" => ("ПУСК ВОЗДУХОМ", "Пневмопуск", "Подача сжатого воздуха высокого давления из баллонов в цилиндры двигателя для его пневматического (дублирующего) запуска."),
            "CoverBtnR3" => ("УПРАВЛЕНИЕ ЖАЛЮЗИ", "Система охлаждения", "Ручное управление створками жалюзи и эжектором для регулировки температурного режима силовой установки."),

            "TogglePanelLight" => ("ОСВЕЩЕНИЕ ЩИТКА", "Тумблер светомаскировки", "Включает ночную подсветку шкал контрольно-измерительных приборов и затемняет общую панель."),
            "BtnDust" => ("КЛАПАН ОТСОСА ПЫЛИ", "Воздухоочиститель", "Управление электропневматическим клапаном эжекционного автоматического отсоса пыли из бескассетного воздухоочистителя."),
            "BtnMidR" => ("ПНЕВМООБОРУДОВАНИЕ", "Управление клапанами", "Управление воздушными магистралями и автоматом давления (АДУ-2С) бортовой пневмосистемы."),
            "Knob" => ("АЗС", "Автомат защиты сети", "Автомат защиты сети (предохранитель на 5А или 10А) для предотвращения коротких замыканий и перегрузок в электроцепях машины."),

            var s when s.StartsWith("LampTopL") && s.Length > 8 => ("СИГНАЛЬНАЯ ЛАМПА", "Индикация включения", "Подтверждает подачу питания на соответствующую систему левого борта (БЦН, ТДА, ПАЗ и др.)."),
            var s when s.StartsWith("LampTopR") && s.Length > 8 => ("СИГНАЛЬНАЯ ЛАМПА", "Индикация включения", "Подтверждает подачу питания на соответствующую систему правого борта (фары, нагнетатель, подогрев и др.)."),
            "LampTopL" => ("АВАРИЙНАЯ ЛАМПА", "Внимание", "Красная контрольная лампа. Загорается при возникновении критических ошибок или выходе вооружения за габарит."),
            "LampTopR" => ("АВАРИЙНАЯ ЛАМПА", "Внимание", "Красная контрольная лампа аварийного режима работы оборудования."),
            "LampCenterLeftRed" => ("ПОЖАР", "Система ППО", "Красная лампа. Загорается при возникновении пожара в силовом отделении, выдавая сигнал на применение ППО."),
            "LampCenterRightGreen" => ("ППО ГОТОВ", "Контроль цепей ППО", "Зеленая лампа. Сигнализирует о целостности электрических цепей и готовности противопожарного оборудования."),
            "LampLeftDoorRed" => ("ДВЕРЬ ОТКРЫТА", "Контроль люков", "Загорается при неплотно закрытой левой или правой кормовой двери десантного отделения машины."),
            "LampValveFvu" => ("КЛАПАН ФПТ", "Система ПАЗ", "Сигнализирует об открытом положении клапана фильтра-поглотителя ФПТ-200М системы противоатомной защиты."),

            _ => ("ЭЛЕМЕНТ УПРАВЛЕНИЯ", "Оборудование БМП-2", "Интерактивный элемент щитка механика-водителя.")
        };
    }

    private void Knob_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el) return;
        ShowControlInfo(el, "Knob");

        _activeKnob = el;
        _lastKnobMousePos = e.GetPosition(this);
        _accumulatedRotation = 0; // Сбрасываем счетчик при новом клике
        el.CaptureMouse();
    }

    private void Knob_MouseMove(object sender, MouseEventArgs e)
    {
        if (_activeKnob == null) return;

        Point currentPos = e.GetPosition(this);
        double deltaX = currentPos.X - _lastKnobMousePos.X;

        if (_activeKnob.RenderTransform is RotateTransform rotate)
        {
            double angleDelta = deltaX * 1.5;
            rotate.Angle = (rotate.Angle + angleDelta) % 360;
            _accumulatedRotation += Math.Abs(angleDelta); // Считаем общий угол поворота

            // Если прокрутили крышку суммарно на 300 условных градусов (открутили)
            if (_accumulatedRotation > 300)
            {
                if (_activeKnob.IsMouseCaptured)
                {
                    _activeKnob.ReleaseMouseCapture();
                }
                
                _activeKnob = null;
                _accumulatedRotation = 0;
                
                // Показываем окно с предохранителем
                FuseInfoOverlay.Visibility = Visibility.Visible;
                return;
            }
        }

        _lastKnobMousePos = currentPos;
    }

    private void Knob_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_activeKnob == null) return;

        if (_activeKnob.IsMouseCaptured)
        {
            _activeKnob.ReleaseMouseCapture();
        }

        _activeKnob = null;
    }

    // Обработчик кнопки закрытия окна предохранителя
    private void BtnCloseFuse_Click(object sender, RoutedEventArgs e)
    {
        FuseInfoOverlay.Visibility = Visibility.Collapsed;
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };
        aboutWindow.ShowDialog();
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}