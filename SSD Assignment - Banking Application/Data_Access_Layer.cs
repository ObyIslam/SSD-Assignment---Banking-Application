using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Banking_Application;

namespace Banking_Application
{
    public class Data_Access_Layer
    {
        private readonly EncryptionUtilities crypto = new EncryptionUtilities();


        private List<Bank_Account> accounts;
        public static String databaseName = "Banking Database.db";
        private static Data_Access_Layer instance = new Data_Access_Layer();

        private Data_Access_Layer()//Singleton Design Pattern (For Concurrency Control) - Use getInstance() Method Instead.
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
                        accountNo TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        name TEXT NOT NULL,add
                        address_line_1 TEXT,
                        address_line_2 TEXT,
                        address_line_3 TEXT,
                        town TEXT NOT NULL,
                        balance REAL NOT NULL,
                        accountType INTEGER NOT NULL,
                        overdraftAmount REAL,
                        interestRate REAL
                    ) WITHOUT ROWID
                ";

                command.ExecuteNonQuery();
                
            }
        }

        public void loadBankAccounts()
        {
            if (!File.Exists(Data_Access_Layer.databaseName))
                initialiseDatabase();
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM Bank_Accounts";
                    SqliteDataReader dr = command.ExecuteReader();
                    
                    while(dr.Read())
                    {

                        int accountType = dr.GetInt16(7);

                        if(accountType == Account_Type.Current_Account)
                        {
                            Current_Account ca = new Current_Account();
                            ca.AccountNo = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(0)))
                            );
                            ca.Name = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(1)))
                            );
                            ca.AddressLine1 = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(2)))
                            );
                            ca.AddressLine2 = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(3)))
                            );
                            ca.AddressLine3 = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(4)))
                            );
                            ca.Town = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(5)))
                            );
                            ca.Balance = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(6)))
                            );
                            ca.overdraftAmount = dr.GetDouble(8);
                            accounts.Add(ca);
                        }
                        else
                        {
                            Savings_Account sa = new Savings_Account();
                            sa.AccountNo = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(0)))
                            );
                            sa.Name = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(1)))
                            );
                            sa.AddressLine1 = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(2)))
                            );
                            sa.AddressLine2 = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(3)))
                            );
                            sa.AddressLine3 = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(4)))
                            );
                            sa.Town = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(5)))
                            );
                            sa.Name = Encoding.UTF8.GetString(
                                crypto.Decrypt(Convert.FromBase64String(dr.GetString(6)))
                            );
                            sa.interestRate = dr.GetDouble(9);
                            accounts.Add(sa);
                        }


                    }

                }

            }
        }

        public String addBankAccount(Bank_Account ba) 
        {

            if (ba.GetType() == typeof(Current_Account))
                ba = (Current_Account)ba;
            else
                ba = (Savings_Account)ba;

            accounts.Add(ba);

            using (var connection = getDatabaseConnection()) //SQL injection attack need to prevent
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    INSERT INTO Bank_Accounts VALUES(" +
                    "'" + Convert.ToBase64String(crypto.Encrypt(ba.AccountNo)) + "', " +
                    "'" + Convert.ToBase64String(crypto.Encrypt(ba.Name)) + "', " +
                    "'" + Convert.ToBase64String(crypto.Encrypt(ba.AddressLine1)) + "', " +
                    "'" + Convert.ToBase64String(crypto.Encrypt(ba.AddressLine2)) + "', " +
                    "'" + Convert.ToBase64String(crypto.Encrypt(ba.AddressLine3)) + "', " +
                    "'" + Convert.ToBase64String(crypto.Encrypt(ba.Town)) + "', " +

                    ba.Balance + ", " +
                    (ba.GetType() == typeof(Current_Account) ? 1 : 2) + ", ";

                if (ba.GetType() == typeof(Current_Account))
                {
                    Current_Account ca = (Current_Account)ba;
                    command.CommandText += ca.overdraftAmount + ", NULL)";
                }

                else
                {
                    Savings_Account sa = (Savings_Account)ba;
                    command.CommandText += "NULL," + sa.interestRate + ")";
                }

                command.ExecuteNonQuery();

            }

            return ba.AccountNo;

        }

        public Bank_Account findBankAccountByAccNo(String accNo) 
        { 
        
            foreach(Bank_Account ba in accounts)
            {

                if (ba.AccountNo.Equals(accNo))
                {
                    return ba;
                }

            }

            return null; 
        }

        public bool closeBankAccount(String accNo) 
        {

            Bank_Account toRemove = null;
            
            foreach (Bank_Account ba in accounts)
            {

                if (ba.AccountNo.Equals(accNo))
                {
                    toRemove = ba;
                    break;
                }

            }

            if (toRemove == null)
                return false;
            else
            {
                accounts.Remove(toRemove);

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Bank_Accounts WHERE accountNo = '" + toRemove.AccountNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

        public bool lodge(String accNo, double amountToLodge)
        {

            Bank_Account toLodgeTo = null;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.AccountNo.Equals(accNo))
                {
                    ba.lodge(amountToLodge);
                    toLodgeTo = ba;
                    break;
                }

            }

            if (toLodgeTo == null)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Bank_Accounts SET balance = " + toLodgeTo.balance + " WHERE accountNo = '" + toLodgeTo.accountNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

        public bool withdraw(String accNo, double amountToWithdraw)
        {

            Bank_Account toWithdrawFrom = null;
            bool result = false;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.AccountNo.Equals(accNo))
                {
                    result = ba.withdraw(amountToWithdraw);
                    toWithdrawFrom = ba;
                    break;
                }

            }

            if (toWithdrawFrom == null || result == false)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Bank_Accounts SET balance = " + toWithdrawFrom.Balance + " WHERE accountNo = '" + toWithdrawFrom.AccountNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

    }
}
