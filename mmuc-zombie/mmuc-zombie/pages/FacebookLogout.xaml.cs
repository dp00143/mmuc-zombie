﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using mmuc_zombie.app.helper;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace mmuc_zombie.pages
{
    public partial class FacebookLogout : PhoneApplicationPage
    {
        private bool m_bDidAppLogout;

        public FacebookLogout()
        {
            InitializeComponent();
        }

        private void wbLogout_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            string strLoweredAddress = e.Uri.OriginalString.ToLower();
            if (strLoweredAddress.Contains("facebook.com/home.php"))
            {	//could be www.fa... or m.fa...
                if (!m_bDidAppLogout)
                {	//this was app logout - now logout the user itself
                    txtStatus.Text = "Application logged out";
                    txtError.Text = "Logging out the user";
                    m_bDidAppLogout = true;
                    //the trick is to use the same host (m or www) and the same parameters
                    //so simply replace home (where we are) with logout
                    //same host - same parameters - but logout page :)
                    string strLogout = strLoweredAddress.Replace("home", "logout");
                    wbLogout.Navigate(new Uri(strLogout, UriKind.Absolute));
                    return;
                }
                else
                {	//again (after navigation to logout) at home page
                    txtStatus.Text = "Could not log out user";
                    txtError.Text = "Please scroll down to the bottom of the page and click logout";
                }
            }
            //if we are not at "home.php" and tried a logout call before assume we are logged out
            else if (m_bDidAppLogout)
            {
                txtStatus.Text = "Logged out";
                txtError.Text = "OK";
                App.AccessToken = "";
                User.getFromState().Facebook = "";
                User.getFromState().FacebookToken = "";                
                return;
            }
        }


        //private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        //{
        //    wbLogout.Navigate(new Uri("http://m.facebook.com/logout.php?confirm=1"));
        //    //wbLogout.Navigate(FacebookURIs.GetLogoutUri(App.AccessToken));            
        //}

        //private void wbLogout_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        //{
        //    string fbLogoutDoc = wbLogout.SaveToString();
        //    Regex regex = new Regex("\\<A href=\\\"(.*)\\\".*data-sigil=\\\"logout\\\"");
        //    MatchCollection matches = regex.Matches(fbLogoutDoc);
        //    if (matches.Count > 0)
        //    {
        //        string finalLogout = string.Format("http://m.facebook.com{0}", matches[0].Groups[1].ToString());
        //        wbLogout.Navigate(new Uri(finalLogout));
        //    }
        //}

        //private void clearCookies(Uri uri)
        //{
        //    var cookies = App.CookiesContainer.GetCookies(uri);
        //    foreach(Cookie c in cookies){
        //        c.Discard = true;
        //        c.Expired = true;
        //    }
        //}
        //private void wbLogout_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        //{            
        //    string strLoweredAddress = e.Uri.OriginalString.ToLower();
        //    if (strLoweredAddress.Contains("facebook.com/home.php") || strLoweredAddress.Contains("facebook.com/logout.php"))
        //    {	//could be www.fa... or m.fa...
        //        if(!m_bDidAppLogout) {	//this was app logout - now logout the user itself
        //            //txtStatus.Text = "Application logged out";
        //            //txtError.Text = "Logging out the user";
        //            m_bDidAppLogout = true;
        //            //the trick is to use the same host (m or www) and the same parameters
        //            //so simply replace home (where we are) with logout
        //            //same host - same parameters - but logout page :)
        //            strLoweredAddress = strLoweredAddress.Replace("home", "logout");

        //            App.AccessToken = "";
        //            User.get().FacebookToken = null;
        //            User.get().Facebook = null;

        //            wbLogout.Navigate(new Uri(strLoweredAddress, UriKind.Absolute));
        //            //NavigationService.GoBack();                    
        //            NavigationService.Navigate(new Uri("/pages/MyProfile.xaml", UriKind.Relative));

        //            return;
        //        }
        //        else {	//again (after navigation to logout) at home page
        //            //txtStatus.Text = "Could not log out user";
        //            //txtError.Text = "Please scroll down to the bottom of the page and click logout";
        //        }
        //    }
        //    //if we are not at "home.php" and tried a logout call before assume we are logged out
        //    else if(m_bDidAppLogout) {
        //        //txtStatus.Text = "Logged out";
        //        //txtError.Text = "OK";
        //        App.AccessToken = "";
        //        User.get().FacebookToken = null;
        //        User.get().Facebook = null;
        //        //App.User.Facebook = null;
        //        //wndLogoutConfirmed.IsOpen = true;
        //        NavigationService.GoBack();
        //        return;
        //    }		
        //}        

    }
}
