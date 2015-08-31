﻿using System;
using System.Collections.Generic;
using System.IO;
using CSharpTradeOffers;
using CSharpTradeOffers.Trading;

namespace DonationBot
{
    internal class Program
    {
        private static string _user, _pass;
        private static string _machineAuth;
        private static string _apiKey;
        private static Account _account = new Account();

        private static void Main(string[] args)
        {
            if(File.Exists("key.txt"))
                _apiKey = File.ReadAllText("key.txt");

            if (string.IsNullOrEmpty(_apiKey))
            {
                Console.WriteLine("Fatal error: Api key was missing. Please put your API key in \"key.txt\".");
                File.Create("key.txt").Close();
                Console.ReadLine();
                Environment.Exit(-1);
            }

            Console.Write("Username: ");
            _user = Console.ReadLine();

            Console.Write("Password: ");
            _pass = Console.ReadLine();

            _machineAuth = File.ReadAllText("auth.txt");

            if (string.IsNullOrEmpty(_machineAuth))
            {
                Web.DoLogin(_user, _pass, ref _account);
                _machineAuth = Web.SteamMachineAuth;
                File.WriteAllText("auth.txt", _machineAuth);
            }
            else
            {
                Web.DoLogin(_user, _pass, ref _account, _machineAuth);
            }

            PollOffers();
        }

        private static void PollOffers()
        {
            bool isPolling = true;

            var offerHandler = new EconServiceHandler(_apiKey);
            var marketHandler = new MarketHandler();
            marketHandler.EligibilityCheck(_account.SteamId, _account.AuthContainer); //required to perform trades (?). Checks to see whether or not we're allowed to trade.

            var recData = new Dictionary<string, string>
            {
                {"get_received_offers", "1"},
                {"active_only", "1"},
                {"time_historical_cutoff", "999999999999"}
                //arbitrarily high number to prevent old offers from appearing
            };
            
            while (isPolling) //permanent loop, can be changed 
            {
                var offers = offerHandler.GetTradeOffers(recData).trade_offers_received;
                foreach (CEconTradeOffer cEconTradeOffer in offers)
                {
                    if (cEconTradeOffer.items_to_give.Count == 0)
                    {
                        offerHandler.AcceptTradeOffer(Convert.ToUInt64(cEconTradeOffer.tradeid), _account.AuthContainer,
                            cEconTradeOffer.accountid_other, "1");
                        Console.WriteLine("Accepted a donation!");
                    }
                    else
                    {
                        offerHandler.DeclineTradeOffer(cEconTradeOffer.tradeofferid);
                        Console.WriteLine("Refused a \"donation\" that would have taken items from us.");
                    }
                }
            }
        }
    }
}