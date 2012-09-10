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
using Parse;
using System.Collections.Generic;
using Microsoft.Phone.Shell;
using System.Diagnostics;

public class Roles : MyParseObject
    {
        public string userId { get; set; } 
        public string gameId { get; set; }
        //public DateTime? endTime { get; set; }
        //public DateTime? startTime { get; set; }
        public int rank { get; set; }
        public int infectionCount { get; set; }
        public string roleType { get; set; }
        public bool alive { get; set; }

        public new void update()
        {

            var parse = new Driver();
            parse.Objects.Update<Roles>(this.Id).
                Set(u => u.userId, userId).
                Set(u => u.gameId, gameId).
                //Set(u => u.endTime, endTime).
                //Set(u => u.startTime, startTime).
                Set(u => u.infectionCount,infectionCount).
                Set(u => u.roleType, roleType).
                Set(u => u.alive, alive).
                Execute(r =>
                {
                    if (r.Success)
                        Debug.WriteLine("Role : " + Id + " successfull updated");

                });

        }

        public void update(Action<Response<DateTime>> callback)
        {

            var parse = new Driver();
            parse.Objects.Update<Roles>(this.Id).
                Set(u => u.userId, userId).
                Set(u => u.gameId, gameId).
                //Set(u => u.endTime, endTime).
                //Set(u => u.startTime, startTime).
                Set(u => u.infectionCount, infectionCount).
                Set(u => u.roleType, roleType).
                Set(u => u.alive, alive).
                Execute(callback);

        }

        public new void create()
        {
            PhoneApplicationService service = PhoneApplicationService.Current;
            var user = (User)service.State["user"];
            var parse = new Driver();
            parse.Objects.Save(this, r =>
            {
                if (r.Success)
                {
                    user.activeRole = r.Data.Id;
                    user.update();
                }
            });
        }

        public static void find(String roleId,Action<Response<Roles>> callback)
        {
            var parse = new Driver();
            parse.Objects.Get<Roles>(roleId, callback);
        }

    }