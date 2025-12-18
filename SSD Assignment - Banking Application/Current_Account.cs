using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    public sealed class Current_Account : Bank_Account // marked class as sealed
    {
        private double overdraftAmount;

        public Current_Account() : base()
        {

        }

        public Current_Account(String name, String address_line_1, String address_line_2, String address_line_3, String town, double balance, double overdraftAmount) : base(name, address_line_1, address_line_2, address_line_3, town, balance)
        {
            OverdraftAmount = overdraftAmount;
        }

        public double OverdraftAmount
        {
            get => overdraftAmount;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Overdraft amount cannot be negative.");
                overdraftAmount = value;
            }
        }

        public override bool withdraw(double amountToWithdraw)
        {
            if (amountToWithdraw <= 0)
                throw new ArgumentException("Withdrawal amount must be positive.");

            // Locking the balance to ensure safety during the withdrawal process
            lock (balanceLock)
            {
                double availableFunds = getAvailableFunds();

                if (availableFunds >= amountToWithdraw)
                {
                    Balance -= amountToWithdraw;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override double getAvailableFunds()
        {
            lock (balanceLock)
            {
                return Balance + OverdraftAmount;
            }
        }


        public override string ToString()
        {
            return base.ToString() +
                   $"Account Type: Current Account\n" +
                   $"Overdraft Amount: {OverdraftAmount:C}\n";  
        }
    }
}