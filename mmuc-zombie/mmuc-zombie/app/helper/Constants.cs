﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace mmuc_zombie.app.helper
{
    public class Constants
    {
        public const string NONICKNAME = "nickname";
        public const string AVATARPATH = "/mmuc-zombie;component/ext/img/avatar.png";        

        public enum USERGAMEMODES : int { INIT = 0, IDLE, LOBBY, INGAME };
        public enum GAMEMODES : int { PENDING = 0, ACTIVE, FINISHED };
        public enum GAMESIZE : int { SMALL = 0, MEDIUM, BIG };
        
        public const string ROLE_ZOMBIE = "Zombie";
        public const string ROLE_SURVIVOR = "Survivor";
        public const string ROLE_OBSERVER = "Observer";

        public const int SMALL_GAME_SIZE = 500;
        public const int MEDIUM_GAME_SIZE = 2000;
        public const int BIG_GAME_SIZE = 4000;

        public const int SMALL_GAME_RANGE = 25;
        public const int MEDIUM_GAME_RANGE = 100;
        public const int BIG_GAME_RANGE = 200;



        public const int SMALL_GAME_ZOOMFACTOR = 17;
        public const int MEDIUM_GAME_ZOOMFACTOR = 16;
        public const int BIG_GAME_ZOOMFACTOR = 15;

        public const int BOT_MOVEMENT = 10;

        public static Random random= new Random();
        //game modes
        //pending = 0, waiting = 1, active = 2, finshed = 3

        //user-game modes
        //int=0: "idle mode" he is doing nothing, he ha no pending games; no timertask is running, if he joins a game he switch to status 1
        //int=1: "lobby-mode" user has joined a game , timertask checks if gameowner creates a game. if he does user switch to status 3. user can leave:if he has pending games left he switch to status 1, if not he switch to status 0.
        //int=2: "ingame" mode  many things are checked ...(infection, userlocation ...)if he leaves a game: it is checked if he has pending events if yes he switches to status 1 else 0

    }
}