using System;
using System.IO;
using OnPayClient.Exceptions;
using OnPayClient.Models.Enums;

namespace OnPayTester
{
    class Program
    {
        private static OnPayClient.OnPayClient _onPayClient;

        static void Main()
        {
            WriteConsoleInfo("------------------");
            WriteConsoleInfo("ACTIONS");
            WriteConsoleInfo("------------------");
            WriteConsoleInfo("ping");
            WriteConsoleInfo("details");
            WriteConsoleInfo("list");
            WriteConsoleInfo("capture");
            WriteConsoleInfo("readtoken");
            WriteConsoleInfo("------------------");

            try
            {
                SetupClient();
            }
            catch (InvalidServerResponseException invalidServerResponseException)
            {
                WriteConsoleError($"OnPay responded with status:{invalidServerResponseException.HttpStatus}");
                ListenForAction();
            }
            catch (Exception e)
            {
                WriteConsoleError(e.Message);
                ListenForAction();
            }

        }

        private static void SetupClient()
        {
            var tokenFilePath = "token.txt";
            var token = string.Empty;
            if (File.Exists(tokenFilePath))
            {
                token = File.ReadAllText("token.txt");
                if (!string.IsNullOrEmpty(token))
                {
                    WriteConsoleStatus("Using token:");
                    WriteConsoleStatus(token);
                }
                else
                {
                    WriteConsoleError($"You need to specify a access token in file:{tokenFilePath}");
                }
            }
            else
            {
                WriteConsoleError($"Failed to load access token from file:{tokenFilePath}");
            }
            _onPayClient = new OnPayClient.OnPayClient(token);
            ListenForAction();
        }

        // ReSharper disable once FunctionRecursiveOnAllPaths
        private static void ListenForAction()
        {
            GetConsoleInput("Please enter action");
            var action = Console.ReadLine();
            switch (action)
            {
                case "ping":
                    PingClient();
                    break;
                case "details":
                    GetTransactionDetails();
                    break;
                case "list":
                    GetTransactionsList();
                    break;
                case "capture":
                    CaptureTransaction();
                    break;
                case "readtoken":
                    SetupClient();
                    break;
                default:
                    Console.WriteLine("Unknown action");
                    break;
            }
            ListenForAction();
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
    }
}
