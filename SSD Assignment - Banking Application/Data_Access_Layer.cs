using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Banking_Application;
using System.Globalization;
using Microsoft.Extensions.Logging;
using SSD_Assignment___Banking_Application;


namespace Banking_Application
{
    public class Data_Access_Layer
    {
        private readonly EncryptionUtilities crypto = new EncryptionUtilities();


        private List<Bank_Account> accounts;
        public static String databaseName = "Banking Database.db";
        private static Data_Access_Layer instance = new Data_Access_Layer();

        private Data_Access_Layer()
        {
            accounts = new List<Bank_Account>();
        }

        public static Data_Access_Layer getInstance()
        {
            return instance;
        }

        private SqliteConnection getDatabaseConnection()
        {

            String databaseConnectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = Data_Access_Layer.databaseName,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            return new SqliteConnection(databaseConnectionString);

        }

        private void initialiseDatabase()
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS Bank_Accounts(    
                        accountNo BLOB TEXT PRIMARY KEY,
                        name BLOB TEXT NOT NULL,
                        address_line_1 BLOB TEXT,
                        address_line_2 BLOB TEXT,
                        address_line_3 TEXT,
                        town BLOB TEXT NOT NULL,
                        balance REAL NOT NULL,
                        accountType INTEGER NOT NULL,
                        overdraftAmount REAL,
                        interestRate REAL
                    ) WITHOUT ROWID
                ";

                command.ExecuteNonQuery();
                Logger.LogTransaction(
                "system",
                "n/a",
                "n/a",
                "Database Init",
                DateTime.Now,
                "Database initialised or already exists",
                "SSD Banking Application v1.0.0"
                );

            }
        }

        public void loadBankAccounts()
        {
            initialiseDatabase();

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Bank_Accounts";
                SqliteDataReader dr = command.ExecuteReader();

                while (dr.Read())
                {
                    int accountType = dr.GetInt16(7);


                    byte[] accNoBlob = (byte[])dr["accountNo"];
                    byte[] nameBlob = (byte[])dr["name"];
                    byte[] a1Blob = dr["address_line_1"] as byte[];
                    byte[] a2Blob = dr["address_line_2"] as byte[];
                    byte[] a3Blob = dr["address_line_3"] as byte[];
                    byte[] townBlob = (byte[])dr["town"];
                    byte[] balanceBlob = (byte[])dr["balance"];

                    // Decrypt
                    string accNo = Encoding.UTF8.GetString(crypto.Decrypt(accNoBlob));
                    string name = Encoding.UTF8.GetString(crypto.Decrypt(nameBlob));
                    string a1 = Encoding.UTF8.GetString(crypto.Decrypt(a1Blob ?? Array.Empty<byte>()));
                    string a2 = Encoding.UTF8.GetString(crypto.Decrypt(a2Blob ?? Array.Empty<byte>()));
                    string a3 = Encoding.UTF8.GetString(crypto.Decrypt(a3Blob ?? Array.Empty<byte>()));
                    string town = Encoding.UTF8.GetString(crypto.Decrypt(townBlob));
                    string balanceString = Encoding.UTF8.GetString(crypto.Decrypt(balanceBlob));
                    double balance = double.Parse(balanceString, CultureInfo.InvariantCulture);

                    if (accountType == Account_Type.Current_Account)
                    {
                        Current_Account ca = new Current_Account();
                        ca.AccountNo = accNo;
                        ca.Name = name;
                        ca.AddressLine1 = a1;
                        ca.AddressLine2 = a2;
                        ca.AddressLine3 = a3;
                        ca.Town = town;
                        ca.Balance = balance;
                        ca.OverdraftAmount = dr.GetDouble(8);

                        accounts.Add(ca);
                    }
                    else
                    {
                        Savings_Account sa = new Savings_Account();
                        sa.AccountNo = accNo;
                        sa.Name = name;
                        sa.AddressLine1 = a1;
                        sa.AddressLine2 = a2;
                        sa.AddressLine3 = a3;
                        sa.Town = town;
                        sa.Balance = balance;
                        sa.InterestRate = dr.GetDouble(9);

                        accounts.Add(sa);
                    }
                }
            }
        }


        public string addBankAccount(Bank_Account ba)
        {

            // Ensure correct type
            if (ba is Current_Account)
                ba = (Current_Account)ba;
            else
                ba = (Savings_Account)ba;

            accounts.Add(ba);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Bank_Accounts
                    (accountNo, name, address_line_1, address_line_2, address_line_3, town, balance, accountType, overdraftAmount, interestRate)
                    VALUES
                    (@accountNo, @name, @addressLine1, @addressLine2, @addressLine3, @town, @balance, @type, @overdraftAmount, @interestRate)
                    ";

                command.Parameters.AddWithValue("@accountNo", crypto.Encrypt(ba.AccountNo));
                command.Parameters.AddWithValue("@name", crypto.Encrypt(ba.Name));
                command.Parameters.AddWithValue("@addressLine1", crypto.Encrypt(ba.AddressLine1 ?? ""));
                command.Parameters.AddWithValue("@addressLine2", crypto.Encrypt(ba.AddressLine2 ?? ""));
                command.Parameters.AddWithValue("@addressLine3", crypto.Encrypt(ba.AddressLine3 ?? ""));
                command.Parameters.AddWithValue("@town", crypto.Encrypt(ba.Town));
                command.Parameters.AddWithValue("@balance", crypto.Encrypt(ba.Balance.ToString(CultureInfo.InvariantCulture)));
                command.Parameters.AddWithValue("@type", ba is Current_Account ? 1 : 2);

                if (ba is Current_Account ca)
                {
                    command.Parameters.AddWithValue("@overdraftAmount", ca.OverdraftAmount);
                    command.Parameters.AddWithValue("@interestRate", DBNull.Value);
                }
                else
                {
                    Savings_Account sa = (Savings_Account)ba;
                    command.Parameters.AddWithValue("@overdraftAmount", DBNull.Value);
                    command.Parameters.AddWithValue("@interestRate", sa.InterestRate);
                }

                command.ExecuteNonQuery();
                Logger.LogTransaction(
                "system",
                BitConverter.ToString(crypto.Encrypt(ba.AccountNo)),
                BitConverter.ToString(crypto.Encrypt(ba.Name)),
                "Account Added",
                DateTime.Now,
                "New account added to database",
                "SSD Banking Application v1.0.0"
                );

            }

            return ba.AccountNo;
        }


        public Bank_Account findBankAccountByAccNo(String accNo)
        {

            foreach (Bank_Account ba in accounts)
            {

                if (ba.AccountNo.Equals(accNo))
                {
                    return ba;
                }

            }

            return null;
        }

        public bool closeBankAccount(string accNo)
        {
            Bank_Account toRemove = findBankAccountByAccNo(accNo);
            if (toRemove == null)
                return false;

            accounts.Remove(toRemove);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = @"
                    DELETE FROM Bank_Accounts 
                    WHERE accountNo = @accNo
                 ";


                command.Parameters.AddWithValue("@accountNo", crypto.Encrypt(accNo));

                command.ExecuteNonQuery();
                Logger.LogTransaction(
                "system",
                BitConverter.ToString(crypto.Encrypt(accNo)),
                "n/a",
                "Account Closed",
                DateTime.Now,
                "Account successfully removed from database",
                "SSD Banking Application v1.0.0"
                );

            }

            return true;
        }


        public bool lodge(string accNo, double amountToLodge)
        {
            Bank_Account ba = findBankAccountByAccNo(accNo);
            if (ba == null)
                return false;

            ba.lodge(amountToLodge);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = @"
                    UPDATE Bank_Accounts 
                    SET balance = @balance 
                    WHERE accountNo = @accountNo
                 ";

                command.Parameters.AddWithValue("@balance",
                    crypto.Encrypt(ba.Balance.ToString(CultureInfo.InvariantCulture))
                );

                command.Parameters.AddWithValue("@accountNo", crypto.Encrypt(accNo));

                command.ExecuteNonQuery();

                Logger.LogTransaction(
                "system",
                BitConverter.ToString(crypto.Encrypt(accNo)),
                BitConverter.ToString(crypto.Encrypt(ba.Name)),
                "Lodgement",
                DateTime.Now,
                $"Lodged {amountToLodge} to account",
                "SSD Banking Application v1.0.0"
                );

            }

            return true;
        }


        public bool withdraw(string accNo, double amountToWithdraw)
        {
            Bank_Account ba = findBankAccountByAccNo(accNo);
            if (ba == null)
                return false;

            bool ok = ba.withdraw(amountToWithdraw);
            if (!ok)
                return false;

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = @"
                    UPDATE Bank_Accounts 
                    SET balance = @balance 
                    WHERE accountNo = @accNo
                 ";

                command.Parameters.AddWithValue("@balance",
                    crypto.Encrypt(ba.Balance.ToString(CultureInfo.InvariantCulture))
                );

                command.Parameters.AddWithValue("@accountNo", crypto.Encrypt(accNo));

                command.ExecuteNonQuery();
                Logger.LogTransaction(
                "system",
                BitConverter.ToString(crypto.Encrypt(accNo)),
                BitConverter.ToString(crypto.Encrypt(ba.Name)),
                "Withdrawal",
                DateTime.Now,
                $"Withdrew {amountToWithdraw} from account",
                "SSD Banking Application v1.0.0"
                );

            }

            return true;
        }


    }
}
