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
using mmuc_zombie.app.facebook;
using mmuc_zombie.app.helper;
using System.Windows.Media.Imaging;
using Parse;
using Microsoft.Phone.Shell;

namespace mmuc_zombie.pages
{
    public partial class MyProfile : PhoneApplicationPage
    {
        
        User user;
        private WebClient m_wcFacebookProfile;
        private WebClient m_wcFacebookFriends;
        private static FBUser m_CurFacebookUser;

        public MyProfile()
        {
            InitializeComponent();
            PhoneApplicationService service = PhoneApplicationService.Current;
            user = (User)service.State["user"];
            string userId=user.Id;
            var parse = new Driver();
            parse.Objects.Query<User>().Where(c => c.Id == c.Id).Execute(r =>
            {
                if (r.Success)
                {
                    List<User> users=(List<User>)r.Data.Results;
                    parse.Objects.Query<Friend>().Where(c => c.user ==userId ).Execute(r2 =>
                       {
                           if (r2.Success)
                           {
                               List<Friend> friends = (List<Friend>)r2.Data.Results;
                              Deployment.Current.Dispatcher.BeginInvoke(() =>
                              {
                                   if (!loadUsers(users,friends))
                                    {            
                                        userListBox.Visibility = System.Windows.Visibility.Collapsed;
                                    }
                                    else
                                    {
                                        userListBox.Visibility = System.Windows.Visibility.Visible;
                                    }
                               });
                           }
                       });
                  }
            });
       }
            
     

        private void appbar_save_Click(object sender, EventArgs e)
        {
            MessageBox.Show("save");
        }

        private void appbar_cancel_Click(object sender, EventArgs e)
        {
            MessageBox.Show("cancel");
        }
        private bool loadUsers(List<User> users,List<Friend> friends)
        {
            /* TEST DATA */

            mmuc_zombie.components.friendsView tmpUI;
            //var _parse = new Driver();
            
            foreach (User tmp in users)
            {
                //_parse.Objects.Save(tmp);
               
                tmpUI = new mmuc_zombie.components.friendsView();
                foreach (Friend friend in friends)
                {
                    if (tmp.Id.Equals(friend.friend))
                    {
                        tmpUI.isFriend = true;
                        tmpUI.textBlock1.Text = "friend";
                    }
                }  
                tmpUI.nameTextBlock.Text = tmp.Id;
                tmpUI.userId = user.Id;
                tmpUI.friendId = tmp.Id;
                userStackPanel.Children.Add(tmpUI);
            }

            return userStackPanel.Children.Count > 0;
        }

        private void initializeFacebookProfile()
        {
            if (m_wcFacebookProfile == null)
            {
                m_wcFacebookProfile = new WebClient();
                m_wcFacebookProfile.DownloadStringCompleted += new DownloadStringCompletedEventHandler(m_wcFacebookProfile_DownloadStringCompleted);
	        }
	        try {
                m_wcFacebookProfile.DownloadStringAsync(FacebookURIs.GetQueryUserUri(App.AccessToken));
		        Console.Out.WriteLine("Loading user data");
	        }
	        catch(Exception eX) {
                Console.Out.WriteLine(eX.Message);
                Console.Out.WriteLine("Could not load user data");                
	        }            
        }


        void m_wcFacebookProfile_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                m_CurFacebookUser = null;
                Console.Out.WriteLine(e.Error.Message);
                Console.Out.WriteLine("Error loading user data");
                return;
            }
            try
            {                
                m_CurFacebookUser = JsonStringSerializer.Deserialize<FBUser>(e.Result);
                fbUserGrid.DataContext = m_CurFacebookUser;                
                Console.Out.WriteLine("User data loaded");
            }
            catch (Exception eX)
            {
                m_CurFacebookUser = null;
                Console.Out.WriteLine(eX.Message);
                Console.Out.WriteLine("Error parsing user data");
            }
            validateUI();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            initializeFacebookProfile();
            initializeFacebookFriends();

            validateUI();
            loadFriends();
        }

        private void validateUI() 
        {
            if (fbUserGrid.DataContext == null)
            {
                fbUserGrid.DataContext = new FBUser("/mmuc-zombie;component/ext/img/avatar.png");
                name.Visibility = Visibility.Collapsed;
                facebook.Visibility = Visibility.Collapsed;
                oauth.Visibility = Visibility.Visible;
            }
            else
            {                
                name.Visibility = Visibility.Visible;
                facebook.Visibility = Visibility.Visible;
                oauth.Visibility = Visibility.Collapsed;
            }
        }

        private void initializeFacebookFriends()
        {
            if (m_wcFacebookFriends == null)
            {
                m_wcFacebookFriends = new WebClient();
                m_wcFacebookFriends.DownloadStringCompleted += new DownloadStringCompletedEventHandler(m_wcFacebookFriends_DownloadStringCompleted);
            }
            try
            {
                m_wcFacebookFriends.DownloadStringAsync(FacebookURIs.GetLoadFriendsUri(App.AccessToken));
            }
            catch (Exception eX)
            {
                Console.WriteLine(eX.Message);
                Console.WriteLine("Error start load friends");
            }
        }

        void m_wcFacebookFriends_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Console.WriteLine(e.Error.Message);
                Console.WriteLine("Error loading friends");
                return;
            }
            try
            {                
                //fbFriends.DataContext = JsonStringSerializer.Deserialize<FBFriends>(e.Result); ;

                FBFriends friends = JsonStringSerializer.Deserialize<FBFriends>(e.Result);

                mmuc_zombie.components.facebookFriendView tmpUI;

                foreach (FBUser tmp in friends.Friends)
                {
                    tmpUI = new mmuc_zombie.components.facebookFriendView();
                    tmpUI.name.Text = tmp.Name;
                    tmpUI.image.Source = new BitmapImage(new Uri(tmp.PicLink, UriKind.Absolute));
                    tmpUI.Margin = new Thickness(0, 5, 0, 5);
                    friendStack.Children.Add(tmpUI);
                }

            }
            catch (Exception eX)
            {
                Console.WriteLine(eX.Message);
                Console.WriteLine("Error parsing friends");
            }
        }

        private void loadFriends()
        {
            loadLocalFriends();            
        }
        
        private void loadLocalFriends()
        {
            //throw new NotImplementedException();
        }

        private void oauth_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/pages/FacebookLogin.xaml", UriKind.Relative));
        }

    }
}