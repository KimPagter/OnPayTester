using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Threading.Tasks;
using OnPayClient.Exceptions;
using OnPayClient.Models.Enums;

namespace OnPayTester
{
    class Program
    {
        //private const string token = @"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImp0aSI6IjM0MzRlYTJjMmY4MzRhMTU1MzA0NGJjYTY3YzkwMDBhZDE0MDA0MTM4Zjg2Njk2N2Q0NWVlZmEwYTdlNWFjZTZlM2Q0YzI1ZmYzYTUzNDg4In0.eyJhdWQiOiJraW0gKGtpbUBkYW5kb21haW4uZGspIiwianRpIjoiMzQzNGVhMmMyZjgzNGExNTUzMDQ0YmNhNjdjOTAwMGFkMTQwMDQxMzhmODY2OTY3ZDQ1ZWVmYTBhN2U1YWNlNmUzZDRjMjVmZjNhNTM0ODgiLCJpYXQiOjE2MDE4ODU4MzIsIm5iZiI6MTYwMTg4NTgzMiwiZXhwIjoxNjAxOTcyMjMyLCJzdWIiOiIzMDE5MDE0MTkzMTQ0NDk4Iiwic2NvcGVzIjpbImZ1bGwiXX0.Mj6dLtgo0mH39NXscDH-kIAnqA35JNYz2F4NTJtCTPPfQJ0eCNmsfmDYGYB-pLYX1VZpOrcvFP5gfJtRvMVvsaU5lKFB8UmbnV5UZ9lyWl1wrrIfSlyhQ9EC-C6KTMiWWd3jZe-vRUVDOH8z30OrlDoSn0VRPY5UtZ3zIQgaGxxRYPr6P6Ufd0EAi5WnW3lPHXrrYjJV7KcB8e4Bnd0RkCc_1PwcUsp_3rLeLiHJbpiHWYNg-2mBCBH1CXeZ2kwbxWS-LSNvvsz7MnL0PgRCAWPCKsxtfGyNUesmP7yZFnKPP8DQ6sNd9FhxXW_0D9Cy8khIbsGhq5N9Z5iib7JUig";
        private static OnPayClient.OnPayClient _onPayClient;

        static async Task Main(string[] args)
        {
            WriteConsoleInfo("SUPPORTED ACTIONS");
            WriteConsoleInfo("------------------");
            WriteConsoleInfo("ping");
            WriteConsoleInfo("details");
            WriteConsoleInfo("list");
            WriteConsoleInfo("capture");
            WriteConsoleInfo("readtoken");
            WriteConsoleInfo("------------------");

            try
            {
                await SetupClient();
            }
            catch (InvalidServerResponseException invalidServerResponseException)
            {
                WriteConsoleError($"OnPay responded with status:{invalidServerResponseException.HttpStatus}");
            }
            catch (Exception e)
            {
                WriteConsoleError(e.Message);
                await SetupClient();
            }

        }

        private static async Task SetupClient()
        {
            var token = File.ReadAllText("token.txt");
            _onPayClient = new OnPayClient.OnPayClient(token);
            WriteConsoleStatus("Using token:");
            WriteConsoleStatus(token);
            await ListenForAction();
        }

        private static async Task ListenForAction()
        {
            GetConsoleInput("Please enter action");
            var action = Console.ReadLine();
            switch (action)
            {
                case "ping":
                    PingClient();
                    ListenForAction();
                    break;
                case "details":
                    GetTransactionDetails();
                    ListenForAction();
                    break;
                case "list":
                    GetTransactionsList();
                    ListenForAction();
                    break;
                case "capture":
                    CaptureTransaction();
                    ListenForAction();
                    break;
                case "readtoken":
                    SetupClient();
                    ListenForAction();
                    break;
                case "oath2":
                    await Oath2();
                    break;
                default:
                    Console.WriteLine("Unknown action");
                    ListenForAction();
                    break;
            }
        }

        private static void PingClient()
        {
            _onPayClient.Ping();
            WriteConsoleStatus("Did ping on OnPay");
        }

        private static void GetTransactionsList()
        {
            try
            {
                var transactions = _onPayClient.Transactions.Page(direction: Direction.Desc, pageIndex: 1, pageSize:10);
                foreach (var transaction in transactions.Data)
                {
                    WriteConsoleStatus($"Transaction id:{transaction.TransactionNumber} - status:{transaction.Status} - created:{transaction.Created}");
                }
            }
            catch (InvalidServerResponseException invalidServerResponseException)
            {
                HandleInvalidServerResponse(invalidServerResponseException);
            }
            catch (Exception ex)
            {
                WriteConsoleError(ex.Message);
            }

        }

        private static void CaptureTransaction()
        {
            GetConsoleInput("Enter transaction id");
            var transactionId = Console.ReadLine();
            try
            {
                var captureResponse = _onPayClient.Transactions.Details(transactionId).Data.Capture();
                if (captureResponse.Errors == null)
                {
                    WriteConsoleStatus($"Transaction with id:{captureResponse.Data.TransactionNumber} was captured");
                }
                else
                {
                    foreach (var error in captureResponse.Errors)
                    {
                        WriteConsoleError($"Transaction with id:{captureResponse.Data.TransactionNumber} capture failed with error {error.Message}");
                    }
                }

            }
            catch (InvalidServerResponseException invalidServerResponseException)
            {
                HandleInvalidServerResponse(invalidServerResponseException);
            }
            catch (Exception ex)
            {
                WriteConsoleError(ex.Message);
            }
        }

        private static void GetTransactionDetails()
        {
            
            GetConsoleInput("Enter transaction id");
            var transactionId = Console.ReadLine();
            try
            {
                var transaction = _onPayClient.Transactions.Details(transactionId);
                if (transaction != null)
                {
                    WriteConsoleStatus($"Transaction id:{transactionId} is in status: {transaction.Data.Status}");
                    WriteConsoleStatus($"It was created:{transaction.Data.Created}");
                }
                else
                {
                    WriteConsoleError($"Transaction with id:{transactionId} was not found");
                }
            }
            catch (InvalidServerResponseException invalidServerResponseException)
            {
                HandleInvalidServerResponse(invalidServerResponseException);

            }
            catch (Exception ex)
            {
                WriteConsoleError(ex.Message);
            }

        }

        private static void HandleInvalidServerResponse(InvalidServerResponseException invalidServerResponseException)
        {
            WriteConsoleError($"Operation failed with http status code {invalidServerResponseException.HttpStatus}");
            WriteConsoleError($"{invalidServerResponseException.Message}");
            WriteConsoleError($"{invalidServerResponseException.Content}");
        }

        private static void GetConsoleInput(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Green;
        }

        private static void WriteConsoleInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }
        private static void WriteConsoleStatus(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(message);
        }
        private static void WriteConsoleError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static async Task Oath2()
        {
            var merchant = "3019014193144498";
            var url = $"https://manage.onpay.io/{merchant}/oauth2/authorize";
            var client = new HttpClient();
            var formVariables = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("scope", "full"),
                new KeyValuePair<string, string>("response_mode", "form_post"),
                new KeyValuePair<string, string>("_username", ""),
                new KeyValuePair<string, string>("_password", ""),
                new KeyValuePair<string, string>("client_id", ""),
                new KeyValuePair<string, string>("_password", "form_post")
            };

            var formContent = new FormUrlEncodedContent(formVariables);

            var response = await client.PostAsync(url, formContent);
            var contents = await response.Content.ReadAsStringAsync();

        }

    }
}
