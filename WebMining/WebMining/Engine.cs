﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebMining
{
    class Engine
    {
        private Parser parser;
        private Dictionary<string, User> extractedUsers;

        public Engine()
        {
            parser = new Parser();
            extractedUsers = new Dictionary<string, User>();
        }


        private Action<int, string> notifyer;
        public Engine setNotifyer(Action<int, string> n)
        {
            notifyer = n;
            return this;
        }


        public List<User> getExtractedUsers()
        {
            return extractedUsers.Values.ToList();
        }

        public Engine ProcessAll(List<string> logfiles)
        {
            notifyer(0, "reading data");

            foreach (var f in logfiles)
            {
                preperingVariables(File.ReadLines(f).Count());
                foreach (var l in File.ReadLines(f))
                    ProcessLine(l);
                notify();
            }
            return this;
        }


        int processedLine,totalLine,checkEvery;

        public void ProcessLine(string logTexts)
        {
            UserIdentification(parser.ParseLine(logTexts));
            notifyEveryWhile();
            ++processedLine;
        }

        public Engine ProcessAllLines(string[] logTexts)
        {
            notifyer(0, "processing data");

            preperingVariables(logTexts.Length);

            foreach (var r in parser.ParseNextRecord(logTexts))
            {
                notifyEveryWhile();

                UserIdentification(r);

                ++processedLine;
            }

            notify();

            return this;
        }

        private void notifyEveryWhile()
        {
            if (processedLine % checkEvery == 0)
                notify();
        }

        private void notify()
        {
            notifyer((int)(processedLine * 100.0 / totalLine), processedLine.ToString("N0") + " lines proccesed");
        }

        private void preperingVariables(int totalsize)
        {
            processedLine = 0;
            totalLine = totalsize;
            checkEvery = 1;

            if (totalLine > 100)
                checkEvery = totalLine / 100;
        }

        private void UserIdentification(Record r)
        {
            if (r == null)
                return;

            if (extractedUsers.ContainsKey(r.CookieID) == false)
                extractedUsers.Add(r.CookieID, new User());

            Sessionization(r, extractedUsers[r.CookieID]);
        }

        private void Sessionization(Record r, User u)
        {
            Session s = findCurrectSession(r.Time, u.Sessions) ?? addNewSession(r, u);
            s.AddRecord(r);    
        }

        private Session findCurrectSession(DateTime time, List<Session> sessions)
        {
            foreach (var s in sessions)
                if (isBelongToSession(time, s))
                    return s;

            return null;
        }

        private bool isBelongToSession(DateTime time, Session s)
        {
            return (s.StartTime - time).Duration().TotalSeconds <= Session.TimeOutSec;
        }

        private Session addNewSession(Record r, User u)
        {
            Session s = new Session();
            u.AddSession(s);
            return s;
        }
    }
}
