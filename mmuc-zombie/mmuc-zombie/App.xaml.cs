﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Parse;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;
using mmuc_zombie.app.model;
using mmuc_zombie.app.helper;

namespace mmuc_zombie
{
    public partial class App : Application
    {
        public static Microsoft.Phone.UserData.Contact con;
        private static Device deviceID;
        private static string m_strAccessToken; // FB        
        //private static User user;
        private static HttpWebRequest _webRequest;
        private static CookieContainer _cookieContainer = new CookieContainer();   
        //private static Games currentGame;

        private class QuitException : Exception { }

        public static void Quit()
        {
            throw new QuitException();
        }

        public static Device DeviceID 
        {
            get { return deviceID; }
            set { deviceID = value; }
        }

        public static string AccessToken //FB
        {
            get { return m_strAccessToken; }
            set { m_strAccessToken = value; }
        }

        //public static User User
        //{
        //    get { return user; }
        //    set { user = value; }
        //}

        public static HttpWebRequest WebRequest
        {
            get { return _webRequest; }
            set { _webRequest = value; }
        }

        public static CookieContainer CookiesContainer
        {
            get { return _cookieContainer; }
            set { _cookieContainer = value; }
        }

        //public static Games CurrentGame
        //{
        //    get { return currentGame; }
        //    set { currentGame = value; }
        //}
       
        //IsolatedtStorage for file saving
        private IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();

        private PhoneApplicationService service = PhoneApplicationService.Current;

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            deviceID = new Device();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            Application_Launched_Activated(sender);
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            Application_Launched_Activated(sender);   
        }

        //user retrieval
        private void Application_Launched_Activated(object sender)
        {
            Progressbar.InitGLobalProgressBar();
            PhoneApplicationService service = PhoneApplicationService.Current;
            service.State["user"] = "-1";
            ParseConfiguration.Configure("w8I4cwfDTXeMzvPPSzkAiinbnkMWijhZkZ7Jnxwd", "BbL0rdiCCzC2yE0fdtm7da6nKEXdBt2EXDTHEvVT");
            if (!store.FileExists("userId.txt"))
            {
                using (var file = store.CreateFile("userId.txt"))
                {
                    User user = new User();
                    user.UserName = "Dizzle";
                    user.DeviceID = App.DeviceID.toString(); 
                    user.create(new StartupListener());                                       
                }
            }
            else
            {
                using (var reader = new StreamReader(store.OpenFile("userId.txt",FileMode.Open)))
                {
                    string userId = reader.ReadToEnd();                   
                    Debug.WriteLine("Reading userId" + userId);
                    User.find(userId,new LoginListener());
                }
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is QuitException)
                return;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
                
    }

    
}