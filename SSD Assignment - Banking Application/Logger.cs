using System.Diagnostics;
using System.Security.Principal;

namespace SSD_Assignment___Banking_Application
{
    internal static class Logger
    {
        private const string SourceName = "SSD Banking Application";
        private const string LogName = "Application";

        public static void SetupEventSource()
        {
            //if (!EventLog.SourceExists(SourceName))
            //{
            //    EventLog.CreateEventSource(SourceName, LogName);
            //    Console.WriteLine($"Event source '{SourceName}' created in log '{LogName}'.");
            //}
        }


        public static void LogTransaction(
            
            //log who, what, where, when, why, how
            string bankTellerName,
            string accountNumber,
            string accountHolderName,
            string transactionType,
            DateTime transactionDateTime,
            string reason,
            string appMetadata)
        {
            string logMessage = $@"
                WHO:
                    Bank Teller: {bankTellerName}
                    Account No: {accountNumber}
                    Account Holder: {accountHolderName}
                WHAT:
                    Transaction Type: {transactionType}
                WHERE:
                    Device Identifier: {GetDeviceIdentifier()}
                WHEN:
                    Date/Time: {transactionDateTime:yyyy-MM-dd HH:mm:ss}
                WHY:
                    Reason: {(string.IsNullOrEmpty(reason) ? "N/A" : reason)}
                HOW:
                    Application Metadata: {appMetadata}
                ";

            try
            {
                EventLog.WriteEntry(SourceName, logMessage, EventLogEntryType.Information);
                Console.WriteLine("Transaction logged successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log transaction: {ex.Message}");
            }
        }

        public static void LogError(string errorMessage)
        {
            try
            {
                EventLog.WriteEntry(SourceName, errorMessage, EventLogEntryType.Error);
                Console.WriteLine("Error logged successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log error: {ex.Message}");
            }
        }

        public static string GetDeviceIdentifier()
        {
            try
            {
                //gets idenfity of user
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return identity.User?.Value ?? "Unknown SID";
            }
            catch
            {
                return "Unknown Device";
            }
        }
    }
}