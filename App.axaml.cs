using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaWindowBug.ViewModels;
using AvaloniaWindowBug.Views;
using NotificationIconSharp;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AvaloniaWindowBug
{
    public class App : Application
    {
        private const bool useFix = false;
        private const bool useContextMenu = false;

        private MainWindow? _mainWindow = null;
        private NotificationIcon? _notificationIcon = null;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            CreateMainWindow();

            base.OnFrameworkInitializationCompleted();

            if (_notificationIcon == null)
            {
                _notificationIcon = new NotificationIcon("Assets/tray_logo.ico");
                _notificationIcon.NotificationIconSelected += OnNotificationIconSelected;
            }
        }

        private void OnNotificationIconSelected(NotificationIcon icon)
        {
            if(useContextMenu)
            {
                if (icon.MenuItems.Count > 0)
                {
                    return;
                }

                var openItem = new NotificationMenuItem("Show AvaloniaWindowBug");
                openItem.NotificationMenuItemSelected += OnOpenMainWindow;

                var exitItem = new NotificationMenuItem("Quit AvaloniaWindowBug");
                exitItem.NotificationMenuItemSelected += ExitApp;

                icon.AddMenuItem(openItem);
                icon.AddMenuItem(exitItem);
            }
            else
            {
                OpenMainWindow();
            }
        }

        private void CreateMainWindow()
        {
            IClassicDesktopStyleApplicationLifetime? desktopApplicationLifetime = Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                _mainWindow.Closing += MainWindow_Closing;
            }
            if (desktopApplicationLifetime?.MainWindow != _mainWindow)
            {
                if (desktopApplicationLifetime == null)
                {
                    throw new NullReferenceException();
                }
                desktopApplicationLifetime.MainWindow = _mainWindow;
            }
        }

        private void OnOpenMainWindow(NotificationMenuItem menuItem)
        {
            OpenMainWindow();
        }

        private void OpenMainWindow()
        {
            Dispatcher.UIThread.Post(() =>
            {
                RestoreMainWindow();
            });
        }

        private void RestoreMainWindow()
        {
            CreateMainWindow();

            if(_mainWindow == null)
            {
                throw new NullReferenceException();
            }

            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;

            if (!useFix || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _mainWindow.Activate();
            }
            else
            {
                var handle = ((Avalonia.Win32.WindowImpl)((TopLevel)_mainWindow.GetVisualRoot()).PlatformImpl)?.Handle?.Handle ?? IntPtr.Zero;
                if (!SetForegroundWindow(handle))
                {
                    _mainWindow.Activate();
                }
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            _mainWindow?.Hide();
            _mainWindow = null;
        }

        private void ExitApp(NotificationMenuItem menuItem)
        {
            (Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown(0);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
