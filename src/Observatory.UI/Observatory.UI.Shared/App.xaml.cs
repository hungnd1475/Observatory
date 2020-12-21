using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.UI.Helpers;
using Observatory.Core;
using Observatory.Core.Services;
using Observatory.Providers.Exchange;
using Observatory.UI.Views;
using ReactiveUI;
using Serilog;
using Splat.Autofac;
using Splat.Microsoft.Extensions.Logging;
using Splat.Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Observatory.UI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        public IContainer Container { get; }

        public static ThemeListener ThemeListener { get; } = new ThemeListener();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            ConfigureSerilog();
            ConfigureMicrosoftLogging(Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);
            Container = ConfigureServices(Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);

            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            var profileRegistration = Container.Resolve<IProfileRegistrationService>();
            await profileRegistration.InitializeAsync();

#if NET5_0 && WINDOWS
            var window = new Window();
            window.Activate();
#else
            var window = Windows.UI.Xaml.Window.Current;

#endif
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(window.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                window.Content = rootFrame;
            }

#if !(NET5_0 && WINDOWS)
            if (e.PrelaunchActivated == false)
#endif
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                window.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// Configures global logging
        /// </summary>
        /// <param name="factory"></param>
        static void ConfigureMicrosoftLogging(ILoggerFactory factory)
        {
            factory
                .WithFilter(new FilterLoggerSettings
                    {
                        { "Uno", LogLevel.Warning },
                        { "Windows", LogLevel.Warning },
                        { DbLoggerCategory.Database.Command.Name, LogLevel.Debug },

                        // Debug JS interop
                        // { "Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug },

                        // Generic Xaml events
                        // { "Windows.UI.Xaml", LogLevel.Debug },
                        // { "Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug },
                        // { "Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug },
                        // { "Windows.UI.Xaml.UIElement", LogLevel.Debug },

                        // Layouter specific messages
                        // { "Windows.UI.Xaml.Controls", LogLevel.Debug },
                        // { "Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug },
                        // { "Windows.UI.Xaml.Controls.Panel", LogLevel.Debug },
                        // { "Windows.Storage", LogLevel.Debug },

                        // Binding related messages
                        // { "Windows.UI.Xaml.Data", LogLevel.Debug },

                        // DependencyObject memory references tracking
                        // { "ReferenceHolder", LogLevel.Debug },

                        // ListView-related messages
                        // { "Windows.UI.Xaml.Controls.ListViewBase", LogLevel.Debug },
                        // { "Windows.UI.Xaml.Controls.ListView", LogLevel.Debug },
                        // { "Windows.UI.Xaml.Controls.GridView", LogLevel.Debug },
                        // { "Windows.UI.Xaml.Controls.VirtualizingPanelLayout", LogLevel.Debug },
                        // { "Windows.UI.Xaml.Controls.NativeListViewBase", LogLevel.Debug },
                        // { "Windows.UI.Xaml.Controls.ListViewBaseSource", LogLevel.Debug }, //iOS
                        // { "Windows.UI.Xaml.Controls.ListViewBaseInternalContainer", LogLevel.Debug }, //iOS
                        // { "Windows.UI.Xaml.Controls.NativeListViewBaseAdapter", LogLevel.Debug }, //Android
                        // { "Windows.UI.Xaml.Controls.BufferViewCache", LogLevel.Debug }, //Android
                        // { "Windows.UI.Xaml.Controls.VirtualizingPanelGenerator", LogLevel.Debug }, //WASM
                    }
                )
                .AddSplat();
        }

        static void ConfigureSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File(Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs/log-.txt"),
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        static IContainer ConfigureServices(ILoggerFactory loggerFactory)
        {
            var builder = new ContainerBuilder();
#if DEBUG
            builder.RegisterModule(new CoreModule(".profiles", "."));
#else
            builder.RegisterModule(new CoreModule(
                Path.Combine(ApplicationData.Current.LocalFolder.Path, ".profiles"),
                ApplicationData.Current.LocalFolder.Path));
#endif
            builder.RegisterModule(new ExchangeModule());
            builder.RegisterModule(new UIModule());
            builder.RegisterInstance(loggerFactory)
                .As<ILoggerFactory>()
                .SingleInstance();

            var resolver = builder.UseAutofacDependencyResolver();
            builder.RegisterInstance(resolver);

            resolver.InitializeReactiveUI();
            resolver.UseSerilogFullLogger();

            var container = builder.Build();
            resolver.SetLifetimeScope(container);

            return container;
        }
    }
}
