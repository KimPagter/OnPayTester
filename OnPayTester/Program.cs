using System;
using System.IO;
using System.Threading.Tasks;
using OnPayClient.Exceptions;
using OnPayClient.Models.Enums;

namespace OnPayTester
{
    class Program
    {
        private static OnPayClient.OnPayClient _onPayClient;

        static async Task Main()
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

        // ReSharper disable once FunctionRecursiveOnAllPaths
        private static async Task ListenForAction()
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
                    await SetupClient();
                    break;
                default:
                    Console.WriteLine("Unknown action");
                    break;
            }
            await ListenForAction();
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
