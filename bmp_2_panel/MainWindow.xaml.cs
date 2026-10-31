using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace bmp_2_panel;

public partial class MainWindow : Window
{
    private string _currentMode = "";
    
    private readonly Dictionary<string, bool> _capStates = new();
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

    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnModeSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string modeTag) return;
        
        _currentMode = modeTag;
        
        TxtCurrentMode.Text = modeTag switch
        {
            "Free" => "СВОБОДНОЕ ОЗНАКОМЛЕНИЕ",
            "Normal" => "ШТАТНЫЙ ПУСК ДВИГАТЕЛЯ",
            "Pump" => "ПУСК ДВИГАТЕЛЯ С ПОДКАЧКОЙ",
            _ => "НЕИЗВЕСТНЫЙ РЕЖИМ"
        };

        MainMenuOverlay.Visibility = Visibility.Collapsed;
        TopHud.Visibility = Visibility.Visible;
    }

    private void BtnShowMenu_Click(object sender, RoutedEventArgs e)
    {
        MainMenuOverlay.Visibility = Visibility.Visible;
        TopHud.Visibility = Visibility.Collapsed;
        TacticalInfoCard.Visibility = Visibility.Collapsed;
    }

    private static DropShadowEffect CreateLampGlow(Color color, double radius = 35)
    {
        return new DropShadowEffect
        {
            Color = color,
            BlurRadius = radius,
            ShadowDepth = 0,
            Opacity = 1.0
        };
    }

    private void UpdateLampVisual(Image lamp, bool isLit, bool isGreen)
    {
        if (isGreen)
        {
            lamp.Source = isLit ? _imgGreenBright : _imgGreenOff;
            lamp.Effect = isLit ? CreateLampGlow(Color.FromRgb(40, 255, 80), 35) : null;
        }
        else
        {
            lamp.Source = isLit ? _imgRedBright : _imgRedOff;
            lamp.Effect = isLit ? CreateLampGlow(Color.FromRgb(255, 45, 25), 32) : null;
        }

        Panel.SetZIndex(lamp, isLit ? 50 : 0);
    }

    private void ShowControlInfo(string tagOrName)
    {
        if (_currentMode != "Free") return;

        var (title, subtitle, desc) = GetControlInfoText(tagOrName);
        TacticalInfoCard.SetInfo(title, subtitle, desc);
        TacticalInfoCard.Visibility = Visibility.Visible;
    }

    private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle) return;

        ShowControlInfo(toggle.Name);

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
        ShowControlInfo(btn.Name);
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
        ShowControlInfo(btnName);

        bool isOpen = _capStates.GetValueOrDefault(btnName, false);
        if (!isOpen)
        {
            _capStates[btnName] = true;
            UpdateCapVisual(btnName, _imgCapOpen);
            return;
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
        ShowControlInfo(tag);

        if (_currentMode != "Free") return;

        var rand = new Random();
        AnimateNeedlesRecursively(el, rand);
    }

    private void AnimateNeedlesRecursively(DependencyObject parent, Random rand)
    {
        if (parent is Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                if (child is Image img && img.Name != null && img.Name.StartsWith("Needle") && img.RenderTransform is RotateTransform rotate)
                {
                    double targetAngle;
                    if (img.Name == "NeedleTemperatureLeft")
                    {
                        targetAngle = rand.Next(40, 125);
                    }
                    else if (img.Name == "NeedleTemperatureRight")
                    {
                        targetAngle = rand.Next(-125, -40);
                    }
                    else if (img.Name == "NeedleAmmeter")
                    {
                        targetAngle = rand.Next(-30, 30);
                    }
                    else
                    {
                        targetAngle = rand.Next(-60, 60);
                    }

                    var anim = new DoubleAnimation
                    {
                        To = targetAngle,
                        Duration = TimeSpan.FromMilliseconds(450),
                        EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut }
                    };
                    rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
                }
                else
                {
                    AnimateNeedlesRecursively(child, rand);
                }
            }
        }
    }

    private (string Title, string Subtitle, string Description) GetControlInfoText(string id)
    {
        return id switch
        {
            "Ammeter" => ("АМПЕРМЕТР В-1", "Контроль тока", "Показывает силу зарядного или разрядного тока аккумуляторных батарей. При работающем двигателе стрелка должна находиться в плюсовой зоне."),
            "HourMeter" => ("СЧЕТЧИК МОТОЧАСОВ", "Учёт времени", "Регистрирует суммарное время работы двигателя в часах."),
            "Turnover" => ("ТАХОМЕТР ТЭ-45", "Обороты двигателя", "Указывает частоту вращения коленчатого вала в сотнях об/мин. Нормальные обороты — 15-20 (1500-2000 об/мин)."),
            "Speedometer" => ("СПИДОМЕТР", "Скорость и пробег", "Отображает текущую скорость движения машины (км/ч) и пройденный километраж."),
            "Temperature" => ("ТЕРМОМЕТР ТУЭ-48", "Контроль температуры", "Отображает температуру охлаждающей жидкости (слева) и масла двигателя (справа)."),
            "Oil" => ("МАСЛО ДВ.", "Давление масла двигателя", "Показывает давление в системе смазки двигателя. Нормальное значение — 4-5 кгс/см²."),
            "Lubricant" => ("СМАЗКА КПП", "Давление смазки", "Отображает текущее давление масла в системе смазки коробки передач."),
            
            "TogglePanelLight" => ("ОСВЕЩЕНИЕ ЩИТКА", "Тумблер", "Включает ночную подсветку шкал приборов и затемняет панель для соблюдения светомаскировки."),
            
            _ => ("ЭЛЕМЕНТ УПРАВЛЕНИЯ", "Система БМП-2", "Интерактивный элемент щитка механика-водителя.")
        };
    }

    private void Knob_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el) return;
        ShowControlInfo("Knob");

        _activeKnob = el;
        _lastKnobMousePos = e.GetPosition(this);
        el.CaptureMouse();
    }

    private void Knob_MouseMove(object sender, MouseEventArgs e)
    {
        if (_activeKnob == null) return;

        Point currentPos = e.GetPosition(this);
        double deltaX = currentPos.X - _lastKnobMousePos.X;

        if (_activeKnob.RenderTransform is RotateTransform rotate)
        {
            rotate.Angle = (rotate.Angle + deltaX * 1.5) % 360;
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