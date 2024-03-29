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
        Roles targetRole;
        bool target = false;
        MyLocation myLocation;
        bool painting=false;
        private int i;
        Quest quest;
        private List<MyLocation> gameArea;
        Rectangle rect = new Rectangle();
        TextBlock playerAmount = new TextBlock();
        TextBlock zombieAmount = new TextBlock();
        TextBlock killCount = new TextBlock();
        TextBlock questCount = new TextBlock();
        TextBlock eventMsg = new TextBlock();
        TextBlock targetPlayerName = new TextBlock();
        TextBlock targetAchievementCount = new TextBlock();
        Image targetPicture = new Image(); 
        List<Rectangle> targetHealthBar = new List<Rectangle>();
        
        private bool init=true;
        int survivors = 0;
        int range = 0;
        int hostCount = 0;
        int zoom = 0;
        private int increment;
        private Button targetButton = new Button();
        private List<Roles> fillRoleList;
        private List<MyLocation> fillLocationList;
   
        public IngameView()
        {
            InitializeComponent();
            PhoneApplicationService service = PhoneApplicationService.Current;
            user = (User)service.State["user"];
            drawGameBorder();
            LayoutRoot.Children.Add(playerAmount);
            LayoutRoot.Children.Add(zombieAmount);
            LayoutRoot.Children.Add(killCount);
            LayoutRoot.Children.Add(questCount);
            LayoutRoot.Children.Add(eventMsg);
            LayoutRoot.Children.Add(targetAchievementCount);
            LayoutRoot.Children.Add(targetPlayerName);
            LayoutRoot.Children.Add(targetButton);

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
        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            Progressbar.ShowProgressBar();
            PositionRetriever.startPositionRetrieving(1);
        }

        //gets the game, area and quest
        public void drawGameBorder()
        {
            Query.getGame(user.activeGame,getGameCallback);
            Query.getGameArea(user.activeGame, getGameAreaCallback);
            Query.getQuest(user.activeGame, r =>
                {
                    if(r.Success)
                    {
                        quest = (Quest)r.Data.Results[0];
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
                            MessageBoxResult mb = MessageBox.Show("Congratulations, there are no Survivors left. Zombies win!", "Alert", MessageBoxButton.OK);
                            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/pages/Menu.xaml", UriKind.Relative));
                        });
                });
        }

        
        //if the user is the game host the ingame computaitions begin here
        private void doIngameStuff()
        {
            if (user==null){
                PhoneApplicationService service = PhoneApplicationService.Current;
                user = (User)service.State["user"];
             }
          
            if (user.Id == game.hostId)
            {
                survivors = survivorsLeft();
                if (survivors == 0)
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
                    Debug.WriteLine("Botwalk");
                    botsWalk();

                    Debug.WriteLine("Queststuff");
                    doQuestStuff();
                    Debug.WriteLine("Starting infection");
                    infectSurvivors();
                    Debug.WriteLine("Update");
                    updateGame();
                }
            }
            else
                drawPins();
        }

        private void updateGame()
        {
            game.update(r =>
            {
                Debug.WriteLine("Updated game: " + game.Id);
                updateQuest();
            });
        }

        private void updateQuest()
        {
            quest.update(r =>
            {
                Debug.WriteLine("Updated quest: " + game.Id);
                updateRoles();
            });
        }


        //movement of bots
        private void botsWalk()
        {
            for (int i = 0; i < userList.Count; i++)
            {
                Debug.WriteLine("Outer for " + i);
                if (userList[i].bot)
                {
                    if (roleList[i].roleType == Constants.ROLE_SURVIVOR)
                        StaticHelper.randomWalk(gameArea, locationList[i]);
                    else
                    {
                        MyLocation nextSurvivor = new MyLocation();
                        double distance = double.MaxValue;
                        for (int j = 0; j < userList.Count; j++)
                        {

                            Debug.WriteLine("Inner for " + i);
                            if (roleList[j].roleType == Constants.ROLE_SURVIVOR)
                            {
                                double newDistance = locationList[j].toGeoCoordinate().GetDistanceTo(locationList[i].toGeoCoordinate());
                                if (newDistance < distance)
                                {
                                    distance = newDistance;
                                    nextSurvivor = locationList[j];
                                }
                            }
                        }
                        StaticHelper.zombieWalk(gameArea, locationList[i], nextSurvivor);
                    }
                }
            }
        }

        //check if player has done a quest
        private void doQuestStuff()
        {
            if (quest.active)
            {
                survivorNearQuest();
            }
            else
            {
                if (Constants.random.NextDouble() < 0.4)
                {
                    quest.healthPlus = 1;
                }
                else if (Constants.random.NextDouble() < 0.8)
                {
                    quest.healthPlus = 2;
                }
                else
                {
                    quest.healthPlus = 3;
                }
                MyPolygon rectangle = StaticHelper.rectangleInsidePolygon(gameArea);
                MyLocation questLoc = StaticHelper.randomPointInRectangle(gameArea,rectangle.Locations[0], rectangle.Locations[2]);
                quest.latitude = questLoc.latitude;
                quest.longitude = questLoc.longitude;
                quest.active = true;
            }
        }

        private void survivorNearQuest()
        {
            GeoCoordinate qLoc = new GeoCoordinate(quest.latitude, quest.longitude);
            for (int i = 0; i < roleList.Count;i++)
            {
                if (roleList[i].roleType == Constants.ROLE_SURVIVOR)
                {
                    GeoCoordinate uLoc = locationList[i].toGeoCoordinate();
                    if (uLoc.GetDistanceTo(qLoc) < range)
                    {
                        //survivor won quest
                        if (roleList[i].maxLife + quest.healthPlus > 10)
                        {
                            roleList[i].maxLife = 10;
                            game.events = userList[i].UserName + "'s health is maxed out,"+game.events;
                        }
                        else
                        {
                            roleList[i].maxLife += quest.healthPlus;
                            game.events = userList[i].UserName + " gained " + quest.healthPlus + " health," + game.events;
                        }
                        ++roleList[i].questCount;
                        quest.active = false;
                    }
                }
            }
        }

        //infection mode
        private void infectSurvivors()
        {
            //if a survivor is killed, this list will be filled with the murderers
            List<Roles> killer = new List<Roles>();
            for (int i = 0; i < locationList.Count; i++)
            {
                if (roleList[i].roleType.Equals(Constants.ROLE_SURVIVOR))
                {
                    roleList[i].rank = survivors;
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
                                if (survivorsLeft() - 1 >= 1)
                                {
                                    //current host died, we need a new one
                                    for (int j = 0; j < userList.Count; j++)
                                    {
                                        if (userList[j].bot || userList[j].Id == game.hostId || roleList[j].roleType == "Observer")
                                        {
                                            continue;
                                        }
                                        game.hostId = userList[j].Id;
                                    }
                                }
                                else
                                {
                                    //there are no players left in the game (possibly there are still bots and observer
                                    game.state = 2;
                                    game.update(r =>
                                    {
                                        Debug.WriteLine("Updated game: " + game.Id);
                                    });
                                }
                            }
                            game.events = userList[i].UserName + " has died," + game.events;
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

        private int survivorsLeft()
        {
            int survivors = 0;
            foreach(Roles r in roleList)
            {
                if (r.roleType == Constants.ROLE_SURVIVOR)
                {
                    ++survivors;
                }
            }
            return survivors;
        }

        
        private int amountOfZombiesNearSurvivor(int i,List<Roles> killer)
        {
            int zombies = 0;
            for (int j = 0; j < userList.Count; j++)
            {
                double distance = locationList[j].toGeoCoordinate().GetDistanceTo(locationList[i].toGeoCoordinate());
                // for testing is true
                bool near = distance<(range+increment*roleList[j].killCount);
                if(roleList[j].roleType.Equals(Constants.ROLE_ZOMBIE)&&near)
                {
                    ++zombies;
                    killer.Add(roleList[j]);
                }
            }
            return zombies;    
        } 

        
        //draws the mapview with all icons etc.
        private void drawPins()
        {

         
            mapLayer.Children.Clear();
            int playerPosition = 0;
            if (quest.active)
            {
                var questPin = new Pushpin();
                questPin.Location = new GeoCoordinate(quest.latitude, quest.longitude);
                questPin.Template = this.Resources["questpin"] as ControlTemplate;
                mapLayer.Children.Add(questPin);
            }
            for (int i = 0; i < userList.Count; i++)
            {   

                var p = new Pushpin();
                p.Location = new GeoCoordinate(locationList[i].latitude, locationList[i].longitude);
                p.Name = userList[i].Id;
                p.Height = 36;
                p.Width = 36;
                
                if (userList[i].Id.Equals(game.hostId))
                    hostCount = i;
                if (locationList[i].Id.Equals(user.locationId))
                        myLocation=locationList[i];
                if (roleList[i].Id.Equals(user.activeRole))
                        role=roleList[i];
                if (roleList[i].roleType.Equals(Constants.ROLE_ZOMBIE))
                {
                    if (user.Id.Equals(userList[i].Id))
                    {
                        playerPosition = i;
                        p.Template = this.Resources["playerzombiepin"] as ControlTemplate;
                        var fg = this.Resources["playerzombiepin"] as ControlTemplate;

                    }
                    else
                    {
                        
                        String h = "zombiepin" + roleList[i].ingameIcon;
                        p.Template = this.Resources[h] as ControlTemplate;
                    }
                 
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
                    {
                       
                        String h = "survivorpin" + roleList[i].ingameIcon;
                        p.Template = this.Resources[h] as ControlTemplate;
                    }
                    }
                p.Name = "" + i;
                p.MouseEnter += new MouseEventHandler(playerClick);
                if (!roleList[i].roleType.Equals(Constants.ROLE_OBSERVER))
                    mapLayer.Children.Add(p);
            }

            drawInfobox();
            if (!(role.roleType.Equals(Constants.ROLE_OBSERVER)))
                map.Center = locationList[playerPosition].toGeoCoordinate();
            map.ZoomLevel = zoom;
            if (init)
            {    
                init = false;
                Progressbar.HideProgressBar();
                if (role.roleType.Equals(Constants.ROLE_OBSERVER))
                    map.Center=locationList[hostCount].toGeoCoordinate();
            }
            if (target)
                reloadTargetInfo();
            painting = false;
        }


        //displays information of the player that has been clicked on the map
        private void playerClick(Object sender,MouseEventArgs e)
        {
            TargetInfobox.Visibility = System.Windows.Visibility.Visible;
            int i = int.Parse(((Pushpin)sender).Name);
            target = true;
            targetRole = roleList[i];
            foreach (Rectangle r in targetHealthBar)
            {
                LayoutRoot.Children.Remove(r);
            }
            targetHealthBar.Clear();
            int marginX = 160;
            int marginY = 5;

            if (roleList[i].roleType == Constants.ROLE_SURVIVOR)
            {
                //display healthbar
                for (int j = 0; j < targetRole.maxLife; j++)
                {
                    rect = new Rectangle();
                    rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    rect.Height = 30;
                    rect.Width = 15;
                    if (j < targetRole.infectionCount)
                        rect.Fill = new SolidColorBrush(Colors.Red);
                    else
                        rect.Fill = new SolidColorBrush(Colors.Green);
                    rect.Margin = new Thickness(0, marginY, marginX, 0);
                    marginX -= 15;
                    rect.SetValue(Grid.RowProperty, 1);
                    targetHealthBar.Add(rect);
                    LayoutRoot.Children.Add(rect);
                }
                marginY += 35;
            }

            targetPlayerName.Text = userList[i].UserName;
            targetPlayerName.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            targetPlayerName.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            targetPlayerName.Height = 30;
            targetPlayerName.Width = 170;
            targetPlayerName.Margin = new Thickness(0, marginY, 0, 0);
            targetPlayerName.SetValue(Grid.RowProperty, 1);
            marginY += 35;
            

            if (roleList[i].roleType == Constants.ROLE_ZOMBIE)
                targetAchievementCount.Text = "Survivors killed: " + roleList[i].killCount;
            else
            {
                targetAchievementCount.Text = "Quests done: " + roleList[i].questCount;   
            }
            targetAchievementCount.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            targetAchievementCount.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            targetAchievementCount.Height = 30;
            targetAchievementCount.Width = 170;
            targetAchievementCount.Margin = new Thickness(0, marginY, 0, 0);
            marginY += 35;
            targetAchievementCount.SetValue(Grid.RowProperty, 1);

            
            targetButton.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            targetButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            targetButton.Height = 70;
            targetButton.Width = 70;
            targetButton.Margin = new Thickness(0, 110, 110, 0);
            targetButton.Click += new RoutedEventHandler(targetButton_Click);
            targetButton.Visibility = System.Windows.Visibility.Visible;
            targetButton.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri("/mmuc-zombie;component/ext/img/del.jpg", UriKind.Relative)) };
            targetButton.SetValue(Grid.RowProperty, 1);
        }

        //realods the info box of the player clicked
        private void reloadTargetInfo()
        {
            bool targetDied = true;
            foreach (Roles r in roleList)
            {
                if (r.Id == targetRole.Id)
                {
                    targetRole = r;
                    targetDied = false;
                }
            }
            if (!targetDied)
            {
                foreach (Rectangle r in targetHealthBar)
                {
                    LayoutRoot.Children.Remove(r);
                }
                targetHealthBar.Clear();
                int marginX = 160;
                for (int j = 0; j < targetRole.maxLife; j++)
                {
                    rect = new Rectangle();
                    rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    rect.Height = 30;
                    rect.Width = 15;
                    if (j < targetRole.infectionCount)
                        rect.Fill = new SolidColorBrush(Colors.Red);
                    else
                        rect.Fill = new SolidColorBrush(Colors.Green);
                    rect.Margin = new Thickness(0, 5, marginX, 0);
                    marginX -= 15;
                    rect.SetValue(Grid.RowProperty, 1);
                    targetHealthBar.Add(rect);
                    LayoutRoot.Children.Add(rect);
                }
                if (targetRole.roleType == Constants.ROLE_ZOMBIE)
                    targetAchievementCount.Text = "Survivors killed: " + targetRole.killCount;
                else
                    targetAchievementCount.Text = "Quests done: " + targetRole.questCount;
            }
            else
            {
                targetButton_Click(null, null);
            }
        }

        //removes the info of the clicked player from the screen
        void targetButton_Click(object sender, RoutedEventArgs e)
        {
            target = false;
            TargetInfobox.Visibility = System.Windows.Visibility.Collapsed;
            foreach (Rectangle r in targetHealthBar)
            {
                LayoutRoot.Children.Remove(r);
            }
            targetAchievementCount.Text = "";
            targetPlayerName.Text = "";
            targetButton.Visibility = System.Windows.Visibility.Collapsed;         
        }

        //draws the information of the player in the upper left corner
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
            int marginY = 40;
            if (role.roleType == "Survivor")
            {
                //display healthbar
                for (int i = 0; i < role.maxLife;i++ )
                {
                    rect = new Rectangle();
                    rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    rect.Height = 30;
                    rect.Width = 15;
                    if (i < role.infectionCount)
                        rect.Fill = new SolidColorBrush(Colors.Red);
                    else
                        rect.Fill = new SolidColorBrush(Colors.Green);
                    rect.Margin = new Thickness(marginX, 5, 0, 0);
                    marginX += 15;
                    rect.SetValue(Grid.RowProperty, 1);
                    LayoutRoot.Children.Add(rect);
                }
            }
            //other gameinfo
            playerAmount.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            playerAmount.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            playerAmount.Height = 30;
            playerAmount.Width = 200;
            playerAmount.Margin = new Thickness(0, marginY, 0, 0);
            playerAmount.Text = "Playercount: " + playerCount;
            marginY += 35;
            playerAmount.SetValue(Grid.RowProperty, 1);

            
            zombieAmount.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            zombieAmount.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            zombieAmount.Height = 30;
            zombieAmount.Width = 200;
            zombieAmount.Margin = new Thickness(0, marginY, 0, 0);
            zombieAmount.Text = "Zombiecount: " + zombieCount;
            marginY += 35;
            zombieAmount.SetValue(Grid.RowProperty, 1);
            
            if (role.roleType == "Zombie")
            {
                
                killCount.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                killCount.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                killCount.Height = 30;
                killCount.Width = 200;
                killCount.Margin = new Thickness(0, marginY, 0, 0);
                killCount.Text = "Killcount: " + role.killCount;
                marginY += 35;
                killCount.SetValue(Grid.RowProperty, 1);
                
            }
            if (role.roleType == "Survivor")
            {
                
                questCount.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                questCount.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                questCount.Height = 30;
                questCount.Width = 200;
                questCount.Margin = new Thickness(0, marginY, 0, 0);
                questCount.Text = "Questcount: " + role.questCount;
                marginY += 35;
                questCount.SetValue(Grid.RowProperty, 1);
                
            }
            eventMsg.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            eventMsg.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            eventMsg.Height = 150;
            eventMsg.Width = 250;
            eventMsg.Margin = new Thickness(0, marginY, 0, 0);
            string evMsg = "";
            string[] msgArray = game.events.Split(new Char [] {','});
            for (int i = 0; i < 2 && i < msgArray.Length;i++)
            {
                evMsg += msgArray[i]+"\n";
            }
            eventMsg.Text = evMsg;
            eventMsg.SetValue(Grid.RowProperty, 1);

        }
        


        public void getGameCallback(Response<Game> r)
        {
            if (r.Success)
            {
                game = r.Data;
                switch (game.size)
                {
                    case 0: range = Constants.SMALL_GAME_INFECTION_RANGE; increment = Constants.SMALL_GAME_INFECTION_RANGE_INC; zoom = Constants.SMALL_GAME_ZOOMFACTOR; break;
                    case 1: range = Constants.MEDIUM_GAME_INFECTION_RANGE; increment = Constants.MEDIUM_GAME_INFECTION_RANGE_INC; zoom = Constants.MEDIUM_GAME_ZOOMFACTOR; break;
                    case 2: range = Constants.BIG_GAME_INFECTION_RANGE; increment = Constants.BIG_GAME_INFECTION_RANGE_INC; zoom = Constants.BIG_GAME_ZOOMFACTOR; break;
                }
            }
        }



        public void getGameAreaCallback(Response<ResultsResponse<MyLocation>> r)
        {
            if (r.Success)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        gameArea = (List<MyLocation>)r.Data.Results;
                        gameAreaLayer.Children.Add(StaticHelper.inGameArea(gameArea));

                    });
            }

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

        private void PhoneApplicationPage_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
        }


        /**
 *all load/update methods load the corresponding data recursively. this is done so that e.g. user userList[i] has location locationList[i] etc.
 * 
 **/
        public void loadData()
        {
            if (!painting)
            {
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
                        loadQuest();
                    }
                }
            });
        }

        private void loadQuest()
        {
            Query.getQuest(user.activeGame, r =>
            {
                if (r.Success)
                {
                    quest = r.Data.Results[0];
                    loadUsers();
                }
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
                        fillLocationList = new List<MyLocation>();
                        loadLocations();
                    });
                }
            });

        }
        private void loadLocations()
        {
            if (i < userList.Count)
                Query.getLocation(userList[i].locationId, r =>
                {
                    if (r.Success)
                    {
                        //Debug.WriteLine("got Location "+ r.Data.Id+" is equal userlocation "+userList[i].locationId+ "  "+r.Data.latitude+","+r.Data.longitude);
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            fillLocationList.Add(r.Data);
                            i++;
                            loadLocations();
                        });
                    }
                });
            else
            {
                i = 0;
                locationList = fillLocationList;
                fillRoleList = new List<Roles>();
                loadRoles();
            }
        }
        private void loadRoles()
        {
            if (i < userList.Count)
                Query.getRole(userList[i].activeRole, r =>
                {
                    if (r.Success)
                    {
                        //Debug.WriteLine("got Role " + r.Data.Id + " is equal userRole " + userList[i].activeRole);
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            fillRoleList.Add(r.Data);
                            i++;
                            loadRoles();
                        });
                    }
                });
            else
            {
                roleList = fillRoleList;
                i = 0;
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
                if (userList[i].bot)
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
                    i++;
                    updateLocations();
                }

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

    }
}