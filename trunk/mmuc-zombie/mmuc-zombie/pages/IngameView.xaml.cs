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
using mmuc_zombie.app.model;
using Microsoft.Phone.Shell;
using Parse;
using mmuc_zombie.app.helper;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace mmuc_zombie.pages
{
    public partial class IngameView : PhoneApplicationPage
    {
        List<User> userList;
        List<MyLocation> locationList  = new List<MyLocation>(); 
        List<Roles>roleList = new List<Roles>();
        User user;
        Game game; 
        Roles role;
        MyLocation myLocation;
        bool painting=false;
        private int i;
        private List<MyLocation> gameArea;
        private int leftGameCounter;
        bool questActive = true;
        bool hostDied = false;
   
        public IngameView()
        {
            InitializeComponent();
            PhoneApplicationService service = PhoneApplicationService.Current;
            user = (User)service.State["user"];
            drawGameBorder();
        }

        //constructor used for testing only
        public IngameView(List<User> uList, List<MyLocation> locList, List<Roles> rList, User user, Game game, Roles role, MyLocation myLoc)
        {
            userList = uList;
            locationList = locList;
            roleList = rList;
            this.user = user;
            this.game = game;
            this.role = role;
            myLocation = myLoc;
        }

        public void drawGameBorder()
        {
            Query.getGame(user.activeGame,getGameCallback);
            Query.getGameArea(user.activeGame, getGameAreaCallback); 
                
        }


        public void loadData()
        {
             if (!painting){
                 painting = true;
                 //Debug.WriteLine("loadData");
                 loadGame();
             }
        }

        private void loadGame()
        {
            Query.getGame(user.activeGame, r =>
                {
                    if (r.Success)
                    {
                        game = r.Data;
                        if (game.state == 2)
                        {
                            //our game is now finished
                            gameFinished();
                        }
                        else
                        {
                            loadUsers();
                        }
                    }
                });
        }

        private void gameFinished()
        {
            //game has finished, set data for user
            user.activeGame = "";
            user.activeRole = "";
            user.status = 0;
            user.update(r =>
                {
                    //display a message, that the game has now ended
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            CoreTask.idleMode();
                            MessageBoxResult mb = MessageBox.Show("Congratulations, there are no Survivors left. Zombies win!", "Alert", MessageBoxButton.OK);
                            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/pages/Menu.xaml", UriKind.Relative));
                            
                        });
                });
        }

        private void loadUsers()
        {
             Query.getUsersByGame(user.activeGame, r =>
            {
                if (r.Success)
                {
                     Deployment.Current.Dispatcher.BeginInvoke(() =>
                     {
                        userList = (List<User>)r.Data.Results;
                        //Debug.WriteLine("got Users");
                        locationList=new List<MyLocation>();
                        loadLocations();    
                     });
                }
            });

        }
        private void loadLocations()
        {   
            if (i<userList.Count)
                Query.getLocation(userList[i].locationId, r =>
                {
                    if (r.Success)
                    {
                        //Debug.WriteLine("got Location "+ r.Data.Id+" is equal userlocation "+userList[i].locationId+ "  "+r.Data.latitude+","+r.Data.longitude);
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                           locationList.Add(r.Data);
                           i++;
                           loadLocations(); 
                        });
                    }
                });
             else
             {
                i=0;
                roleList = new List<Roles>();
                loadRoles();
             }
        }
        private void loadRoles()
        {
            if (i<userList.Count)
                Query.getRole(userList[i].activeRole, r =>
                {   
                    if (r.Success)
                    {
                        //Debug.WriteLine("got Role " + r.Data.Id + " is equal userRole " + userList[i].activeRole);
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            roleList.Add(r.Data);
                            i++;
                            loadRoles();
                        });
                    }
                });
            else
            {
                i=0;
                doIngameStuff();
            }
        }

        private void updateRoles()
        {
            if (i < roleList.Count)
            {
                roleList[i].update(r =>
                    {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    Debug.WriteLine("Updated role " + roleList[i].Id);
                                    i++;
                                    updateRoles();
                                });
                      
                    });
            }
            else
            {
                i = 0;
                updateLocations();
            }
        }

        private void updateLocations()
        {
            if (i < locationList.Count)
            {
                locationList[i].update(r =>
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        Debug.WriteLine("Updated location " + locationList[i].Id);
                        i++;
                        updateLocations();
                    });

                });
            }
            else
            {
                i = 0;
                updateUser();
            }
        }

        private void updateUser()
        {
            if (i < userList.Count)
            {
                userList[i].update(r =>
                {
             
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            Debug.WriteLine("Updated user " + userList[i].Id);
                            i++;
                            updateUser();
                        });
                    
                });
            }
            else
            {
                i = 0;
                drawPins();
            }
        }

        private void doIngameStuff()
        {
            if (user==null){
                PhoneApplicationService service = PhoneApplicationService.Current;
                user = (User)service.State["user"];
             }
          
            if (user.Id == game.hostId)
            {
                if (noSurvivorsLeft())
                {
                    game.state = 2;
                    game.update(r =>
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    Debug.WriteLine("updated game " + game.Id);
                                    drawPins();
                                });
                        });

                }
                else
                {
                    Debug.WriteLine("Starting infection");
                    botsWalk();
                    infectSurvivors();
                    if (hostDied)
                    {
                        updateGame();
                    }
                    else
                    {
                        updateRoles();
                    }
                }
            }
            else
                drawPins();
        }

        private void botsWalk()
        {
            for (int i = 1; i <= userList.Count; i++)
                if (userList[i].bot)
                    StaticHelper.randomWalk(locationList[i]);

        }
        private void infectSurvivors()
        {
            hostDied = false;
            //if a survivor is killed, this list will be filled with the murderers
            List<Roles> killer = new List<Roles>();
            for (int i = 0; i < locationList.Count; i++)
            {
                if (roleList[i].roleType.Equals(Constants.ROLE_SURVIVOR))
                {
                    //clear killer list 
                    killer.Clear();
                    //check how many zombies are near this survivor. this method also fills the killer list
                    int infectionPlus = amountOfZombiesNearSurvivor(i,killer);
                    if (infectionPlus > 0)
                    {
                        //there are zombies near survivor
                        Debug.WriteLine(String.Format("There are {0} zombies near user {1}, maxLife = {2}", infectionPlus, userList[i].UserName, roleList[i].maxLife));
                        if (roleList[i].infectionCount + infectionPlus > roleList[i].maxLife)
                        {

                            Debug.WriteLine(String.Format("User {0} is about to die", userList[i].UserName));
                            //survivor got infected and died
                            roleList[i].alive = false;
                            userList[i].activeGame = "";
                            userList[i].status = 0;
                            userList[i].activeRole = "";

                            //increase killCount of all murderers
                            foreach (Roles r in killer)
                            {
                                ++r.killCount;
                            }

                            if (userList[i].Id == game.hostId)
                            {
                                //current host died, we need a new one
                                hostDied = true;
                                for (int j = 0; j < userList.Count;j++ )
                                {
                                    if (userList[j].bot || userList[j].Id == game.hostId || roleList[j].roleType == "Observer")
                                    {
                                        continue;
                                    }
                                    game.hostId = userList[j].Id;
                                }
                            }
                        }
                        else
                        {

                            //survivor is getting infected but still alive
                            roleList[i].infectionCount += infectionPlus;
                            Debug.WriteLine(String.Format("User {0} is currently getting infected, progress = {1}", userList[i].UserName, roleList[i].infectionCount));

                        }
                    }
                    else
                    {
                        //there are no zombies near the survivor
                        Debug.WriteLine(String.Format("There are {0} zombies near user {1}", infectionPlus, userList[i].UserName));
                        if (roleList[i].infectionCount > 0)
                        {
                            //this means that the user managed to escape the zombies while they were infecting him
                            Debug.WriteLine(String.Format("User {0} got savely away",  userList[i].UserName));
                            roleList[i].infectionCount = 0;
                        }
                    }
                }
            }
            this.i = 0;
        }

        private bool noSurvivorsLeft()
        {
            bool noSurvivors = true;
            foreach(Roles r in roleList)
            {
                if (r.roleType == Constants.ROLE_SURVIVOR)
                {
                    noSurvivors = false;
                }
            }
            return noSurvivors;
        }

        private void updateGame()
        {
            game.update(r =>
                {
                    Debug.WriteLine("Updated game: " + game.Id);
                    updateRoles();
                });
        }

        private int amountOfZombiesNearSurvivor(int i,List<Roles> killer)
        {
            int zombies = 0;
            for (int j = 0; j < userList.Count; j++)
            {
                double distance = locationList[j].toGeoCoordinate().GetDistanceTo(locationList[i].toGeoCoordinate());
                // for testing is true
                bool near = true;
                if(roleList[j].roleType.Equals(Constants.ROLE_ZOMBIE)&&near)
                {
                    ++zombies;
                    killer.Add(roleList[j]);
                }
            }
            return zombies;    
        } 
        
        private void drawPins()
        {
            

            //Debug.WriteLine("Start Drawing Users");
            //debug.Text = "";
            PhoneApplicationService service = PhoneApplicationService.Current;
            MyLocation location = (MyLocation)service.State["location"];
            if (!StaticHelper.pointInPolygon(gameArea, location))
            {
                leftGameCounter++;
                if (leftGameCounter > 5)
                {
                    MessageBoxResult mb = MessageBox.Show("You have left the Gaming Area. Do you want to quit the game?", "Alert", MessageBoxButton.OKCancel);
                    leftGameCounter = 3;
                    if (mb == MessageBoxResult.OK)
                    {
                        leftGameCounter = 0;
                        (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/pages/Menu.xaml", UriKind.Relative));
                        CoreTask.idleMode();
                    }
                }
            }
            else leftGameCounter = 0;
            mapLayer.Children.Clear();
            int playerPosition = 0;
            for (int i = 0; i < userList.Count; i++)
            {
                var p = new Pushpin();
                //Debug.WriteLine("-----------------------------------");
                //Debug.WriteLine("User" + userList[i].Id);
                p.Location = new GeoCoordinate(locationList[i].latitude, locationList[i].longitude);
                p.Name = userList[i].Id;
                //debug.Text += "User: " + userList[i].Id + "\n Location (" + locationList[i].latitude + "," + locationList[i].longitude + ")\n";
                if (locationList[i].Id.Equals(user.locationId))
                        myLocation=locationList[i];
                //Debug.WriteLine("location "+locationList[i].Id); 
                if (roleList[i].Id.Equals(user.activeRole))
                        role=roleList[i];
           
                if (roleList[i].roleType.Equals(Constants.ROLE_ZOMBIE))
                {
                  //  p.Style = (Style)(Application.Current.Resources["PushpinStyle2"]);
                 //   Debug.WriteLine("Zombiestyle");
                    if (user.Id.Equals(userList[i].Id))
                    {
                        playerPosition = i;
                        p.Template = this.Resources["playerzombiepin"] as ControlTemplate;
                    }
                    else
                        p.Template = this.Resources["zombiepin"] as ControlTemplate;
                   
                 
                }
                else if (roleList[i].roleType.Equals("Survivor"))
                {
                 //   p.Style = (Style)(Application.Current.Resources["PushpinStyle"]);
                    Debug.WriteLine("Survivorstyle");
                    if (user.Id.Equals(userList[i].Id))
                    {
                        playerPosition = i;
                        p.Template = this.Resources["playersurvivorpin"] as ControlTemplate;
                    }
                    else
                        p.Template = this.Resources["survivorpin"] as ControlTemplate;
                }
            
                mapLayer.Children.Add(p);
            }
            drawInfobox();
            map.Center=locationList[playerPosition].toGeoCoordinate();
            map.ZoomLevel = 14;
            painting = false;
        }

        private void drawInfobox()
        {
            int playerCount = 0;
            int zombieCount = 0;
            foreach(Roles r in roleList)
            {
                if(r.roleType=="Zombie")
                {
                    ++playerCount;
                    ++zombieCount;
                }else if(r.roleType=="Survivor")
                {
                    ++playerCount;
                }
            }
            int marginX = 0;
            int marginY = 35;
            LayoutRoot.Children.Clear();
            if (role.roleType == "Survivor")
            {
                //display healthbar
                for (int i = 0; i < role.maxLife;i++ )
                {
                    Rectangle rect = new Rectangle();
                    rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    rect.Height = 30;
                    rect.Width = 25;
                    if (i <= role.infectionCount)
                        rect.Fill = new SolidColorBrush(Colors.Red);
                    else
                        rect.Fill = new SolidColorBrush(Colors.Green);
                    rect.Margin = new Thickness(marginX, 0, 0, 0);
                    marginX += 25;
                    rect.SetValue(Grid.RowProperty, 1);
                    LayoutRoot.Children.Add(rect);
                }
            }
            //other gameinfo
            TextBlock playerAmount = new TextBlock();
            playerAmount.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            playerAmount.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            playerAmount.Height = 30;
            playerAmount.Width = 200;
            playerAmount.Margin = new Thickness(0, marginY, 0, 0);
            playerAmount.Text = "Playercount: " + playerCount;
            marginY += 35;
            playerAmount.SetValue(Grid.RowProperty, 1);
            LayoutRoot.Children.Add(playerAmount);

            TextBlock zombieAmount = new TextBlock();
            zombieAmount.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            zombieAmount.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            zombieAmount.Height = 30;
            zombieAmount.Width = 200;
            zombieAmount.Margin = new Thickness(0, marginY, 0, 0);
            zombieAmount.Text = "Zombiecount: " + zombieCount;
            marginY += 35;
            zombieAmount.SetValue(Grid.RowProperty, 1);
            LayoutRoot.Children.Add(zombieAmount);
            if (role.roleType == "Zombie")
            {
                TextBlock killCount = new TextBlock();
                killCount.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                killCount.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                killCount.Height = 30;
                killCount.Width = 200;
                killCount.Margin = new Thickness(0, marginY, 0, 0);
                killCount.Text = "Killcount: " + role.killCount;
                marginY += 35;
                killCount.SetValue(Grid.RowProperty, 1);
                LayoutRoot.Children.Add(killCount);
            }
            if (role.roleType == "Survivor")
            {
                TextBlock questCount = new TextBlock();
                questCount.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                questCount.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                questCount.Height = 30;
                questCount.Width = 200;
                questCount.Margin = new Thickness(0, marginY, 0, 0);
                questCount.Text = "Questcount: " + role.questCount;
                marginY += 35;
                questCount.SetValue(Grid.RowProperty, 1);
                LayoutRoot.Children.Add(questCount);
            }

        }
        


        public void getGameCallback(Response<Game> r)
        {
            if (r.Success)
            {
                game = r.Data;
            }
        }



        public void getGameAreaCallback(Response<ResultsResponse<MyLocation>> r)
        {
            if (r.Success)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        gameArea = (List<MyLocation>)r.Data.Results;
                        gameAreaLayer.Children.Add(StaticHelper.inGameArea(list));

                    });
            }

        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            PositionRetriever.startPositionRetrieving(1);
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult mb = MessageBox.Show("Do you want to leave the game?", "Alert", MessageBoxButton.OKCancel);
            if (mb != MessageBoxResult.OK)
            {
                e.Cancel = true;
            }
            else
            {
                user.activeGame = "";
                user.status = 0;
                user.update();
                if (user.Id == game.hostId)
                {
                    bool noHost = true;
                    for (int j = 0; j < userList.Count; j++)
                    {
                        if (userList[j].bot || userList[j].Id == game.hostId || roleList[j].roleType == "Observer")
                        {
                            continue;
                        }
                        game.hostId = userList[j].Id;
                        noHost = false;
                        game.update(r =>
                        {
                            Debug.WriteLine("Updated game: " + game.Id);   
                        });
                    }
                    if (noHost)
                    {
                        //there are no players left in the game (possibly there are still bots and observer
                        game.state = 2;
                        game.update(r =>
                        {
                            Debug.WriteLine("Updated game: " + game.Id);
                        });
                    }
                }
                (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/pages/Menu.xaml", UriKind.Relative));
            }
        }


    }
}