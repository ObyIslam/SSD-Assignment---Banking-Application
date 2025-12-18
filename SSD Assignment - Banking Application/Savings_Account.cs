using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    public sealed class Savings_Account : Bank_Account  // marked as sealed
    {
       

        private double interestRate;

        public Savings_Account() : base()
        {

        }

        public Savings_Account(String accountNo, String name, String address_line_1, String address_line_2, String address_line_3, String town, double balance, double interestRate) : base( name, address_line_1, address_line_2, address_line_3, town, balance)
        {
            InterestRate = interestRate;
        }

        public double InterestRate
        {
            get => interestRate;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentException("Interest rate must be between 0 and 100.");
                interestRate = value;
            }
        }
        public override double getAvailableFunds()
        {
            lock (balanceLock)
            {
                return Balance; // Thread-safe access to balance
            }
        }


        // Override withdraw with Validation and Thread Safety
        public override bool withdraw(double amountToWithdraw)
        {
            if (amountToWithdraw <= 0)
                throw new ArgumentException("Withdrawal amount must be positive.");

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

        public override string ToString()
        {
            return base.ToString() +
                   $"Account Type: Savings Account\n" +
                   $"Interest Rate: {InterestRate}%\n";
        }

    }
}