using System;
using System.Threading;
using System.Xml.Linq;

namespace Banking_Application
{
    public abstract class Bank_Account
    {

        private string accountNo; // better access modifiers
        private string name;
        private string address_line_1;
        private string address_line_2;
        private string address_line_3;
        private string town;
        private double balance;

        protected readonly object balanceLock = new object(); // Thread safety for balance

        protected Bank_Account()
        {
            this.accountNo = GenerateAccountNumber();
        }


        protected Bank_Account(string name, string address_line_1, string address_line_2, string address_line_3, string town, double balance)
        {
            this.accountNo = GenerateAccountNumber();
            this.name = name;
            this.address_line_1 = address_line_1;
            this.address_line_2 = address_line_2;
            this.address_line_3 = address_line_3;
            this.town = town;
            this.balance = balance;
        }

        private string GenerateAccountNumber()
        {
            return Guid.NewGuid().ToString();
        }

     
        public string AccountNo
        {
            get => accountNo;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("AccountNumber cannot be null or empty.");
                accountNo = value;
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Name cannot be null or empty.");
                name = value;
            }
        }

        public string AddressLine1
        {
            get => address_line_1;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Address Line 1 cannot be null or empty.");
                address_line_1 = value;
            }
        }

        public string AddressLine2
        {
            get => address_line_2;
            set => address_line_2 = value; 
        }

        public string AddressLine3
        {
            get => address_line_3;
            set => address_line_3 = value; 
        }

        public string Town
        {
            get => town;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Town cannot be null or empty.");
                town = value;
            }
        }

        public double Balance 
        {
            get => balance;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Balance cannot be negative.");
                balance = value;
            }
        }


        public void lodge(double amountIn)
        {
            if (amountIn <= 0)
                throw new ArgumentException("Lodged amount must be positive.");

            lock (balanceLock)
            {
                balance += amountIn;
            }
        }

        public abstract bool withdraw(double amountToWithdraw);

        public abstract double getAvailableFunds();


        public override string ToString()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; 

            return $"\nAccount No: {accountNo}\n" +
                   $"Name: {name}\n" +
                   $"Address Line 1: {address_line_1}\n" +
                   $"Address Line 2: {address_line_2}\n" +
                   $"Address Line 3: {address_line_3}\n" +
                   $"Town: {town}\n" +
                   $"Balance: {balance:C}\n";
        }
    }
}